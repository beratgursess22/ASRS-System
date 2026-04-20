using System.Globalization;

namespace ASRS.API.Services;

public static class RfidUidNormalizer
{
    // Ornek cikti: "F3 21 6E 2E"
    public static string Normalize(string? rawUid)
    {
        if (string.IsNullOrWhiteSpace(rawUid))
            return string.Empty;

        var tokens = rawUid
            .Trim()
            .ToUpperInvariant()
            .Replace("-", " ")
            .Replace(":", " ")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var bytes = new List<string>(tokens.Length);
        foreach (var t in tokens)
        {
            var token = t.StartsWith("0X", StringComparison.Ordinal) ? t[2..] : t;
            if (token.Length is 0 or > 2)
                throw new FormatException($"Invalid UID token: {t}");

            var value = byte.Parse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            bytes.Add(value.ToString("X2", CultureInfo.InvariantCulture));
        }

        // MFRC522 anticollision often returns 5 bytes for 4-byte cards:
        // first 4 bytes are UID and 5th byte is BCC (XOR checksum).
        // If this pattern is detected, drop BCC so DB mappings with 4-byte UID match.
        if (bytes.Count == 5 && TryParseHexByte(bytes[4], out var bcc))
        {
            var b0 = byte.Parse(bytes[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var b1 = byte.Parse(bytes[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var b2 = byte.Parse(bytes[2], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var b3 = byte.Parse(bytes[3], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var calculatedBcc = (byte)(b0 ^ b1 ^ b2 ^ b3);

            if (calculatedBcc == bcc)
                bytes.RemoveAt(4);
        }

        return string.Join(" ", bytes);
    }

    private static bool TryParseHexByte(string value, out byte result)
    {
        return byte.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
    }
}
