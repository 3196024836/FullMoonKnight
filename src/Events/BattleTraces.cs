using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;

using FullMoonKnight.Cards;

namespace FullMoonKnight.Events;

/// <summary>
/// 战斗痕迹（原作 37，地图事件卡）：调查 → 获得"外婆的拐杖"（不能打出）。持有它时第 3 幕可能出现可疑的痕迹。
/// 原作是线索剧情，开关关着；模组第 3 幕低概率。
/// </summary>
public sealed class BattleTraces : KnightEvent
{
    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        return [Choice(Investigate, InitialPage, "INVESTIGATE", HoverTipFactory.FromCard<GrandmasCrutch>())];
    }

    /// <summary>
    /// 调查：拿到外婆的拐杖。
    /// </summary>
    /// <returns>异步任务。</returns>
    private async Task Investigate()
    {
        await AddToDeck(Owner!.RunState.CreateCard<GrandmasCrutch>(Owner));
        Finish("INVESTIGATE");
    }
}
