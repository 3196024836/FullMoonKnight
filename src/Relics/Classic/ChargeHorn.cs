using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 冲锋号角（原作"普通攻击的伤害+2"）：打击类牌（带 Strike 标签）额外造成 2 点伤害，同原版的打击木人。
/// </summary>
[Pool(typeof(KnightRelicPool))]
public sealed class ChargeHorn : KnightRelic
{
    private const decimal BonusDamage = 2m;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Common;

    /// <inheritdoc />
    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (dealer != Owner.Creature || !props.IsPoweredAttack())
        {
            return 0m;
        }

        return cardSource != null && cardSource.Tags.Contains(CardTag.Strike) ? BonusDamage : 0m;
    }
}
