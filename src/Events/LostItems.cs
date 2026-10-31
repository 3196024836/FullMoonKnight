using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Events.Data;

namespace FullMoonKnight.Events;

/// <summary>
/// 遗落的道具（原作 50，DropSTNPanel）：从悔恨、创造之力里抽 1 个掉落祝福，和 1 张稀有牌二选一。必须选。
/// 原作另外两个掉落（剧团调查、斯坦快报）依赖异装/装备数量，不做。
/// </summary>
public sealed class LostItems : KnightEvent
{
    /// <summary>掉落祝福（原作编号）。</summary>
    private static readonly int[] Drops = [14001, 14002];

    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        List<EventOption> options = [BlessingOption(Drops[Rng.NextInt(Drops.Length)], TakeRelic)];
        // 稀有牌：自己卡池里随机一张
        List<CardModel> rares = Owner!.Character.CardPool
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.Rarity == CardRarity.Rare)
            .ToList();
        if (rares.Count > 0)
        {
            CardModel card = NewCard(rares[Rng.NextInt(rares.Count)]);
            options.Add(CardOption(card, () => TakeCard(card), InitialPage, "CARD"));
        }
        return options;
    }

    /// <summary>
    /// 拿祝福。
    /// </summary>
    /// <param name="relic">祝福。</param>
    /// <returns>异步任务。</returns>
    private async Task TakeRelic(RelicModel relic)
    {
        await Obtain(relic);
        Finish("DONE");
    }

    /// <summary>
    /// 拿牌。
    /// </summary>
    /// <param name="card">牌。</param>
    /// <returns>异步任务。</returns>
    private async Task TakeCard(CardModel card)
    {
        await AddToDeck(card);
        Finish("DONE");
    }
}
