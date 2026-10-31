using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;

namespace FullMoonKnight.Mechanics;

/// <summary>
/// 装备牌在引擎里是 0 费能力牌。原版“打出能力牌时”触发的效果按每局几张能力牌设计：
/// 遗物木乃伊之手（随机一张手牌本回合免费）、游戏棋子（抽 1 张）、迷失鬼火（对所有敌人 8 点伤害），
/// 故障机器人的能力子程序（打出能力牌回能）、风暴（打出能力牌充能闪电，可经棱镜宝石等拿到）。
/// 女骑士有二十多张 0 费装备，每张都变成免费的能量、抽牌或伤害，所以装备牌不触发这些效果。
/// </summary>
[HarmonyPatch]
public static class EquipmentPowerRelicPatch
{
    /// <summary>
    /// 要处理的遗物出牌钩子。
    /// </summary>
    /// <returns>方法列表。</returns>
    public static IEnumerable<System.Reflection.MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(MummifiedHand), nameof(MummifiedHand.AfterCardPlayed));
        yield return AccessTools.Method(typeof(GamePiece), nameof(GamePiece.AfterCardPlayed));
        yield return AccessTools.Method(typeof(LostWisp), nameof(LostWisp.AfterCardPlayed));
        yield return AccessTools.Method(typeof(SubroutinePower), nameof(SubroutinePower.BeforeCardPlayed));
        yield return AccessTools.Method(typeof(StormPower), nameof(StormPower.BeforeCardPlayed));
    }

    /// <summary>
    /// 打出的是装备牌时跳过原效果。
    /// </summary>
    /// <param name="cardPlay">出牌信息。</param>
    /// <param name="__result">跳过时返回的任务。</param>
    /// <returns>不是装备牌时为 true（走原逻辑）。</returns>
    public static bool Prefix(CardPlay cardPlay, ref Task __result)
    {
        if (!cardPlay.Card.Keywords.Contains(KnightKeywords.Equip))
        {
            return true;
        }
        __result = Task.CompletedTask;
        return false;
    }
}
