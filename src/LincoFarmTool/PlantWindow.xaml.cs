using System.Windows;
using System.Windows.Controls;
using LincoFarmTool.Models;
using LincoFarmTool.Services;
using Brush = System.Windows.Media.Brush;
using BrushConverter = System.Windows.Media.BrushConverter;

namespace LincoFarmTool;

/// <summary>
/// 种菜对话框，两种模式：
///   现在开种 —— 从刚种下(R=T, w=0)排肝帝闹钟；
///   种了有一会了 —— 用剩余成熟时间 R 和湿润剩余 w，从当前状态排闹钟。
/// 确定后 <see cref="Result"/> 为要添加的闹钟列表。
/// </summary>
public partial class PlantWindow : Window
{
    public List<FarmTask>? Result { get; private set; }

    private bool _midMode; // 当前是否在「种了有一会了」tab

    // 预览卡片配色：正常（琥珀）/ 报错（红）
    private static readonly Brush OkBg = Hex("#FFFFF4E2"), OkLine = Hex("#FFF3DCB0"), OkTitle = Hex("#FFC0791A"), OkBody = Hex("#FF8A6428");
    private static readonly Brush ErrBg = Hex("#FFFDECEC"), ErrLine = Hex("#FFF0C0C0"), ErrTitle = Hex("#FFC0392B"), ErrBody = Hex("#FFB0463C");
    private static Brush Hex(string hex) => (Brush)new BrushConverter().ConvertFromString(hex)!;

    public PlantWindow()
    {
        InitializeComponent();
        CropCombo.ItemsSource = CropDb.All;
        CropCombo.DisplayMemberPath = nameof(CropInfo.Display);

        Loaded += (_, _) => { RefreshPreview(); NameBox.Focus(); };

        Header.MouseLeftButtonDown += (_, e) => { if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed) DragMove(); };

        NameBox.TextChanged += (_, _) => RefreshPreview();
        foreach (var rb in new[] { Cycle1, Cycle8, Cycle16, Cycle32 })
            rb.Checked += (_, _) => RefreshPreview();
        foreach (var tb in new[] { RHour, RMin, WHour, WMin })
            tb.TextChanged += (_, _) => RefreshPreview();
    }

    private void OnTabChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source != Tabs) return; // 只响应外层 TabControl
        _midMode = Tabs.SelectedIndex == 1;
        RefreshPreview();
    }

    private void OnCropSelected(object sender, SelectionChangedEventArgs e)
    {
        if (CropCombo.SelectedItem is not CropInfo crop) return;
        NameBox.Text = crop.Name;
        SelectCycle(crop.NaturalHours);
        RefreshPreview();
    }

    private void SelectCycle(int hours)
    {
        Cycle1.IsChecked = hours == 1;
        Cycle8.IsChecked = hours == 8;
        Cycle16.IsChecked = hours == 16;
        Cycle32.IsChecked = hours == 32;
    }

    private int SelectedCycle()
    {
        if (Cycle1.IsChecked == true) return 1;
        if (Cycle8.IsChecked == true) return 8;
        if (Cycle32.IsChecked == true) return 32;
        return 16;
    }

    private string CropName() =>
        string.IsNullOrWhiteSpace(NameBox.Text) ? $"{SelectedCycle()}h作物" : NameBox.Text.Trim();

    /// <summary>读「种了有一会了」的剩余成熟 R 和湿润剩余 w（分钟），并校验上限。</summary>
    private bool TryGetState(out double remaining, out double moisture, out string error)
    {
        remaining = moisture = 0;
        error = "";

        double t = SelectedCycle() * 60.0;   // 自然周期
        double w = t / 3.0;                   // 湿润满值上限

        if (!TryMinutes(RHour.Text, RMin.Text, out remaining) || remaining <= 0)
        {
            error = "剩余成熟时间要大于 0";
            return false;
        }
        if (remaining > t)
        {
            error = $"剩余成熟时间不能超过自然周期 {SelectedCycle()} 小时";
            return false;
        }
        if (!TryMinutes(WHour.Text, WMin.Text, out moisture))
        {
            error = "水分剩余时间填写有误";
            return false;
        }
        if (moisture > w)
        {
            error = $"水分剩余不能超过湿润满值 {FormatMinutes(w)}（周期÷3）";
            return false;
        }
        return true;
    }

    private static string FormatMinutes(double minutes)
    {
        int m = (int)Math.Round(minutes);
        return m >= 60 ? $"{m / 60}小时{m % 60}分" : $"{m}分";
    }

    private static bool TryMinutes(string h, string m, out double minutes)
    {
        minutes = 0;
        if (!int.TryParse(h.Trim(), out int hh) || hh < 0) return false;
        if (!int.TryParse(m.Trim(), out int mm) || mm < 0) return false;
        minutes = hh * 60 + mm;
        return true;
    }

    private List<FarmTask>? BuildAlarms(out string error)
    {
        error = "";
        if (!_midMode)
            return GandiPlanner.BuildFreshPlant(CropName(), SelectedCycle(), DateTime.Now);

        if (!TryGetState(out double r, out double w, out error))
            return null;
        return GandiPlanner.BuildFromState(CropName(), SelectedCycle(), r, w, DateTime.Now);
    }

    private void RefreshPreview()
    {
        if (PreviewBody == null) return; // 初始化早期

        var alarms = BuildAlarms(out string error);
        if (alarms == null)
        {
            SetPreviewError(true);
            PreviewTitle.Text = "⚠️ 无法设置";
            PreviewBody.Text = error;
            return;
        }
        SetPreviewError(false);
        if (alarms.Count == 0)
        {
            PreviewTitle.Text = "肝帝模式预览";
            PreviewBody.Text = "已经成熟啦，可以直接收割～";
            return;
        }

        var harvest = alarms[^1].TargetTime;
        var lines = alarms.Select(a => $"· {a.Name.Split('·').Last().Trim()}  →  {a.TargetTime:MM-dd HH:mm}");
        int lead = SettingsStore.Load().LeadMinutes;
        string leadNote = lead > 0 ? $"（实际会提前 {lead} 分钟提醒，方便登录）" : "（准点提醒）";
        string head = _midMode ? "从现在算，接下来：" : "种下时先亲手浇一次，之后：";

        PreviewTitle.Text = $"肝帝模式 · 共 {alarms.Count} 个闹钟，{harvest:MM-dd HH:mm} 收割";
        PreviewBody.Text = head + "\n" + string.Join("\n", lines) + "\n" + leadNote;
    }

    private void SetPreviewError(bool isError)
    {
        PreviewCard.Background = isError ? ErrBg : OkBg;
        PreviewCard.BorderBrush = isError ? ErrLine : OkLine;
        PreviewTitle.Foreground = isError ? ErrTitle : OkTitle;
        PreviewBody.Foreground = isError ? ErrBody : OkBody;
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        var alarms = BuildAlarms(out _);
        // 出错或已成熟时，预览区已给出提示，这里直接不关闭即可
        if (alarms is not { Count: > 0 })
        {
            RefreshPreview();
            return;
        }
        Result = alarms;
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
