using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 狂战斧：你打出打击牌后，把它的临时复制加入手牌。狂战斧生成的复制不再触发。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class BerserkerAxe : KnightEquipmentCard<BerserkerAxePower>
{
    /// <summary>
    /// 创建狂战斧。
    /// </summary>
    public BerserkerAxe() : base(CardRarity.Uncommon)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(KnightKeywords.Temporary)];

    /// <inheritdoc />
    protected override decimal EquipAmount => DynamicVars.Cards.BaseValue;

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}

/// <summary>
/// 狂战斧装备：层数为每次生成的复制张数。
/// </summary>
public sealed class BerserkerAxePower : KnightEquipmentPower
{
    /// <summary>狂战斧生成的复制，打出时不再触发（首次使用时创建，避免克隆共享同一集合）。</summary>
    private HashSet<CardModel>? generated;

    /// <summary>
    /// 持有者打出打击牌后，把它的临时复制加入手牌。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="cardPlay">出牌信息。</param>
    /// <returns>异步任务。</returns>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 打出的牌
        CardModel card = cardPlay.Card;
        if (card.Owner.Creature != Owner || !card.Tags.Contains(CardTag.Strike))
        {
            return;
        }
        AssertMutable();
        generated ??= [];
        // 复制被打出多次（例如海克斯的双刀流让攻击打两次）也不触发，所以不从集合里移除
        if (generated.Contains(card))
        {
            return;
        }
        Flash();
        for (int i = 0; i < Amount; i++)
        {
            // 新复制
            CardModel copy = KnightCards.CombatCopy(card, Owner.Player!);
            generated.Add(copy);
            await KnightCards.AddTemporary(copy, PileType.Hand, Owner.Player!);
        }
    }
}
