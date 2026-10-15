using System.Text;
using System.Text.Json;
using DtoB.Cad.Dxf;
using DtoB.Cad.Raw;
using Xunit;

namespace DtoB.Tests;

public partial class ExtractionQaCampaignTests
{
    [Theory]
    [InlineData("03A")][InlineData("03B")][InlineData("03C")][InlineData("03D")][InlineData("03E")][InlineData("03F")]
    public async Task PreCanceledStagesNeverPublishOwnCompletionAndRejectDownstreamSnapshot(string stage)
    {
        var original = await FreshControlled(); var a = original.LexicalEvidence; var b = original.StructuralEvidence;
        var c = original.GeometryEvidence; var d = original.BlockAnnotationEvidence; var e = original.IdentityEvidence;
        var token = new CancellationToken(true);
        if (stage == "03A")
        {
            using var stream = File.OpenRead(Controlled); a = await new TextDxfLexicalReader().ReadAsync(stream, token);
            Assert.Equal(0, stream.Position); Assert.Equal(DxfLexicalStatus.Canceled, a.Status);
            b = new TextDxfStructuralReader().Read(a);
        }
        if (stage == "03B") { b = new TextDxfStructuralReader().Read(a, token); Assert.Equal(DxfStructuralStatus.Canceled, b.Status); }
        if (stage is "03A" or "03B") { Assert.False(b.StructureValidated); c = new TextDxfGeometryReader().Read(b); }
        if (stage == "03C") c = new TextDxfGeometryReader().Read(b, token);
        if (stage is "03A" or "03B" or "03C") { Assert.Equal(DxfGeometryStatus.Canceled, c.Status); Assert.False(c.GeometryMappingComplete); d = new TextDxfBlockAnnotationReader().Read(c); }
        if (stage == "03D") d = new TextDxfBlockAnnotationReader().Read(c, token);
        if (stage != "03E" && stage != "03F") { Assert.Equal(DxfBlockAnnotationStatus.Canceled, d.Status); Assert.False(d.MappingComplete); e = new TextDxfIdentityGraphReader().Read(d); }
        if (stage == "03E") e = new TextDxfIdentityGraphReader().Read(d, cancellationToken: token);
        if (stage != "03F") { Assert.Equal(DxfGraphStatus.Canceled, e.Status); Assert.False(e.GraphValidated); Assert.False(e.IdentityCoverageComplete); Assert.False(e.ReferenceResolutionComplete); }
        var result = new TextDxfRawDrawingSnapshotAssembler().Assemble(e, stage == "03F" ? token : default);
        Assert.Null(result.Snapshot); Assert.False(result.Completeness.SnapshotAssemblyComplete);
        Assert.Equal(stage == "03F" ? RawSnapshotAssemblyStatus.Canceled : RawSnapshotAssemblyStatus.InvalidInput, result.Status);
        Evidence("cancel-pre-" + stage + ".json", JsonSerializer.Serialize(result));
    }
    private sealed class CancelReadStream(byte[] bytes, CancellationTokenSource source) : MemoryStream(bytes, false)
    {
        public int ReadCalls { get; private set; }
        public bool WasDisposed { get; private set; }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var n = base.Read(buffer.Span[..Math.Min(buffer.Length, 1)]);
            if (++ReadCalls == 30) source.Cancel();
            return ValueTask.FromResult(n);
        }
        protected override void Dispose(bool disposing) { WasDisposed = true; base.Dispose(disposing); }
    }
    [Fact]
    public async Task CancellationDuringActualLexicalReadsIsDeterministicAndCallerOwnsStream()
    {
        using var source = new CancellationTokenSource(); using var stream = new CancelReadStream(Encoding.UTF8.GetBytes(Doc(Point)), source);
        var r = await new TextDxfLexicalReader().ReadAsync(stream, source.Token);
        Assert.Equal(DxfLexicalStatus.Canceled, r.Status); Assert.Equal(30, stream.ReadCalls); Assert.False(stream.WasDisposed); Assert.True(stream.CanRead);
        Assert.Null(Snapshot(r).Snapshot); Evidence("cancel-during-read.json", JsonSerializer.Serialize(r));
    }
    private static IEnumerable<T> CancelOnSecond<T>(IEnumerable<T> original, CancellationTokenSource source)
    {
        var i = 0; foreach (var value in original) { if (++i == 2) source.Cancel(); yield return value; }
        Assert.True(i >= 2, "Fixture must exercise cancellation after at least one accepted item.");
    }
    [Theory]
    [InlineData("03B-records")][InlineData("03C-geometry")][InlineData("03D-records")][InlineData("03D-blocks")]
    [InlineData("03E-nodes")][InlineData("03E-references")][InlineData("03E-ownership")]
    [InlineData("03F-records")][InlineData("03F-diagnostics")]
    public async Task CancellationDuringExistingFreezeEnumerationsNeverPublishesObject(string location)
    {
        var s = await FreshControlled(); var a = s.LexicalEvidence; var b = s.StructuralEvidence; var c = s.GeometryEvidence;
        var d = s.BlockAnnotationEvidence; var e = s.IdentityEvidence; using var cancel = new CancellationTokenSource();
        object? published = null;
        Assert.Throws<OperationCanceledException>(() =>
        {
            published = location switch
            {
                "03B-records" => new DxfStructuralReadResult(a, b.Status, b.VersionAcceptance, b.Sections, b.Header, b.Tables, CancelOnSecond(b.Records, cancel), b.Diagnostics, true, cancel.Token),
                "03C-geometry" => new DxfGeometryReadResult(b, c.Status, CancelOnSecond(c.Geometry, cancel), c.Diagnostics, true, cancel.Token),
                "03D-records" or "03D-blocks" => new DxfBlockAnnotationReadResult(c, d.Status, location == "03D-records" ? CancelOnSecond(d.Records, cancel) : d.Records,
                    location == "03D-blocks" ? CancelOnSecond(d.BlockDefinitions, cancel) : d.BlockDefinitions, d.AttributeSequences, d.Diagnostics, true, cancel.Token),
                "03E-nodes" or "03E-references" or "03E-ownership" => new DxfIdentityGraphReadResult(d, e.DocumentProvenance, e.Status,
                    location == "03E-nodes" ? CancelOnSecond(e.Nodes, cancel) : e.Nodes, e.NonIdentityRecords,
                    location == "03E-references" ? CancelOnSecond(e.References, cancel) : e.References,
                    location == "03E-ownership" ? CancelOnSecond(e.Ownership, cancel) : e.Ownership,
                    e.DuplicateGroups, e.OwnerCycles, e.Diagnostics, true, true, true, cancel.Token),
                _ => new RawDrawingSnapshot(e, location == "03F-records" ? CancelOnSecond(s.Records, cancel) : s.Records,
                    location == "03F-diagnostics" ? CancelOnSecond(s.Diagnostics, cancel) : s.Diagnostics, s.AssemblyDiagnostics, s.Completeness, cancel.Token)
            };
        });
        Assert.Null(published); Assert.True(cancel.IsCancellationRequested);
        Evidence("cancel-freeze-" + location + ".json", "{\"Outcome\":\"OperationCanceledException; no object published\"}");
    }
}
