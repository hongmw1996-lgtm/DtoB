using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace DtoB.Cad.V2;

/// <summary>CAD-owned validation. Never routes CAD2 through the legacy major-one gate.</summary>
public static class CadContract
{
    public const int SchemaMajor = 2;
    public static void Require(bool condition, string message)
    { if (!condition) throw new InvalidDataException(message); }
    public static int Version(int value)
    { Require(value == SchemaMajor, $"Unsupported CAD schema major {value}."); return value; }
    public static string Text(string value)
    { Require(!string.IsNullOrWhiteSpace(value), "Nonempty contract text required."); return value; }
    public static int Index(int value)
    { Require(value >= 0, "Nonnegative index required."); return value; }
    public static T EnumValue<T>(T value) where T : struct, Enum
    { Require(Enum.IsDefined(value), $"Undefined {typeof(T).Name}."); return value; }
    public static T Required<T>(T value) where T : class
    { Require(value is not null, $"{typeof(T).Name} required."); return value!; }
    public static double Finite(double value)
    { Require(double.IsFinite(value), "Finite number required."); return value == 0 ? 0 : value; }
    public static double Positive(double value)
    { Finite(value); Require(value > 0, "Positive number required."); return value; }
    public static ReadOnlyCollection<T> Copy<T>(IReadOnlyList<T> values)
    {
        Required(values); var copy = values.ToArray();
        Require(copy.All(v => v is not null), "Null collection element.");
        return Array.AsReadOnly(copy);
    }
    public static string Hash(string value)
    {
        Require(value is not null && value.Length == 64 && value.All(Uri.IsHexDigit), "SHA-256 hex required.");
        return value!.ToUpperInvariant();
    }
    public static string CanonicalHandle(string value)
    {
        Require(!string.IsNullOrEmpty(value) && value.All(Uri.IsHexDigit), "Hex source handle required.");
        var canonical = value.ToUpperInvariant().TrimStart('0');
        return canonical.Length == 0 ? "0" : canonical;
    }
}

public enum CadLengthUnit { Millimeters }
public enum CadAngleUnit { Radians }
public enum CadCoordinateSystem { RightHandedXyz }
public enum CadUpAxis { PositiveZ }
public enum CadPositiveRotation { CounterClockwise }
public enum CadNormalizationState { Normalized, PartiallyNormalized, UnsupportedNormalization, InvalidNormalization, NotApplicable }
public enum CadResolutionState { Known, Inherited, Defaulted, Unsupported, Absent }
public enum CadPropertyMode { Explicit, ByLayer, ByBlock, Absent }
public enum CadSeverity { Information, Warning, Error }
public enum CadStage { Contract, Units, Coordinates, Placement, Occurrences, Styles, Geometry, Assembly }
public enum CadEntityKind { Primitive, Annotation, Insert, Unsupported }
public enum CadAnnotationKind { Text, MText, Dimension, Attribute, AttributeDefinition }
public enum CadUnitDecisionKind { Unresolved, Declared, Explicit }

/// <summary>Declares the target convention; does not perform any conversion.</summary>
public sealed class CadConventions(CadLengthUnit length, CadAngleUnit angle,
    CadCoordinateSystem coordinates, CadUpAxis up, CadPositiveRotation rotation)
{
    public CadLengthUnit Length { get; } = CadContract.EnumValue(length);
    public CadAngleUnit Angle { get; } = CadContract.EnumValue(angle);
    public CadCoordinateSystem Coordinates { get; } = CadContract.EnumValue(coordinates);
    public CadUpAxis Up { get; } = CadContract.EnumValue(up);
    public CadPositiveRotation Rotation { get; } = CadContract.EnumValue(rotation);
    public static CadConventions Canonical => new(CadLengthUnit.Millimeters, CadAngleUnit.Radians,
        CadCoordinateSystem.RightHandedXyz, CadUpAxis.PositiveZ, CadPositiveRotation.CounterClockwise);
}

/// <summary>Stores a future unit decision without calculating a unit scale in 04A.</summary>
public sealed class CadUnitDecision
{
    public CadUnitDecisionKind Kind { get; }
    public int? SourceUnitCode { get; }
    public double? SourceToMillimeters { get; }
    public string? DecisionId { get; }
    public string? Reason { get; }
    public CadSourceFactRef? Evidence { get; }
    public CadUnitDecision(CadUnitDecisionKind kind, int? sourceUnitCode, double? sourceToMillimeters,
        string? decisionId, string? reason, CadSourceFactRef? evidence)
    {
        Kind = CadContract.EnumValue(kind); SourceUnitCode = sourceUnitCode;
        SourceToMillimeters = sourceToMillimeters is { } f ? CadContract.Positive(f) : null;
        DecisionId = decisionId; Reason = reason; Evidence = evidence;
        CadContract.Require(kind == CadUnitDecisionKind.Unresolved ? sourceToMillimeters is null : sourceToMillimeters is not null,
            "Unit decision and scale presence disagree.");
        if (kind == CadUnitDecisionKind.Explicit) { CadContract.Text(decisionId!); CadContract.Text(reason!); }
    }
}
