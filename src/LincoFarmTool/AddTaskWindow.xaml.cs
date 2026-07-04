using System.Windows;
using LincoFarmTool.Models;
using MessageBox = System.Windows.MessageBox;

namespace LincoFarmTool;

/// <summary>
/// 添加闹钟对话框。确定后 <see cref="Result"/> 即为新任务，否则为 null。
/// </summary>
public partial class AddTaskWindow : Window
{
    public FarmTask? Result { get; private set; }

    public AddTaskWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => NameBox.Focus();
        Header.MouseLeftButtonDown += (_, e) => { if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed) DragMove(); };
    }

    private void OnModeChanged(object sender, RoutedEventArgs e)
    {
        // 两个面板可能在 InitializeComponent 阶段还没建好
        if (CountdownPanel == null || AbsolutePanel == null) return;

        bool countdown = ModeCountdown.IsChecked == true;
        CountdownPanel.Visibility = countdown ? Visibility.Visible : Visibility.Collapsed;
        AbsolutePanel.Visibility = countdown ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        string name = NameBox.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show(this, "请先填写任务名", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        DateTime target;

        if (ModeCountdown.IsChecked == true)
        {
            if (!int.TryParse(HoursBox.Text.Trim(), out int h) || h < 0 ||
                !int.TryParse(MinutesBox.Text.Trim(), out int m) || m < 0)
            {
                MessageBox.Show(this, "小时和分钟请填非负整数", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (h == 0 && m == 0)
            {
                MessageBox.Show(this, "倒计时不能为 0", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            target = DateTime.Now.AddHours(h).AddMinutes(m);
        }
        else
        {
            if (!int.TryParse(HourBox.Text.Trim(), out int hh) || hh < 0 || hh > 23 ||
                !int.TryParse(MinuteBox.Text.Trim(), out int mm) || mm < 0 || mm > 59)
            {
                MessageBox.Show(this, "时间请填 00:00 ~ 23:59", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var day = DayTomorrow.IsChecked == true ? DateTime.Today.AddDays(1) : DateTime.Today;
            target = day.AddHours(hh).AddMinutes(mm);

            // 选了“今天”但时间点已过 → 顺延到明天，避免立刻响
            if (target <= DateTime.Now && DayToday.IsChecked == true)
                target = target.AddDays(1);
        }

        Result = new FarmTask { Name = name, TargetTime = target };
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
