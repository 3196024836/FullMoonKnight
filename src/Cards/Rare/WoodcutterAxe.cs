using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 樵夫之斧：回合开始时，获得 1 张随机临时攻击牌。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class WoodcutterAxe : KnightEquipmentCard<WoodcutterAxePower>
{
    /// <summary>
    /// 创建樵夫之斧。
    /// </summary>
    public WoodcutterAxe() : base(CardRarity.Rare)
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
/// 樵夫之斧装备：层数为每回合获得的攻击牌张数。
/// </summary>
public sealed class WoodcutterAxePower : KnightEquipmentPower
{
    /// <summary>
    /// 持有者回合开始时，获得随机临时攻击牌。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="player">回合开始的玩家。</param>
    /// <returns>异步任务。</returns>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
        {
            return;
        }
        Flash();
        foreach (CardModel card in KnightCards.RandomKnightCards(player, Amount, c => c.Type == CardType.Attack))
        {
            await KnightCards.AddTemporary(card, PileType.Hand, player);
        }
    }
}
