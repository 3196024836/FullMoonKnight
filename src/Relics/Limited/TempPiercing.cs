using System.Reflection;

using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;

using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Relics;

/// <summary>
/// 限次·穿透伤害：下次战斗你造成的伤害都是穿刺伤害（无视格挡）。
/// 没有改写伤害属性的钩子，用补丁在伤害结算入口给持有者的伤害加上无视格挡。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class TempPiercing : KnightTemporaryRelic
{
    /// <summary>
    /// 伤害结算入口：持有穿透伤害的玩家造成的伤害改为无视格挡。
    /// </summary>
    [HarmonyPatch]
    private static class DamagePatch
    {
        /// <summary>补丁和伤害调用使用同一分支入口。</summary>
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() => KnightDamage.DamageTarget;

        /// <summary>
        /// 改写伤害属性。
        /// </summary>
        /// <param name="props">伤害属性。</param>
        /// <param name="dealer">伤害来源。</param>
        [HarmonyPrefix]
        private static void Prefix(ref ValueProp props, Creature? dealer)
        {
            if (dealer?.Player?.GetRelic<TempPiercing>() != null)
            {
                props |= ValueProp.Unblockable;
            }
        }
    }
}
