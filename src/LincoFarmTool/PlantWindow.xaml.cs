using System.Windows;
using System.Windows.Controls;
using LincoFarmTool.Models;
using LincoFarmTool.Services;
using MessageBox = System.Windows.MessageBox;

namespace LincoFarmTool;

/// <summary>
/// 种菜对话框：选作物/周期 + 种植时间 → 一键生成肝帝模式的 3 个浇水/收割闹钟。
/// 确定后 <see cref="Result"/> 为要添加的闹钟列表。
/// </summary>
public partial class PlantWindow : Window
{
    public List<FarmTask>? Result { get; private set; }

    public PlantWindow()
    {
        InitializeComponent();
        CropCombo.ItemsSource = CropDb.All;
        CropCombo.DisplayMemberPath = nameof(CropInfo.Display);

        Loaded += (_, _) =>
        {
            SetNow();
            RefreshPreview();
            NameBox.Focus();
        };

        Header.MouseLeftButtonDown += (_, e) => { if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed) DragMove(); };

        NameBox.TextChanged += (_, _) => RefreshPreview();
        foreach (var rb in new[] { Cycle1, Cycle8, Cycle16, Cycle32 })
            rb.Checked += (_, _) => RefreshPreview();
        foreach (var rb in new[] { DayToday, DayYesterday, DayBefore })
            rb.Checked += (_, _) => RefreshPreview();
        HourBox.TextChanged += (_, _) => RefreshPreview();
        MinuteBox.TextChanged += (_, _) => RefreshPreview();
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

    private void OnNow(object sender, RoutedEventArgs e)
    {
        SetNow();
        RefreshPreview();
    }

    private void SetNow()
    {
        var now = DateTime.Now;
        DayToday.IsChecked = true;
        HourBox.Text = now.Hour.ToString("00");
        MinuteBox.Text = now.Minute.ToString("00");
    }

    private DateTime SelectedDay()
    {
        if (DayYesterday.IsChecked == true) return DateTime.Today.AddDays(-1);
        if (DayBefore.IsChecked == true) return DateTime.Today.AddDays(-2);
        return DateTime.Today;
    }

    private bool TryGetPlantTime(out DateTime plantTime)
    {
        plantTime = default;
        if (!int.TryParse(HourBox.Text.Trim(), out int h) || h < 0 || h > 23) return false;
        if (!int.TryParse(MinuteBox.Text.Trim(), out int m) || m < 0 || m > 59) return false;
        plantTime = SelectedDay().AddHours(h).AddMinutes(m);
        return true;
    }

    private void RefreshPreview()
    {
        if (PreviewBody == null) return; // 初始化早期

        if (!TryGetPlantTime(out var plant))
        {
            PreviewBody.Text = "种植时间填写有误（时 0~23、分 0~59）";
            return;
        }

        string name = string.IsNullOrWhiteSpace(NameBox.Text) ? $"{SelectedCycle()}h作物" : NameBox.Text.Trim();
        var alarms = GandiPlanner.BuildAlarms(name, SelectedCycle(), plant);
        var harvest = alarms[^1].TargetTime;

        var lines = alarms.Select(a => $"· {a.Name.Split('·').Last().Trim()}  →  {a.TargetTime:MM-dd HH:mm}");
        int lead = SettingsStore.Load().LeadMinutes;
        string leadNote = lead > 0 ? $"（实际会提前 {lead} 分钟提醒，方便登录）" : "（准点提醒）";
        PreviewTitle.Text = $"肝帝模式 · 共 {alarms.Count} 个闹钟，{harvest:MM-dd HH:mm} 收割";
        PreviewBody.Text = "种下时先亲手浇一次，之后：\n" + string.Join("\n", lines) + "\n" + leadNote;
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        if (!TryGetPlantTime(out var plant))
        {
            MessageBox.Show(this, "种植时间填写有误（时 0~23、分 0~59）", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string name = string.IsNullOrWhiteSpace(NameBox.Text) ? $"{SelectedCycle()}h作物" : NameBox.Text.Trim();
        Result = GandiPlanner.BuildAlarms(name, SelectedCycle(), plant);
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
