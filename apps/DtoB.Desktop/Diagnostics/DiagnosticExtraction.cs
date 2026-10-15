using System.IO;
using System.Security.Cryptography;
using DtoB.Cad.Dxf;
using DtoB.Cad.Raw;
using DtoB.Project;

namespace DtoB.Desktop.Diagnostics;

public sealed record DiagnosticIssue(string Stage, string Code, string Message, string Source);
public sealed record DiagnosticExtractionResult(RawDrawingSnapshot? Snapshot, string Stage, string Status, IReadOnlyList<DiagnosticIssue> Issues)
{
    public bool Succeeded => Snapshot != null;
}
public sealed record DiagnosticRequest(Guid ProjectId, string Manifest, DrawingFile Drawing)
{
    public bool Matches(ProjectSession session, DrawingFile? selected) => session.Project?.ProjectId == ProjectId &&
        StringComparer.OrdinalIgnoreCase.Equals(session.CurrentFilePath, Manifest) && selected?.DrawingId == Drawing.DrawingId &&
        selected.RevisionId == Drawing.RevisionId && selected.FileHash == Drawing.FileHash &&
        session.Project.Drawings!.Any(d => d == Drawing);
}
// UI-local epoch. Neither a source identity nor persisted project state.
public sealed class DiagnosticRequestGate
{
    private long epoch;
    public long Next() => ++epoch;
    public bool IsCurrent(long request) => request == epoch;
}
public sealed class DiagnosticExtraction(Func<string, Stream>? openRead = null, DxfReaderLimits? lexicalLimits = null)
{
    public const string DwgGuidance = "DtoB V1 reads Text DXF. Export/Save As the DWG to Text DXF externally, then register/select that DXF.";
    public Task<DiagnosticExtractionResult> ReadAsync(DiagnosticRequest request, CancellationToken token = default) => Task.Run(async () =>
    {
        var issues = new List<DiagnosticIssue>();
        DiagnosticExtractionResult Failure(string stage, string status, string code, string message)
        { issues.Add(new(stage, code, message, "")); return new(null, stage, status, issues.AsReadOnly()); }
        try
        {
            token.ThrowIfCancellationRequested();
            if (request.Drawing.FileExtension != ".dxf") return Failure("Input", "UnsupportedInput", "TextDxfRequired", DwgGuidance);
            var path = AssetPaths.Resolve(request.Manifest, request.ProjectId, request.Drawing);
            using var stream = openRead != null ? openRead(path) : new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.Asynchronous | FileOptions.SequentialScan);
            var limits = lexicalLimits ?? new DxfReaderLimits(); limits.Validate();
            if (!stream.CanRead || !stream.CanSeek || stream.CanWrite) return Failure("Input", "InvalidInput", "ReadLeaseRequired", "A read-only seekable managed file lease is required.");
            if (stream.Length > limits.MaxInputBytes) return Failure("03A", "ResourceLimitExceeded", "InputTooLarge", "Managed input exceeds the lexical byte cap.");
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, token).ConfigureAwait(false));
            if (!StringComparer.OrdinalIgnoreCase.Equals(hash, request.Drawing.FileHash)) return Failure("Integrity", "HashMismatch", "HashMismatch", "Managed asset differs from its registered revision; extraction refused.");
            stream.Position = 0;
            var a = await new TextDxfLexicalReader(limits).ReadAsync(stream, token).ConfigureAwait(false);
            issues.AddRange(a.Diagnostics.Select(x => new DiagnosticIssue("03A", x.Code.ToString(), x.Message, $"pair {x.PairIndex}, line {x.PhysicalLineNumber}")));
            if (a.Status != DxfLexicalStatus.Succeeded) return new(null, "03A", a.Status.ToString(), issues.AsReadOnly());
            var b = new TextDxfStructuralReader().Read(a, token);
            issues.AddRange(b.Diagnostics.Select(x => Issue("03B", x.Code.ToString(), x.Message, x.Source)));
            if (!b.StructureValidated) return new(null, "03B", b.Status.ToString(), issues.AsReadOnly());
            var c = new TextDxfGeometryReader().Read(b, token);
            issues.AddRange(c.Diagnostics.Select(x => Issue("03C", x.Code.ToString(), x.Message, x.Source)));
            if (c.Status is not (DxfGeometryStatus.Succeeded or DxfGeometryStatus.Partial)) return new(null, "03C", c.Status.ToString(), issues.AsReadOnly());
            var d = new TextDxfBlockAnnotationReader().Read(c, token);
            issues.AddRange(d.Diagnostics.Select(x => Issue("03D", x.Code.ToString(), x.Message, x.Source)));
            if (d.Status is not (DxfBlockAnnotationStatus.Succeeded or DxfBlockAnnotationStatus.Partial)) return new(null, "03D", d.Status.ToString(), issues.AsReadOnly());
            var e = new TextDxfIdentityGraphReader().Read(d, new(request.Drawing.DrawingId.ToString(), request.Drawing.RevisionId.ToString(), request.Drawing.FileHash), token);
            issues.AddRange(e.Diagnostics.Select(x => Issue("03E", x.Code.ToString(), x.Message, x.Source)));
            var f = new TextDxfRawDrawingSnapshotAssembler().Assemble(e, token);
            issues.AddRange(f.Diagnostics.Select(x => Issue("03F", x.Code.ToString(), x.Message, x.Source)));
            token.ThrowIfCancellationRequested();
            return new(f.Snapshot, f.Snapshot == null ? "03F" : "Snapshot", f.Status.ToString(), issues.AsReadOnly());
        }
        catch (OperationCanceledException) { return Failure("Request", "Canceled", "Canceled", "Extraction canceled; no result published."); }
        catch (Exception ex) when (LocalFileErrors.IsExpected(ex)) { return Failure("ManagedRead", "IoFailure", ex.GetType().Name, ex.Message); }
    }); // No cancellation scheduler argument: a pre-canceled request still returns structured failure.

    private static DiagnosticIssue Issue(string stage, string code, string message, DxfSourceSpan? source) =>
        new(stage, code, message, source == null ? "" : $"pairs {source.StartPairIndex}..{source.EndPairIndex}; lines {source.StartLine}..{source.EndLine}");
}
