using BaseLib.Config;

using MegaCrit.Sts2.Core.Runs;

using FullMoonKnight.Character;

namespace FullMoonKnight;

/// <summary>
/// 月圆之夜事件的出现方式。
/// </summary>
public enum FullMoonEventMode
{
    /// <summary>本局有女骑士（单人或联机）才出现。</summary>
    Auto,

    /// <summary>总是出现。</summary>
    Always,

    /// <summary>不出现。</summary>
    Off,
}

/// <summary>
/// 模组设置（模组设置页）。文字在 settings_ui 表：FULLMOONKNIGHT-&lt;选项&gt;.title、FULLMOONKNIGHT-&lt;选项&gt;.&lt;取值&gt;。
/// </summary>
public sealed class FullMoonConfig : SimpleModConfig
{
    /// <summary>月圆之夜事件：自动（默认）/ 总是 / 关闭。</summary>
    public static FullMoonEventMode FullMoonEvents { get; set; } = FullMoonEventMode.Auto;

    /// <summary>
    /// 本局是否开启月圆之夜事件。联机时各端设置可能不同，为了不失去同步一律按"自动"。
    /// </summary>
    /// <param name="runState">本局。</param>
    /// <returns>开启时为 true。</returns>
    public static bool EventsEnabled(IRunState runState)
    {
        FullMoonEventMode mode = runState.Players.Count > 1 ? FullMoonEventMode.Auto : FullMoonEvents;
        return mode switch
        {
            FullMoonEventMode.Always => true,
            FullMoonEventMode.Off => false,
            _ => runState.Players.Any(p => p.Character is Knight),
        };
    }
}
