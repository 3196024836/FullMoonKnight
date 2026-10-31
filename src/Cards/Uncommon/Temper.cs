using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Cards;

/// <summary>
/// 暴躁：查看牌组中随机几张牌，选择 1 张，把它的临时复制加入手牌。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class Temper : KnightCard
{
    /// <summary>
    /// 创建暴躁。
    /// </summary>
    public Temper() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(KnightKeywords.Temporary)];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 牌组里随机几张（不含暴躁本身）
        List<CardModel> options = PileType.Deck.GetPile(Owner).Cards
            .Where(c => c is not Temper)
            .TakeRandom(DynamicVars.Cards.IntValue, Owner.RunState.Rng.CombatCardGeneration)
            .Select(c => KnightCards.CombatCopy(c, Owner))
            .ToList();
        if (options.Count == 0)
        {
            return;
        }
        CardModel? chosen = await KnightCards.ChooseOne(choiceContext, options, Owner, SelectionScreenPrompt);
        if (chosen != null)
        {
            await KnightCards.AddTemporary(chosen, PileType.Hand, Owner);
        }
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
