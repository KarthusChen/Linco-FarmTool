using LincoFarmTool.Models;

namespace LincoFarmTool.Services;

/// <summary>排程中的一步：浇水或收割。</summary>
public record PlanStep(string Label, double OffsetMinutes, bool IsHarvest);

/// <summary>
/// 肝帝模式排程模拟器。基于游戏「计算原理」：
///   ① 湿润持续 W = T/3
///   ② 浇水减时 = (W - w) / 4          （w = 当前湿润剩余；干透 w=0 时减 W/4 = T/12）
///   ③ 可浇水阈值：W - w ≥ T/30         （约 10% 蒸发后才能浇）
///   ④ 肝帝最优：干透就浇（每次减 T/12），末次提前浇水秒熟
///
/// 最优策略：非末次浇水都在「干透」时进行（减时最大 = T/12）；
/// 末次在还湿润时提前浇 —— 设等待 x，则减时 = x/4，秒熟条件 x/4 ≥ R−x ⇒ x = 4R/5。
/// 由 (R=T, w=0) 出发可复现 0 / T3 / 2T3 / 11T15 四次浇水，与游戏一致。
/// </summary>
public static class GandiPlanner
{
    /// <summary>
    /// 从当前状态 (剩余成熟 R, 当前湿润剩余 w) 模拟出全部浇水/收割步骤，偏移量单位为分钟。
    /// </summary>
    public static List<PlanStep> Simulate(double totalMinutes, double remaining, double moisture)
    {
        double W = totalMinutes / 3.0;      // 湿润满值
        double dec = totalMinutes / 12.0;   // 干透浇水的减时
        double thr = totalMinutes / 30.0;   // 可浇水阈值

        var steps = new List<PlanStep>();
        double o = 0;       // 距现在的偏移（分钟）
        double R = remaining;
        double w = Math.Max(0, moisture);
        int waterIdx = 0;
        int guard = 0;

        while (guard++ < 100)
        {
            if (R <= 0) { steps.Add(new PlanStep("收割 🌾", o, true)); break; }

            // 末次提前浇水秒熟：还在湿润期内（x ≤ w）且满足浇水阈值
            double xFinal = 4.0 * R / 5.0;
            if (xFinal <= w && xFinal >= thr)
            {
                steps.Add(new PlanStep("浇水秒熟·收割 🌾", o + xFinal, true));
                break;
            }

            // 湿润期内自然成熟（且无法有效催熟）
            if (R <= w)
            {
                steps.Add(new PlanStep("收割 🌾", o + R, true));
                break;
            }

            // 干透时来一次满减时浇水
            waterIdx++;
            double waterAt = o + w;
            R = (R - w) - dec;      // 等到干透(减 w 分钟) + 浇水减 dec
            o = waterAt;

            if (R <= 0)
            {
                // 这次浇水正好秒熟
                steps.Add(new PlanStep("浇水秒熟·收割 🌾", waterAt, true));
                break;
            }

            steps.Add(new PlanStep($"浇第{waterIdx}次水 💧", waterAt, false));
            w = W;                  // 浇完湿润回满
        }

        return steps;
    }

    /// <summary>
    /// 「现在开种」：从刚种下(R=T, w=0)排程，种下那次玩家亲手浇，
    /// 因此丢掉偏移 0 的第 1 次浇水，只为后续步骤设闹钟。
    /// </summary>
    public static List<FarmTask> BuildFreshPlant(string cropName, int naturalHours, DateTime plantTime)
    {
        double t = naturalHours * 60.0;
        var steps = Simulate(t, t, 0);
        // 第一步是种下即浇(@0)，去掉
        var future = steps.Where(s => s.OffsetMinutes > 0.001).ToList();
        return ToTasks(cropName, naturalHours, plantTime, future);
    }

    /// <summary>
    /// 「种了有一会了」：从当前状态(剩余成熟 R、湿润剩余 w)排程，
    /// 全部步骤都是未来要做的，所以都设闹钟。偏移从现在算。
    /// </summary>
    public static List<FarmTask> BuildFromState(string cropName, int naturalHours,
        double remainingMinutes, double moistureMinutes, DateTime now)
    {
        double t = naturalHours * 60.0;
        var steps = Simulate(t, remainingMinutes, moistureMinutes);
        return ToTasks(cropName, naturalHours, now, steps);
    }

    private static List<FarmTask> ToTasks(string cropName, int naturalHours, DateTime baseTime, List<PlanStep> steps)
    {
        string prefix = string.IsNullOrWhiteSpace(cropName) ? $"{naturalHours}h作物" : cropName.Trim();
        return steps.Select(s => new FarmTask
        {
            Name = $"{prefix} · {s.Label}",
            TargetTime = baseTime.AddMinutes(s.OffsetMinutes)
        }).ToList();
    }
}
