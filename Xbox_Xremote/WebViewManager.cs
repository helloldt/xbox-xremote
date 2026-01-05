using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace Wpf_webxbox;

/// <summary>
/// WebView2 管理器，负责初始化和配置
/// </summary>
public class WebViewManager
{
    #region Fields

    private readonly WebView2 _webView;

    #endregion

    #region Constructor

    /// <summary>
    /// 初始化 <see cref="WebViewManager"/> 类的新实例
    /// </summary>
    /// <param name="webView">WebView2 控件实例</param>
    public WebViewManager(WebView2 webView)
    {
        _webView = webView;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 异步初始化 WebView2 环境
    /// </summary>
    /// <returns>初始化任务</returns>
    public async Task InitializeAsync()
    {
        try
        {
            // 创建WebView2环境选项，启用扩展支持
            var options = new CoreWebView2EnvironmentOptions
            {
                AreBrowserExtensionsEnabled = true
            };

            // 创建WebView2环境
            var environment = await CoreWebView2Environment.CreateAsync(null, null, options);

            // 确保WebView2运行时已安装
            await _webView.EnsureCoreWebView2Async(environment);

            ConfigureSettings();
            RegisterEvents();

            // 注入防后台检测脚本
            await InjectAntiIdlingScriptAsync();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"WebView2 初始化失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }

    /// <summary>
    /// 导航到指定 URL
    /// </summary>
    /// <param name="url">目标 URL</param>
    public void Navigate(string url)
    {
        if (_webView.CoreWebView2 != null)
        {
            _webView.CoreWebView2.Navigate(url);
        }
    }

    #endregion

    #region Private Methods

    private void ConfigureSettings()
    {
        if (_webView.CoreWebView2 == null) return;

        var settings = _webView.CoreWebView2.Settings;
        settings.IsGeneralAutofillEnabled = true;
        settings.IsPasswordAutosaveEnabled = true;
        settings.AreDefaultScriptDialogsEnabled = true;
        settings.AreDevToolsEnabled = true;
        settings.AreHostObjectsAllowed = true;
        settings.IsScriptEnabled = true;
        settings.IsWebMessageEnabled = true;
    }

    private void RegisterEvents()
    {
        if (_webView.CoreWebView2 == null) return;

        _webView.CoreWebView2.PermissionRequested += OnPermissionRequested;
        _webView.CoreWebView2.DOMContentLoaded += OnDOMContentLoaded;
    }

    private void OnPermissionRequested(object? sender, CoreWebView2PermissionRequestedEventArgs e)
    {
        e.State = CoreWebView2PermissionState.Allow;
    }

    private void OnDOMContentLoaded(object? sender, CoreWebView2DOMContentLoadedEventArgs e)
    {
        if (sender is not CoreWebView2 webViewInstance) return;

        // 移除页面焦点限制
        _ = webViewInstance.ExecuteScriptAsync(@"
            (function() {
                try {
                    if (document.body) {
                        document.body.tabIndex = -1;
                        document.body.focus();
                    }
                } catch (e) {
                }
            })();
        ");
    }

    private async Task InjectAntiIdlingScriptAsync()
    {
        if (_webView.CoreWebView2 == null) return;

        try 
        {
            string script = await ResourceHelper.ReadEmbeddedScriptAsync("anti_idling.js");
            await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(script);
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to inject anti-idling script: {ex.Message}");
        }
    }

    #endregion
}
