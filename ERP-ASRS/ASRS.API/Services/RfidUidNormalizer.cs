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

        return string.Join(" ", bytes);
    }
}
