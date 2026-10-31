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
/// 鞭炮：多段火焰伤害，打出后从牌组中永久移除。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class Firecrackers : KnightCard
{
    /// <summary>
    /// 创建鞭炮。
    /// </summary>
    public Firecrackers() : base(1, CardType.Attack, CardRarity.Common, TargetType.RandomEnemy)
    {
    }

    /// <inheritdoc />
    public override IEnumerable<CardKeyword> CanonicalKeywords => [BaseLibKeywords.Purge];

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(5m, KnightDamage.FireMove), new RepeatVar(5)];

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(KnightKeywords.Fire)];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 原作只有单个敌人；尖塔里每一段打随机敌人
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCardCompatibility(this, cardPlay).TargetingRandomOpponents(CombatState)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .AsFire()
            .WithHitFx(KnightDamage.FireVfx, KnightAudio.FireHit)
            .Execute(choiceContext);
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
    }
}
