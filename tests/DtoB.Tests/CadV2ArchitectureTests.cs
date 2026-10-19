using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using DtoB.Cad.Normalize;
using DtoB.Cad.V2;
using Xunit;

namespace DtoB.Tests;

public sealed class CadV2ArchitectureTests
{
    private static string Root => CadV2ContractFixture.Root();
    private static XDocument Project(string path) => XDocument.Load(Path.Combine(Root, path));
    private static string[] Refs(XDocument project) => project.Descendants("ProjectReference").Select(x => Path.GetFileNameWithoutExtension(x.Attribute("Include")!.Value.Replace('/', Path.DirectorySeparatorChar))).Order(StringComparer.Ordinal).ToArray();
    [Fact] public void ExactPackageGraphHasNoReverseUiProjectVendorOrAdapterEdges()
    {
        var raw = Project("packages/DtoB.Cad.Raw/DtoB.Cad.Raw.csproj"); var dxf = Project("packages/DtoB.Cad.Dxf/DtoB.Cad.Dxf.csproj");
        var cad = Project("packages/DtoB.Cad/DtoB.Cad.csproj"); var normalizer = Project("packages/DtoB.Cad.Normalize/DtoB.Cad.Normalize.csproj");
        Assert.Empty(Refs(raw)); Assert.Equal(new[] {"DtoB.Cad.Raw"}, Refs(dxf));
        Assert.Equal(new[] {"DtoB.Core", "DtoB.Geometry"}, Refs(cad)); Assert.Equal(new[] {"DtoB.Cad", "DtoB.Cad.Raw"}, Refs(normalizer));
        foreach (var project in new[] {raw, dxf, cad, normalizer})
        { Assert.Empty(project.Descendants("PackageReference")); Assert.Empty(project.Descendants("Reference")); Assert.Empty(project.Descendants("UseWPF")); }
    }
    [Fact] public void ContractStateAndImportsContainNoRawGraphOrRevitTypes()
    {
        var assembly = typeof(CadDocument).Assembly;
        var types = assembly.GetExportedTypes().Where(t => t.Namespace == "DtoB.Cad.V2").ToArray();
        Assert.All(types.SelectMany(t => t.GetProperties(BindingFlags.Instance | BindingFlags.Public)), p =>
        { Assert.Null(p.SetMethod); Assert.DoesNotContain("DtoB.Cad.Raw", p.PropertyType.ToString()); });
        Assert.All(typeof(CadGeometry).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance), c => Assert.True(c.IsFamilyAndAssembly));
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), a => a.Name!.Contains("Revit", StringComparison.Ordinal) || a.Name.Contains("Autodesk", StringComparison.Ordinal));
        Assert.DoesNotContain(typeof(ICadNormalizer).Assembly.GetReferencedAssemblies(), a => new[] {"DtoB.Desktop", "DtoB.Project", "PresentationFramework", "DtoB.Cad.Dxf"}.Contains(a.Name));
        var files = Directory.GetFiles(Path.Combine(Root, "packages/DtoB.Cad/V2"), "*.cs").Concat(Directory.GetFiles(Path.Combine(Root, "packages/DtoB.Cad.Normalize"), "*.cs"));
        var forbidden = new Regex(@"\b(Autodesk\.Revit|RevitAPI(?:UI)?|ElementId|BuiltInCategory|FamilySymbol|WallType)\b");
        Assert.All(files, file => Assert.False(forbidden.IsMatch(File.ReadAllText(file)), file));
    }
    [Fact] public void LegacyVersionAndAnalysisBoundariesStayV1()
    {
        var method = typeof(DtoB.Analysis.IAnalysisEngine).GetMethod("AnalyzeAsync")!;
        Assert.Equal(typeof(DtoB.Cad.CadDocument), method.GetParameters()[0].ParameterType);
        Assert.DoesNotContain(typeof(DtoB.Analysis.IAnalysisEngine).Assembly.GetReferencedAssemblies(), a => a.Name == "DtoB.Cad.Normalize");
        var v2 = Directory.GetFiles(Path.Combine(Root, "packages/DtoB.Cad/V2"), "*.cs").Select(File.ReadAllText);
        Assert.All(v2, text => Assert.DoesNotMatch(@"(?<!Cad)Contract\.Version\s*\(|\bIrJson\.Deserialize\s*<", text));
    }
    [Fact] public void NormalizerBoundaryHasNoIoOrLaterPhaseAlgorithm()
    {
        var source = File.ReadAllText(Path.Combine(Root, "packages/DtoB.Cad.Normalize/NormalizationBoundary.cs"));
        Assert.DoesNotMatch(@"\b(File|Directory|FileStream|Task\.Run)\s*\.|Math\.(Sin|Cos|Tan|Atan|Sqrt)|\.Then\(", source);
        Assert.DoesNotContain(typeof(ICadNormalizer).Assembly.GetTypes(), t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Any(m => m.Name is "Expand" or "ResolveStyle" or "NormalizeCoordinates"));
    }
}
