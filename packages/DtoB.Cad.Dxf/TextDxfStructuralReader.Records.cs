using DtoB.Cad.Raw;

namespace DtoB.Cad.Dxf;

public sealed partial class TextDxfStructuralReader
{
    private sealed partial class Operation
    {
        private sealed record ContextInspection(List<int> Eligible, List<DxfContextMarker> Markers, List<DxfRawReference> References);

        private ContextInspection Inspect(int start, int end, bool countReferences = true)
        {
            var eligible = new List<int>(); var markers = new List<DxfContextMarker>(); var refs = new List<DxfRawReference>();
            var controls = new List<(int Code, string Name)>(); string? subclass = null; string? application = null;
            for (var i = start; i <= end; i++)
            {
                token.ThrowIfCancellationRequested();
                var pair = P[i]; var code = pair.GroupCode; var text = pair.RawTextValue;
                if (Comment(i)) continue;
                if (code == 100 && controls.Count == 0 && application == null) subclass = text;
                if (code == 1001 && controls.Count == 0) application = text;
                if (code is 102 or 1002)
                {
                    if (code == 1002 && application == null)
                        Add(DxfStructuralDiagnosticCode.InvalidControlGroup, DxfStructuralDiagnosticSeverity.Error, "XDATA control requires an application marker.", Span(i, i));
                    if (text.StartsWith('{'))
                    {
                        Guard(controls.Count >= limits.MaxControlGroupDepth, "control nesting", i);
                        controls.Add((code, text));
                    }
                    else if (text == "}")
                    {
                        if (controls.Count == 0 || controls[^1].Code != code)
                            Add(DxfStructuralDiagnosticCode.InvalidControlGroup, DxfStructuralDiagnosticSeverity.Error, "Control close does not match its opener.", Span(i, i));
                        else controls.RemoveAt(controls.Count - 1);
                    }
                    else
                        Add(DxfStructuralDiagnosticCode.InvalidControlGroup, DxfStructuralDiagnosticSeverity.Error, "Control must open or close a brace group.", Span(i, i));
                }
                if (code is 100 or 102 or 1001 or 1002)
                    markers.Add(new(i, code, subclass, application, string.Join('/', controls.Select(c => c.Name))));
                if (controls.Count == 0 && application == null && code is not (100 or 102 or 1001 or 1002)) eligible.Add(i);
                if (code is >= 330 and <= 369 or >= 390 and <= 399 or 480 or 481 or 1005)
                {
                    if (countReferences) Guard(++referenceCount > limits.MaxReferenceOccurrences, "reference occurrences", i);
                    var role = application != null ? DxfReferenceRole.XData :
                        controls.Any(c => c.Name == "{ACAD_REACTORS") ? DxfReferenceRole.Reactor :
                        controls.Any(c => c.Name == "{ACAD_XDICTIONARY") ? DxfReferenceRole.ExtensionDictionary :
                        controls.Count == 0 && code == 330 && subclass is null or "AcDbSymbolTable" or "AcDbSymbolTableRecord"
                            ? DxfReferenceRole.Owner : DxfReferenceRole.Other;
                    refs.Add(new(i, code, role, subclass, application, string.Join('/', controls.Select(c => c.Name))));
                }
            }
            if (controls.Count > 0)
                Add(DxfStructuralDiagnosticCode.UnclosedControlGroup, DxfStructuralDiagnosticSeverity.Error, "Control group crosses a record/variable boundary.", Span(start, end));
            return new(eligible, markers, refs);
        }

        private DxfStructuralRecord MakeRecord(string type, string scope, int start, int end,
            DxfDisposition disposition, bool tableHeader = false)
        {
            Guard(++recordWorkCount > limits.MaxRecords, "record/variable count", start);
            Guard(end - start + 1 > limits.MaxFieldsPerRecord, "record fields", start);
            var errorsBefore = errorCount;
            var inspection = Inspect(start, end); var fields = new List<DxfNamedFact>(); var mapped = new HashSet<int>();
            void Map(string name, int code, DxfFactKind kind, IReadOnlyList<int>? overrideIndices = null)
            {
                var indices = overrideIndices ?? inspection.Eligible.Where(i => P[i].GroupCode == code).ToArray();
                foreach (var i in indices) { token.ThrowIfCancellationRequested(); mapped.Add(i); }
                fields.Add(new(name, Fact(indices, kind, DxfStructuralDiagnosticCode.InvalidTableValue)));
            }
            if (tableHeader)
            {
                Map("Name", 2, DxfFactKind.Text); Map("RawHandle", 5, DxfFactKind.Text);
                Map("DeclaredEntryCount", 70, DxfFactKind.Integer);
                Map("RawOwner", 330, DxfFactKind.Text, inspection.References.Where(r => r.Role == DxfReferenceRole.Owner).Select(r => r.PairIndex).ToArray());
            }
            else if (disposition == DxfDisposition.Supported && scope.StartsWith("TABLES/", StringComparison.Ordinal))
            {
                Map("Name", 2, DxfFactKind.Text);
                Map("RawHandle", type == "DIMSTYLE" ? 105 : 5, DxfFactKind.Text);
                Map("RawOwner", 330, DxfFactKind.Text, inspection.References.Where(r => r.Role == DxfReferenceRole.Owner).Select(r => r.PairIndex).ToArray());
                if (type == "DIMSTYLE")
                {
                    var roles = DimstyleCode70Roles(start, end, inspection);
                    Map("Flags", 70, DxfFactKind.Integer, roles.Flags);
                    Map("TextFillColor", 70, DxfFactKind.Integer, roles.TextFillColor);
                }
                else Map("Flags", 70, DxfFactKind.Integer);
                switch (type)
                {
                    case "LAYER":
                        Map("AciColor", 62, DxfFactKind.Integer); Map("TrueColor", 420, DxfFactKind.Integer);
                        Map("LinetypeName", 6, DxfFactKind.Text); Map("PlotFlag", 290, DxfFactKind.Integer);
                        Map("Lineweight", 370, DxfFactKind.Integer); Map("PlotStyleReference", 390, DxfFactKind.Text);
                        break;
                    case "LTYPE":
                        Map("Description", 3, DxfFactKind.Text); Map("Alignment", 72, DxfFactKind.Integer);
                        Map("DeclaredElementCount", 73, DxfFactKind.Integer); Map("PatternLength", 40, DxfFactKind.Real);
                        // Repeated pattern fields stay in source order, including shape/text/offset facts.
                        foreach (var i in inspection.Eligible.Where(i => P[i].GroupCode is 49 or 74 or 75 or 340 or 46 or 50 or 44 or 45 or 9))
                        {
                            token.ThrowIfCancellationRequested();
                            var code = P[i].GroupCode; mapped.Add(i);
                            fields.Add(new("Pattern/" + code, Fact(new[] { i }, code is 74 or 75 ? DxfFactKind.Integer :
                                code is 340 or 9 ? DxfFactKind.Text : DxfFactKind.Real, DxfStructuralDiagnosticCode.InvalidTableValue)));
                        }
                        break;
                    case "STYLE":
                        Map("FixedHeight", 40, DxfFactKind.Real); Map("WidthFactor", 41, DxfFactKind.Real);
                        Map("ObliqueAngle", 50, DxfFactKind.Real); Map("GenerationFlags", 71, DxfFactKind.Integer);
                        Map("LastHeight", 42, DxfFactKind.Real); Map("FontFile", 3, DxfFactKind.Text); Map("BigFontFile", 4, DxfFactKind.Text);
                        break;
                    case "DIMSTYLE":
                        Map("LegacyCode5Setting", 5, DxfFactKind.Text); Map("Postfix", 3, DxfFactKind.Text);
                        Map("AlternatePostfix", 4, DxfFactKind.Text); Map("OverallScale", 40, DxfFactKind.Real);
                        Map("ArrowSize", 41, DxfFactKind.Real); Map("TextHeight", 140, DxfFactKind.Real);
                        Map("TextStyleReference", 340, DxfFactKind.Text);
                        break;
                    case "BLOCK_RECORD":
                        Map("LayoutReference", 340, DxfFactKind.Text); Map("Explodability", 280, DxfFactKind.Integer);
                        Map("Scalability", 281, DxfFactKind.Integer); break;
                }
            }
            else if (type == "CLASS" && scope == "CLASSES")
            {
                Map("DxfName", 1, DxfFactKind.Text); Map("CppClassName", 2, DxfFactKind.Text);
                Map("ApplicationName", 3, DxfFactKind.Text); Map("ProxyFlags", 90, DxfFactKind.Integer);
                Map("InstanceCount", 91, DxfFactKind.Integer); Map("WasProxy", 280, DxfFactKind.Integer);
                Map("IsEntity", 281, DxfFactKind.Integer);
            }
            if (errorCount > errorsBefore)
                disposition = DxfDisposition.Invalid;
            var unmapped = new List<int>();
            for (var i = start; i <= end; i++) { token.ThrowIfCancellationRequested(); if (!mapped.Contains(i)) unmapped.Add(i); }
            return new(type, scope, Span(start, end), disposition, fields, unmapped, inspection.Markers, inspection.References);
        }

        private enum DimstyleFieldContext { BeforeName, StandardFlags, Variables, TextFillColor }

        /// <summary>AC1032's named symbol header owns its code70 run, before variable payload.
        /// DIMTFILL(code69) opens a distinct code70 DIMTFILLCLR run, ending at the next
        /// payload field. Comments/control data/common identity-reference fields do not
        /// select a role. Subclass transitions close runs; foreign subclasses never map70.
        /// All occurrences in a run are collected, so real same-role duplicates stay invalid.</summary>
        private (List<int> Flags, List<int> TextFillColor) DimstyleCode70Roles(
            int start, int end, ContextInspection inspection)
        {
            var flags = new List<int>(); var colors = new List<int>();
            var eligible = inspection.Eligible.ToHashSet();
            var subclasses = inspection.Markers.Where(m => m.GroupCode == 100 &&
                m.Application == null && m.ControlPath.Length == 0 && m.Subclass == P[m.PairIndex].RawTextValue)
                .ToDictionary(m => m.PairIndex, m => m.Subclass);
            string? subclass = null;
            var context = DimstyleFieldContext.BeforeName;
            var variablesStarted = false;
            for (var i = start; i <= end; i++)
            {
                token.ThrowIfCancellationRequested();
                if (subclasses.TryGetValue(i, out var marker))
                { subclass = marker; context = DimstyleFieldContext.BeforeName; continue; }
                if (!eligible.Contains(i)) continue;
                var code = P[i].GroupCode;
                if (code is 0 or 5 or 105 or >= 330 and <= 369 or >= 390 and <= 399 or 480 or 481) continue;
                var knownContext = subclass is null or "AcDbSymbolTableRecord" or "AcDbDimStyleTableRecord";
                if (code == 2 && !variablesStarted && knownContext)
                    context = DimstyleFieldContext.StandardFlags;
                else if (code == 69 && knownContext)
                { context = DimstyleFieldContext.TextFillColor; variablesStarted = true; }
                else if (code == 70)
                {
                    if (knownContext && context == DimstyleFieldContext.StandardFlags) flags.Add(i);
                    else if (knownContext && context == DimstyleFieldContext.TextFillColor) colors.Add(i);
                    else Add(DxfStructuralDiagnosticCode.InvalidTableValue, DxfStructuralDiagnosticSeverity.Error,
                        "DIMSTYLE code 70 has no supported symbol-header or DIMTFILLCLR field context; retained unmapped.", Span(i, i));
                }
                else
                { context = DimstyleFieldContext.Variables; variablesStarted = true; }
            }
            return (flags, colors);
        }
    }
}
