using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;

namespace FullMoonKnight.Cards;

/// <summary>
/// 防御：获得格挡。原作没有这张牌，按尖塔初始牌组补上。
/// 0 费：和打击一样是基础动作，不占唯一的行动点；格挡因此比 1 费时低。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class Defend : KnightCard
{
    /// <summary>
    /// 创建防御。
    /// </summary>
    public Defend() : base(0, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
    }

    /// <inheritdoc />
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Defend];

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(10m, ValueProp.Move)];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
