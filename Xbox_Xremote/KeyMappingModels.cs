using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Wpf_webxbox;

/// <summary>
/// 按键映射定义类 (用于 JSON 序列化)
/// </summary>
public class KeyMappingDefinition
{
    /// <summary>
    /// 获取或设置输入类型 (button/axis)
    /// </summary>
    public string Type { get; set; } = "";

    /// <summary>
    /// 获取或设置索引
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// 获取或设置值 (仅用于 Axis)
    /// </summary>
    public string Value { get; set; } = "";

    /// <summary>
    /// 获取或设置功能描述
    /// </summary>
    public string Function { get; set; } = "";
}

/// <summary>
/// 按键映射项数据模型 (用于 UI 绑定)
/// </summary>
public class KeyMappingItem : INotifyPropertyChanged
{
    private string _newKey = "";

    /// <summary>
    /// 获取或设置功能描述
    /// </summary>
    public string Function { get; set; } = "";

    /// <summary>
    /// 获取或设置当前按键显示名称
    /// </summary>
    public string CurrentKey { get; set; } = "";

    /// <summary>
    /// 获取或设置默认按键显示名称
    /// </summary>
    public string DefaultKey { get; set; } = "";

    /// <summary>
    /// 获取或设置新的按键显示名称
    /// </summary>
    public string NewKey
    {
        get => _newKey;
        set
        {
            if (_newKey != value)
            {
                _newKey = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 获取或设置输入类型
    /// </summary>
    public string Type { get; set; } = "";

    /// <summary>
    /// 获取或设置索引
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// 获取或设置值（可选）
    /// </summary>
    public string Value { get; set; } = "";

    /// <summary>
    /// 获取或设置原始按键代码
    /// </summary>
    public string OriginalKey { get; set; } = "";

    /// <summary>
    /// 属性更改事件
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 触发属性更改事件
    /// </summary>
    /// <param name="propertyName">属性名称</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
