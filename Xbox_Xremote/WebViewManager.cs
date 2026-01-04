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

        await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
            (function() {
                // 1. Visibility API Spoofing
                const getVisible = () => 'visible';
                const getFalse = () => false;
                const getTrue = () => true;

                try {
                    Object.defineProperty(document, 'hidden', { get: getFalse, configurable: true });
                    Object.defineProperty(document, 'visibilityState', { get: getVisible, configurable: true });
                    Object.defineProperty(document, 'webkitHidden', { get: getFalse, configurable: true });
                    Object.defineProperty(document, 'webkitVisibilityState', { get: getVisible, configurable: true });
                    Object.defineProperty(document, 'hasFocus', { value: getTrue, configurable: true });
                } catch (e) {}
                
                // Spoof window properties
                try {
                    Object.defineProperty(window, 'hidden', { get: getFalse, configurable: true });
                    Object.defineProperty(window, 'visibilityState', { get: getVisible, configurable: true });
                } catch (e) {}

                // 2. Block Visibility and Focus Events
                const originalAddEventListener = EventTarget.prototype.addEventListener;
                try {
                    const blockedEventTypes = new Set([
                        'visibilitychange',
                        'webkitvisibilitychange',
                        'mozvisibilitychange',
                        'msvisibilitychange',
                        'blur',
                        'focusout',
                        'pagehide'
                    ]);

                    EventTarget.prototype.addEventListener = function(type, listener, options) {
                        if (blockedEventTypes.has(type)) {
                            // console.log('Blocked event listener:', type);
                            return;
                        }
                        return originalAddEventListener.call(this, type, listener, options);
                    };
                } catch (e) {}
                
                // Stop event propagation for blur/focusout
                try {
                    window.addEventListener('blur', (e) => {
                        e.stopImmediatePropagation();
                        e.stopPropagation();
                        // console.log('Blocked window blur');
                    }, true);
                    
                    window.addEventListener('focusout', (e) => {
                        e.stopImmediatePropagation();
                        e.stopPropagation();
                    }, true);
                } catch (e) {}
                
                // 3. Audio Context Hack (Prevent suspension)
                try {
                    const AudioContext = window.AudioContext || window.webkitAudioContext;
                    if (AudioContext) {
                        const ctx = new AudioContext();
                        const osc = ctx.createOscillator();
                        const gain = ctx.createGain();
                        gain.gain.value = 0.001; // Inaudible
                        osc.connect(gain);
                        gain.connect(ctx.destination);
                        osc.start();
                    }
                } catch (e) {}

                // 4. RequestAnimationFrame Hack (Run in background)
                try {
                    let lastTime = 0;
                    window.requestAnimationFrame = function(callback) {
                        const currTime = new Date().getTime();
                        const timeToCall = Math.max(0, 16 - (currTime - lastTime));
                        const id = window.setTimeout(function() { callback(currTime + timeToCall); }, timeToCall);
                        lastTime = currTime + timeToCall;
                        return id;
                    };
                    window.cancelAnimationFrame = function(id) {
                        clearTimeout(id);
                    };
                } catch (e) {}
            })();
        ");
    }

    #endregion
}
