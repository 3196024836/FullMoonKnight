using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Cards;
using FullMoonKnight.Character;

using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Relics;

/// <summary>
/// 正义审判：每个回合结束时，对随机敌人造成本场战斗你已造成伤害的 20%（不计正义审判自己的伤害）。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class JusticeRuling : KnightRelic
{
    /// <summary>伤害比例。</summary>
    private const decimal DamagePercent = 0.20m;

    /// <summary>本场战斗已造成的伤害（敌人失去的生命）。</summary>
    private decimal _totalDamageDealt;

    /// <summary>正在结算自己的伤害。</summary>
    private bool _judging;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override Task BeforeCombatStart()
    {
        _totalDamageDealt = 0m;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result,
        ValueProp props, Creature target, CardModel? cardSource)
    {
        if (!_judging && dealer == Owner.Creature && target.Side == CombatSide.Enemy)
        {
            _totalDamageDealt += result.UnblockedDamage;
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        // 判决伤害
        decimal damage = Math.Floor(_totalDamageDealt * DamagePercent);
        if (side != CombatSide.Player || damage <= 0m || KnightCards.RandomEnemy(Owner) is not { } enemy)
        {
            return;
        }
        Flash();
        _judging = true;
        try
        {
            await KnightDamage.Deal(choiceContext, enemy, damage, ValueProp.Unpowered, Owner.Creature, null);
        }
        finally
        {
            _judging = false;
        }
    }
}
