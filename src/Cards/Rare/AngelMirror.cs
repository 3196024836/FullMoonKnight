using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 天使手镜：回合开始时，把牌组中随机 1 张非装备牌的临时复制加入手牌。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class AngelMirror : KnightEquipmentCard<AngelMirrorPower>
{
    /// <summary>
    /// 创建天使手镜。
    /// </summary>
    public AngelMirror() : base(CardRarity.Rare)
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
/// 天使手镜装备：层数为每回合复制的张数。
/// </summary>
public sealed class AngelMirrorPower : KnightEquipmentPower
{
    /// <summary>
    /// 持有者回合开始时，复制牌组中的随机非装备牌到手牌。
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
        // 牌组中能打出的非装备牌（诅咒、状态、不能打出的牌复制过来永远不会消耗，会越积越多）
        List<CardModel> picks = PileType.Deck.GetPile(player).Cards
            .Where(c => !KnightCards.IsEquipment(c) && c.Type is not (CardType.Curse or CardType.Status)
                && !c.Keywords.Contains(CardKeyword.Unplayable))
            .TakeRandom(Amount, player.RunState.Rng.CombatCardGeneration)
            .ToList();
        if (picks.Count == 0)
        {
            return;
        }
        Flash();
        foreach (CardModel card in picks)
        {
            await KnightCards.AddTemporary(KnightCards.CombatCopy(card, player), PileType.Hand, player);
        }
    }
}
