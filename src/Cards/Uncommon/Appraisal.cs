using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

using FullMoonKnight.Character;

namespace FullMoonKnight.Cards;

/// <summary>
/// 品鉴：消耗，从若干张随机女骑士牌中选 1 张永久加入牌组。可以使用 3 次，用完后从牌组移除。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class Appraisal : KnightCard
{
    /// <summary>总使用次数。</summary>
    private const int MaxUses = 3;

    /// <summary>剩余次数变量名。</summary>
    private const string UsesVar = "Uses";

    /// <summary>剩余使用次数（存档保存）。</summary>
    private int usesLeft = MaxUses;

    /// <summary>
    /// 创建品鉴。
    /// </summary>
    public Appraisal() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    /// <summary>剩余使用次数（存档保存）。</summary>
    [SavedProperty]
    public int FmkAppraisalUses
    {
        get
        {
            return usesLeft;
        }
        set
        {
            AssertMutable();
            usesLeft = value;
            DynamicVars[UsesVar].BaseValue = value;
        }
    }

    /// <inheritdoc />
    public override bool CanBeGeneratedInCombat => false;

    /// <inheritdoc />
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3), new IntVar(UsesVar, usesLeft)];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 候选牌（战斗用实例，只用来展示）
        List<CardModel> options = KnightCards.RandomKnightCards(Owner, DynamicVars.Cards.IntValue);
        CardModel? chosen = await KnightCards.ChooseOne(choiceContext, options, Owner, SelectionScreenPrompt);
        // 牌组里那张品鉴；复制品（天使手镜、无中生有、"打出两次"）没有牌组版，或者次数已经用完
        Appraisal? deckVersion = DeckVersion as Appraisal;
        if (deckVersion == null || deckVersion.FmkAppraisalUses <= 0)
        {
            // 不给永久牌，选中的牌以临时复制加入手牌
            if (chosen != null)
            {
                await KnightCards.AddTemporary(chosen, PileType.Hand, Owner);
            }
            return;
        }
        if (chosen != null)
        {
            // 牌组里的新牌
            CardModel deckCard = Owner.RunState.CreateCard(chosen.CanonicalInstance, Owner);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(deckCard, PileType.Deck));
        }

        FmkAppraisalUses--;
        deckVersion.FmkAppraisalUses--;
        if (deckVersion.FmkAppraisalUses <= 0 && deckVersion.Pile?.Type == PileType.Deck)
        {
            await CardPileCmd.RemoveFromDeck(deckVersion);
        }
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
