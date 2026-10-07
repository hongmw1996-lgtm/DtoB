using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace DtoB.Core;

public sealed record SourceReference(Guid DrawingId, Guid RevisionId, string Handle, string CadEntityId)
{
    public void ValidateStructure()
    {
        Contract.Require(RevisionId != Guid.Empty && !string.IsNullOrWhiteSpace(CadEntityId), "Source revision and CAD identity required.");
        Contract.Require(CadEntityId == CadIdentity.Create(DrawingId, Handle, CadEntityId.Split('/').Skip(2)), "Source handle/CAD identity mismatch or noncanonical insertion path.");
    }
}
public enum ProvenanceOrigin { SourceDrawing, Manual }
public sealed record ManualConfirmation(string Actor, string Action, DateTimeOffset AtUtc)
{
    public void ValidateStructure() => Contract.Require(!string.IsNullOrWhiteSpace(Actor) && !string.IsNullOrWhiteSpace(Action) && AtUtc != default, "Manual confirmation requires actor/action/timestamp.");
}
public sealed record Provenance(ProvenanceOrigin Origin, string Description, SourceReference[] Sources, ManualConfirmation? Confirmation = null);
public enum DiagnosticSeverity { Info, Warning, Error }
public sealed record Diagnostic(string Code, DiagnosticSeverity Severity, string Message, string? SourceHandle = null);
public sealed record Prediction(double Confidence, string[] Evidence)
{
    public void ValidateStructure()
    {
        Contract.Require(double.IsFinite(Confidence) && Confidence is >= 0 and <= 1, "Prediction confidence must be in [0,1].");
        Contract.Require(Evidence != null && Evidence.Length > 0 && Evidence.All(e => !string.IsNullOrWhiteSpace(e)), "Prediction evidence is required.");
    }
}

// Recognition APIs return this wrapper; plain domain objects do not pretend to be predictions.
public sealed record SemanticPrediction<T>(T Value, Prediction Prediction)
{
    public void ValidateStructure()
    {
        Contract.Require(Value != null && Prediction != null, "Semantic prediction value and metadata required.");
        Prediction!.ValidateStructure();
    }
}

public static class Contract
{
    public static void Require([DoesNotReturnIf(false)] bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }

    public static void Finite(params double[] values) => Require(values.All(double.IsFinite), "Geometry must be finite.");
    public static void Positive(params double[] values) => Require(values.All(v => double.IsFinite(v) && v > 0), "Dimensions must be positive and finite.");
    public static void Version(int version) => Require(version == 1, $"Unsupported schema major version {version}.");
}

public static class CadIdentity
{
    public static string NormalizeHandle(string handle)
    {
        Contract.Require(!string.IsNullOrWhiteSpace(handle) && handle.All(Uri.IsHexDigit), "DWG handle must be hexadecimal.");
        return handle.ToUpperInvariant();
    }
    public static string Create(Guid drawingId, string handle, IEnumerable<string>? insertionPath = null)
    {
        Contract.Require(drawingId != Guid.Empty && !string.IsNullOrWhiteSpace(handle), "Drawing identity and handle are required.");
        // Original handle is retained separately; IDs use canonical hexadecimal components.
        return $"{drawingId:N}/{NormalizeHandle(handle)}" + string.Concat((insertionPath ?? []).Select(p => "/" + NormalizeHandle(p)));
    }
}

public static class IrJson
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);
    public static T Deserialize<T>(string json)
    {
        try
        {
            using var envelope = JsonDocument.Parse(json);
            if (envelope.RootElement.ValueKind != JsonValueKind.Object) throw new InvalidDataException("IR document must be an object.");
            if (!envelope.RootElement.TryGetProperty("schemaVersion", out var version) || !version.TryGetInt32(out int major))
                throw new InvalidDataException("Integer schemaVersion required.");
            Contract.Version(major);
            return JsonSerializer.Deserialize<T>(json, Options) ?? throw new InvalidDataException("Null IR document.");
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        { throw new InvalidDataException("Invalid IR v1 JSON: unknown/missing fields or polymorphic discriminator order. " + ex.Message, ex); }
    }
}
