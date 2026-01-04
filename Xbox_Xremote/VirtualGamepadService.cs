using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Text.Json;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;

namespace Wpf_webxbox;

/// <summary>
/// 虚拟手柄服务类，负责键盘到手柄的映射与注入
/// </summary>
public class VirtualGamepadService
{
    #region Fields

    private readonly CoreWebView2 _webView;
    private bool _isEnabled;
    private const string MappingsFileName = "key_mappings.json";
    private string _loadStatusLog = "Not loaded yet";

    // 本地定义，确保反序列化兼容性
    private class LocalKeyMappingDefinition
    {
        public string Type { get; set; } = "";
        public int Index { get; set; }
        public string Value { get; set; } = "";
        public string Function { get; set; } = "";
    }

    // 默认按键映射 (与原 JS 文件保持一致)
    private const string DefaultMappings = @"
    'ArrowUp': { type: 'button', index: 12 },    // D-pad up
    'ArrowDown': { type: 'button', index: 13 },  // D-pad down
    'ArrowLeft': { type: 'button', index: 14 },  // D-pad left
    'ArrowRight': { type: 'button', index: 15 }, // D-pad right
    'KeyZ': { type: 'button', index: 0 },        // A button
    'KeyX': { type: 'button', index: 1 },        // B button
    'KeyC': { type: 'button', index: 2 },        // X button
    'KeyV': { type: 'button', index: 3 },        // Y button
    'KeyQ': { type: 'button', index: 4 },        // Left bumper
    'KeyE': { type: 'button', index: 5 },        // Right bumper
    'KeyU': { type: 'button', index: 6 },        // Left trigger (LT)
    'Digit2': { type: 'button', index: 9 },      // Start button
    'Digit3': { type: 'button', index: 8 },      // Back/Select button
    'KeyO': { type: 'button', index: 7 },        // Right trigger (RT)
    'Space': { type: 'button', index: 16 },      // Home button
    // Left stick axes
    'KeyW': { type: 'axis', index: 1, value: -1 }, // Left stick up
    'KeyS': { type: 'axis', index: 1, value: 1 },  // Left stick down
    'KeyA': { type: 'axis', index: 0, value: -1 }, // Left stick left
    'KeyD': { type: 'axis', index: 0, value: 1 },  // Left stick right
    // Right stick axes
    'KeyI': { type: 'axis', index: 3, value: -1 }, // Right stick up
    'KeyK': { type: 'axis', index: 3, value: 1 },  // Right stick down
    'KeyJ': { type: 'axis', index: 2, value: -1 }, // Right stick left
    'KeyL': { type: 'axis', index: 2, value: 1 }   // Right stick right
    ";

    /// <summary>
    /// 从 JSON 文件读取映射并转换为 JS 对象字符串
    /// </summary>
    private string GetMappingsJsObject()
    {
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, MappingsFileName);
        try
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                // Debug.WriteLine($"Read JSON content: {json}"); // Log raw content

                var options = new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };
                
                var mappingDict = JsonSerializer.Deserialize<Dictionary<string, LocalKeyMappingDefinition>>(json, options);
                
                if (mappingDict != null && mappingDict.Count > 0)
                {
                    var entries = new List<string>();
                    foreach (var kvp in mappingDict)
                    {
                        var def = kvp.Value;
                        string valuePart = def.Type == "axis" ? $", value: {def.Value}" : "";
                        // JS object entry: 'Key': { type: '...', index: ..., value: ... }
                        entries.Add($"'{kvp.Key}': {{ type: '{def.Type}', index: {def.Index}{valuePart} }}"); 
                    }
                    _loadStatusLog = $"Success: Loaded {mappingDict.Count} mappings from file";
                    Debug.WriteLine(_loadStatusLog);
                    return string.Join(",\n    ", entries);
                }
                else
                {
                    _loadStatusLog = "Warning: File existed but deserialized to empty/null";
                }
            }
            else
            {
                _loadStatusLog = "Warning: File not found, using defaults";
                Debug.WriteLine($"Key mappings file not found at {filePath}, using defaults.");
            }
        }
        catch (Exception ex)
        {
            _loadStatusLog = $"Error: {ex.Message}";
            Debug.WriteLine($"Error reading key mappings from {filePath}: {ex.Message}");
            System.Windows.Application.Current.Dispatcher.Invoke(() => 
            {
                System.Windows.MessageBox.Show($"读取按键配置失败: {ex.Message}\n路径: {filePath}\n\n将使用默认配置。", "按键映射错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            });
        }

        // Fallback to default
        return DefaultMappings;
    }

    #endregion

    #region Constructor

    /// <summary>
    /// 初始化 <see cref="VirtualGamepadService"/> 类的新实例
    /// </summary>
    /// <param name="webView">WebView2 核心实例</param>
    public VirtualGamepadService(CoreWebView2 webView)
    {
        _webView = webView;
    }

    #endregion

    #region Properties

    /// <summary>
    /// 获取一个值，该值指示手柄模拟器是否已启用
    /// </summary>
    public bool IsEnabled => _isEnabled;

    #endregion

    #region Public Methods

    /// <summary>
    /// 异步初始化虚拟手柄管理器
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    public async Task InitializeAsync()
    {
        // 页面加载时自动注入脚本
        await _webView.AddScriptToExecuteOnDocumentCreatedAsync(GetGamepadSimulatorScript());
    }

    /// <summary>
    /// 异步启用虚拟手柄
    /// </summary>
    /// <returns>如果启用成功则返回 true，否则返回 false</returns>
    /// <exception cref="Exception">启用失败时抛出异常</exception>
    public async Task<bool> EnableAsync()
    {
        if (_isEnabled)
        {
            return true;
        }

        try
        {
            // 先注入模拟器脚本（如果还没注入）
            await InjectGamepadSimulatorScriptAsync();

            // 执行启用脚本
            string enableScript = @"
                (function() {
                    try {
                        console.log('Enabling virtual gamepad...');
                        
                        // 检查函数是否存在
                        if (typeof window.connectGamepad !== 'function') {
                            console.error('Gamepad simulator not loaded!');
                            return false;
                        }
                        
                        // 连接虚拟手柄
                        window.connectGamepad();
                        
                        // 激活键盘处理器（这是关键！）
                        if (typeof window.activateKeyboardHandler === 'function') {
                            window.activateKeyboardHandler();
                        } else {
                            console.error('activateKeyboardHandler function not found');
                        }
                        
                        console.log('Virtual gamepad enabled successfully');
                        return true;
                    } catch (e) {
                        console.error('Failed to enable virtual gamepad:', e);
                        return false;
                    }
                })();
            ";

            var result = await _webView.ExecuteScriptAsync(enableScript);
            _isEnabled = result == "true";

            // 可选：添加一个测试来验证手柄是否工作
            if (_isEnabled)
            {
                await TestGamepadFunctionalityAsync();
            }

            return _isEnabled;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error enabling virtual gamepad: {ex.Message}");
            throw new Exception($"启用虚拟手柄失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 异步禁用虚拟手柄
    /// </summary>
    /// <returns>操作是否成功</returns>
    public async Task<bool> DisableAsync()
    {
        if (_webView == null) return false;

        string disableScript = @"
                (function() {
                    try {
                        console.log('Disabling virtual gamepad...');
                        
                        // 停用键盘处理器
                        if (typeof window.deactivateKeyboardHandler === 'function') {
                            window.deactivateKeyboardHandler();
                        }
                        
                        // 断开虚拟手柄
                        if (typeof window.disconnectGamepad === 'function') {
                            window.disconnectGamepad();
                        }
                        
                        console.log('Virtual gamepad disabled successfully');
                        return true;
                    } catch (e) {
                        console.error('Failed to disable virtual gamepad:', e);
                        return false;
                    }
                })();
            ";

        try
        {
            // 无论脚本执行结果如何，都将本地状态设为禁用，确保UI同步
            _isEnabled = false;
            
            var result = await _webView.ExecuteScriptAsync(disableScript);
            return result == "true";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DisableAsync error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 异步切换虚拟手柄状态
    /// </summary>
    /// <returns>如果切换后为启用状态则返回 true，否则返回 false</returns>
    public async Task<bool> ToggleAsync()
    {
        if (_isEnabled)
        {
            await DisableAsync();
            return _isEnabled;
        }
        else
        {
            await EnableAsync();
            return _isEnabled;
        }
    }

    /// <summary>
    /// 重置内部状态（例如在页面刷新后）
    /// </summary>
    public void ResetState()
    {
        _isEnabled = false;
    }

    /// <summary>
    /// 异步更新按键映射
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    public async Task UpdateKeyMappingsAsync()
    {
        string mappingsContent = GetMappingsJsObject();
        // 构造更新脚本
        // 注意：mappingsContent 已经是 JS 对象属性格式 ('Key': { ... }, 'Key2': { ... })
        // 我们需要将其包裹在对象字面量中
        string updateScript = $"if (typeof window.updateKeyMappings === 'function') {{ window.updateKeyMappings({{ {mappingsContent} }}); }}";
        await _webView.ExecuteScriptAsync(updateScript);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 异步注入模拟器脚本
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    private async Task InjectGamepadSimulatorScriptAsync()
    {
        // 检查是否已经注入
        string checkScript = "typeof window.connectGamepad === 'function'";
        var isInjected = await _webView.ExecuteScriptAsync(checkScript);

        if (isInjected != "true")
        {
            // 读取模拟器脚本
            string simulatorScript = GetGamepadSimulatorScript();

            // 注入脚本
            await _webView.AddScriptToExecuteOnDocumentCreatedAsync(simulatorScript);

            // 对于当前页面也立即执行
            await _webView.ExecuteScriptAsync(simulatorScript);

            Console.WriteLine("Gamepad simulator script injected");
        }
    }

    /// <summary>
    /// 异步测试手柄功能
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    private async Task TestGamepadFunctionalityAsync()
    {
        string testScript = @"
            (function() {
                try {
                    // 测试手柄检测
                    const gamepads = navigator.getGamepads();
                    console.log('Detected gamepads:', gamepads);
                    
                    if (gamepads[0]) {
                        console.log('Virtual gamepad found:', gamepads[0].id);
                        
                        // 模拟按键测试
                        window.dispatchEvent(new KeyboardEvent('keydown', { code: 'KeyW' }));
                        setTimeout(() => {
                            const gamepad = navigator.getGamepads()[0];
                            console.log('Left stick Y axis after W key:', gamepad.axes[1]);
                            window.dispatchEvent(new KeyboardEvent('keyup', { code: 'KeyW' }));
                        }, 100);
                        
                        return true;
                    } else {
                        console.error('No gamepad detected!');
                        return false;
                    }
                } catch (error) {
                    console.error('Error during gamepad test:', error);
                    return false;
                }
            })();
        ";

        await _webView.ExecuteScriptAsync(testScript);
    }

    /// <summary>
    /// 获取手柄模拟器脚本内容，包含自定义按键映射
    /// </summary>
    /// <returns>脚本内容字符串</returns>
    private string GetGamepadSimulatorScript()
    {
        string mappingsContent = GetMappingsJsObject();

        // JS script content
        return $@"
// 虚拟手柄模拟器 - 统一版本 v20250104-FixKeyM-v2
console.log('Gamepad Simulator v20250104-FixKeyM-v2 loaded');
console.log('Server Load Status: {_loadStatusLog}');
(function() {{
  'use strict';
  
  // 存储原始的getGamepads函数
  const originalGetGamepads = navigator.getGamepads;
  
  // 手柄状态
  let virtualGamepad = null;
  let gamepadConnected = false;
  let keyboardHandlerActive = false;
  
  // 默认按键映射 (使用 let 允许更新)
  let keyMappings = {{
    {mappingsContent}
  }};
  
  // 打印当前加载的按键映射 (调试用)
  console.log('Loaded key mappings keys:', Object.keys(keyMappings));
  if (keyMappings['KeyM']) {{
      console.log('KeyM mapping exists:', keyMappings['KeyM']);
  }} else {{
      console.log('KeyM mapping NOT found in initial load');
  }}

  // 更新按键映射
  function updateKeyMappings(newMappings) {{
    keyMappings = newMappings;
    console.log('Key mappings updated via external call');
  }}
  
  // 显式挂载到 window 对象，确保外部可调用
  window.updateKeyMappings = updateKeyMappings;
  
  // 创建虚拟手柄对象
  function createVirtualGamepad() {{
    const gamepad = {{
      id: 'Virtual Gamepad (Keyboard Only) (Vendor: 0000 Product: 0001)',
      index: 0,
      connected: true,
      mapping: 'standard',
      axes: [0, 0, 0, 0], // [Left Stick X, Left Stick Y, Right Stick X, Right Stick Y]
      buttons: Array(17).fill().map(() => ({{ pressed: false, value: 0, touched: false }})),
      timestamp: performance.now()
    }};
    
    // 设置只读属性以符合Gamepad接口规范
    Object.defineProperty(gamepad, 'id', {{ writable: false }});
    Object.defineProperty(gamepad, 'index', {{ writable: false }});
    Object.defineProperty(gamepad, 'connected', {{ writable: false }});
    Object.defineProperty(gamepad, 'mapping', {{ writable: false }});
    
    return gamepad;
  }}
  
  // 重写navigator.getGamepads函数
  navigator.getGamepads = function() {{
    //console.log('navigator.getGamepads() called, gamepadConnected:', gamepadConnected);
    
    if (gamepadConnected && virtualGamepad) {{
      const gamepads = Array(4).fill(null);
      gamepads[0] = virtualGamepad;
      //console.log('Returning virtual gamepad:', gamepads[0]);
      return gamepads;
    }}
    
    const originalResult = originalGetGamepads.call(this);
    //console.log('Returning original gamepads:', originalResult);
    return originalResult;
  }};
  
  // 连接虚拟手柄
  function connectGamepad() {{
    if (!gamepadConnected) {{
      virtualGamepad = createVirtualGamepad();
      gamepadConnected = true;
      
      // 分发手柄连接事件
      try {{
        const event = new CustomEvent('gamepadconnected', {{ 
          detail: {{ gamepad: virtualGamepad }},
          bubbles: true,
          cancelable: true
        }});
        window.dispatchEvent(event);
        
        // 尝试标准GamepadEvent
        if (typeof GamepadEvent !== 'undefined') {{
          try {{
            const standardEvent = new GamepadEvent('gamepadconnected', {{ gamepad: virtualGamepad }});
            window.dispatchEvent(standardEvent);
          }} catch (e) {{
            console.log('Standard GamepadEvent failed, using CustomEvent only:', e.message);
          }}
        }}
      }} catch (e) {{
        console.error('Failed to dispatch gamepad connected event:', e);
      }}
      
      console.log('Virtual Gamepad connected');
      console.log('Virtual gamepad object:', virtualGamepad);
    }}
  }}
  
  // 断开虚拟手柄
  function disconnectGamepad() {{
    if (gamepadConnected) {{
      try {{
        const event = new CustomEvent('gamepaddisconnected', {{ 
          detail: {{ gamepad: virtualGamepad }},
          bubbles: true,
          cancelable: true
        }});
        window.dispatchEvent(event);
        
        if (typeof GamepadEvent !== 'undefined') {{
          try {{
            const standardEvent = new GamepadEvent('gamepaddisconnected', {{ gamepad: virtualGamepad }});
            window.dispatchEvent(standardEvent);
          }} catch (e) {{
            console.log('Standard GamepadEvent failed, using CustomEvent only:', e.message);
          }}
        }}
      }} catch (e) {{
        console.error('Failed to dispatch gamepad disconnected event:', e);
      }}
      
      virtualGamepad = null;
      gamepadConnected = false;
      console.log('Virtual Gamepad disconnected');
    }}
  }}
  
  // 存储按键按下时间
  const keyPressStartTimes = {{}};

  // 处理键盘输入
  function handleKeyboardInput(keyCode, pressed) {{
    if (!gamepadConnected || !virtualGamepad) {{
      console.log('Gamepad not connected, ignoring key:', keyCode);
      return;
    }}
    
    const mapping = keyMappings[keyCode];
    if (mapping) {{
      // 记录按键时长
      if (pressed) {{
        if (!keyPressStartTimes[keyCode]) {{
          keyPressStartTimes[keyCode] = performance.now();
          //console.log(`Key ${{keyCode}} down`);
        }}
      }} else {{
        if (keyPressStartTimes[keyCode]) {{
          const duration = performance.now() - keyPressStartTimes[keyCode];
          console.log(`Key ${{keyCode}} released after ${{duration.toFixed(2)}}ms`);
          delete keyPressStartTimes[keyCode];
        }}
      }}

      //console.log('Processing key:', keyCode, 'pressed:', pressed, 'mapping:', mapping);
      
      if (mapping.type === 'button') {{
        virtualGamepad.buttons[mapping.index].pressed = pressed;
        virtualGamepad.buttons[mapping.index].value = pressed ? 1 : 0;
        virtualGamepad.buttons[mapping.index].touched = pressed;
        //console.log('Button', mapping.index, 'set to:', pressed);
      }} else if (mapping.type === 'axis') {{
        if (pressed) {{
          virtualGamepad.axes[mapping.index] = mapping.value;
        }} else {{
          virtualGamepad.axes[mapping.index] = 0;
        }}
        //console.log('Axis', mapping.index, 'set to:', virtualGamepad.axes[mapping.index]);
      }}
      
      // 更新时间戳
      virtualGamepad.timestamp = performance.now();
    }} else {{
      console.log('No mapping found for key:', keyCode);
    }}
  }}
  
  // 键盘事件处理器
  function handleKeyDown(event) {{
    if (!keyboardHandlerActive) return;
    
    // 如果存在映射，阻止默认行为（防止网页滚动或原生处理）
    if (keyMappings[event.code]) {{
      event.preventDefault();
      event.stopPropagation();
    }}
    
    // 发送自定义事件给模拟器
    window.dispatchEvent(new CustomEvent('gamepadKeyPress', {{
      detail: {{
        key: event.code,
        pressed: true
      }}
    }}));
    
    handleKeyboardInput(event.code, true);
  }}
  
  function handleKeyUp(event) {{
    if (!keyboardHandlerActive) return;
    
    // 如果存在映射，阻止默认行为
    if (keyMappings[event.code]) {{
      event.preventDefault();
      event.stopPropagation();
    }}
    
    // 发送自定义事件给模拟器
    window.dispatchEvent(new CustomEvent('gamepadKeyPress', {{
      detail: {{
        key: event.code,
        pressed: false
      }}
    }}));
    
    handleKeyboardInput(event.code, false);
  }}
  
  // 激活键盘处理器
  function activateKeyboardHandler() {{
    if (!keyboardHandlerActive) {{
      document.addEventListener('keydown', handleKeyDown);
      document.addEventListener('keyup', handleKeyUp);
      keyboardHandlerActive = true;
      console.log('Keyboard handler activated');
    }}
  }}
  
  // 停用键盘处理器
  function deactivateKeyboardHandler() {{
    if (keyboardHandlerActive) {{
      document.removeEventListener('keydown', handleKeyDown);
      document.removeEventListener('keyup', handleKeyUp);
      keyboardHandlerActive = false;
      console.log('Keyboard handler deactivated');
    }}
  }}
  
  // 测试函数
  function testGamepadDetection() {{
    console.log('=== Gamepad Detection Test ===');
    const gamepads = navigator.getGamepads();
    console.log('navigator.getGamepads() result:', gamepads);
    console.log('Gamepad count:', gamepads.length);
    
    for (let i = 0; i < gamepads.length; i++) {{
      if (gamepads[i]) {{
        console.log(`Gamepad ${{i}}:`, {{
          id: gamepads[i].id,
          index: gamepads[i].index,
          connected: gamepads[i].connected,
          mapping: gamepads[i].mapping,
          buttons: gamepads[i].buttons.length,
          axes: gamepads[i].axes.length
        }});
      }}
    }}
    
    console.log('Virtual gamepad connected:', gamepadConnected);
    console.log('Keyboard handler active:', keyboardHandlerActive);
  }}
  
  // 暴露全局函数
  window.connectGamepad = connectGamepad;
  window.disconnectGamepad = disconnectGamepad;
  window.activateKeyboardHandler = activateKeyboardHandler;
  window.deactivateKeyboardHandler = deactivateKeyboardHandler;
  window.testGamepadDetection = testGamepadDetection;
  
  console.log('Gamepad Simulator loaded successfully');
  console.log('Available functions: connectGamepad, disconnectGamepad, activateKeyboardHandler, deactivateKeyboardHandler, testGamepadDetection');
  
}})();
        ";
    }

    #endregion
}


