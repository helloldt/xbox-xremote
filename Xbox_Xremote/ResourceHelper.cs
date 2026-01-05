using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Wpf_webxbox;

/// <summary>
/// 资源帮助类，用于读取嵌入式资源
/// </summary>
public static class ResourceHelper
{
    /// <summary>
    /// 异步读取嵌入的脚本文件
    /// </summary>
    /// <param name="scriptName">脚本文件名（不含路径，例如 "gamepad_simulator.js"）</param>
    /// <returns>脚本内容</returns>
    public static async Task<string> ReadEmbeddedScriptAsync(string scriptName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        // 资源名称格式：项目名.文件夹名.文件名
        // 注意：默认命名空间通常与项目名一致，但如果文件夹包含子文件夹，需要用点号分隔
        // 对于 Scripts/gamepad_simulator.js，资源名应该是 Xbox_Xremote.Scripts.gamepad_simulator.js
        var resourceName = $"Xbox_Xremote.Scripts.{scriptName}";

        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
            {
                throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");
            }

            using (var reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
            }
        }
    }
}
