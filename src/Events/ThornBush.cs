using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Relics;

namespace FullMoonKnight.Events;

/// <summary>
/// 荆棘丛（原作 40，ThornBushPanel）：2 个代价选 1 才能通过（失去生命、减速、付金币、失去生命上限）。
/// 第 2 幕用小代价、第 3 幕用大代价（生命 −10/−20、1 张减速、金币 −10/−20、上限 −2/−5，数值照原作 1:1）。
/// 必须选；付不起的锁定。
/// </summary>
public sealed class ThornBush : KnightEvent
{
    /// <summary>展示的代价数。</summary>
    private const int OfferCount = 2;

    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        // 本幕的代价
        bool greater = Chapter >= 3;
        List<KnightCostRelic> candidates = KnightEventRelics.Costs.Where(c => c.Greater == greater).ToList();
        List<EventOption> options = [];
        while (options.Count < OfferCount && candidates.Count > 0)
        {
            int index = Rng.NextInt(candidates.Count);
            KnightCostRelic cost = (KnightCostRelic)candidates[index].ToMutable();
            candidates.RemoveAt(index);
            cost.Owner = Owner!;
            options.Add(RelicOption(cost, cost.CanPay(Owner!) ? () => Pay(cost) : null));
        }
        // 都付不起时也要能走
        if (options.All(o => o.IsLocked))
        {
            options.Add(LeaveOption());
        }
        return options;
    }

    /// <summary>
    /// 付出代价（拾取时当场结算）。
    /// </summary>
    /// <param name="cost">代价。</param>
    /// <returns>异步任务。</returns>
    private async Task Pay(RelicModel cost)
    {
        await Obtain(cost);
        Finish("DONE");
    }
}
