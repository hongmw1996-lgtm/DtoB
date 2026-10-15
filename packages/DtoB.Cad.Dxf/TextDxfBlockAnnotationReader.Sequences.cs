using DtoB.Cad.Raw;

namespace DtoB.Cad.Dxf;

public sealed partial class TextDxfBlockAnnotationReader
{
    private sealed partial class Operation
    {
        private IEnumerable<int> SectionRecords(DxfSectionRecord section)
        {
            for (var i = 0; i < S.Records.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                if (S.Records[i].Source.StartPairIndex > section.Source.StartPairIndex &&
                    S.Records[i].Source.EndPairIndex < section.Source.EndPairIndex) yield return i;
            }
        }
        private void SequenceError(DxfBlockAnnotationDiagnosticCode code, string message, int index)
        {
            brokenSequence = true; invalidRecords.Add(index);
            Emit(code, DxfStructuralDiagnosticSeverity.Error, message, S.Records[index].Source);
        }
        private void BuildBlocks()
        {
            var geometry = new Dictionary<int, int>();
            for (var i = 0; i < evidence.Geometry.Count; i++)
            { token.ThrowIfCancellationRequested(); geometry.Add(evidence.Geometry[i].StructuralRecordIndex, i); }
            var started = 0;
            foreach (var section in S.Sections.Where(s => s.Name == "BLOCKS"))
            {
                token.ThrowIfCancellationRequested(); int? begin = null; var last = 0; var children = new List<RawBlockChild>();
                void Close(int? end)
                {
                    token.ThrowIfCancellationRequested(); var b = begin!.Value;
                    blocks.Add(new(b, end, Span(S.Records[b].Source.StartPairIndex, S.Records[end ?? last].Source.EndPairIndex),
                        end.HasValue && !invalidRecords.Contains(b) &&
                        recordLookup.TryGetValue(b, out var bi) && records[bi].Disposition == DxfDisposition.Supported &&
                        recordLookup.TryGetValue(end.Value, out var ei) && records[ei].Disposition == DxfDisposition.Supported
                            ? DxfDisposition.Supported : DxfDisposition.Invalid, children));
                    begin = null; children = [];
                }
                foreach (var i in SectionRecords(section))
                {
                    token.ThrowIfCancellationRequested(); var r = S.Records[i];
                    if (r.RecordType == "BLOCK")
                    {
                        if (begin.HasValue)
                        {
                            SequenceError(DxfBlockAnnotationDiagnosticCode.NestedBlock, "Nested BLOCK definition; preceding definition remains incomplete.", begin.Value);
                            invalidRecords.Add(i); Close(null);
                        }
                        Guard(++started > limits.MaxDefinitions, "block definitions", r.Source); begin = i; last = i;
                    }
                    else if (r.RecordType == "ENDBLK")
                    {
                        if (!begin.HasValue) SequenceError(DxfBlockAnnotationDiagnosticCode.UnexpectedBlockEnd, "ENDBLK without active BLOCK.", i);
                        else Close(i);
                    }
                    else if (begin.HasValue)
                    {
                        Guard(children.Count >= limits.MaxChildrenPerBlock, "block children", r.Source);
                        children.Add(new(i, geometry.TryGetValue(i, out var g) ? g : null, recordLookup.TryGetValue(i, out var a) ? a : null)); last = i;
                    }
                    else SequenceError(DxfBlockAnnotationDiagnosticCode.ChildOutsideBlock, "BLOCKS child outside a definition.", i);
                }
                if (begin.HasValue)
                {
                    SequenceError(DxfBlockAnnotationDiagnosticCode.MissingBlockEnd, "BLOCK reaches section end without ENDBLK.", begin.Value); Close(null);
                }
            }
        }
        private void BuildAttributeSequences()
        {
            foreach (var section in S.Sections.Where(s => s.Name is "BLOCKS" or "ENTITIES"))
            {
                token.ThrowIfCancellationRequested(); int? insert = null; var attributes = new List<int>(); var legacy = false;
                void Close(int? end)
                {
                    token.ThrowIfCancellationRequested();
                    var valid = end.HasValue && !invalidRecords.Contains(insert!.Value) &&
                        recordLookup.TryGetValue(insert.Value, out var a) && records[a].Disposition == DxfDisposition.Supported;
                    foreach (var i in attributes)
                    {
                        token.ThrowIfCancellationRequested();
                        if (!recordLookup.TryGetValue(i, out var ai) || records[ai].Disposition != DxfDisposition.Supported) valid = false;
                    }
                    sequences.Add(new(insert!.Value, attributes, end, valid ? DxfDisposition.Supported : DxfDisposition.Invalid)); insert = null; attributes = [];
                }
                foreach (var i in SectionRecords(section))
                {
                    token.ThrowIfCancellationRequested(); var r = S.Records[i];
                    if (insert.HasValue)
                    {
                        if (r.RecordType == "ATTRIB")
                        { Guard(attributes.Count >= limits.MaxAttributeSequenceLength, "attribute sequence length", r.Source); attributes.Add(i); continue; }
                        if (r.RecordType == "SEQEND")
                        {
                            if (recordLookup.TryGetValue(i, out var a))
                            {
                                var old = records[a];
                                records[a] = new(i, old.SourceRecord, DxfDisposition.Supported, old.Layer, new RawSequenceEnd("InsertAttributes"), old.Fields, old.UnmappedPairIndices);
                            }
                            else SequenceError(DxfBlockAnnotationDiagnosticCode.InvalidSourceRecord, "Attribute terminator is not a validated deferred03D record.", insert.Value);
                            Close(i); continue;
                        }
                        SequenceError(DxfBlockAnnotationDiagnosticCode.MissingAttributeSequenceEnd, "INSERT attribute sequence interrupted; following record preserved.", insert.Value); Close(null);
                    }
                    if (r.RecordType == "INSERT")
                    {
                        legacy = false;
                        if (recordLookup.TryGetValue(i, out var a) && records[a].Payload is RawInsert { AttributesFollow.Integer: 1 }) insert = i;
                    }
                    else if (r.RecordType == "ATTRIB") SequenceError(DxfBlockAnnotationDiagnosticCode.OrphanAttribute, "ATTRIB without active explicit INSERT sequence.", i);
                    else if (r.RecordType == "POLYLINE") legacy = true;
                    else if (r.RecordType == "SEQEND")
                    {
                        if (legacy)
                            Emit(DxfBlockAnnotationDiagnosticCode.LegacySequenceGated, DxfStructuralDiagnosticSeverity.Warning, "Legacy POLYLINE sequence remains deferred; SEQEND not assigned to attributes.", r.Source);
                        else
                        {
                            Emit(DxfBlockAnnotationDiagnosticCode.UnassignedSequenceEnd, DxfStructuralDiagnosticSeverity.Warning, "SEQEND has no reviewed active sequence role.", r.Source);
                            if (recordLookup.TryGetValue(i, out var a))
                            { var old = records[a]; records[a] = new(i, old.SourceRecord, DxfDisposition.Unsupported, old.Layer, old.Payload, old.Fields, old.UnmappedPairIndices); }
                        }
                        legacy = false;
                    }
                    else if (r.RecordType != "VERTEX") legacy = false;
                }
                if (insert.HasValue)
                { SequenceError(DxfBlockAnnotationDiagnosticCode.MissingAttributeSequenceEnd, "INSERT attribute sequence reaches section end without SEQEND.", insert.Value); Close(null); }
            }
        }
    }
}
