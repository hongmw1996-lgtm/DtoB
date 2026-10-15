using System.Globalization;
using System.Text;
using DtoB.Cad.Raw;

namespace DtoB.Cad.Dxf;

/// <summary>Reads from the caller's current stream position without seeking, writing,
/// disposing or reopening it. The caller supplies an immutable stream starting at the input.
/// Only UTF-8/physical lines/group-code integers are interpreted. Version remains unvalidated.</summary>
public sealed class TextDxfLexicalReader
{
    private readonly DxfReaderLimits limits;
    public TextDxfLexicalReader(DxfReaderLimits? limits = null)
    {
        this.limits = limits ?? new();
        this.limits.Validate();
    }

    public Task<DxfLexicalReadResult> ReadAsync(Stream input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        return new ReadOperation(input, limits, cancellationToken).RunAsync();
    }

    private sealed class ReadOperation(Stream input, DxfReaderLimits limits, CancellationToken token)
    {
        private static readonly UTF8Encoding Utf8 = new(false, true);
        private readonly List<DxfRawPair> pairs = [];
        private readonly List<DxfLexicalDiagnostic> diagnostics = [];
        private readonly List<byte> line = [];
        private long consumed;
        private int completedLines;
        private int pairIndex;
        private string? codeText;
        private int? groupCode;
        private int codeLine;
        private bool hasBom;
        private bool pendingCr;
        private DxfLineEndings endings;

        public async Task<DxfLexicalReadResult> RunAsync()
        {
            try
            {
                token.ThrowIfCancellationRequested();
                if (!input.CanRead) throw new IOException("Stream is not readable.");
                if (input.CanSeek && input.Length - input.Position > limits.MaxInputBytes)
                    Fail(DxfLexicalStatus.ResourceLimitExceeded, DxfLexicalDiagnosticCode.InputTooLarge,
                        DxfDiagnosticCategory.Resource, "Input exceeds the configured byte budget.");

                var bytes = new BufferedInput(input, limits.MaxInputBytes, token);
                var prefix = new List<byte>(22);
                for (var i = 0; i < 22; i++)
                {
                    var b = await bytes.NextAsync();
                    if (b < 0) break;
                    CountByte();
                    prefix.Add((byte)b);
                }
                if (prefix.Count == 0)
                    Fail(DxfLexicalStatus.InvalidInput, DxfLexicalDiagnosticCode.EmptyInput,
                        DxfDiagnosticCategory.Input, "Empty input has no lexical pairs.");

                if (Starts(prefix, [0xFF, 0xFE]) || Starts(prefix, [0xFE, 0xFF]) ||
                    Starts(prefix, [0, 0, 0xFE, 0xFF]))
                    Fail(DxfLexicalStatus.UnsupportedEncoding, DxfLexicalDiagnosticCode.UnsupportedBom,
                        DxfDiagnosticCategory.Encoding, "UTF-16/UTF-32 byte-order marks are unsupported.", 1, 0);
                if (Starts(prefix, Encoding.ASCII.GetBytes("AutoCAD Binary DXF")) ||
                    IsDwgSignature(prefix))
                    Fail(DxfLexicalStatus.UnsupportedInput, DxfLexicalDiagnosticCode.UnsupportedInputFormat,
                        DxfDiagnosticCategory.Input, "Binary DXF and DWG are not Text DXF inputs.", 1, 0);

                hasBom = Starts(prefix, [0xEF, 0xBB, 0xBF]);
                foreach (var b in prefix.Skip(hasBom ? 3 : 0)) ProcessByte(b);
                while (true)
                {
                    var b = await bytes.NextAsync();
                    if (b < 0) break;
                    CountByte();
                    ProcessByte((byte)b);
                }
                token.ThrowIfCancellationRequested();
                if (pendingCr)
                    Fail(DxfLexicalStatus.InvalidInput, DxfLexicalDiagnosticCode.InvalidLineEnding,
                        DxfDiagnosticCategory.Lexical, "A carriage return must be followed by LF.", completedLines + 1, pairIndex);
                if (line.Count > 0) CompleteLine();
                if (codeText != null)
                    Fail(DxfLexicalStatus.InvalidInput, DxfLexicalDiagnosticCode.OddPhysicalLineCount,
                        DxfDiagnosticCategory.Lexical, "Code line has no following value line.", codeLine, pairIndex);
                if (completedLines == 0)
                    Fail(DxfLexicalStatus.InvalidInput, DxfLexicalDiagnosticCode.EmptyInput,
                        DxfDiagnosticCategory.Input, "Input has no lexical pairs.");
                token.ThrowIfCancellationRequested();
                var result = Result(diagnostics.Count == 0 ? DxfLexicalStatus.Succeeded : DxfLexicalStatus.InvalidInput);
                token.ThrowIfCancellationRequested();
                return result;
            }
            catch (ReadStop stop) { return Result(stop.Status); }
            catch (DecoderFallbackException)
            {
                return Terminal(DxfLexicalStatus.UnsupportedEncoding, DxfLexicalDiagnosticCode.InvalidUtf8,
                    DxfDiagnosticCategory.Encoding, "Physical line contains invalid UTF-8.");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return Terminal(DxfLexicalStatus.Canceled, DxfLexicalDiagnosticCode.Canceled,
                    DxfDiagnosticCategory.Cancellation, "Lexical read was canceled.");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
                                      ObjectDisposedException or NotSupportedException or OperationCanceledException)
            {
                return Terminal(DxfLexicalStatus.IoFailure, DxfLexicalDiagnosticCode.IoFailure,
                    DxfDiagnosticCategory.Io, "Input stream could not be read.");
            }
        }

        private void CountByte()
        {
            token.ThrowIfCancellationRequested();
            consumed++;
            if (consumed > limits.MaxInputBytes)
                Fail(DxfLexicalStatus.ResourceLimitExceeded, DxfLexicalDiagnosticCode.InputTooLarge,
                    DxfDiagnosticCategory.Resource, "Input exceeds the configured byte budget.", completedLines + 1, pairIndex);
        }

        private void ProcessByte(byte b)
        {
            token.ThrowIfCancellationRequested();
            if (pendingCr)
            {
                if (b != 10)
                    Fail(DxfLexicalStatus.InvalidInput, DxfLexicalDiagnosticCode.InvalidLineEnding,
                        DxfDiagnosticCategory.Lexical, "A carriage return must be followed by LF.", completedLines + 1, pairIndex);
                pendingCr = false;
                endings |= DxfLineEndings.CrLf;
                CompleteLine();
            }
            else if (b == 13) pendingCr = true;
            else if (b == 10)
            {
                endings |= DxfLineEndings.Lf;
                CompleteLine();
            }
            else
            {
                if (b == 0)
                    Fail(DxfLexicalStatus.UnsupportedEncoding, DxfLexicalDiagnosticCode.EmbeddedNul,
                        DxfDiagnosticCategory.Encoding, "Embedded NUL is not supported Text DXF.", completedLines + 1, pairIndex);
                if (line.Count >= limits.MaxPhysicalLineBytes)
                    Fail(DxfLexicalStatus.ResourceLimitExceeded, DxfLexicalDiagnosticCode.LineTooLong,
                        DxfDiagnosticCategory.Resource, "Physical line exceeds the configured source-byte budget.", completedLines + 1, pairIndex);
                line.Add(b);
            }
        }

        private void CompleteLine()
        {
            token.ThrowIfCancellationRequested();
            var text = Utf8.GetString(line.ToArray());
            // U+FEFF outside the optional byte-zero preamble is never silently removed.
            if (text.Contains('\uFEFF'))
                Fail(DxfLexicalStatus.UnsupportedEncoding, DxfLexicalDiagnosticCode.UnexpectedBom,
                    DxfDiagnosticCategory.Encoding, "UTF-8 BOM is permitted only at input byte zero.", completedLines + 1, pairIndex);
            line.Clear();
            completedLines++;
            if (codeText == null)
            {
                if (pairIndex >= limits.MaxPairCount)
                    Fail(DxfLexicalStatus.ResourceLimitExceeded, DxfLexicalDiagnosticCode.PairLimitExceeded,
                        DxfDiagnosticCategory.Resource, "Lexical pair count exceeds the configured budget.", completedLines, pairIndex);
                codeText = text;
                codeLine = completedLines;
                var trimmed = text.Trim(' ', '\t');
                // Code syntax is decimal digits with optional ASCII sign/margin, never locale coercion.
                var digits = trimmed.AsSpan();
                if (!digits.IsEmpty && (digits[0] is '+' or '-')) digits = digits[1..];
                var validDigits = !digits.IsEmpty;
                foreach (var c in digits) if (c is < '0' or > '9') validDigits = false;
                if (!validDigits || !int.TryParse(trimmed, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var code))
                {
                    AddError(DxfLexicalDiagnosticCode.InvalidGroupCode, "Group code is not a checked invariant decimal integer.");
                    groupCode = null;
                }
                else if (code is < 0 or > 1071)
                {
                    AddError(DxfLexicalDiagnosticCode.GroupCodeOutOfRange, "Group code is outside the supported lexical range 0..1071.");
                    groupCode = null;
                }
                else groupCode = code;
            }
            else
            {
                token.ThrowIfCancellationRequested();
                if (groupCode.HasValue)
                    pairs.Add(new(pairIndex, groupCode.Value, codeText, text, codeLine, completedLines));
                pairIndex++;
                codeText = null;
                groupCode = null;
            }
        }

        private void AddError(DxfLexicalDiagnosticCode code, string message)
        {
            if (diagnostics.Count >= limits.MaxStoredDiagnostics - 1)
                Fail(DxfLexicalStatus.ResourceLimitExceeded, DxfLexicalDiagnosticCode.DiagnosticLimitExceeded,
                    DxfDiagnosticCategory.Resource, "Diagnostic budget exhausted; read is incomplete.", completedLines, pairIndex);
            diagnostics.Add(new(code, DxfDiagnosticSeverity.Error, DxfDiagnosticCategory.Lexical,
                message, completedLines, pairIndex));
        }

        private void Fail(DxfLexicalStatus status, DxfLexicalDiagnosticCode code,
            DxfDiagnosticCategory category, string message, int? lineNumber = null, int? index = null)
        {
            diagnostics.Add(new(code, DxfDiagnosticSeverity.Error, category, message, lineNumber, index));
            throw new ReadStop(status);
        }

        private DxfLexicalReadResult Terminal(DxfLexicalStatus status, DxfLexicalDiagnosticCode code,
            DxfDiagnosticCategory category, string message)
        {
            diagnostics.Add(new(code, DxfDiagnosticSeverity.Error, category, message, completedLines + 1, pairIndex));
            return Result(status);
        }

        private DxfLexicalReadResult Result(DxfLexicalStatus status) =>
            new(status, pairs, diagnostics, consumed, completedLines, hasBom, endings);

        private static bool Starts(List<byte> source, byte[] prefix) =>
            source.Count >= prefix.Length && source.Take(prefix.Length).SequenceEqual(prefix);

        private static bool IsDwgSignature(List<byte> prefix) => prefix.Count >= 6 &&
            new[] { "AC1006", "AC1009", "AC1012", "AC1014", "AC1015", "AC1018", "AC1021", "AC1024", "AC1027", "AC1032" }
                .Contains(Encoding.ASCII.GetString(prefix.Take(6).ToArray()), StringComparer.Ordinal);

        private sealed class ReadStop(DxfLexicalStatus status) : Exception
        {
            public DxfLexicalStatus Status { get; } = status;
        }
    }

    private sealed class BufferedInput(Stream input, long maxBytes, CancellationToken token)
    {
        private readonly byte[] buffer = new byte[4096];
        private int offset;
        private int count;
        private long fetched;

        public async ValueTask<int> NextAsync()
        {
            token.ThrowIfCancellationRequested();
            if (offset >= count)
            {
                // At most one byte of lookahead beyond the configured input limit.
                var remaining = maxBytes - fetched;
                var request = remaining >= buffer.Length ? buffer.Length : (int)Math.Max(1, remaining + 1);
                count = await input.ReadAsync(buffer.AsMemory(0, request), token).ConfigureAwait(false);
                fetched += count;
                offset = 0;
                if (count == 0) return -1;
            }
            token.ThrowIfCancellationRequested();
            return buffer[offset++];
        }
    }
}
