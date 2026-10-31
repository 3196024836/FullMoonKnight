using System.Reflection;

using BaseLib.Config;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace FullMoonKnight;

/// <summary>
/// 模组入口：注册 Godot 脚本并应用全部 Harmony 补丁。
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class KnightMod
{
    // ===== 常量 =====

    /// <summary>模组 ID，与清单和资源根目录 res://FullMoonKnight/ 一致。</summary>
    public const string ModId = "FullMoonKnight";

    /// <summary>资源根目录。</summary>
    public const string ResRoot = "res://" + ModId + "/";

    // ===== 入口 =====

    /// <summary>
    /// 游戏加载本模组程序集后调用。
    /// </summary>
    public static void Initialize()
    {
        // 本程序集
        Assembly assembly = Assembly.GetExecutingAssembly();

        // 游戏不会为模组程序集注册 Godot 脚本，需手动登记，场景里的脚本节点才能实例化
        ScriptManagerBridge.LookupScriptsInAssembly(assembly);
        new Harmony(ModId).PatchAll(assembly);
        ModConfigRegistry.Register(ModId, new FullMoonConfig());
        Log.Info($"[{ModId}] initialized");
    }
}
