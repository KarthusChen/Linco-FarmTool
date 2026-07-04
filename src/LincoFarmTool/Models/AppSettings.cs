namespace LincoFarmTool.Models;

/// <summary>
/// 全局设置。目前只有「提前提醒量」——闹钟会在实际到点前这么多分钟就响，
/// 给用户留出登录游戏的时间。
/// </summary>
public class AppSettings
{
    /// <summary>提前提醒的分钟数，0 表示准点。默认 3 分钟。</summary>
    public int LeadMinutes { get; set; } = 3;
}
