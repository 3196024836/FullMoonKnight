using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Events.Data;

namespace FullMoonKnight.Events;

/// <summary>
/// 仙女祝福（原作 10，BansheePanel）：3 个本章等级的祝福（第 1、2 章 6 级，第 3 章 9 级），选 1。必须选。
/// 原作 GetLostThingId.GetBansheeId。
/// </summary>
public sealed class FairyBlessing : KnightEvent
{
    /// <summary>展示的祝福数。</summary>
    private const int OfferCount = 3;

    /// <summary>每章的祝福等级。</summary>
    private static readonly int[] Levels = [6, 6, 9];

    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        int level = Levels[Chapter - 1];
        return ClassicBlessings.Draw(Owner!, Rng, level, level, false, OfferCount)
            .Select(id => BlessingOption(id, Take))
            .ToList();
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
