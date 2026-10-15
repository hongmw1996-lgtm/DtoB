using System.Globalization;
using DtoB.Cad.Raw;

namespace DtoB.Cad.Dxf;

public sealed partial class TextDxfStructuralReader
{
    private sealed partial class Operation
    {
        private static readonly HashSet<string> MappedVariables = new(StringComparer.Ordinal)
            { "$ACADVER", "$INSUNITS", "$EXTMIN", "$EXTMAX", "$DWGCODEPAGE", "$HANDSEED" };

        private void ParseHeader()
        {
            var variables = new List<DxfHeaderVariable>();
            var section = sections.SingleOrDefault(s => s.Name == "HEADER");
            if (section != null)
            {
                var i = ContentStart(section); var end = section.Source.EndPairIndex;
                while (i < end)
                {
                    token.ThrowIfCancellationRequested();
                    if (Comment(i)) { i++; continue; }
                    if (P[i].GroupCode != 9 || !P[i].RawTextValue.StartsWith('$') || P[i].RawTextValue.Length < 2)
                        Fail(DxfStructuralDiagnosticCode.MalformedHeaderVariable, "HEADER variable requires a code 9 dollar-prefixed name.", i);
                    var start = i++; var name = P[start].RawTextValue;
                    while (i < end && P[i].GroupCode != 9)
                    {
                        token.ThrowIfCancellationRequested();
                        if (P[i].GroupCode == 0)
                            Fail(DxfStructuralDiagnosticCode.MalformedHeaderVariable, "Unexpected code 0 within HEADER variable.", i);
                        i++;
                    }
                    Guard(i - start > limits.MaxFieldsPerRecord, "header variable fields", start);
                    if (!Enumerable.Range(start + 1, i - start - 1).Any(j => P[j].GroupCode != 999))
                        Fail(DxfStructuralDiagnosticCode.MalformedHeaderVariable, "HEADER variable has no value pair.", start);
                    Guard(++recordWorkCount > limits.MaxRecords, "record/variable count", start);
                    Inspect(start, i - 1); // HEADER code 5 remains metadata, never identity.
                    var disposition = MappedVariables.Contains(name) ? DxfDisposition.Supported : DxfDisposition.IgnoredWithDiagnostic;
                    variables.Add(new(name, Span(start, i - 1), disposition));
                }
            }
            foreach (var group in variables.GroupBy(v => v.Name, StringComparer.Ordinal))
            {
                token.ThrowIfCancellationRequested();
                if (group.Count() > 1)
                    Add(group.Key == "$ACADVER" ? DxfStructuralDiagnosticCode.DuplicateAcadVersion : DxfStructuralDiagnosticCode.DuplicateHeaderVariable,
                        DxfStructuralDiagnosticSeverity.Error, "Duplicate singleton HEADER variable; all occurrences retained.", group.First().Source, group.Count());
            }
            var unmapped = variables.Count(v => v.Disposition == DxfDisposition.IgnoredWithDiagnostic);
            if (unmapped > 0)
                Add(DxfStructuralDiagnosticCode.UnmappedHeaderVariables, DxfStructuralDiagnosticSeverity.Information,
                    "Additional HEADER variables retained as raw ranges without typed mapping.",
                    variables.First(v => v.Disposition == DxfDisposition.IgnoredWithDiagnostic).Source, unmapped);

            DxfSourceFact Field(string name, int code, DxfFactKind kind)
            {
                var candidates = new List<int>(); var variableCount = 0;
                foreach (var variable in variables.Where(v => v.Name == name))
                {
                    token.ThrowIfCancellationRequested(); variableCount++;
                    var eligible = Inspect(variable.Source.StartPairIndex, variable.Source.EndPairIndex, countReferences: false).Eligible;
                    foreach (var j in eligible)
                    {
                        token.ThrowIfCancellationRequested();
                        if (P[j].GroupCode == code) candidates.Add(j);
                    }
                }
                return Fact(candidates, kind, DxfStructuralDiagnosticCode.InvalidHeaderValue, variableCount > 1);
            }
            var acad = Field("$ACADVER", 1, DxfFactKind.Text);
            var units = Field("$INSUNITS", 70, DxfFactKind.Integer);
            var codepage = Field("$DWGCODEPAGE", 3, DxfFactKind.Text);
            var seed = Field("$HANDSEED", 5, DxfFactKind.Text);
            var min = new DxfRawHeaderPoint(Field("$EXTMIN", 10, DxfFactKind.Real), Field("$EXTMIN", 20, DxfFactKind.Real), Field("$EXTMIN", 30, DxfFactKind.Real));
            var max = new DxfRawHeaderPoint(Field("$EXTMAX", 10, DxfFactKind.Real), Field("$EXTMAX", 20, DxfFactKind.Real), Field("$EXTMAX", 30, DxfFactKind.Real));
            var state = DxfReportedExtentsState.NotAvailable;
            if (new[] { min.X, min.Y, min.Z, max.X, max.Y, max.Z }.All(f => f.Validity == DxfFactValidity.Valid))
            {
                state = DxfReportedExtentsState.SourceConfirmed;
                if (min.X.Real > max.X.Real || min.Y.Real > max.Y.Real || min.Z.Real > max.Z.Real)
                {
                    state = DxfReportedExtentsState.UnusableReportedExtents;
                    Add(DxfStructuralDiagnosticCode.UnusableReportedExtents, DxfStructuralDiagnosticSeverity.Warning,
                        "Source-confirmed reported extents are inverted; retained without repair or replacement bounds.", min.X.Source);
                }
            }
            header = new(variables, acad, units, codepage, seed, min, max, state);
        }

        private void ValidateVersion()
        {
            var fact = header!.AcadVersion;
            if (fact.Presence == DxfFactPresence.Absent)
            {
                version = DxfStructuralVersionAcceptance.Invalid;
                Add(DxfStructuralDiagnosticCode.MissingAcadVersion, DxfStructuralDiagnosticSeverity.Error, "$ACADVER is required.");
                return;
            }
            if (fact.Validity != DxfFactValidity.Valid)
            { version = DxfStructuralVersionAcceptance.Invalid; return; }
            var text = fact.Text!;
            if (text.Length != 6 || !text.StartsWith("AC", StringComparison.Ordinal) || text.AsSpan(2).ContainsAnyExceptInRange('0', '9'))
            {
                version = DxfStructuralVersionAcceptance.Invalid;
                Add(DxfStructuralDiagnosticCode.InvalidHeaderValue, DxfStructuralDiagnosticSeverity.Error, "$ACADVER must be an explicit ACdddd version.", fact.Source);
            }
            else if (text != "AC1032")
            {
                version = DxfStructuralVersionAcceptance.Unsupported;
                Add(DxfStructuralDiagnosticCode.UnsupportedAcadVersion, DxfStructuralDiagnosticSeverity.Warning,
                    "Only the tested AC1032 Text DXF profile is supported.", fact.Source);
            }
            else version = DxfStructuralVersionAcceptance.Supported;
        }

        private DxfSourceFact Fact(IReadOnlyList<int> indices, DxfFactKind kind,
            DxfStructuralDiagnosticCode invalidCode, bool duplicateVariable = false)
        {
            token.ThrowIfCancellationRequested();
            if (indices.Count == 0 && !duplicateVariable)
                return new(kind, DxfFactPresence.Absent, DxfFactValidity.NotApplicable, null, null, null, [], null);
            var source = indices.Count > 0 ? Span(indices[0], indices[^1]) : null;
            if (indices.Count != 1 || duplicateVariable)
            {
                Add(invalidCode, DxfStructuralDiagnosticSeverity.Error, "Factual singleton has duplicate or invalid source occurrences.", source);
                return new(kind, DxfFactPresence.Present, DxfFactValidity.Invalid, null, null, null, indices, source);
            }
            var raw = P[indices[0]].RawTextValue;
            string? text = null; int? integer = null; double? real = null;
            var valid = true;
            if (kind == DxfFactKind.Text) text = raw;
            else if (kind == DxfFactKind.Integer)
            {
                valid = int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value);
                if (valid) integer = value;
            }
            else
            {
                valid = double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value);
                if (valid) real = value;
            }
            if (!valid) Add(invalidCode, DxfStructuralDiagnosticSeverity.Error, "Factual scalar has invalid invariant syntax or is nonfinite.", source);
            return new(kind, DxfFactPresence.Present, valid ? DxfFactValidity.Valid : DxfFactValidity.Invalid,
                text, integer, real, indices, source);
        }
    }
}
