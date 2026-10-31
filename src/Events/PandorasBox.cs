using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Events.Data;

namespace FullMoonKnight.Events;

/// <summary>
/// 潘多拉魔盒（原作 11，MagicBoxPanel）：2 个 6 级负面祝福（有好处也有坏处），选 1 或离开。原作 GetMagicBoxId。
/// </summary>
public sealed class PandorasBox : KnightEvent
{
    /// <summary>展示的祝福数。</summary>
    private const int OfferCount = 2;

    /// <summary>祝福等级。</summary>
    private const int Level = 6;

    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        List<EventOption> options = ClassicBlessings.Draw(Owner!, Rng, Level, Level, true, OfferCount)
            .Select(id => BlessingOption(id, Take))
            .ToList();
        options.Add(LeaveOption());
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
