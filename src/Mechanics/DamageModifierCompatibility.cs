using System.Reflection;

using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace FullMoonKnight.Mechanics;

/// <summary>
/// 测试版在伤害修正钩子后增加了 CardPlay。只为本模组转接旧签名的覆盖实现，
/// 正式版继续使用原生虚调用；原版与其他模组的伤害修正不经过此转接。
/// </summary>
[HarmonyPatch]
public static class DamageModifierCompatibility
{
    /// <summary>本模组实现的旧版签名。</summary>
    private static readonly Type[] LegacyParameters =
        [typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel)];

    /// <summary>仅新分支需要桥接，避免正式版重复计算。</summary>
    [HarmonyPrepare]
    private static bool Prepare()
    {
        return typeof(AbstractModel).GetMethod(nameof(AbstractModel.ModifyDamageAdditive),
            [.. LegacyParameters, typeof(MegaCrit.Sts2.Core.Entities.Cards.CardPlay)]) != null;
    }

    /// <summary>两个新增出牌上下文的原生钩子。</summary>
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(AbstractModel), nameof(AbstractModel.ModifyDamageAdditive));
        yield return AccessTools.Method(typeof(AbstractModel), nameof(AbstractModel.ModifyDamageMultiplicative));
    }

    /// <summary>在原生默认实现后调用本模组对应的旧签名；没有覆盖的模型保持原生默认值。</summary>
    [HarmonyPostfix]
    private static void Dispatch(AbstractModel __instance, MethodBase __originalMethod, object[] __args,
        ref decimal __result)
    {
        Type type = __instance.GetType();
        if (type.Assembly != typeof(DamageModifierCompatibility).Assembly) return;
        MethodInfo? implementation = type.GetMethod(__originalMethod.Name, LegacyParameters);
        if (implementation == null || implementation.DeclaringType?.Assembly != type.Assembly) return;
        __result = (decimal)implementation.Invoke(__instance, __args[..LegacyParameters.Length])!;
    }
}
