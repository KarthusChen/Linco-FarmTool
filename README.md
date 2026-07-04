# Linco-FarmTool · 收菜闹钟桌宠

一只蹲在桌面角落的猫，帮你盯着王者农场每块地的**浇水 / 收菜时间**，到点了就提醒你，别再错过催熟。BongoCat 风格——常驻桌面、始终可见。

## 技术栈

- **C# + WPF (.NET 8)** — Windows 原生，透明置顶窗口、低资源占用
- **System.Text.Json** — 闹钟列表本地持久化
- **WinForms NotifyIcon** — 系统托盘 + 通知气泡
- **Win32 FlashWindowEx** — 到点窗口闪烁提醒

## 功能

- 🐱 透明置顶、可拖动的桌宠猫，常驻屏幕右下角
- ⏰ 多个收菜/浇水闹钟，实时倒计时显示
- ➕ 两种设法：**倒计时**（11小时13分钟后浇水）/ **指定时间**（今天 13:35）
- 🔔 到点四重提醒：**声音 + Windows 系统通知 + 猫跳动 + 窗口闪烁**
- 💾 闹钟存到本地（`%AppData%\LincoFarmTool\tasks.json`），关掉重开不丢
- 📌 托盘菜单：添加闹钟 / 回到右下角 / 清除已到点 / 退出

## 运行

```sh
cd src/LincoFarmTool
dotnet run
```

启动后猫出现在屏幕右下角，点 `＋` 添加闹钟（可先设个"1分钟后"测试提醒）。

## 目录结构

```
Linco-FarmTool/
├── global.json                       # 锁定 .NET 8 SDK
└── src/LincoFarmTool/
    ├── MainWindow.xaml(.cs)           # 桌宠窗口 + 闹钟列表 + 到点提醒
    ├── AddTaskWindow.xaml(.cs)        # 添加闹钟对话框（倒计时/指定时间）
    ├── Models/FarmTask.cs             # 闹钟任务模型
    ├── Services/TaskStore.cs          # JSON 本地存档
    └── Native/WindowFlasher.cs        # Win32 窗口闪烁
```

## 路线图

- [ ] 用真实美术替换矢量猫（精灵图 / Live2D，王者英雄主题皮肤）
- [ ] 内置作物周期表（肝帝 / 佛系 / 自然三种模式，自动算浇水时间点）
- [ ] 自定义提示音（猫叫 / 语音）
- [ ] 闹钟到点后可「一键续期」下一轮
- [ ] 开机自启 + 记忆窗口位置
- [ ] 打包为单文件 exe / 安装包
