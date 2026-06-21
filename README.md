# Linco-FarmTool

王者荣耀「农场」桌宠工具 —— 一只常驻桌面、会随你敲键盘拍爪子的猫（BongoCat 风格）。

## 技术栈

- **C# + WPF (.NET 8)** — Windows 原生桌面应用，透明置顶窗口、低资源占用
- **Win32 全局键盘钩子**（`SetWindowsHookEx` / `WH_KEYBOARD_LL`）— 不抢焦点也能感知按键
- **WinForms NotifyIcon** — 系统托盘常驻

## 当前已实现（原型）

- ✅ 透明无边框、始终置顶的桌宠窗口
- ✅ 鼠标左键拖动移动位置
- ✅ 全局按键时左右爪交替拍打动画
- ✅ 系统托盘图标（右键菜单：回到右下角 / 退出）
- ✅ 高分屏 DPI 感知

角色目前是用矢量图形临时画的一只猫，方便先把交互机制跑通。后续可替换为精灵图 / Live2D 立绘。

## 运行

```sh
cd src/LincoFarmTool
dotnet run
```

启动后猫出现在屏幕右下角，敲键盘看它拍爪子，拖动可移动，托盘右键退出。

## 目录结构

```
Linco-FarmTool/
├── global.json                     # 锁定 .NET 8 SDK
└── src/LincoFarmTool/
    ├── LincoFarmTool.csproj
    ├── App.xaml(.cs)
    ├── MainWindow.xaml(.cs)         # 桌宠窗口：拖动 / 拍爪 / 托盘
    └── Native/
        └── GlobalKeyboardHook.cs    # Win32 全局键盘钩子
```

## 路线图（待办）

- [ ] 用真实美术资源替换矢量猫（精灵图帧动画或 Live2D）
- [ ] 鼠标点击 / 移动也触发反应
- [ ] 多套皮肤切换（王者英雄主题）
- [ ] 「农场」核心玩法：挂机产出 / 资源面板
- [ ] 记忆窗口位置、开机自启选项
- [ ] 打包为单文件 exe / 安装包
