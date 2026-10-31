using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 幸运币：获得时 +1 金币。每场战斗闪避第一次伤害。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class LuckyCoin : KnightRelic
{
    private bool _dodgedThisCombat;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override bool HasUponPickupEffect => true;

    /// <inheritdoc />
    public override async Task AfterObtained()
    {
        await PlayerCmd.GainGold(1, Owner);
    }

    /// <inheritdoc />
    public override Task BeforeCombatStart()
    {
        _dodgedThisCombat = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override decimal ModifyHpLostBeforeOsty(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        // 只在战斗中闪避（事件、诅咒的扣血代价不算）
        if (target != Owner.Creature || target.CombatState == null || _dodgedThisCombat || amount <= 0m)
        {
            return amount;
        }

        _dodgedThisCombat = true;
        Flash();
        return 0m;
    }
}
