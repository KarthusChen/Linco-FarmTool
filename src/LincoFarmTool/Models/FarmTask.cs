using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LincoFarmTool.Models;

/// <summary>
/// 一个收菜/浇水闹钟任务。TargetTime 是绝对到点时间，
/// 无论用倒计时还是指定时间设置，最终都换算成它。
/// </summary>
public class FarmTask : INotifyPropertyChanged
{
    public Guid Id { get; set; } = Guid.NewGuid();

    private string _name = "";
    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(nameof(Name)); }
    }

    public DateTime TargetTime { get; set; }

    /// <summary>是否已经触发过提醒（避免重复响铃）。</summary>
    public bool Fired { get; set; }

    // ↓↓ 以下是显示用属性，不写入存档 ↓↓

    private string _countdownText = "";
    [JsonIgnore]
    public string CountdownText
    {
        get => _countdownText;
        private set { if (_countdownText != value) { _countdownText = value; OnPropertyChanged(nameof(CountdownText)); } }
    }

    [JsonIgnore]
    public string TargetText => TargetTime.ToString("MM-dd HH:mm");

    /// <summary>根据当前时间刷新倒计时文案。</summary>
    public void RefreshCountdown(DateTime now)
    {
        var remaining = TargetTime - now;
        if (remaining <= TimeSpan.Zero)
        {
            CountdownText = "⏰ 到点啦！";
        }
        else if (Fired)
        {
            // 已提前响过，还没到实际时间 → 提示赶紧登录
            CountdownText = $"⏰ 快登录！还剩 {remaining.Minutes:00}:{remaining.Seconds:00}";
        }
        else
        {
            int totalHours = (int)remaining.TotalHours;
            CountdownText = $"还剩 {totalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
