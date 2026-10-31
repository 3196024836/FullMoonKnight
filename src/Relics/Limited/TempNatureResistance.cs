using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 限次·自然抗性（原作"下次战斗受到的元素伤害减半"，尖塔敌人没有元素伤害）：下次战斗受到的伤害减少 25%。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class TempNatureResistance : KnightTemporaryRelic
{
    /// <summary>伤害倍率。</summary>
    private const decimal DamageMultiplier = 0.75m;

    /// <inheritdoc />
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        return target == Owner.Creature ? DamageMultiplier : 1m;
    }
}
