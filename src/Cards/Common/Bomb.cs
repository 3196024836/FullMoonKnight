using BaseLib.Cards;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;
using FullMoonKnight.Visuals;

namespace FullMoonKnight.Cards;

/// <summary>
/// 炸弹：对所有敌人造成大量火焰伤害，打出后从牌组中永久移除。
/// 原作只有单个敌人；尖塔里炸弹波及全体。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class Bomb : KnightCard
{
    /// <summary>
    /// 创建炸弹。
    /// </summary>
    public Bomb() : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
    }

    /// <inheritdoc />
    public override IEnumerable<CardKeyword> CanonicalKeywords => [BaseLibKeywords.Purge];

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(30m, KnightDamage.FireMove)];

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(KnightKeywords.Fire)];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCardCompatibility(this, cardPlay).TargetingAllOpponents(CombatState)
            .AsFire()
            .WithHitFx(KnightDamage.FireVfx, KnightAudio.FireHit)
            .Execute(choiceContext);
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(10m);
    }
}
