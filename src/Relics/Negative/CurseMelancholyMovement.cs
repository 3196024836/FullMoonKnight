using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 负面·忧郁乐章（原作"探索技能和战斗技能的冷却增加"，按"顽强"主题改为负面效果）：
/// 你的牌每次给予的格挡 -1。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class CurseMelancholyMovement : KnightRelic
{
    /// <summary>每次格挡减少量。</summary>
    private const decimal BlockPenalty = 1m;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props,
        CardModel? cardSource, CardPlay? cardPlay)
    {
        return target == Owner.Creature && cardSource?.Owner == Owner ? -BlockPenalty : 0m;
    }
}
