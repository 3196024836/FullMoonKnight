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
/// 小丑戏法：给予目标恶作剧（原作是往敌人牌组塞恶作剧牌，按 D25 改为层数）。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class ClownTrick : KnightCard
{
    /// <summary>
    /// 创建小丑戏法。
    /// </summary>
    public ClownTrick() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<MischiefPower>(2m)];

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MischiefPower>()];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        await PowerCmd.Apply<MischiefPower>(choiceContext, cardPlay.Target, DynamicVars["MischiefPower"].BaseValue,
            Owner.Creature, this);
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars["MischiefPower"].UpgradeValueBy(1m);
    }
}
