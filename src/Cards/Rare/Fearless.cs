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
/// 无畏（原作 0 费，抽 3 张，下回合少抽 3 张）：抽牌，获得疲惫。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class Fearless : KnightCard
{
    /// <summary>
    /// 创建无畏。
    /// </summary>
    public Fearless() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3), new PowerVar<WearyPower>(3m)];

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
