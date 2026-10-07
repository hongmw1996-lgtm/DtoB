using System.Xml.Linq;
using Xunit;
namespace DtoB.Tests;
public class FoundationDocumentTests
{
    private static string Root()
    {
        var dir=new DirectoryInfo(AppContext.BaseDirectory);
        while(dir!=null&&!File.Exists(Path.Combine(dir.FullName,"DtoB.sln")))dir=dir.Parent;
        return dir?.FullName??throw new InvalidDataException("Repository root not found.");
    }
    private static readonly Dictionary<string,string[]> Allowed=new()
    {
        ["DtoB.Core"]=[],["DtoB.Geometry"]=["DtoB.Core"],["DtoB.Cad"]=["DtoB.Core","DtoB.Geometry"],
        ["DtoB.Bim"]=["DtoB.Core","DtoB.Geometry"],["DtoB.Ipc"]=[],["DtoB.Analysis"]=["DtoB.Cad","DtoB.Bim"],
        ["DtoB.Revit.Core"]=["DtoB.Core"],["DtoB.Revit2025"]=["DtoB.Ipc"],["DtoB.Desktop"]=["DtoB.Ipc"]
    };
    private static void Check(string name,XDocument xml)
    {
        foreach(var reference in xml.Descendants("ProjectReference"))
            Assert.Contains(Path.GetFileNameWithoutExtension((string)reference.Attribute("Include")!),Allowed[name]);
        foreach(var reference in xml.Descendants("Reference"))
        {
            var assembly=(string)reference.Attribute("Include")!;
            Assert.True(name=="DtoB.Revit2025"&&assembly is "RevitAPI" or "RevitAPIUI",$"Forbidden assembly {assembly} in {name}");
            Assert.Equal("false",(string?)reference.Element("Private"));
        }
    }
    [Fact] public void AllProductionProjectDirectionsAreEnforced()
    {
        foreach(var pair in Allowed)
        {
            var line=File.ReadLines(Path.Combine(Root(),"DtoB.sln")).Single(line=>line.Contains("= "+Convert.ToChar(34)+pair.Key+Convert.ToChar(34)) && line.Contains(".csproj"));
            var relative=line.Split(Convert.ToChar(34))[5]; var path=Path.Combine(Root(),relative);
            Check(pair.Key,XDocument.Load(path));
        }
    }
    [Fact] public void ForbiddenDependencyMutationIsRejected()
    { Assert.ThrowsAny<Exception>(()=>Check("DtoB.Desktop",XDocument.Parse("<Project><ItemGroup><ProjectReference Include='DtoB.Revit2025.csproj'/></ItemGroup></Project>"))); }
    [Fact] public void AdrsContainEvaluatedAlternativesAndValidUtf8()
    {
        var utf8=new System.Text.UTF8Encoding(false,true);
        foreach(var path in Directory.EnumerateFiles(Path.Combine(Root(),"docs","adr"),"ADR-*.md"))
        {
            var text=utf8.GetString(File.ReadAllBytes(path));Assert.DoesNotContain("\uFFFD",text);
            Assert.DoesNotContain("Accepted ?",text);Assert.Contains("| Option | Evaluation",text);
            var alternatives=text.Split("## Alternatives")[1].Split("## Decision")[0].Split('\n').Where(line=>line.StartsWith("| ")&&!line.Contains("Option |")).ToArray();
            Assert.True(alternatives.Length>=2);
            Assert.All(alternatives,row=>Assert.True(row.Length>55 && (row.Contains("Selected:")||row.Contains("Rejected")||row.Contains("Deferred:")||row.Contains("Candidate only:")),"Alternatives need evaluated reasons."));
        }
    }
}
