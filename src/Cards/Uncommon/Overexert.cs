using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

using FullMoonKnight.Character;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 力量透支：抽牌，下回合少抽 1 张。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class Overexert : KnightCard
{
    /// <summary>
    /// 创建力量透支。
    /// </summary>
    public Overexert() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2), new PowerVar<WearyPower>(1m)];

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<WearyPower>()];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await KnightCards.DrawFromPile(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        await PowerCmd.Apply<WearyPower>(choiceContext, Owner.Creature, DynamicVars["WearyPower"].BaseValue,
            Owner.Creature, this);
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
