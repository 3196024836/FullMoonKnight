using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Cards;

/// <summary>
/// 突发奇想：从若干张随机女骑士牌中选 1 张，它的 1 张临时复制加入手牌，另 2 张洗入抽牌堆。
/// 原作 3 张都洗入抽牌堆；女骑士每回合只抽 2 张，效果来得太慢（单卡模拟 -3.4 层），改为 1 张立刻可用。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class Inspiration : KnightCard
{
    /// <summary>复制总张数（1 张进手牌，其余洗入抽牌堆）。</summary>
    private const int Copies = 3;

    /// <summary>
    /// 创建突发奇想。
    /// </summary>
    public Inspiration() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3), new DynamicVar("Copies", Copies)];

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(KnightKeywords.Temporary)];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 候选
        List<CardModel> options = KnightCards.RandomKnightCards(Owner, DynamicVars.Cards.IntValue);
        CardModel? chosen = await KnightCards.ChooseOne(choiceContext, options, Owner, SelectionScreenPrompt);
        if (chosen == null)
        {
            return;
        }
        await KnightCards.AddTemporary(chosen, PileType.Hand, Owner);
        for (int i = 1; i < Copies; i++)
        {
            await KnightCards.AddTemporary(chosen.CreateClone(), PileType.Draw, Owner, CardPilePosition.Random);
        }
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
