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
/// 怒火攻击：造成火焰伤害，已损失生命每有 5 点伤害 +1。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class WrathStrike : KnightCard
{
    /// <summary>每损失多少生命加 1 点伤害。</summary>
    private const int HpPerBonus = 4;

    /// <summary>
    /// 创建怒火攻击。
    /// </summary>
    public WrathStrike() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        MakeCalculatedDamage(7, (card, _) => (card.Owner.Creature.MaxHp - card.Owner.Creature.CurrentHp) / HpPerBonus,
            1, KnightDamage.FireMove);

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(KnightKeywords.Fire)];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        await DamageCmd.Attack(DynamicVars.CalculatedDamage).FromCardCompatibility(this, cardPlay).Targeting(cardPlay.Target)
            .AsFire()
            .WithHitFx(KnightDamage.FireVfx, KnightAudio.FireHit)
            .Execute(choiceContext);
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(3m);
    }
}
