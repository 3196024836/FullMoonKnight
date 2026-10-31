using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 限次·迷你护盾：下次战斗受到的伤害 -1。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class TempMiniShield : KnightTemporaryRelic
{
    /// <inheritdoc />
    public override decimal ModifyHpLostBeforeOsty(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner.Creature || amount <= 0m)
        {
            return amount;
        }

        return Math.Max(0m, amount - 1m);
    }
}
