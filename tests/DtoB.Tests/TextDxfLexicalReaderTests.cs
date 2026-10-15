using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DtoB.Cad.Dxf;
using DtoB.Cad.Raw;
using Xunit;

namespace DtoB.Tests;

public class TextDxfLexicalReaderTests
{
    private static byte[] Bytes(string text) => new UTF8Encoding(false, true).GetBytes(text);
    private static async Task<DxfLexicalReadResult> Read(string text, DxfReaderLimits? limits = null) => await Read(Bytes(text), limits);
    private static async Task<DxfLexicalReadResult> Read(byte[] bytes, DxfReaderLimits? limits = null)
    {
        using var input = new MemoryStream(bytes, writable: false);
        return await new TextDxfLexicalReader(limits).ReadAsync(input);
    }
    private static string Root()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "DtoB.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidDataException("Repository root not found.");
    }

    [Fact]
    public async Task ActualControlledFixturePreservesEverySourceLineAndHash()
    {
        var path = Path.Combine(Root(), "datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf");
        var before = await File.ReadAllBytesAsync(path);
        const string expected = "2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC";
        Assert.Equal(expected, Convert.ToHexString(SHA256.HashData(before)));
        Assert.Equal(96_621, before.Length);
        Assert.All(before, b => Assert.InRange(b, (byte)0, (byte)127));
        // Independent BCL physical-line oracle, not the byte/line parser under test.
        var reference = File.ReadAllLines(path, new UTF8Encoding(false, true));
        Assert.Equal(17_474, reference.Length);
        Assert.Equal(8_737, reference.Length / 2);
        using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var result = await new TextDxfLexicalReader().ReadAsync(input);
        Assert.Equal(DxfLexicalStatus.Succeeded, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(DxfVersionAcceptance.NotValidated, result.VersionAcceptance);
        Assert.Equal(8_737, result.Pairs.Count);
        Assert.Equal(17_474, result.PhysicalLineCount);
        Assert.Equal(96_621, result.SourceBytesConsumed);
        Assert.False(result.HasUtf8Bom);
        Assert.Equal(DxfLineEndings.CrLf, result.LineEndings);
        foreach (var pair in result.Pairs)
        {
            var i = pair.PairIndex;
            Assert.Equal(i * 2 + 1, pair.CodeLineNumber);
            Assert.Equal(i * 2 + 2, pair.ValueLineNumber);
            Assert.Equal(reference[i * 2], pair.RawCodeText);
            Assert.Equal(reference[i * 2 + 1], pair.RawTextValue);
        }
        Assert.True(input.CanRead); // Caller retains ownership and its immutable lease.
        Assert.Equal(expected, Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(path))));
        var evidence = Path.Combine(Root(), "artifacts/phase03/03a-lexical-20261014");
        Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "controlled-pairs.json"), JsonSerializer.Serialize(result));
        await File.WriteAllTextAsync(Path.Combine(evidence, "controlled-qa.json"), JsonSerializer.Serialize(new
        {
            sha256 = expected, bytes = before.Length, physicalLines = reference.Length,
            independentPairCount = reference.Length / 2, lexicalPairCount = result.Pairs.Count,
            versionAcceptance = result.VersionAcceptance.ToString(), rawPairsSha256 = Convert.ToHexString(SHA256.HashData(Bytes(JsonSerializer.Serialize(result))))
        }, new JsonSerializerOptions { WriteIndented = true }));
    }

    [Theory]
    [InlineData("1234.500")]
    [InlineData("-123.500")]
    [InlineData("1e+20")]
    [InlineData("NaN")]
    [InlineData("")]
    [InlineData("  value  ")]
    [InlineData("aB01")]
    [InlineData("AB01")]
    [InlineData(@"\U+AC00 \M+1FFFF")]
    public async Task ValuesRemainUninterpretedAndSpellingIsExact(string value)
    {
        var result = await Read(" \t10\t \n" + value + "\n");
        var pair = Assert.Single(result.Pairs);
        Assert.Equal(DxfLexicalStatus.Succeeded, result.Status);
        Assert.Equal(10, pair.GroupCode);
        Assert.Equal(" \t10\t ", pair.RawCodeText);
        Assert.Equal(value, pair.RawTextValue);
        Assert.Equal(0, pair.PairIndex);
        Assert.Equal(1, pair.CodeLineNumber);
        Assert.Equal(2, pair.ValueLineNumber);
    }

    [Theory]
    [InlineData("\n", true)]
    [InlineData("\n", false)]
    [InlineData("\r\n", true)]
    [InlineData("\r\n", false)]
    public async Task LineTerminatorsDoNotChangePairs(string ending, bool finalNewline)
    {
        var text = string.Join(ending, new[] { "0", "SECTION", "9", "$ACADVER", "1", "AC1032" }) + (finalNewline ? ending : "");
        var result = await Read(text);
        var expected = await Read("0\nSECTION\n9\n$ACADVER\n1\nAC1032\n");
        Assert.Equal(DxfLexicalStatus.Succeeded, result.Status);
        Assert.Equal(6, result.PhysicalLineCount);
        Assert.Equal(expected.Pairs, result.Pairs);
    }

    [Theory]
    [InlineData("한글 도면", false)]
    [InlineData("한글 도면", true)]
    [InlineData("café Ångström 😀", false)]
    [InlineData("café Ångström 😀", true)]
    public async Task StrictUtf8AcceptsMultilingualTextWithOptionalByteZeroBom(string value, bool bom)
    {
        var payload = Bytes("1\n" + value + "\n");
        var result = await Read(bom ? new byte[] { 239,187,191 }.Concat(payload).ToArray() : payload);
        Assert.Equal(DxfLexicalStatus.Succeeded, result.Status);
        Assert.Equal(value, Assert.Single(result.Pairs).RawTextValue);
        Assert.Equal(bom, result.HasUtf8Bom);
        Assert.Equal(2, result.PhysicalLineCount);
    }

    [Theory]
    [InlineData(new byte[] { 0xFF, 0xFE, 48, 0, 10, 0 })]
    [InlineData(new byte[] { 0xFE, 0xFF, 0, 48, 0, 10 })]
    [InlineData(new byte[] { 0xFF, 0xFE, 0, 0, 48, 0, 0, 0 })]
    [InlineData(new byte[] { 0, 0, 0xFE, 0xFF, 0, 0, 0, 48 })]
    public async Task Utf16AndUtf32BomsAreExplicitlyUnsupported(byte[] bytes)
    {
        var result = await Read(bytes);
        Assert.Equal(DxfLexicalStatus.UnsupportedEncoding, result.Status);
        Assert.Equal(DxfLexicalDiagnosticCode.UnsupportedBom, Assert.Single(result.Diagnostics).Code);
    }

    [Theory]
    [InlineData(new byte[] { 49,10,0xC0,0xAF,10 })]
    [InlineData(new byte[] { 49,10,0xED,0xA0,0x80,10 })]
    [InlineData(new byte[] { 49,10,0xF0,0x9F,10 })]
    [InlineData(new byte[] { 49,10,0xFF,10 })]
    public async Task InvalidUtf8NeverUsesReplacementFallback(byte[] bytes)
    {
        var result = await Read(bytes);
        Assert.Equal(DxfLexicalStatus.UnsupportedEncoding, result.Status);
        Assert.Equal(DxfLexicalDiagnosticCode.InvalidUtf8, Assert.Single(result.Diagnostics).Code);
        Assert.Empty(result.Pairs);
        Assert.Equal(2, result.Diagnostics[0].PhysicalLineNumber);
    }

    [Theory]
    [InlineData("1\n\0\n", DxfLexicalDiagnosticCode.EmbeddedNul)]
    [InlineData("1\n\uFEFFtext\n", DxfLexicalDiagnosticCode.UnexpectedBom)]
    [InlineData("\uFEFF\uFEFF1\ntext\n", DxfLexicalDiagnosticCode.UnexpectedBom)]
    public async Task NonstandardTextOrMisplacedBomIsRejected(string text, DxfLexicalDiagnosticCode code)
    {
        var result = await Read(text);
        Assert.Equal(DxfLexicalStatus.UnsupportedEncoding, result.Status);
        Assert.Equal(code, Assert.Single(result.Diagnostics).Code);
    }

    [Theory]
    [InlineData("AC1024arbitrarydwgbytes")]
    [InlineData("AutoCAD Binary DXF\r\n\u001a\0rest")]
    public async Task DwgAndBinaryDxfAreNotDecoded(string text)
    {
        var result = await Read(text);
        Assert.Equal(DxfLexicalStatus.UnsupportedInput, result.Status);
        Assert.Equal(DxfLexicalDiagnosticCode.UnsupportedInputFormat, Assert.Single(result.Diagnostics).Code);
    }

    [Theory]
    [InlineData("", DxfLexicalDiagnosticCode.EmptyInput)]
    [InlineData("0", DxfLexicalDiagnosticCode.OddPhysicalLineCount)]
    [InlineData("0\n", DxfLexicalDiagnosticCode.OddPhysicalLineCount)]
    [InlineData("0\nvalue\n1\n", DxfLexicalDiagnosticCode.OddPhysicalLineCount)]
    [InlineData("x\nvalue\n", DxfLexicalDiagnosticCode.InvalidGroupCode)]
    [InlineData("2147483648\nvalue\n", DxfLexicalDiagnosticCode.InvalidGroupCode)]
    [InlineData("1.0\nvalue\n", DxfLexicalDiagnosticCode.InvalidGroupCode)]
    [InlineData("١\nvalue\n", DxfLexicalDiagnosticCode.InvalidGroupCode)]
    [InlineData("-1\nvalue\n", DxfLexicalDiagnosticCode.GroupCodeOutOfRange)]
    [InlineData("1072\nvalue\n", DxfLexicalDiagnosticCode.GroupCodeOutOfRange)]
    [InlineData("0\rvalue\r", DxfLexicalDiagnosticCode.InvalidLineEnding)]
    public async Task MalformedLexicalInputHasExplicitLocatedFailure(string text, DxfLexicalDiagnosticCode code)
    {
        var result = await Read(text);
        Assert.Equal(DxfLexicalStatus.InvalidInput, result.Status);
        Assert.Contains(result.Diagnostics, d => d.Code == code);
        if (text.Length > 0) Assert.All(result.Diagnostics, d => Assert.NotNull(d.PhysicalLineNumber));
    }

    [Fact]
    public async Task LineLimitsCountUtf8SourceBytesNotCharsOrTerminators()
    {
        var limits = new DxfReaderLimits { MaxPhysicalLineBytes = 6 };
        Assert.Equal(DxfLexicalStatus.Succeeded, (await Read("1\r\n한글\r\n", limits)).Status);
        var tooLong = await Read("1\n한글x\n", limits);
        Assert.Equal(DxfLexicalStatus.ResourceLimitExceeded, tooLong.Status);
        Assert.Equal(DxfLexicalDiagnosticCode.LineTooLong, Assert.Single(tooLong.Diagnostics).Code);
        Assert.Equal(2, tooLong.Diagnostics[0].PhysicalLineNumber);
        var bom = await Read(new byte[] {239,187,191}.Concat(Bytes("1\nabcdef\n")).ToArray(), limits);
        Assert.Equal(DxfLexicalStatus.Succeeded, bom.Status);
    }

    [Fact]
    public async Task PairLimitCountsInvalidAttemptsAndNeverPublishesTruncatedSuccess()
    {
        var limits = new DxfReaderLimits { MaxPairCount = 1 };
        Assert.Equal(DxfLexicalStatus.Succeeded, (await Read("0\nx\n", limits)).Status);
        var over = await Read("0\nx\n1\ny\n", limits);
        Assert.Equal(DxfLexicalStatus.ResourceLimitExceeded, over.Status);
        Assert.Single(over.Pairs);
        Assert.Equal(DxfLexicalDiagnosticCode.PairLimitExceeded, Assert.Single(over.Diagnostics).Code);
        var invalid = await Read("bad\nx\n1\ny\n", limits);
        Assert.Equal(DxfLexicalStatus.ResourceLimitExceeded, invalid.Status);
        Assert.Equal(DxfLexicalDiagnosticCode.PairLimitExceeded, invalid.Diagnostics.Last().Code);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InputBudgetEnforcedWithAndWithoutLengthMetadata(bool seekable)
    {
        var limits = new DxfReaderLimits { MaxInputBytes = 4 };
        using var at = new ChunkStream(Bytes("0\nx\n"), seekable: seekable);
        Assert.Equal(DxfLexicalStatus.Succeeded, (await new TextDxfLexicalReader(limits).ReadAsync(at)).Status);
        using var over = new ChunkStream(Bytes("0\nx\n1\ny\n"), seekable: seekable);
        var result = await new TextDxfLexicalReader(limits).ReadAsync(over);
        Assert.Equal(DxfLexicalStatus.ResourceLimitExceeded, result.Status);
        Assert.Equal(DxfLexicalDiagnosticCode.InputTooLarge, Assert.Single(result.Diagnostics).Code);
        Assert.True(over.ReadBytes <= 5);
        Assert.True(over.MaxRequest <= 5);
    }

    [Fact]
    public async Task DiagnosticBudgetHasReservedTerminalSlotAndDeterministicRefusal()
    {
        var limits = new DxfReaderLimits { MaxStoredDiagnostics = 3 };
        var result = await Read("x\na\ny\nb\nz\nc\n", limits);
        Assert.Equal(DxfLexicalStatus.ResourceLimitExceeded, result.Status);
        Assert.Equal(3, result.Diagnostics.Count);
        Assert.Equal(new[] {DxfLexicalDiagnosticCode.InvalidGroupCode,DxfLexicalDiagnosticCode.InvalidGroupCode,DxfLexicalDiagnosticCode.DiagnosticLimitExceeded}, result.Diagnostics.Select(d=>d.Code));
        Assert.Equal(2, result.Diagnostics.Last().PairIndex);
        Assert.Equal(JsonSerializer.Serialize(result), JsonSerializer.Serialize(await Read("x\na\ny\nb\nz\nc\n", limits)));
    }

    [Fact]
    public void DefaultsMatchAcceptedAdrAndInvalidConfigurationIsRejected()
    {
        var defaults = new DxfReaderLimits();
        Assert.Equal(65_536, defaults.MaxPhysicalLineBytes);
        Assert.Equal(16, defaults.MaxControlGroupDepth);
        Assert.Equal(5_000, defaults.MaxStoredDiagnostics);
        Assert.Equal(250_000, defaults.MaxPairCount);
        Assert.Equal(16_777_216, defaults.MaxInputBytes);
        foreach(var invalid in new[]{defaults with {MaxInputBytes=0}, defaults with {MaxPairCount=0}, defaults with {MaxPhysicalLineBytes=0}, defaults with {MaxStoredDiagnostics=1}, defaults with {MaxControlGroupDepth=0}})
            Assert.Throws<ArgumentOutOfRangeException>(()=>new TextDxfLexicalReader(invalid));
    }

    [Fact]
    public async Task CallerStreamIsReadOnlyAndNeverDisposedOrWritten()
    {
        using var stream = new ChunkStream(Bytes("0\nx\n"));
        var reader = new TextDxfLexicalReader();
        Assert.Equal(DxfLexicalStatus.Succeeded, (await reader.ReadAsync(stream)).Status);
        Assert.False(stream.Disposed);
        Assert.Equal(0, stream.WriteAttempts);
    }

    [Fact]
    public async Task IoFailureHasStableDiagnosticsAndKeepsCallerOwnership()
    {
        using var stream = new ChunkStream(Bytes("0\nx\n"), throwOnRead: 2);
        var result = await new TextDxfLexicalReader().ReadAsync(stream);
        Assert.Equal(DxfLexicalStatus.IoFailure, result.Status);
        Assert.Equal(DxfLexicalDiagnosticCode.IoFailure, Assert.Single(result.Diagnostics).Code);
        Assert.False(stream.Disposed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CancellationNeverPublishesSuccessOrDisposesCaller(bool preCanceled)
    {
        using var cts = new CancellationTokenSource();
        using var stream = new ChunkStream(Bytes(string.Concat(Enumerable.Repeat("0\nx\n", 100))), afterRead: n=> {if(!preCanceled && n==30)cts.Cancel();});
        if(preCanceled)cts.Cancel();
        var result = await new TextDxfLexicalReader().ReadAsync(stream, cts.Token);
        Assert.Equal(DxfLexicalStatus.Canceled, result.Status);
        Assert.Equal(DxfLexicalDiagnosticCode.Canceled, Assert.Single(result.Diagnostics).Code);
        Assert.False(stream.Disposed);
        if(preCanceled)Assert.Equal(0, stream.ReadBytes);
    }

    [Fact]
    public async Task CancellationAtSourceEofStillRejectsSuccessPublication()
    {
        using var cts = new CancellationTokenSource();
        using var stream = new ChunkStream(Bytes("0\nx\n"), afterRead: n=> {if(n==5)cts.Cancel();});
        var result = await new TextDxfLexicalReader().ReadAsync(stream,cts.Token);
        Assert.Equal(DxfLexicalStatus.Canceled,result.Status);
        Assert.Equal(DxfLexicalDiagnosticCode.Canceled,Assert.Single(result.Diagnostics).Code);
        Assert.False(stream.Disposed);
    }

    [Fact]
    public async Task RepetitionShortReadsAndBufferBoundariesAreDeterministic()
    {
        var bytes = Bytes("1\n"+new string('a',4092)+"한글\n0\nx\n");
        var first = await Read(bytes);
        using var oneByte = new ChunkStream(bytes);
        var fragmented = await new TextDxfLexicalReader().ReadAsync(oneByte);
        Assert.Equal(DxfLexicalStatus.Succeeded, first.Status);
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(fragmented));
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(await Read(bytes)));
        var pairs = Assert.IsAssignableFrom<IList<DxfRawPair>>(first.Pairs);
        Assert.Throws<NotSupportedException>(()=>pairs.Clear());
    }

    [Fact]
    public async Task VersionStructureControlsAndValuesAreNotInterpretedIn03A()
    {
        var result = await Read("9\n$ACADVER\n1\nAC1009\n102\n{one\n1002\n{two\n10\nNaN\n0\nNOT_A_SECTION\n", new DxfReaderLimits {MaxControlGroupDepth=1});
        Assert.Equal(DxfLexicalStatus.Succeeded, result.Status);
        Assert.Equal(DxfVersionAcceptance.NotValidated, result.VersionAcceptance);
        Assert.Equal("AC1009",result.Pairs[1].RawTextValue);
        Assert.Equal("NaN",result.Pairs[4].RawTextValue);
        Assert.Equal(DxfVersionAcceptance.NotValidated,(await Read("0\nEOF\n")).VersionAcceptance);
    }

    private sealed class ChunkStream(byte[] bytes, bool seekable=false, int throwOnRead=0, Action<int>? afterRead=null) : Stream
    {
        private int position;
        private int reads;
        public bool Disposed {get;private set;}
        public int WriteAttempts {get;private set;}
        public int ReadBytes => position;
        public int MaxRequest {get;private set;}
        public override bool CanRead=>!Disposed;
        public override bool CanSeek=>seekable;
        public override bool CanWrite=>false;
        public override long Length=>seekable?bytes.Length:throw new NotSupportedException();
        public override long Position {get=>position;set=>throw new NotSupportedException();}
        public override ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken token=default)
        {
            token.ThrowIfCancellationRequested();reads++;MaxRequest=Math.Max(MaxRequest,buffer.Length);
            if(reads==throwOnRead)throw new IOException("Machine-specific message is excluded from deterministic result.");
            if(position==bytes.Length){afterRead?.Invoke(reads);return ValueTask.FromResult(0);}
            buffer.Span[0]=bytes[position++];afterRead?.Invoke(reads);return ValueTask.FromResult(1);
        }
        protected override void Dispose(bool disposing){Disposed=true;base.Dispose(disposing);}
        public override void Write(byte[] buffer,int offset,int count){WriteAttempts++;throw new NotSupportedException();}
        public override int Read(byte[] buffer,int offset,int count)=>throw new NotSupportedException();
        public override void Flush()=>throw new NotSupportedException();
        public override long Seek(long offset,SeekOrigin origin)=>throw new NotSupportedException();
        public override void SetLength(long value)=>throw new NotSupportedException();
    }
}
