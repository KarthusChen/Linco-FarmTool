using System.IO;
using System.Text.Json;
using LincoFarmTool.Models;

namespace LincoFarmTool.Services;

/// <summary>
/// 把闹钟列表持久化到 %AppData%\LincoFarmTool\tasks.json，
/// 关掉重开也不丢。
/// </summary>
public static class TaskStore
{
    private static readonly string Dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LincoFarmTool");
    private static readonly string FilePath = Path.Combine(Dir, "tasks.json");

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static List<FarmTask> Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new List<FarmTask>();
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<FarmTask>>(json) ?? new List<FarmTask>();
        }
        catch
        {
            // 存档损坏时不崩溃，直接从空开始
            return new List<FarmTask>();
        }
    }

    public static void Save(IEnumerable<FarmTask> tasks)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            var json = JsonSerializer.Serialize(tasks, Options);
            File.WriteAllText(FilePath, json);
        }
        catch
        {
            // 存盘失败不影响使用
        }
    }
}
