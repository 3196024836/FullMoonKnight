using System;

using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace FullMoonKnight.Visuals;

/// <summary>
/// 女骑士眨眼：战斗、休息处、商店里的女骑士骨骼每隔 3–5 秒随机在第 2 轨播放一次 blink（只切换闭眼插槽）。
/// 游戏只在第 0 轨切换动作，所以眨眼不受攻击、受击等动作影响；死亡后不再眨眼。只影响本地显示。
/// </summary>
[HarmonyPatch]
public static class KnightBlink
{
    /// <summary>女骑士骨骼数据的资源路径，用来认出女骑士的骨骼节点。</summary>
    private static readonly string SkeletonDataPath = KnightMod.ResRoot + "animations/knight/knight_skel_data.tres";

    /// <summary>眨眼动作名。</summary>
    private const string BlinkAnimation = "blink";

    /// <summary>死亡动作名（播放后停止眨眼）。</summary>
    private const string DieAnimation = "die";

    /// <summary>眨眼所在的轨道。</summary>
    private const int BlinkTrack = 2;

    /// <summary>两次眨眼的最短间隔（秒）。</summary>
    private const double MinInterval = 3.0;

    /// <summary>两次眨眼的最长间隔（秒）。</summary>
    private const double MaxInterval = 5.0;

    /// <summary>已开始眨眼的标记（节点元数据），避免重复排程。</summary>
    private static readonly StringName StartedMeta = "fmk_blink_started";

    /// <summary>
    /// 战斗里的生物外观就绪后，女骑士开始眨眼。
    /// </summary>
    /// <param name="__instance">生物外观节点。</param>
    [HarmonyPatch(typeof(NCreatureVisuals), nameof(NCreatureVisuals._Ready))]
    [HarmonyPostfix]
    public static void OnCreatureReady(NCreatureVisuals __instance)
    {
        TryStart(__instance.GetCurrentBody());
    }

    /// <summary>
    /// 休息处的角色就绪后，女骑士开始眨眼。
    /// </summary>
    /// <param name="__instance">休息处角色节点。</param>
    [HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter._Ready))]
    [HarmonyPostfix]
    public static void OnRestSiteReady(NRestSiteCharacter __instance)
    {
        TryStart(__instance.GetNodeOrNull<Node2D>("SpineSprite"));
    }

    /// <summary>
    /// 商店里的角色就绪后，女骑士开始眨眼。
    /// </summary>
    /// <param name="__instance">商店角色节点。</param>
    [HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter._Ready))]
    [HarmonyPostfix]
    public static void OnMerchantReady(NMerchantCharacter __instance)
    {
        TryStart(__instance.GetNodeOrNull<Node2D>("SpineSprite"));
    }

    /// <summary>
    /// 是女骑士的骨骼节点时，排第一次眨眼。
    /// </summary>
    /// <param name="body">骨骼节点（可能不是 SpineSprite，也可能是别的角色）。</param>
    private static void TryStart(Node2D? body)
    {
        if (body == null || body.GetClass() != MegaSprite.spineClassName)
        {
            return;
        }
        Resource? data = body.Get("skeleton_data_res").As<Resource>();
        if (data?.ResourcePath != SkeletonDataPath || body.HasMeta(StartedMeta))
        {
            return;
        }
        body.SetMeta(StartedMeta, true);
        // 还没进场景树时（例如外观先实例化再添加），等进树后再排
        if (body.IsInsideTree())
        {
            Schedule(body);
        }
        else
        {
            body.Connect(Node.SignalName.TreeEntered, Callable.From(() => Schedule(body)),
                (uint)GodotObject.ConnectFlags.OneShot);
        }
    }

    /// <summary>
    /// 随机 3–5 秒后眨一次眼，然后排下一次；节点释放或进入死亡动作后停止。
    /// </summary>
    /// <param name="body">女骑士的骨骼节点。</param>
    private static void Schedule(Node2D body)
    {
        // 随机间隔用系统随机数，不动游戏的随机数
        double delay = MinInterval + Random.Shared.NextDouble() * (MaxInterval - MinInterval);
        // 计时器挂在主场景树上：进树信号里节点的 GetTree() 可能还是空的
        if (Engine.GetMainLoop() is not SceneTree tree)
        {
            return;
        }
        tree.CreateTimer(delay).Timeout += () =>
        {
            if (!GodotObject.IsInstanceValid(body) || !body.IsInsideTree())
            {
                return;
            }
            MegaSprite sprite = new(body);
            MegaAnimationState? state = sprite.TryGetAnimationState();
            if (state == null || state.GetCurrentAnimationName() == DieAnimation)
            {
                return;
            }
            state.SetAnimation(BlinkAnimation, loop: false, BlinkTrack);
            // 眨完清空轨道：否则停在最后一帧，会一直把闭眼插槽盖成睁眼（死亡、受击的闭眼就不显示）
            state.AddEmptyAnimation(BlinkTrack);
            Schedule(body);
        };
    }
}
