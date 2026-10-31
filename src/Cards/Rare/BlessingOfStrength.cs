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
/// 力量祝福：本回合攻击伤害提高。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class BlessingOfStrength : KnightCard
{
    /// <summary>
    /// 创建力量祝福。
    /// </summary>
    public BlessingOfStrength() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<InspiredPower>(5m)];

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<InspiredPower>()];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<InspiredPower>(choiceContext, Owner.Creature, DynamicVars["InspiredPower"].BaseValue,
            Owner.Creature, this);
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars["InspiredPower"].UpgradeValueBy(2m);
    }
}
