namespace LincoFarmTool.Models;

/// <summary>
/// 一种作物：名称、解锁等级、自然成熟周期（小时）。
/// </summary>
public record CropInfo(string Name, int Level, int NaturalHours)
{
    /// <summary>下拉里显示用文案，如「枇杷（16h · Lv.69）」。</summary>
    public string Display => $"{Name}（{NaturalHours}h · Lv.{Level}）";

    // 让下拉框直接显示友好文案，而不是 record 默认的 "CropInfo { ... }"
    public override string ToString() => Display;
}
