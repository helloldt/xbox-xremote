using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace Wpf_webxbox;

#region KeyMappingWindow Class

/// <summary>
/// 按键映射配置窗口交互逻辑
/// </summary>
public partial class KeyMappingWindow : Window
{
    #region Fields

    private readonly string _mappingsFilePath;
    private readonly ObservableCollection<KeyMappingItem> _keyMappings;
    private const string MappingsFileName = "key_mappings.json";

    #endregion

    #region Constructor

    /// <summary>
    /// 初始化 <see cref="KeyMappingWindow"/> 类的新实例
    /// </summary>
    public KeyMappingWindow()
    {
        InitializeComponent();
        _mappingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, MappingsFileName);
        _keyMappings = new ObservableCollection<KeyMappingItem>();
        KeyMappingDataGrid.ItemsSource = _keyMappings;
        
        // 异步加载数据
        Loaded += async (s, e) => await LoadKeyMappingsAsync();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 异步从 JSON 文件加载按键映射配置
    /// </summary>
    private async Task LoadKeyMappingsAsync()
    {
        try
        {
            Dictionary<string, KeyMappingDefinition>? loadedDict = null;
            var defaultDict = GetDefaultMappings();

            if (File.Exists(_mappingsFilePath))
            {
                string json = await File.ReadAllTextAsync(_mappingsFilePath);
                try 
                {
                    var options = new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString
                    };
                    loadedDict = JsonSerializer.Deserialize<Dictionary<string, KeyMappingDefinition>>(json, options);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"读取按键配置文件失败，将使用默认配置: {ex.Message}", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            _keyMappings.Clear();
            
            // 基于默认映射构建列表，确保所有功能都存在
            foreach (var defaultKvp in defaultDict)
            {
                var defaultKey = defaultKvp.Key;
                var def = defaultKvp.Value;
                
                string currentKey = defaultKey;
                string originalKey = defaultKey; // 用于记录当前生效的键代码

                // 尝试从加载的配置中查找此功能对应的键
                if (loadedDict != null)
                {
                    // 策略1：优先匹配 Function 字段 (最准确，因为这是业务逻辑上的唯一标识)
                    var matchByFunc = loadedDict.FirstOrDefault(x => 
                        string.Equals(x.Value.Function, def.Function, StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrEmpty(matchByFunc.Key))
                    {
                        currentKey = matchByFunc.Key;
                        originalKey = matchByFunc.Key;
                    }
                    else
                    {
                        // 策略2：如果 Function 匹配失败（例如旧版配置文件），回退到物理属性匹配 (Type/Index/Value)
                        var matchByProps = loadedDict.FirstOrDefault(x => 
                            string.Equals(x.Value.Type, def.Type, StringComparison.OrdinalIgnoreCase) && 
                            x.Value.Index == def.Index && 
                            string.Equals(x.Value.Value ?? "", def.Value ?? "", StringComparison.OrdinalIgnoreCase));
                        
                        if (!string.IsNullOrEmpty(matchByProps.Key))
                        {
                            currentKey = matchByProps.Key;
                            originalKey = matchByProps.Key;
                        }
                    }
                }

                _keyMappings.Add(new KeyMappingItem
                {
                    Function = def.Function,
                    CurrentKey = ConvertKeyCodeToDisplayName(currentKey),
                    NewKey = ConvertKeyCodeToDisplayName(currentKey),
                    DefaultKey = ConvertKeyCodeToDisplayName(defaultKey),
                    Type = def.Type,
                    Index = def.Index,
                    Value = def.Value,
                    OriginalKey = originalKey
                });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载按键映射失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private Dictionary<string, KeyMappingDefinition> GetDefaultMappings()
    {
        return new Dictionary<string, KeyMappingDefinition>
        {
            { "ArrowUp", new KeyMappingDefinition { Type = "button", Index = 12, Function = "D-pad up" } },
            { "ArrowDown", new KeyMappingDefinition { Type = "button", Index = 13, Function = "D-pad down" } },
            { "ArrowLeft", new KeyMappingDefinition { Type = "button", Index = 14, Function = "D-pad left" } },
            { "ArrowRight", new KeyMappingDefinition { Type = "button", Index = 15, Function = "D-pad right" } },
            { "KeyZ", new KeyMappingDefinition { Type = "button", Index = 0, Function = "A button" } },
            { "KeyX", new KeyMappingDefinition { Type = "button", Index = 1, Function = "B button" } },
            { "KeyC", new KeyMappingDefinition { Type = "button", Index = 2, Function = "X button" } },
            { "KeyV", new KeyMappingDefinition { Type = "button", Index = 3, Function = "Y button" } },
            { "KeyQ", new KeyMappingDefinition { Type = "button", Index = 4, Function = "Left bumper" } },
            { "KeyE", new KeyMappingDefinition { Type = "button", Index = 5, Function = "Right bumper" } },
            { "KeyU", new KeyMappingDefinition { Type = "button", Index = 6, Function = "Left trigger (LT)" } },
            { "Digit2", new KeyMappingDefinition { Type = "button", Index = 9, Function = "Start button" } },
            { "Digit3", new KeyMappingDefinition { Type = "button", Index = 8, Function = "Back/Select button" } },
            { "KeyO", new KeyMappingDefinition { Type = "button", Index = 7, Function = "Right trigger (RT)" } },
            { "Space", new KeyMappingDefinition { Type = "button", Index = 16, Function = "Home button" } },
            { "KeyW", new KeyMappingDefinition { Type = "axis", Index = 1, Value = "-1", Function = "Left stick up" } },
            { "KeyS", new KeyMappingDefinition { Type = "axis", Index = 1, Value = "1", Function = "Left stick down" } },
            { "KeyA", new KeyMappingDefinition { Type = "axis", Index = 0, Value = "-1", Function = "Left stick left" } },
            { "KeyD", new KeyMappingDefinition { Type = "axis", Index = 0, Value = "1", Function = "Left stick right" } },
            { "KeyI", new KeyMappingDefinition { Type = "axis", Index = 3, Value = "-1", Function = "Right stick up" } },
            { "KeyK", new KeyMappingDefinition { Type = "axis", Index = 3, Value = "1", Function = "Right stick down" } },
            { "KeyJ", new KeyMappingDefinition { Type = "axis", Index = 2, Value = "-1", Function = "Right stick left" } },
            { "KeyL", new KeyMappingDefinition { Type = "axis", Index = 2, Value = "1", Function = "Right stick right" } }
        };
    }

    /// <summary>
    /// 将键盘代码转换为用户友好的显示名称
    /// </summary>
    /// <param name="keyCode">键盘代码</param>
    /// <returns>显示名称</returns>
    private string ConvertKeyCodeToDisplayName(string keyCode)
    {
        return keyCode switch
        {
            "ArrowUp" => "↑",
            "ArrowDown" => "↓",
            "ArrowLeft" => "←",
            "ArrowRight" => "→",
            "KeyZ" => "Z",
            "KeyX" => "X",
            "KeyC" => "C",
            "KeyV" => "V",
            "KeyQ" => "Q",
            "KeyE" => "E",
            "KeyU" => "U",
            "KeyO" => "O",
            "KeyW" => "W",
            "KeyS" => "S",
            "KeyA" => "A",
            "KeyD" => "D",
            "KeyI" => "I",
            "KeyK" => "K",
            "KeyJ" => "J",
            "KeyL" => "L",
            "Space" => "空格",
            "Digit2" => "2",
            "Digit3" => "3",
            _ => keyCode.StartsWith("Key") && keyCode.Length == 4 ? keyCode.Substring(3) : keyCode
        };
    }

    /// <summary>
    /// 将显示名称转换回键盘代码
    /// </summary>
    /// <param name="displayName">显示名称</param>
    /// <returns>键盘代码</returns>
    private string ConvertDisplayNameToKeyCode(string displayName)
    {
        // 先处理特殊符号映射
        switch (displayName)
        {
            case "↑": return "ArrowUp";
            case "↓": return "ArrowDown";
            case "←": return "ArrowLeft";
            case "→": return "ArrowRight";
            case "空格": return "Space";
            // 某些单字母可能是手动映射的，保留原有逻辑
        }

        // 如果已经是有效的 JS Code 格式 (KeyX, DigitX, ArrowX, 这里的特殊键等)
        if (displayName.StartsWith("Key") || 
            displayName.StartsWith("Digit") || 
            displayName.StartsWith("Arrow") ||
            displayName == "Enter" ||
            displayName == "Tab" ||
            displayName == "Backspace" ||
            displayName == "Escape" ||
            displayName == "Delete" ||
            displayName == "Home" ||
            displayName == "End" ||
            displayName == "PageUp" ||
            displayName == "PageDown" ||
            displayName.StartsWith("Shift") ||
            displayName.StartsWith("Control") ||
            displayName.StartsWith("Alt"))
        {
            return displayName;
        }

        // 处理原有逻辑中的单字母映射 (Z, X, C...)
        // 原有逻辑中有大量 case "Z" => "KeyZ"，这里简化处理：
        // 如果是单个字母，且不是上面的特殊情况，则认为是 KeyX
        if (displayName.Length == 1 && char.IsLetter(displayName[0]))
        {
            return $"Key{displayName.ToUpper()}";
        }
        
        // 数字处理 (如果显示名称是 "2", "3" 等)
        if (displayName.Length == 1 && char.IsDigit(displayName[0]))
        {
            return $"Digit{displayName}";
        }

        // F键处理 (F1-F12)
        if (displayName.Length >= 2 && displayName.StartsWith("F") && char.IsDigit(displayName[1]))
        {
            return displayName;
        }

        // 默认回退
        return displayName.StartsWith("Key") ? displayName : $"Key{displayName.ToUpper()}";
    }

    /// <summary>
    /// 异步保存按键映射到 JSON 文件
    /// </summary>
    private async Task SaveKeyMappingsAsync()
    {
        try
        {
            var mappingDict = new Dictionary<string, KeyMappingDefinition>();

            foreach (var item in _keyMappings)
            {
                var keyCode = ConvertDisplayNameToKeyCode(item.NewKey);
                mappingDict[keyCode] = new KeyMappingDefinition
                {
                    Type = item.Type,
                    Index = item.Index,
                    Value = item.Value,
                    Function = item.Function
                };
            }

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(mappingDict, jsonOptions);
            
            await File.WriteAllTextAsync(_mappingsFilePath, json);
            
            MessageBox.Show("按键映射保存成功！请刷新页面或重启程序以生效。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存按键映射失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// 保存按钮点击事件处理程序
    /// </summary>
    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await SaveKeyMappingsAsync();
            //MessageBox.Show($"按键映射保存成功！\n保存路径：{_mappingsFilePath}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            //MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 取消按钮点击事件处理程序
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// 重置单个按键点击事件处理程序
    /// </summary>
    private void ResetKey_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is KeyMappingItem item)
        {
            // 恢复为默认键，而不是原始加载的键
            item.NewKey = item.DefaultKey;
        }
    }

    /// <summary>
    /// 重置全部按键点击事件处理程序
    /// </summary>
    private void ResetAllButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("确定要重置所有按键映射到默认值吗？", "确认", 
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            foreach (var item in _keyMappings)
            {
                // 恢复为默认键
                item.NewKey = item.DefaultKey;
            }
        }
    }

    /// <summary>
    /// DataGrid 键盘事件处理，用于捕获按键并更新映射
    /// </summary>
    private void DataGrid_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // 如果没有选中的行，则忽略
        var dataGrid = sender as DataGrid;
        if (dataGrid?.SelectedItem is not KeyMappingItem selectedItem)
            return;

        // 忽略 IME 处理键
        if (e.Key == System.Windows.Input.Key.ImeProcessed)
            return;

        // 获取真实按键 (处理 Alt 等系统键)
        var key = (e.Key == System.Windows.Input.Key.System) ? e.SystemKey : e.Key;

        // 获取按键对应的字符串代码
        string keyCode = ConvertWpfKeyToJsCode(key);
        
        // 如果转换成功
        if (!string.IsNullOrEmpty(keyCode))
        {
            // 更新新按键显示
            selectedItem.NewKey = ConvertKeyCodeToDisplayName(keyCode);
            
            // 标记事件已处理，防止 DataGrid 默认行为
            e.Handled = true;
        }
    }

    /// <summary>
    /// 将 WPF Key 枚举转换为 JS code 字符串
    /// </summary>
    private string ConvertWpfKeyToJsCode(System.Windows.Input.Key key)
    {
        // 处理 A-Z
        if (key >= System.Windows.Input.Key.A && key <= System.Windows.Input.Key.Z)
        {
            return $"Key{key}";
        }
        
        // 处理 0-9 (主键盘)
        if (key >= System.Windows.Input.Key.D0 && key <= System.Windows.Input.Key.D9)
        {
            return $"Digit{key - System.Windows.Input.Key.D0}";
        }
        
        // 处理方向键及其他常用键
        return key switch
        {
            System.Windows.Input.Key.Up => "ArrowUp",
            System.Windows.Input.Key.Down => "ArrowDown",
            System.Windows.Input.Key.Left => "ArrowLeft",
            System.Windows.Input.Key.Right => "ArrowRight",
            System.Windows.Input.Key.Space => "Space",
            System.Windows.Input.Key.Enter => "Enter",
            System.Windows.Input.Key.Tab => "Tab",
            System.Windows.Input.Key.Back => "Backspace",
            System.Windows.Input.Key.Escape => "Escape",
            System.Windows.Input.Key.Delete => "Delete",
            System.Windows.Input.Key.Home => "Home",
            System.Windows.Input.Key.End => "End",
            System.Windows.Input.Key.PageUp => "PageUp",
            System.Windows.Input.Key.PageDown => "PageDown",
            System.Windows.Input.Key.LeftShift => "ShiftLeft",
            System.Windows.Input.Key.RightShift => "ShiftRight",
            System.Windows.Input.Key.LeftCtrl => "ControlLeft",
            System.Windows.Input.Key.RightCtrl => "ControlRight",
            System.Windows.Input.Key.LeftAlt => "AltLeft",
            System.Windows.Input.Key.RightAlt => "AltRight",
            // F1-F12
            >= System.Windows.Input.Key.F1 and <= System.Windows.Input.Key.F12 => key.ToString(),
            // 默认返回 ToString (可能不完全匹配 JS code，但大部分够用)
            _ => key.ToString()
        };
    }

    #endregion
}

#endregion
