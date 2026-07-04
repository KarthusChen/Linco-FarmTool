using System.IO;
using System.Text.Json;
using LincoFarmTool.Models;

namespace LincoFarmTool.Services;

/// <summary>
/// 把全局设置持久化到 %AppData%\LincoFarmTool\settings.json。
/// </summary>
public static class SettingsStore
{
    private static readonly string Dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LincoFarmTool");
    private static readonly string FilePath = Path.Combine(Dir, "settings.json");

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new AppSettings();
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, Options));
        }
        catch
        {
            // 存盘失败不影响使用
        }
    }
}
