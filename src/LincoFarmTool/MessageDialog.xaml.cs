using System.Windows;

namespace LincoFarmTool;

/// <summary>统一风格的确认/提示弹窗，替代系统 MessageBox。</summary>
public partial class MessageDialog : Window
{
    public MessageDialog(string title, string body, string okText = "确定", string? cancelText = "取消")
    {
        InitializeComponent();
        TitleText.Text = title;
        BodyText.Text = body;
        OkBtn.Content = okText;

        if (cancelText == null)
            CancelBtn.Visibility = Visibility.Collapsed;
        else
            CancelBtn.Content = cancelText;
    }

    /// <summary>弹出并返回是否点了「确定」。</summary>
    public static bool Confirm(Window? owner, string title, string body, string okText = "确定", string? cancelText = "取消")
    {
        var dlg = new MessageDialog(title, body, okText, cancelText);
        if (owner != null) dlg.Owner = owner;
        return dlg.ShowDialog() == true;
    }

    private void OnOk(object sender, RoutedEventArgs e) => DialogResult = true;
    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
