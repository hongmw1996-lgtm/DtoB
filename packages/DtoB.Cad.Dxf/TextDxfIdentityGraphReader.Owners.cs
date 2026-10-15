using DtoB.Cad.Raw;

namespace DtoB.Cad.Dxf;

public sealed partial class TextDxfIdentityGraphReader
{
    private sealed partial class Operation
    {
        private void Work()
        { Guard(++traversalWork > limits.MaxTraversalWork, "ownership traversal"); }
        private void ValidateOwners()
        {
            var byChild = new Dictionary<DxfGraphRecordLocator, List<int>>();
            for (var i = 0; i < edges.Count; i++)
            {
                Work(); var e = edges[i]; if (e.ContextEvidence.Role != DxfReferenceRole.Owner) continue;
                if (!nodeLookup.ContainsKey(e.SourceLocator))
                    Emit(DxfGraphDiagnosticCode.OwnershipSourceUnavailable, DxfStructuralDiagnosticSeverity.Error,
                        "Owner-role reference has no applicable source identity node; ownership cannot be validated.", e.Source, e.SourceLocator);
                if (!byChild.TryGetValue(e.SourceLocator, out var list)) { list = []; byChild.Add(e.SourceLocator, list); } list.Add(i);
            }
            foreach (var n in nodes)
            {
                Work(); var indices = byChild.GetValueOrDefault(n.Locator);
                var requiredOwner = n.SourceRecord.ContextMarkers.Any(m => m.GroupCode == 100 && m.Application == null && m.ControlPath.Length == 0 && m.Subclass == "AcDbEntity");
                if (indices == null)
                {
                    if (requiredOwner) Emit(DxfGraphDiagnosticCode.MissingRequiredOwner, DxfStructuralDiagnosticSeverity.Error, "AcDbEntity profile requires an explicit Owner fact.", n.SourceRecord.Source, n.Locator);
                    continue;
                }
                if (indices.Count > 1)
                    Emit(DxfGraphDiagnosticCode.MultipleOwners, DxfStructuralDiagnosticSeverity.Error, "Multiple Owner facts retained; no competing parent selected.", n.SourceRecord.Source, n.Locator);
                foreach (var i in indices)
                {
                    Work(); var e = edges[i];
                    if (e.Resolution == DxfReferenceResolution.Resolved && n.Validity == DxfIdentityValidity.Valid)
                        owners.Add(new(n.Locator, e.TargetLocator!, i));
                }
            }
            // Iterative DFS of Owner edges only; pointer cycles have no effect. Source-order traversal.
            var adjacency = new Dictionary<DxfGraphRecordLocator, List<DxfGraphRecordLocator>>();
            foreach (var e in owners)
            { Work(); if (!adjacency.TryGetValue(e.Child, out var list)) { list = []; adjacency.Add(e.Child, list); } list.Add(e.Owner); }
            var colors = new Dictionary<DxfGraphRecordLocator, int>();
            foreach (var n in nodes)
            {
                Work(); if (colors.GetValueOrDefault(n.Locator) != 0) continue;
                var stack = new List<(DxfGraphRecordLocator Node, int Next)> { (n.Locator, 0) }; colors[n.Locator] = 1;
                while (stack.Count > 0)
                {
                    Work(); var frame = stack[^1]; var next = adjacency.GetValueOrDefault(frame.Node);
                    if (next == null || frame.Next >= next.Count) { colors[frame.Node] = 2; stack.RemoveAt(stack.Count - 1); continue; }
                    var target = next[frame.Next]; stack[^1] = (frame.Node, frame.Next + 1);
                    var color = colors.GetValueOrDefault(target);
                    if (color == 0) { colors[target] = 1; stack.Add((target, 0)); }
                    else if (color == 1)
                    {
                        var participants = new List<DxfGraphRecordLocator>(); var inside = false;
                        foreach (var active in stack) { Work(); if (active.Node == target) inside = true; if (inside) participants.Add(active.Node); }
                        cycles.Add(new(participants));
                        foreach (var locator in participants)
                        {
                            Work(); var item = nodes[nodeLookup[locator]];
                            Emit(participants.Count == 1 ? DxfGraphDiagnosticCode.SelfOwner : DxfGraphDiagnosticCode.OwnerCycle,
                                DxfStructuralDiagnosticSeverity.Error, "Owner cycle includes original Handle " + item.OriginalHandle.Text + ".", item.OriginalHandle.Source, locator);
                        }
                    }
                }
            }
        }
    }
}
