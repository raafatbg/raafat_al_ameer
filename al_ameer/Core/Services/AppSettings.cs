using System.Globalization;
using System.IO;
using System.Text.Json;

namespace al_ameer.Services;

public sealed class AppSettings
{
    private static readonly string PathName = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AlAmeer", "settings.json");

    public string Language { get; set; } = "en";
    public decimal LbpPerUsd { get; set; } = 90000m;

    public static AppSettings Current { get; private set; } = Load();

    private static AppSettings Load()
    {
        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(PathName));
            if (settings is not null && settings.LbpPerUsd > 0 && settings.LbpPerUsd <= 10000000m &&
                decimal.Round(settings.LbpPerUsd, 4) == settings.LbpPerUsd && settings.Language is "en" or "ar")
                return settings;
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        catch (JsonException) { }
        return new AppSettings();
    }

    public void Save()
    {
        if (Language is not ("en" or "ar") || LbpPerUsd <= 0 || LbpPerUsd > 10000000m ||
            decimal.Round(LbpPerUsd, 4) != LbpPerUsd)
            throw new ArgumentException("Choose a language and an exchange rate up to 10,000,000 with at most four decimals.");
        Directory.CreateDirectory(Path.GetDirectoryName(PathName)!);
        string temporary = PathName + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(this));
        File.Move(temporary, PathName, true);
        Current = this;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(Language);
    }
}
