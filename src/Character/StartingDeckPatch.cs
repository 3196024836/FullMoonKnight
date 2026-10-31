using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace FullMoonKnight.Character;

/// <summary>
/// 女骑士初始牌组含一张预先升级的打击+；原生流程只复制规范牌，这里改为按 Knight.StartingDeckSpec 生成。
/// </summary>
[HarmonyPatch(typeof(Player), "PopulateStartingDeck")]
public static class StartingDeckPatch
{
    /// <summary>
    /// 女骑士玩家改用带升级标记的牌组明细；其他角色走原逻辑。
    /// </summary>
    /// <param name="__instance">正在初始化的玩家。</param>
    /// <returns>是否继续执行原方法。</returns>
    public static bool Prefix(Player __instance)
    {
        if (__instance.Character is not Knight)
        {
            return true;
        }

        // 生成的可变牌
        List<CardModel> cards = [];
        foreach ((CardModel canonical, bool upgraded) in Knight.StartingDeckSpec)
        {
            // 可变副本
            CardModel card = canonical.ToMutable();
            card.FloorAddedToDeck = 1;
            if (upgraded)
            {
                card.UpgradeInternal();
                card.FinalizeUpgradeInternal();
            }
            cards.Add(card);
        }
        __instance.PopulateDeck(cards);
        return false;
    }
}
