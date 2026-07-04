namespace LincoFarmTool.Models;

/// <summary>
/// 一种作物：名称、解锁等级、自然成熟周期（小时）。
/// 肝帝模式耗时 = 自然周期 × 11/15（经周期对比表验证：1h→44m、8h→5h52m、16h→11h44m、32h→23h28m）。
/// </summary>
public record CropInfo(string Name, int Level, int NaturalHours)
{
    /// <summary>肝帝模式下从现在到成熟的总时长。</summary>
    public TimeSpan GandiDuration => TimeSpan.FromMinutes(NaturalHours * 60 * 11.0 / 15.0);

    /// <summary>下拉里显示用文案，如「枇杷（16h · Lv.69）肝帝 11h44m」。</summary>
    public string Display
    {
        get
        {
            var g = GandiDuration;
            string gandi = g.TotalHours >= 1
                ? $"{(int)g.TotalHours}小时{g.Minutes}分"
                : $"{g.Minutes}分";
            return $"{Name}（{NaturalHours}h · Lv.{Level}） 肝帝 {gandi}";
        }
    }

    // 让下拉框直接显示友好文案，而不是 record 默认的 "CropInfo { ... }"
    public override string ToString() => Display;
}
