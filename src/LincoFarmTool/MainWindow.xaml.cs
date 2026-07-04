using System.Collections.ObjectModel;
using System.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using LincoFarmTool.Models;
using LincoFarmTool.Native;
using LincoFarmTool.Services;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace LincoFarmTool;

/// <summary>
/// 收菜闹钟桌宠主窗口：一只蹲在桌面的猫，管理多个浇水/收菜倒计时，
/// 到点用「声音 + 系统通知 + 猫跳动 + 窗口闪烁」四种方式提醒。
/// </summary>
public partial class MainWindow : Window
{
    private readonly ObservableCollection<FarmTask> _tasks = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private Forms.NotifyIcon? _trayIcon;
    private AppSettings _settings = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _settings = SettingsStore.Load();

        // 载入存档
        foreach (var t in TaskStore.Load().OrderBy(t => t.TargetTime))
            _tasks.Add(t);
        TaskList.ItemsSource = _tasks;
        _tasks.CollectionChanged += (_, _) => UpdateEmptyHint();
        UpdateEmptyHint();

        // 默认放到屏幕右下角
        var area = SystemParameters.WorkArea;
        Left = area.Right - Width - 30;
        Top = area.Bottom - Height - 30;

        _timer.Tick += OnTick;
        _timer.Start();

        SetupTrayIcon();
    }

    private void UpdateEmptyHint()
        => EmptyHint.Visibility = _tasks.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>每秒刷新倒计时并检查是否有任务到点。</summary>
    private void OnTick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        bool anyFired = false;

        foreach (var task in _tasks)
        {
            task.RefreshCountdown(now);
            // 提前 LeadMinutes 分钟就触发，给用户留登录时间
            if (!task.Fired && task.TargetTime.AddMinutes(-_settings.LeadMinutes) <= now)
            {
                task.Fired = true;
                anyFired = true;
                FireAlert(task);
            }
        }

        if (anyFired) TaskStore.Save(_tasks);
    }

    /// <summary>到点提醒：声音 + 系统通知 + 猫跳动 + 窗口闪烁。</summary>
    private void FireAlert(FarmTask task)
    {
        // 提前量提示
        var remain = task.TargetTime - DateTime.Now;
        string when = remain.TotalSeconds > 45
            ? $"还有约 {Math.Max(1, (int)Math.Round(remain.TotalMinutes))} 分钟，快登录！"
            : "时间到，快操作！";

        // 1. 声音（连响三声更醒目）
        PlayAlertSound();
        // 2. Windows 系统通知（托盘气泡）
        _trayIcon?.ShowBalloonTip(6000, "🌾 该操作啦！", $"{task.Name}\n{when}", Forms.ToolTipIcon.Info);
        // 3. 猫跳动
        BounceCat();
        // 4. 窗口闪烁 + 置顶抢一下注意力
        WindowFlasher.Flash(this);
        Topmost = false;
        Topmost = true;
    }

    private static void PlayAlertSound()
    {
        // 用系统提示音，连响三声。之后可替换成自定义猫叫 wav。
        var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        int count = 0;
        t.Tick += (_, _) =>
        {
            SystemSounds.Exclamation.Play();
            if (++count >= 3) t.Stop();
        };
        SystemSounds.Exclamation.Play();
        count = 1;
        t.Start();
    }

    private void BounceCat()
    {
        var anim = new DoubleAnimation
        {
            From = 0,
            To = -14,
            Duration = TimeSpan.FromMilliseconds(160),
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(4),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        PetShift.BeginAnimation(TranslateTransform.YProperty, anim);
    }

    /// <summary>点 ＋ 添加闹钟。</summary>
    private void OnAddTask(object sender, RoutedEventArgs e)
    {
        var dlg = new AddTaskWindow { Owner = this };
        if (dlg.ShowDialog() == true && dlg.Result != null)
        {
            InsertSorted(dlg.Result);
            TaskStore.Save(_tasks);
        }
    }

    /// <summary>点 🌱 种菜：一键生成肝帝模式的整串浇水/收割闹钟。</summary>
    private void OnPlant(object sender, RoutedEventArgs e)
    {
        var dlg = new PlantWindow { Owner = this };
        if (dlg.ShowDialog() == true && dlg.Result is { Count: > 0 } alarms)
        {
            foreach (var a in alarms) InsertSorted(a);
            TaskStore.Save(_tasks);
            _trayIcon?.ShowBalloonTip(4000, "🌱 已排好肝帝闹钟",
                $"共 {alarms.Count} 个提醒，{alarms[^1].TargetTime:MM-dd HH:mm} 收割", Forms.ToolTipIcon.Info);
        }
    }

    /// <summary>点 ✕ 删除某个闹钟。</summary>
    private void OnDeleteTask(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is FarmTask task)
        {
            _tasks.Remove(task);
            TaskStore.Save(_tasks);
        }
    }

    private void InsertSorted(FarmTask task)
    {
        int i = 0;
        while (i < _tasks.Count && _tasks[i].TargetTime <= task.TargetTime) i++;
        _tasks.Insert(i, task);
    }

    /// <summary>按住猫/面板空白处拖动整个窗口。</summary>
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void SetupTrayIcon()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("🌱 种菜（肝帝一键）", null, (_, _) => OnPlant(this, new RoutedEventArgs()));
        menu.Items.Add("添加单个闹钟", null, (_, _) => OnAddTask(this, new RoutedEventArgs()));
        menu.Items.Add("回到右下角", null, (_, _) => MoveToCorner());
        menu.Items.Add("清除已到点", null, (_, _) => ClearFired());
        menu.Items.Add(BuildLeadMenu());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => System.Windows.Application.Current.Shutdown());

        _trayIcon = new Forms.NotifyIcon
        {
            Icon = Drawing.SystemIcons.Application,
            Visible = true,
            Text = "Linco 收菜闹钟",
            ContextMenuStrip = menu
        };
        _trayIcon.DoubleClick += (_, _) => MoveToCorner();
    }

    /// <summary>「提前提醒量」子菜单：0 / 1 / 3 / 5 / 10 分钟，当前项打勾。</summary>
    private Forms.ToolStripMenuItem BuildLeadMenu()
    {
        var root = new Forms.ToolStripMenuItem($"提前提醒：{_settings.LeadMinutes} 分钟");
        foreach (int m in new[] { 0, 1, 3, 5, 10 })
        {
            int minutes = m;
            var item = new Forms.ToolStripMenuItem(minutes == 0 ? "准点提醒" : $"提前 {minutes} 分钟")
            {
                Checked = _settings.LeadMinutes == minutes
            };
            item.Click += (_, _) =>
            {
                _settings.LeadMinutes = minutes;
                SettingsStore.Save(_settings);
                // 刷新菜单标题与勾选
                root.Text = $"提前提醒：{minutes} 分钟";
                foreach (Forms.ToolStripMenuItem sub in root.DropDownItems)
                    sub.Checked = sub == item;
            };
            root.DropDownItems.Add(item);
        }
        return root;
    }

    private void MoveToCorner()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Right - Width - 30;
        Top = area.Bottom - Height - 30;
    }

    private void ClearFired()
    {
        for (int i = _tasks.Count - 1; i >= 0; i--)
            if (_tasks[i].Fired) _tasks.RemoveAt(i);
        TaskStore.Save(_tasks);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _timer.Stop();
        TaskStore.Save(_tasks);
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
    }
}
