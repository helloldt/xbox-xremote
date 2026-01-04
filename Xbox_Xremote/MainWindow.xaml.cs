using Microsoft.Web.WebView2.Core;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Wpf_webxbox;

/// <summary>
/// 主窗口交互逻辑，负责 UI 事件处理与协调
/// </summary>
public partial class MainWindow : Window
{
    #region Fields

    private const string XboxRemotePlayUrl = "https://www.xbox.com/en-US/play/consoles";
    
    private readonly WebViewManager _webViewManager;
    private VirtualGamepadService? _virtualGamepadService;
    private DispatcherTimer? _keepAliveTimer;
    
    private bool _isWebViewInitialized;

    #endregion

    #region Constructor

    /// <summary>
    /// 初始化 <see cref="MainWindow"/> 类的新实例
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        _webViewManager = new WebViewManager(webView);
        
        // 异步初始化流程
        Loaded += OnWindowLoaded;
    }

    #endregion

    #region Event Handlers

    private async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        await InitializeAsync();
    }

    private async void XboxButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isWebViewInitialized)
        {
            _webViewManager.Navigate(XboxRemotePlayUrl);
        }
    }

    private async void GamepadButton_Click(object sender, RoutedEventArgs e)
    {
        if (_virtualGamepadService == null) return;

        try
        {
            bool isEnabled = await _virtualGamepadService.ToggleAsync();
            UpdateGamepadStatus(isEnabled);
            
            if (isEnabled)
            {
                webView.Focus();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"切换手柄状态失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void KeyMapButton_Click(object sender, RoutedEventArgs e)
    {
        var mappingWindow = new KeyMappingWindow();
        mappingWindow.Owner = this;
        if (mappingWindow.ShowDialog() == true)
        {
            // 异步更新映射
            _ = _virtualGamepadService?.UpdateKeyMappingsAsync();
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        webView.Reload();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        webView.CoreWebView2?.OpenDevToolsWindow();
    }


    private void WebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        UpdateStatus($"正在加载: {e.Uri}");
        
        RefreshButton.IsEnabled = false;

        // 页面刷新或导航时，重置手柄服务状态，因为 JS 环境会被重置
        if (_virtualGamepadService != null)
        {
            _virtualGamepadService.ResetState();
            UpdateGamepadStatus(false); // 更新 UI 显示为禁用状态
        }
    }

    private async void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            UpdateStatus("就绪");

            // 强制刷新一次按键映射，确保最新配置生效
            if (_virtualGamepadService != null)
            {
                await _virtualGamepadService.UpdateKeyMappingsAsync();
            }

            // 页面加载完成后尝试自动启用手柄
            await AutoEnableGamepadAsync();

        }
        else
        {
            UpdateStatus($"加载失败: {e.WebErrorStatus}");
        }

        RefreshButton.IsEnabled = true;
    }

    #endregion

    #region Private Methods

    private async Task InitializeAsync()
    {
        try
        {
            await _webViewManager.InitializeAsync();
            
            // 初始化虚拟手柄服务
            _virtualGamepadService = new VirtualGamepadService(webView.CoreWebView2);
            await _virtualGamepadService.InitializeAsync();

            StartKeepAlive();

            _isWebViewInitialized = true;
            
            // 导航到默认页面
            _webViewManager.Navigate(XboxRemotePlayUrl);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"初始化失败: {ex.Message}", "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task AutoEnableGamepadAsync()
    {
        if (_virtualGamepadService == null) return;

        // 简单的延时重试策略，确保页面脚本已就绪
        await Task.Delay(2000);
        
        try
        {
            bool isEnabled = await _virtualGamepadService.EnableAsync();
            UpdateGamepadStatus(isEnabled);
        }
        catch
        {
            // 自动启用失败不弹窗，仅记录或忽略
            UpdateGamepadStatus(false);
        }
    }

    private void UpdateStatus(string message)
    {
        StatusText.Text = message;
    }

    private void UpdateGamepadStatus(bool isEnabled)
    {
        if (isEnabled)
        {
            GamepadStatus.Text = "虚拟手柄: 已启用";
            GamepadStatus.Foreground = new SolidColorBrush(Colors.LightGreen);
            GamepadButton.Content = "🎮 ✓";
            GamepadButton.ToolTip = "禁用虚拟手柄";
        }
        else
        {
            GamepadStatus.Text = "虚拟手柄: 已禁用";
            GamepadStatus.Foreground = new SolidColorBrush(Colors.Gray);
            GamepadButton.Content = "🎮";
            GamepadButton.ToolTip = "启用虚拟手柄";
        }
    }

    private void StartKeepAlive()
    {
        _keepAliveTimer = new DispatcherTimer();
        _keepAliveTimer.Interval = TimeSpan.FromMinutes(10);
        _keepAliveTimer.Tick += KeepAliveTimer_Tick;
        _keepAliveTimer.Start();
    }

    private void KeepAliveTimer_Tick(object? sender, EventArgs e)
    {
        // 简单的保活逻辑，防止会话超时
        if (webView.CoreWebView2 != null)
        {
            // 执行一个无副作用的脚本
            webView.ExecuteScriptAsync("console.log('Keep-alive tick');");
        }
    }

    #endregion
}
