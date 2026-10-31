using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Cards;

namespace FullMoonKnight.Mechanics;

/// <summary>
/// 战斗中由原版效果生成的装备牌基础费用为 1。
/// 装备是 0 费、整场生效、同名叠加的能力牌；创造性 AI（每回合一张随机能力牌）、你好世界（每回合一张随机普通牌）
/// 这类效果给别的角色的是要花 1~3 能量的牌，给女骑士却是每回合一件免费装备，越叠越多。
/// 改的是基础费用，所以能量药水这类"本回合免费"的效果照常生效；女骑士自己生成的临时装备不受影响，
/// 但原版效果再复制这件临时装备时，副本按原版生成算。
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.AddGeneratedCardsToCombat))]
public static class GeneratedEquipmentCostPatch
{
    /// <summary>生成装备的基础费用。</summary>
    public const int GeneratedCost = 1;

    /// <summary>
    /// 加入战斗前调整费用，并记下这些牌是战斗中生成的（灾厄据此不连锁）。
    /// 先把牌列表求值一次再写回，避免原方法再次枚举时重新随机生成。
    /// </summary>
    /// <param name="cards">要加入的牌。</param>
    [HarmonyPrefix]
    public static void Prefix(ref IEnumerable<CardModel> cards)
    {
        // 求值后的牌列表
        List<CardModel> list = cards.ToList();
        cards = list;
        foreach (CardModel card in list)
        {
            VanillaAdaptations.GeneratedCards.AddOrUpdate(card, card);
            if (card is not KnightCard knightCard)
            {
                continue;
            }
            if (!knightCard.GeneratedByKnight && KnightCards.IsEquipment(card)
                && card.EnergyCost.GetWithModifiers(CostModifiers.None) == 0)
            {
                card.EnergyCost.SetCustomBaseCost(GeneratedCost);
            }
            // 标记用过就清掉：双持、梦魇这类原版效果复制这张牌时，副本不再带着标记 0 费进场
            knightCard.GeneratedByKnight = false;
        }
    }
}
