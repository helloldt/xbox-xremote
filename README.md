# Xbox Xremote

[English](#english) | [中文](#chinese)

---

`<a name="english"></a>`

## 🇺🇸 English

**Xbox Xremote** is a WPF application based on **WebView2** technology, designed specifically for **Xbox Cloud Gaming (xCloud)**. It is not just a web wrapper but features a powerful built-in **Virtual Gamepad**, allowing players to control Xbox cloud games directly using a keyboard without needing a physical controller.

### ✨ Key Features

- **☁️ Cloud Gaming Integration**: Directly embeds the Xbox Cloud Gaming webpage for an immersive experience.
- **🎮 Virtual Gamepad**: Simulates standard Xbox controller signals using keyboard inputs via JavaScript injection.
- **⌨️ Custom Key Mapping**: Supports custom mapping between keyboard keys and controller buttons to suit different gaming habits.
- **🛡️ Anti-Idling**: Built-in scripts prevent disconnection from the cloud gaming service due to inactivity.
- **🔧 Developer Tools**: Integrated WebView2 developer tools for easy debugging and script extension.

### 🚀 Quick Start

#### Requirements

- Windows 10/11
- .NET 7.0 or higher runtime
- [WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) (Usually built-in on Windows 10/11)

#### How to Run

1. Clone or download this repository.
2. Open the solution using Visual Studio 2022 or JetBrains Rider.
3. Restore NuGet packages.
4. Build and run the `Xbox_Xremote` project.

### 📖 Usage

#### Enable Virtual Gamepad

1. After launching the program, click the **"Gamepad"** button (or similar icon) on the interface to activate the virtual gamepad.
2. The program will inject simulation scripts, and keyboard inputs will be converted to controller signals.
3. Click the button again to disable the virtual gamepad and restore normal keyboard input (for typing, etc.).

#### Default Key Mappings

By default, the keyboard mapping is as follows:

| Keyboard Key                       | Xbox Controller Function |
| :--------------------------------- | :----------------------- |
| **Arrow Keys (↑ ↓ ← →)** | D-Pad                    |
| **W / S / A / D**            | Left Stick               |
| **I / K / J / L**            | Right Stick              |
| **Z**                        | A Button                 |
| **X**                        | B Button                 |
| **C**                        | X Button                 |
| **V**                        | Y Button                 |
| **Q**                        | LB (Left Bumper)         |
| **E**                        | RB (Right Bumper)        |
| **U**                        | LT (Left Trigger)        |
| **O**                        | RT (Right Trigger)       |
| **Space**                    | Xbox Button (Home)       |
| **2**                        | Menu Button (Start)      |
| **3**                        | View Button (Back)       |

#### Custom Keys

1. Click the **"Key Mapping"** button on the interface to open the configuration window.
2. Modify the keys for each function in the popup window.
3. After saving, the configuration is written to the local `key_mappings.json` file and takes effect immediately.

### 🛠️ Tech Stack

- **Frontend Framework**: WPF (Windows Presentation Foundation)
- **Language**: C# (.NET 7.0/10.0)
- **Core Component**: Microsoft.Web.WebView2
- **Scripting**: JavaScript (for gamepad simulation and event injection)

### ⚠️ Disclaimer

This project is for learning and exchange purposes only and is not official Xbox software. Xbox Cloud Gaming is a trademark of Microsoft Corporation. Please adhere to the Xbox Terms of Service when using this software.

---

`<a name="chinese"></a>`

## 🇨🇳 中文

**Xbox Xremote** 是一个基于 **WebView2** 技术的 WPF 应用程序，专为 **Xbox Cloud Gaming (xCloud)** 打造。它不仅仅是一个网页包装器，更内置了强大的**虚拟手柄**功能，允许玩家使用键盘直接操控 Xbox 云游戏，无需连接物理手柄。

### ✨ 主要功能

- **☁️ 云游戏集成**：直接嵌入 Xbox Cloud Gaming 网页，提供沉浸式的游戏体验。
- **🎮 虚拟手柄**：通过 JavaScript 注入技术，将键盘操作模拟为标准的 Xbox 手柄信号。
- **⌨️ 自定义按键映射**：支持用户自定义键盘与手柄按键的映射关系，适应不同游戏习惯。
- **🛡️ 防挂机机制**：内置脚本防止因长时间无操作而被云游戏服务断开连接。
- **🔧 开发者工具**：集成 WebView2 开发者工具，方便调试与脚本扩展。

### 🚀 快速开始

#### 环境要求

- Windows 10/11
- .NET 7.0 或更高版本运行时
- [WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) (通常 Windows 10/11 已内置)

#### 运行指南

1. 克隆或下载本项目代码。
2. 使用 Visual Studio 2022 或 JetBrains Rider 打开解决方案。
3. 还原 NuGet 包。
4. 编译并运行 `Xbox_Xremote` 项目。

### 📖 使用说明

#### 启用虚拟手柄

1. 启动程序后，点击界面上的 **"手柄"** 按钮（或类似图标）以激活虚拟手柄功能。
2. 程序会注入模拟脚本，此时键盘输入将被转换为手柄信号。
3. 再次点击该按钮可禁用虚拟手柄，恢复正常的键盘输入（用于打字等）。

#### 默认按键映射

默认配置下，键盘按键映射如下：

| 键盘按键                       | Xbox 手柄功能        |
| :----------------------------- | :------------------- |
| **方向键 (↑ ↓ ← →)** | 十字键 (D-Pad)       |
| **W / S / A / D**        | 左摇杆 (Left Stick)  |
| **I / K / J / L**        | 右摇杆 (Right Stick) |
| **Z**                    | A 键                 |
| **X**                    | B 键                 |
| **C**                    | X 键                 |
| **V**                    | Y 键                 |
| **Q**                    | LB (Left Bumper)     |
| **E**                    | RB (Right Bumper)    |
| **U**                    | LT (Left Trigger)    |
| **O**                    | RT (Right Trigger)   |
| **空格 (Space)**         | Xbox 键 (Home)       |
| **2**                    | 菜单键 (Start/Menu)  |
| **3**                    | 视图键 (Back/View)   |

#### 自定义按键

1. 点击界面上的 **"按键映射"** 按钮打开配置窗口。
2. 在弹出的窗口中修改各功能的对应按键。
3. 保存后配置将写入本地 `key_mappings.json` 文件并即时生效。

### 🛠️ 技术栈

- **前端框架**: WPF (Windows Presentation Foundation)
- **编程语言**: C# (.NET 7.0/10.0)
- **核心组件**: Microsoft.Web.WebView2
- **脚本交互**: JavaScript (用于手柄模拟与事件注入)

### ⚠️ 免责声明

本项目仅供学习与交流使用，非 Xbox 官方软件。Xbox Cloud Gaming 是 Microsoft Corporation 的商标。请遵守 Xbox 服务条款使用。

<img width="1483" height="889" alt="image" src="https://github.com/user-attachments/assets/f5d567db-5a8c-4a29-bf40-e210b35e0c10" />
