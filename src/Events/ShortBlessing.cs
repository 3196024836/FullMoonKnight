using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Relics;

namespace FullMoonKnight.Events;

/// <summary>
/// 短暂的祝福（原作 20，没做完的事件；EventPixie.txt 的"下次战斗"增益）：从限次祝福里随机 3 个没有的，选 1。必须选。
/// </summary>
public sealed class ShortBlessing : KnightEvent
{
    /// <summary>展示的祝福数。</summary>
    private const int OfferCount = 3;

    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        // 候选：还没有的限次祝福
        List<RelicModel> candidates = KnightEventRelics.Temporary.Where(r => Owner!.GetRelicById(r.Id) == null).ToList();
        List<EventOption> options = [];
        while (options.Count < OfferCount && candidates.Count > 0)
        {
            int index = Rng.NextInt(candidates.Count);
            RelicModel relic = candidates[index].ToMutable();
            candidates.RemoveAt(index);
            options.Add(RelicOption(relic, () => Take(relic)));
        }
        if (options.Count == 0)
        {
            options.Add(LeaveOption());
        }
        return options;
    }

    /// <summary>
    /// 获得选中的祝福。
    /// </summary>
    /// <param name="relic">祝福。</param>
    /// <returns>异步任务。</returns>
    private async Task Take(RelicModel relic)
    {
        await Obtain(relic);
        Finish("DONE");
    }
}
