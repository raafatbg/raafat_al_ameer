using System.Globalization;

namespace al_ameer.Services;

public static class InputParser
{
    public static bool TryMoney(string? text, out decimal value)
    {
        string normalized = (text ?? "").Trim().Replace(" ", "");
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
            || decimal.TryParse(normalized.Replace(",", ""), NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    public static bool TryNonNegativeMoney(string? text, out decimal value) => TryMoney(text, out value) && value >= 0;
    public static bool TryNonNegativeInt(string? text, out int value) => int.TryParse(text, out value) && value >= 0;
    public static bool TryPositiveInt(string? text, out int value) => int.TryParse(text, out value) && value > 0;
}
