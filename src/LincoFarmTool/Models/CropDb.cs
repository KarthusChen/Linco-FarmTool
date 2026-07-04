namespace LincoFarmTool.Models;

/// <summary>
/// 收菜计算器里的全部作物，按自然成熟周期分组。数据来自游戏「选择作物」列表。
/// </summary>
public static class CropDb
{
    public static readonly IReadOnlyList<CropInfo> All = new List<CropInfo>
    {
        // —— 32 小时作物 ——
        new("龙胆花", 80, 32), new("莲花", 75, 32), new("灯笼果", 70, 32),
        new("樱桃", 65, 32), new("梨子", 60, 32), new("菠萝", 55, 32),
        new("荞麦", 50, 32), new("猕猴桃", 45, 32), new("辣椒", 40, 32),
        new("卷心菜", 30, 32), new("蓝莓", 20, 32),

        // —— 16 小时作物 ——
        new("芋头", 999, 16), new("牛油果", 79, 16), new("石榴", 78, 16),
        new("山楂", 74, 16), new("山竹", 73, 16), new("枇杷", 69, 16),
        new("哈密瓜", 68, 16), new("柠檬", 64, 16), new("桃子", 63, 16),
        new("杨桃", 59, 16), new("李子", 58, 16), new("橘子", 54, 16),
        new("棉花", 53, 16), new("木瓜", 48, 16), new("花生", 46, 16),
        new("西瓜", 38, 16), new("生菜", 36, 16), new("柚子", 28, 16),
        new("南瓜", 26, 16), new("香蕉", 18, 16), new("大蒜", 16, 16),
        new("草莓", 10, 16),

        // —— 8 小时作物 ——
        new("火龙果", 77, 8), new("芒果", 72, 8), new("杏子", 67, 8),
        new("葫芦", 62, 8), new("冬瓜", 57, 8), new("红枣", 52, 8),
        new("花菜", 44, 8), new("葡萄", 34, 8), new("茄子", 24, 8),
        new("青椒", 14, 8), new("玉米", 8, 8),

        // —— 1 小时作物 ——
        new("仙人掌果", 81, 1), new("咖啡豆", 76, 1), new("树莓", 71, 1),
        new("桑葚", 66, 1), new("甘蔗", 61, 1), new("四季豆", 56, 1),
        new("莴笋", 51, 1), new("洋葱", 42, 1), new("白萝卜", 32, 1),
        new("黄瓜", 22, 1), new("向日葵", 12, 1), new("土豆", 6, 1),
    };

    /// <summary>按周期分组（32/16/8/1），组内按等级从高到低，供下拉展示。</summary>
    public static IEnumerable<IGrouping<int, CropInfo>> Grouped() =>
        All.GroupBy(c => c.NaturalHours)
           .OrderByDescending(g => g.Key);
}
