namespace DtoB.Cad.Raw;

/// <summary>PairIndex is zero-based; physical source lines are one-based.
/// Text excludes only its line terminator and an optional byte-zero UTF-8 BOM.</summary>
public sealed record DxfRawPair(int PairIndex, int GroupCode, string RawCodeText,
    string RawTextValue, int CodeLineNumber, int ValueLineNumber);

public enum DxfLexicalStatus
{
    Succeeded, InvalidInput, UnsupportedInput, UnsupportedEncoding,
    ResourceLimitExceeded, Canceled, IoFailure
}

public enum DxfVersionAcceptance { NotValidated }
public enum DxfDiagnosticSeverity { Error }
public enum DxfDiagnosticCategory { Lexical, Encoding, Input, Resource, Cancellation, Io }
public enum DxfLexicalDiagnosticCode
{
    EmptyInput, UnsupportedInputFormat, UnsupportedBom, UnexpectedBom, InvalidUtf8,
    EmbeddedNul, InvalidLineEnding, OddPhysicalLineCount, InvalidGroupCode,
    GroupCodeOutOfRange, InputTooLarge, LineTooLong, PairLimitExceeded,
    DiagnosticLimitExceeded, Canceled, IoFailure
}

public sealed record DxfLexicalDiagnostic(DxfLexicalDiagnosticCode Code,
    DxfDiagnosticSeverity Severity, DxfDiagnosticCategory Category, string Message,
    int? PhysicalLineNumber = null, int? PairIndex = null);

[Flags]
public enum DxfLineEndings { None = 0, Lf = 1, CrLf = 2 }

/// <summary>Lexical success is not Text DXF structure/version acceptance. No CAD semantics
/// or $ACADVER validation are performed; 03B must establish AC1032 compatibility.</summary>
public sealed class DxfLexicalReadResult
{
    public DxfLexicalStatus Status { get; }
    public DxfVersionAcceptance VersionAcceptance => DxfVersionAcceptance.NotValidated;
    public IReadOnlyList<DxfRawPair> Pairs { get; }
    public IReadOnlyList<DxfLexicalDiagnostic> Diagnostics { get; }
    public long SourceBytesConsumed { get; }
    public int PhysicalLineCount { get; }
    public bool HasUtf8Bom { get; }
    public DxfLineEndings LineEndings { get; }

    public DxfLexicalReadResult(DxfLexicalStatus status, IEnumerable<DxfRawPair> pairs,
        IEnumerable<DxfLexicalDiagnostic> diagnostics, long sourceBytesConsumed,
        int physicalLineCount, bool hasUtf8Bom, DxfLineEndings lineEndings)
    {
        Status = status;
        Pairs = Array.AsReadOnly(pairs.ToArray());
        Diagnostics = Array.AsReadOnly(diagnostics.ToArray());
        SourceBytesConsumed = sourceBytesConsumed;
        PhysicalLineCount = physicalLineCount;
        HasUtf8Bom = hasUtf8Bom;
        LineEndings = lineEndings;
    }
}
