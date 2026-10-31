using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Cards;

/// <summary>
/// 苦战：把手牌中每张攻击牌的临时复制洗入抽牌堆，抽牌。
/// 原作 1 费；效果都在之后的回合，占掉女骑士唯一的 1 点能量太亏（单卡模拟 -2.6 层），改为 0 费。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class GrimBattle : KnightCard
{
    /// <summary>
    /// 创建苦战。
    /// </summary>
    public GrimBattle() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(KnightKeywords.Temporary)];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 手牌中的攻击牌
        List<CardModel> attacks = PileType.Hand.GetPile(Owner).Cards.Where(c => c.Type == CardType.Attack).ToList();
        foreach (CardModel attack in attacks)
        {
            await KnightCards.AddTemporary(attack.CreateClone(), PileType.Draw, Owner, CardPilePosition.Random);
        }
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
