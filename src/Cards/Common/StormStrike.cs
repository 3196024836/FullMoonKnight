using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;
using FullMoonKnight.Powers;
using FullMoonKnight.Visuals;

namespace FullMoonKnight.Cards;

/// <summary>
/// 风暴攻击：对所有敌人造成雷电伤害，本回合攻击伤害提高。
/// 原作只有单个敌人；尖塔多敌人战斗里女骑士缺少群体伤害，风暴改为打全体（伤害 8 降到 6）。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class StormStrike : KnightCard
{
    /// <summary>
    /// 创建风暴攻击。
    /// </summary>
    public StormStrike() : base(0, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(6m, KnightDamage.ThunderMove), new PowerVar<InspiredPower>(2m)];

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(KnightKeywords.Thunder), HoverTipFactory.FromPower<InspiredPower>()];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCardCompatibility(this, cardPlay).TargetingAllOpponents(CombatState)
            .AsThunder()
            .WithHitFx(KnightDamage.ThunderVfx, KnightAudio.ThunderHit)
            .Execute(choiceContext);
        await PowerCmd.Apply<InspiredPower>(choiceContext, Owner.Creature, DynamicVars["InspiredPower"].BaseValue,
            Owner.Creature, this);
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["InspiredPower"].UpgradeValueBy(1m);
    }
}
