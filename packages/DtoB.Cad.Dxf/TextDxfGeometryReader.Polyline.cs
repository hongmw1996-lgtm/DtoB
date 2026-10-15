using DtoB.Cad.Raw;

namespace DtoB.Cad.Dxf;

public sealed partial class TextDxfGeometryReader
{
    private sealed partial class Operation
    {
        private sealed partial class Mapping
        {
            private RawLwPolyline Polyline()
            {
                var count = Scalar("DeclaredVertexCount", 90, DxfFactKind.Integer, true);
                var flags = Scalar("Flags", 70, DxfFactKind.Integer);
                var elevation = Scalar("Elevation", 38); var width = Scalar("ConstantWidth", 43);
                if (count.Integer is <= 0)
                    count = Invalidate("DeclaredVertexCount", count, DxfGeometryDiagnosticCode.InvalidVertexCount,
                        "Raw LWPOLYLINE profile requires a positive declared vertex count; no repair.");
                var groups = new List<List<int>>(); List<int>? current = null; var seenY = false;
                foreach (var i in native)
                {
                    Token.ThrowIfCancellationRequested(); var code = P[i].GroupCode;
                    if (code == 10)
                    {
                        owner.Guard(groups.Count >= owner.Limits.MaxVerticesPerPolyline, "vertices per polyline", owner.Span(i, i));
                        owner.Guard(owner.totalVertices >= owner.Limits.MaxTotalVertices, "total polyline vertices", owner.Span(i, i));
                        current = []; groups.Add(current); owner.totalVertices++; seenY = false;
                    }
                    if (code is not (10 or 20 or 40 or 41 or 42 or 91)) continue;
                    if (current == null)
                    { Error(DxfGeometryDiagnosticCode.InvalidVertexOrder, "LWPOLYLINE vertex field precedes its X start; retained unmapped.", owner.Span(i, i)); continue; }
                    if (code == 20) seenY = true;
                    else if (code != 10 && !seenY)
                        Error(DxfGeometryDiagnosticCode.InvalidVertexOrder, "LWPOLYLINE optional vertex field precedes required Y.", owner.Span(i, i));
                    current.Add(i);
                }
                if (groups.Count == 0)
                    Error(DxfGeometryDiagnosticCode.MissingRequiredField, "LWPOLYLINE has no source vertices.", source.Source);
                var vertices = new List<RawLwVertex>();
                foreach (var group in groups)
                {
                    Token.ThrowIfCancellationRequested(); var ordinal = vertices.Count;
                    DxfSourceFact VertexFact(string name, int code, bool required = false, DxfFactKind kind = DxfFactKind.Real) =>
                        Fact("Vertices[" + ordinal + "]." + name, group.Where(i => P[i].GroupCode == code).ToArray(), kind, required);
                    vertices.Add(new(ordinal, owner.Span(group[0], group[^1]), VertexFact("X", 10, true), VertexFact("Y", 20, true),
                        VertexFact("StartWidth", 40), VertexFact("EndWidth", 41), VertexFact("Bulge", 42), VertexFact("Identifier", 91, kind: DxfFactKind.Integer)));
                }
                if (count.Integer.HasValue && count.Integer.Value != groups.Count)
                    Error(DxfGeometryDiagnosticCode.VertexCountMismatch, "Declared LWPOLYLINE vertex count differs from source X-delimited groups; no repair.", count.Source!);
                Token.ThrowIfCancellationRequested(); return new(count, flags, elevation, width, vertices);
            }
        }
    }
}
