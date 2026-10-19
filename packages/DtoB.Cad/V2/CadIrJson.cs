using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace DtoB.Cad.V2;

/// <summary>Typed, CAD2-owned codec. No legacy IrJson or Contract.Version call path.</summary>
public static class CadIrJson
{
    private static readonly JsonSerializerOptions Options = CreateOptions();
    private static JsonSerializerOptions CreateOptions()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(info =>
        {
            if (info.Kind != JsonTypeInfoKind.Object) return;
            var ordered = info.Properties.OrderBy(p => p.Name == "schemaVersion" ? 0 : 1).ThenBy(p => p.Name, StringComparer.Ordinal).ToArray();
            info.Properties.Clear(); foreach (var p in ordered) info.Properties.Add(p);
        });
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, TypeInfoResolver = resolver,
            WriteIndented = true, MaxDepth = 64
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        options.Converters.Add(new CanonicalDoubleConverter());
        return options;
    }
    public static string Serialize(CadDocument document)
    {
        CadContract.Required(document).ValidateStructure();
        try { return JsonSerializer.Serialize(document, Options).Replace("\r\n", "\n", StringComparison.Ordinal) + "\n"; }
        catch (Exception e) when (e is JsonException or ArgumentException or InvalidOperationException or NotSupportedException)
        { throw new InvalidDataException("Invalid CAD2 document.", e); }
    }
    public static byte[] SerializeUtf8(CadDocument document) => new UTF8Encoding(false, true).GetBytes(Serialize(document));
    public static CadDocument Deserialize(string json)
    {
        try
        {
            using var envelope = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 64 });
            var root = envelope.RootElement;
            CadContract.Require(root.ValueKind == JsonValueKind.Object && root.TryGetProperty("schemaVersion", out _), "Integer CAD schemaVersion required.");
            var version = root.GetProperty("schemaVersion");
            CadContract.Require(version.ValueKind == JsonValueKind.Number && version.TryGetInt32(out _), "Integer CAD schemaVersion required.");
            CadContract.Version(version.GetInt32());
            ValidateShape(root, typeof(CadDocument));
            var value = JsonSerializer.Deserialize<CadDocument>(json, Options) ?? throw new InvalidDataException("Null CAD document.");
            value.ValidateStructure(); return value;
        }
        catch (Exception e) when (e is JsonException or ArgumentException or InvalidOperationException or NotSupportedException)
        { throw new InvalidDataException("Invalid CAD2 JSON.", e); }
    }

    // .NET8 does not require all constructor parameters. Check every declared member,
    // including explicit null for unavailable nullable facts, before binding immutable DTOs.
    private static void ValidateShape(JsonElement element, Type type)
    {
        if (element.ValueKind == JsonValueKind.Null) return; // constructors reject null for required state
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type.IsEnum)
        {
            CadContract.Require(element.ValueKind == JsonValueKind.String && Enum.GetNames(type)
                .Select(JsonNamingPolicy.CamelCase.ConvertName).Contains(element.GetString(), StringComparer.Ordinal), "Named CAD enum required.");
            return;
        }
        if (type == typeof(string) || type == typeof(Guid) || type.IsPrimitive || type == typeof(decimal)) return;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
        {
            CadContract.Require(element.ValueKind == JsonValueKind.Array, "CAD collection must be an array.");
            foreach (var item in element.EnumerateArray()) ValidateShape(item, type.GenericTypeArguments[0]);
            return;
        }
        CadContract.Require(element.ValueKind == JsonValueKind.Object, "CAD object required.");
        var discriminator = type == typeof(CadGeometry);
        if (discriminator)
        {
            CadContract.Require(element.TryGetProperty("$kind", out var kind) && kind.ValueKind == JsonValueKind.String, "Geometry discriminator required.");
            var derived = type.GetCustomAttributes<JsonDerivedTypeAttribute>().SingleOrDefault(a => Equals(a.TypeDiscriminator, kind.GetString()));
            CadContract.Require(derived is not null, "Unsupported geometry discriminator."); type = derived!.DerivedType;
        }
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() is null)
            .ToDictionary(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name), p => p.PropertyType, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var p in element.EnumerateObject())
        {
            CadContract.Require(seen.Add(p.Name), "Duplicate JSON member.");
            if (discriminator && p.Name == "$kind") continue;
            CadContract.Require(properties.TryGetValue(p.Name, out _), $"Unknown CAD member {p.Name}.");
            ValidateShape(p.Value, properties[p.Name]);
        }
        CadContract.Require(properties.Keys.All(seen.Contains), "Missing CAD member; nullable facts require explicit null.");
    }
    private sealed class CanonicalDoubleConverter : JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => CadContract.Finite(reader.GetDouble());
        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options) => writer.WriteNumberValue(CadContract.Finite(value));
    }
}
