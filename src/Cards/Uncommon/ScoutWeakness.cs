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
/// 弱点侦查：下一张造成伤害的牌伤害翻倍；升级后还抽 1 张牌。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class ScoutWeakness : KnightCard
{
    /// <summary>
    /// 创建弱点侦查。
    /// </summary>
    public ScoutWeakness() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<PoisedPower>(1m), new CardsVar(1)];

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<PoisedPower>()];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<PoisedPower>(choiceContext, Owner.Creature, DynamicVars["PoisedPower"].BaseValue,
            Owner.Creature, this);
        if (DynamicVars.Cards.BaseValue > 0m)
        {
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        }
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
