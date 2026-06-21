using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using LincoFarmTool.Native;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace LincoFarmTool;

/// <summary>
/// 桌宠主窗口：透明置顶、可拖动、全局按键时左右爪交替拍打、常驻系统托盘。
/// </summary>
public partial class MainWindow : Window
{
    private GlobalKeyboardHook? _keyboardHook;
    private Forms.NotifyIcon? _trayIcon;

    // 爪子静止角度
    private const double LeftRestAngle = -32;
    private const double RightRestAngle = 32;
    // 爪子拍到桌面的角度
    private const double LeftDownAngle = -2;
    private const double RightDownAngle = 2;

    private bool _useLeftNext = true; // 交替拍爪

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // 默认放到屏幕右下角
        var area = SystemParameters.WorkArea;
        Left = area.Right - Width - 40;
        Top = area.Bottom - Height - 40;

        // 安装全局键盘钩子
        _keyboardHook = new GlobalKeyboardHook();
        _keyboardHook.KeyDown += OnGlobalKeyDown;

        SetupTrayIcon();
    }

    /// <summary>按住左键拖动整只猫。</summary>
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    /// <summary>全局任意按键 → 拍一下爪子（左右交替）。</summary>
    private void OnGlobalKeyDown(int vkCode)
    {
        if (_useLeftNext)
            TapPaw(LeftPawRotate, LeftRestAngle, LeftDownAngle);
        else
            TapPaw(RightPawRotate, RightRestAngle, RightDownAngle);

        _useLeftNext = !_useLeftNext;
    }

    private static void TapPaw(System.Windows.Media.RotateTransform paw, double rest, double down)
    {
        // 快速拍下再弹回
        var anim = new DoubleAnimation
        {
            From = down,
            To = rest,
            Duration = TimeSpan.FromMilliseconds(90),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        paw.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, anim);
    }

    private void SetupTrayIcon()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("回到右下角", null, (_, _) =>
        {
            var area = SystemParameters.WorkArea;
            Left = area.Right - Width - 40;
            Top = area.Bottom - Height - 40;
        });
        menu.Items.Add("退出", null, (_, _) => System.Windows.Application.Current.Shutdown());

        _trayIcon = new Forms.NotifyIcon
        {
            // 暂用系统图标占位，之后可替换成自己的 .ico
            Icon = Drawing.SystemIcons.Application,
            Visible = true,
            Text = "Linco 桌宠",
            ContextMenuStrip = menu
        };
        // 双击托盘也可把猫叫回右下角
        _trayIcon.DoubleClick += (_, _) =>
        {
            var area = SystemParameters.WorkArea;
            Left = area.Right - Width - 40;
            Top = area.Bottom - Height - 40;
        };
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _keyboardHook?.Dispose();
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
    }
}
