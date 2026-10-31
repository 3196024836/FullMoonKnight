using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

namespace FullMoonKnight.Events;

/// <summary>
/// 恶魔交易（原作 19，ImpPanel）：血的代偿（金币 +66，生命上限 −10）、行动迟缓（金币 +66，2 张减速），选 1 或离开。
/// 原作第三项"记忆错乱"（−1 装备槽）不做。祝福数值照原作 1:1。
/// </summary>
public sealed class DemonicTrade : KnightEvent
{
    /// <summary>两份交易（原作编号）。</summary>
    private static readonly int[] Deals = [1085, 1087];

    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        List<EventOption> options = Deals.Select(id => BlessingOption(id, Take)).ToList();
        options.Add(LeaveOption());
        return options;
    }

    /// <summary>
    /// 接受交易：获得交易祝福（拾取时结算金币和代价）。
    /// </summary>
    /// <param name="relic">交易祝福。</param>
    /// <returns>异步任务。</returns>
    private async Task Take(RelicModel relic)
    {
        await Obtain(relic);
        Finish("DONE");
    }
}
