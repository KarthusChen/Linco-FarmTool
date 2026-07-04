using LincoFarmTool.Models;

namespace LincoFarmTool.Services;

/// <summary>
/// 肝帝模式排程：4 次浇水，总耗时 = 自然周期 × 11/15。
///
/// 从「种下」起的浇水时刻：
///   ① 0        —— 种下立即浇水（玩家亲手浇，不设闹钟）
///   ② N/3      —— 湿润期结束，第 2 次浇水
///   ③ 2N/3     —— 第 3 次浇水
///   ④ 11N/15   —— 浇水秒熟、收割（③到④间隔 N/15）
///
/// 因此种下后只需生成 ②③④ 三个闹钟。经 16h 实例（种于 23:16）验证：
/// 04:36 / 09:56 / 11:00，与游戏完全一致。
/// </summary>
public static class GandiPlanner
{
    public static List<FarmTask> BuildAlarms(string cropName, int naturalHours, DateTime plantTime)
    {
        double n = naturalHours * 60.0; // 自然周期（分钟）

        var steps = new (double minutes, string label)[]
        {
            (n / 3.0,          "浇第2次水 💧"),
            (2.0 * n / 3.0,    "浇第3次水 💧"),
            (11.0 * n / 15.0,  "浇水秒熟·收割 🌾"),
        };

        string prefix = string.IsNullOrWhiteSpace(cropName) ? $"{naturalHours}h作物" : cropName.Trim();

        return steps.Select(s => new FarmTask
        {
            Name = $"{prefix} · {s.label}",
            TargetTime = plantTime.AddMinutes(s.minutes)
        }).ToList();
    }
}
