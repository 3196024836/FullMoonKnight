using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 限次·潜能爆发：下次战斗的第一回合，你造成的伤害翻倍。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class TempPotentialBurst : KnightTemporaryRelic
{
    /// <inheritdoc />
    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (dealer != Owner.Creature || !props.IsPoweredAttack() || Owner.PlayerCombatState?.TurnNumber != 1)
        {
            return 1m;
        }

        return 2m;
    }
}
