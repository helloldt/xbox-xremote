# Wpf_webxbox

A specialized WPF application designed to enhance the Xbox Cloud Gaming experience on Windows. This tool wraps the Xbox Cloud Gaming web interface in a WebView2 control and adds powerful features like keyboard-to-gamepad mapping, automated state detection, and session management.

## ✨ Key Features

* **Virtual Gamepad Emulation**: Play Xbox Cloud games using your keyboard. The application injects a virtual gamepad driver that maps keyboard inputs to controller actions.
* **Custom Key Mapping**: Fully configurable key bindings to suit your playstyle.
* **Automated Session Management**: Includes anti-idling mechanisms to keep your cloud gaming session active.
* **Image Recognition**: Integrated OpenCV support for detecting game states (e.g., failure screens) to assist with automation or monitoring.
* **Network Optimization**: Custom header injection (`X-Forwarded-For`) to assist with connectivity scenarios.
* **Modern WPF Interface**: A clean, native Windows application interface wrapping the web experience.

## 🛠️ Tech Stack

* **Framework**: .NET 8.0 (Windows)
* **UI**: WPF (Windows Presentation Foundation)
* **Browser Engine**: Microsoft WebView2 (Edge Chromium)
* **Computer Vision**: OpenCvSharp4

## 📦 Prerequisites

* Windows 10/11
* .NET 8.0 Desktop Runtime
* WebView2 Runtime (usually pre-installed on modern Windows)

## 🚀 Getting Started

1. **Clone the repository**
2. **Open the solution** (`Wpf_webxbox.sln`) in Visual Studio 2022 or later.
3. **Restore NuGet packages**:
   * `Microsoft.Web.WebView2`
   * `OpenCvSharp4` and related packages.
4. **Build and Run** the project.

## 🎮 Usage

1. Launch the application.
2. The embedded browser will navigate to `https://www.xbox.com/en-US/play/consoles`.
3. Log in with your Xbox/Microsoft account.
4. **Enable Virtual Gamepad**: Click the gamepad icon or toggle in the UI to enable keyboard controls.
5. **Key Mapping**: Open the Key Mapping settings to customize your controls.

## 📂 Project Structure

* `Wpf_webxbox/`: Main application source code.
  * `MainWindow.xaml`: Main UI and WebView2 host.
  * `Scripts/`: JavaScript files for gamepad emulation (`gamepad-simulator.js`).
  * `KeyMappingWindow.xaml`: UI for configuring key binds.
  * `ImageTemplateMatcher.cs`: Logic for OpenCV-based image recognition.
  * `GameLoopManager.cs`: Handles game automation loops.

## ⚠️ Disclaimer

This project is an unofficial tool and is not affiliated with Microsoft or Xbox. Use at your own risk.

<img width="1483" height="889" alt="image" src="https://github.com/user-attachments/assets/f5d567db-5a8c-4a29-bf40-e210b35e0c10" />
