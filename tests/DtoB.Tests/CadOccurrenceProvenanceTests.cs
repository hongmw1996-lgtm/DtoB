using System.Reflection;
using System.Text.Json;
using DtoB.Cad.Normalize;
using DtoB.Cad.Normalize.Occurrences;
using DtoB.Cad.Raw;
using DtoB.Cad.V2;
using DtoB.Geometry;
using Xunit;
using static DtoB.Tests.Cad04EOccurrenceFixture;

namespace DtoB.Tests;

public sealed class CadOccurrenceProvenanceTests
{
    [Fact] public async Task CompactOwnedSourcesResolveExactRecordPairLineAndRealHandles()
    {
        var snapshot=await Snapshot(Block("B","A",Insert("d","B",columns:2,columnSpacing:10))+Block("C","B"),Insert("a","A",new(100,0,0)));
        var before=JsonSerializer.Serialize(snapshot); var r=Expand(snapshot); var a=Complete(r);
        Assert.Equal(before,JsonSerializer.Serialize(snapshot)); Assert.Equal(3,r.TotalOccurrencesAllocated);
        Assert.Equal(snapshot.Records.Count,a.Sources.Records.Count);
        for(var i=0;i<a.Occurrences.Count;i++)
        {
            var o=a.Occurrences[i]; var source=a.Sources.Resolve(o.Value.InsertSource); var raw=snapshot.Records[o.Value.InsertSource.Index];
            Assert.Equal(i,o.SourceOrderOrdinal); Assert.Equal(raw.Identity!.OriginalHandle.Text,source.OriginalHandle);
            Assert.Equal(raw.Identity.CanonicalHandle,source.CanonicalHandle); Assert.Equal(raw.SourceRecord.Source.StartPairIndex,source.Locator.FirstPair);
            Assert.Equal(raw.SourceRecord.Source.EndPairIndex,source.Locator.LastPair); Assert.Equal(raw.SourceRecord.Source.StartLine,source.Locator.FirstPhysicalLine);
            Assert.Equal(raw.SourceRecord.Source.EndLine,source.Locator.LastPhysicalLine);
            Assert.Equal(o.Definition.Id.Value,o.Value.DefinitionId.Value);
            Assert.Equal(o.Value.Path.Steps[^1].DefinitionSource.Index,o.Definition.Source.Index);
            Assert.Equal(snapshot.DocumentProvenance!.FileHash,a.Sources.Source.FileHash);
        }
        var child=a.Occurrences[2]; Assert.Equal(a.Occurrences[0].Value.Id.Value,child.Value.ParentOccurrenceId!.Value);
        Assert.Equal(1,child.CellOrdinal); Assert.Equal(0,child.Value.Path.Steps[^1].Row); Assert.Equal(1,child.Value.Path.Steps[^1].Column);
        Assert.Equal(10,child.Value.LocalTransform.Values[3]); Assert.Equal(110,child.Value.AccumulatedTransform.Values[3]);
        Assert.Equal("d",a.Sources.Resolve(child.Value.InsertSource).OriginalHandle);
        Assert.Equal("D",a.Sources.Resolve(child.Value.InsertSource).CanonicalHandle);
        Assert.NotEmpty(child.Grid.ColumnSpacingSource.Source.PairIndices);
        Assert.Equal(DxfFactPresence.Absent,child.Grid.RowsSource.Presence);
        Assert.Null(child.Value.StyleId);
    }
    [Fact] public async Task ExpansionCollectionsAndMatricesCannotBeMutated()
    {
        var r=Expand(await Nested()); var a=Complete(r); var o=a.Occurrences[1];
        Assert.Throws<NotSupportedException>(()=>((IList<CadExpandedOccurrence>)a.Occurrences).Clear());
        Assert.Throws<NotSupportedException>(()=>((IList<CadOccurrenceDefinition>)a.Definitions).Clear());
        Assert.Throws<NotSupportedException>(()=>((IList<CadSourceRecordRef>)a.Occurrences[0].Definition.ChildSources).Clear());
        Assert.Throws<NotSupportedException>(()=>((IList<double>)o.Value.LocalTransform.Values)[0]=999);
        var copy=o.Value.AccumulatedTransform.ToTransform3(); copy.Matrix[3]=999;
        Assert.Equal(100,o.Value.AccumulatedTransform.Values[3]);
        foreach(var type in typeof(CadOccurrenceExpansion).Assembly.GetExportedTypes().Where(t=>t.Namespace=="DtoB.Cad.Normalize.Occurrences" && !t.IsEnum))
        foreach(var p in type.GetProperties(BindingFlags.Public|BindingFlags.Instance))
        { Assert.Null(p.SetMethod); Assert.False(p.PropertyType.IsArray); Assert.DoesNotContain(p.PropertyType,new[]{typeof(RawDrawingSnapshot),typeof(RawBlockDefinition),typeof(RawInsert)}); }
    }
    [Fact] public async Task EmptyDrawingPublishesAnEmptyOccurrenceArtifactWithoutGeometry()
    {
        var r=Expand(await Snapshot(Block("B","B"),"")); var a=Complete(r);
        Assert.Empty(a.Occurrences); Assert.Empty(a.Definitions); Assert.Equal(0,r.TotalOccurrencesAllocated);
        Assert.Equal(250_000,a.Options.Limits.MaxDerivedGeometry);
    }
    [Fact] public async Task InvalidUnitsRefusePublicationWithDiagnostic()
    {
        var r=Expand(await Snapshot(Block("B","B"),Insert("A","B"),units:0));
        Assert.Equal(CadExpansionStatus.InvalidInput,r.Status); Assert.Null(r.Expansion); Assert.Single(r.Diagnostics);
    }
}
public sealed class Cad04EScopeArchitectureTests
{
    [Fact] public void OccurrenceProductionHasNoStyleGeometryAssemblyIoOrVendorImplementation()
    {
        foreach(var file in Directory.GetFiles(Path.Combine(CadV2ContractFixture.Root(),"packages/DtoB.Cad.Normalize/Occurrences"),"*.cs"))
        {
            var text=File.ReadAllText(file);
            Assert.DoesNotMatch(@"\b(CadDocument|CadCircle|CadArc|CadPolyline|ByLayer|ByBlock|TrueColor|Bulge|Bounds|Autodesk|RevitAPI|ElementId|ACadSharp|FileStream|Directory|File)\b",text);
            Assert.DoesNotMatch(@"\b(ResolveStyle|Flatten|NormalizeGeometry|CreateGeometry)\s*\(",text);
        }
        Assert.DoesNotContain(typeof(CadOccurrenceExpander).Assembly.GetReferencedAssemblies(),a=>new[]{"DtoB.Cad.Dxf","DtoB.Bim","DtoB.Desktop","PresentationFramework","RevitAPI"}.Contains(a.Name));
        Assert.Equal(typeof(DtoB.Cad.Normalize.Assembly.CadNormalizer), Assert.Single(typeof(ICadNormalizer).Assembly.GetTypes(), t => t.IsClass && typeof(ICadNormalizer).IsAssignableFrom(t)));
    }
    [Fact] public void TraversalHasCancellationAtSourceCellLookupAndAllocationBoundaries()
    {
        var text=File.ReadAllText(Path.Combine(CadV2ContractFixture.Root(),"packages/DtoB.Cad.Normalize/Occurrences/CadOccurrenceExpander.cs"));
        Assert.Contains("Stack<IEnumerator<Candidate>>",text);
        Assert.True(text.Split("token.ThrowIfCancellationRequested()",StringSplitOptions.None).Length-1>=12);
        Assert.Contains("allocated++;",text); Assert.DoesNotMatch(@"allocated\s*(?:--|-=|=\s*0)|allocated\s*=\s*allocated\s*-",text);
        Assert.True(text.IndexOf("allocated++;",StringComparison.Ordinal)<text.IndexOf("local = CellTransform",StringComparison.Ordinal));
    }
}
