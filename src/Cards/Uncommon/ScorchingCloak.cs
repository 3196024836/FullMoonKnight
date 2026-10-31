using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 炙热斗篷：敌人每次攻击你后，对它造成火焰伤害。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class ScorchingCloak : KnightEquipmentCard<ScorchingCloakPower>
{
    /// <summary>
    /// 创建炙热斗篷。
    /// </summary>
    public ScorchingCloak() : base(CardRarity.Uncommon)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(5m, ValueProp.Unpowered | KnightDamage.Fire)];

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(KnightKeywords.Fire)];

    /// <inheritdoc />
    protected override decimal EquipAmount => DynamicVars.Damage.BaseValue;

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}

/// <summary>
/// 炙热斗篷装备：层数为每次火焰伤害。
/// </summary>
public sealed class ScorchingCloakPower : KnightEquipmentPower
{
    /// <summary>
    /// 敌人的攻击命中持有者后，对攻击者造成火焰伤害（每次攻击一次）。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="command">攻击命令。</param>
    /// <returns>异步任务。</returns>
    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        // 攻击者
        Creature? attacker = command.Attacker;
        if (attacker == null || attacker.Side == Owner.Side || !attacker.IsAlive
            || !command.Results.SelectMany(r => r).Any(r => r.Receiver == Owner))
        {
            return;
        }
        Flash();
        await KnightDamage.Deal(choiceContext, attacker, Amount,
            ValueProp.Unpowered | KnightDamage.Fire | KnightDamage.Equipment, Owner, null);
    }
}
