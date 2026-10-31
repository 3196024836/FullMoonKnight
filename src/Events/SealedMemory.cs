using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Cards;

namespace FullMoonKnight.Events;

/// <summary>
/// 封存的记忆（原作 34，CrazyChooseStart）：3 张神龛牌选 1——沉睡的发条骑士、致命餐叉、弹簧拳套。必须选。
/// 原作异变模式第 2 章；模组第 2 幕低概率。
/// </summary>
public sealed class SealedMemory : KnightEvent
{

    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        CardModel[] shrines =
        [
            NewCard(ModelDb.Card<SleepingClockworkKnight>()), NewCard(ModelDb.Card<DeadlyFork>()),
            NewCard(ModelDb.Card<SpringGlove>()),
        ];
        return shrines.Select((card, i) => CardOption(card, () => Take(card), InitialPage, $"TAKE{i}", -1, i.ToString()))
            .ToList();
    }

    /// <summary>
    /// 拿走选中的牌。
    /// </summary>
    /// <param name="card">牌。</param>
    /// <returns>异步任务。</returns>
    private async Task Take(CardModel card)
    {
        await AddToDeck(card);
        Finish("DONE");
    }
}
