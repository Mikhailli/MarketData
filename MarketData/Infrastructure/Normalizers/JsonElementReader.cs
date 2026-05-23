using System.Globalization;
using System.Text.Json;

namespace MarketData.Infrastructure.Normalizers;

internal static class JsonElementReader
{
    public static bool TryGetString(
        JsonElement element,
        string propertyName,
        out string value)
    {
        value = string.Empty;

        if (!element.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        return TryReadString(property, out value);
    }

    public static bool TryGetDecimal(
        JsonElement element,
        string propertyName,
        out decimal value)
    {
        value = default;

        if (!element.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        return TryReadDecimal(property, out value);
    }

    public static bool TryGetTimestampUtc(
        JsonElement element,
        string propertyName,
        out DateTime timestampUtc)
    {
        timestampUtc = default;

        if (!element.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        return TryReadTimestampUtc(property, out timestampUtc);
    }

    public static bool TryReadString(JsonElement element, out string value)
    {
        value = string.Empty;

        if (element.ValueKind == JsonValueKind.String)
        {
            value = element.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value);
        }

        if (element.ValueKind == JsonValueKind.Number)
        {
            value = element.GetRawText();
            return !string.IsNullOrWhiteSpace(value);
        }

        return false;
    }

    public static bool TryReadDecimal(JsonElement element, out decimal value)
    {
        value = default;

        if (element.ValueKind == JsonValueKind.Number)
        {
            return element.TryGetDecimal(out value);
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        return decimal.TryParse(
            element.GetString(),
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out value);
    }

    public static bool TryReadTimestampUtc(JsonElement element, out DateTime timestampUtc)
    {
        timestampUtc = default;

        if (element.ValueKind == JsonValueKind.Number
            && element.TryGetInt64(out var unixMilliseconds))
        {
            timestampUtc = DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds).UtcDateTime;
            return true;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var rawValue = element.GetString();

        if (long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out unixMilliseconds))
        {
            timestampUtc = DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds).UtcDateTime;
            return true;
        }

        if (DateTimeOffset.TryParse(
                rawValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var timestamp))
        {
            timestampUtc = timestamp.UtcDateTime;
            return true;
        }

        return false;
    }
}
