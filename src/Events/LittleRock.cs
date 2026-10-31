using MegaCrit.Sts2.Core.Events;

namespace FullMoonKnight.Events;

/// <summary>
/// 小石头（原作 28，地图事件卡）：踢开，没有任何效果。原作只在新手引导出现。
/// </summary>
public sealed class LittleRock : KnightEvent
{
    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        return [Choice(Kick, InitialPage, "KICK")];
    }

    /// <summary>
    /// 踢开。
    /// </summary>
    /// <returns>异步任务。</returns>
    private Task Kick()
    {
        Finish("KICK");
        return Task.CompletedTask;
    }
}
