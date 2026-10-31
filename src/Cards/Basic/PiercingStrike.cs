using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Cards;

/// <summary>
/// 穿刺攻击：造成穿刺伤害，自己每有 4 点格挡伤害 +1。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class PiercingStrike : KnightCard
{
    /// <summary>每多少点格挡加 1 点伤害。</summary>
    private const int BlockPerBonus = 4;

    /// <summary>
    /// 创建穿刺攻击。
    /// </summary>
    public PiercingStrike() : base(0, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        MakeCalculatedDamage(6, (card, _) => card.Owner.Creature.Block / BlockPerBonus, 1, KnightDamage.PierceMove);

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(KnightKeywords.Pierce)];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        await DamageCmd.Attack(DynamicVars.CalculatedDamage).FromCardCompatibility(this, cardPlay).Targeting(cardPlay.Target)
            .AsPierce()
            .WithHitFx(KnightDamage.SlashVfx)
            .Execute(choiceContext);
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(2m);
    }
}
