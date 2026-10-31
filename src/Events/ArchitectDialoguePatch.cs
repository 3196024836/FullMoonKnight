using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Runs;

using FullMoonKnight.Character;

namespace FullMoonKnight.Events;

/// <summary>
/// 建筑师（最终首领后的对话）：原版只给五个原版角色写了对话，女骑士查不到对话时选项直接是“继续”，
/// 点击后 WinRun 读取空对话的攻击设置抛出空引用，游戏卡死。这里给女骑士补三段对话（文字在 ancients.json），
/// 并兜底：任何角色没有对话时，直接跳过攻击动画进入结算。
/// </summary>
[HarmonyPatch]
public static class ArchitectDialoguePatch
{
    /// <summary>每段对话的行数（建筑师、女骑士、建筑师）。</summary>
    private const int LinesPerDialogue = 3;

    /// <summary>女骑士的对话段数。</summary>
    private const int DialogueCount = 3;

    /// <summary>
    /// 定义对话后加入女骑士的对话。
    /// </summary>
    /// <param name="__result">建筑师的对话集。</param>
    [HarmonyPatch(typeof(TheArchitect), "DefineDialogues")]
    [HarmonyPostfix]
    public static void AddKnightDialogues(AncientDialogueSet __result)
    {
        // 女骑士的角色键
        string key = ModelDb.Character<Knight>().Id.Entry;
        // 对话（每行一个空音效路径，和原版一样）
        List<AncientDialogue> dialogues = [];
        for (int i = 0; i < DialogueCount; i++)
        {
            dialogues.Add(new AncientDialogue(Enumerable.Repeat("", LinesPerDialogue).ToArray())
            {
                VisitIndex = i,
                StartAttackers = ArchitectAttackers.None,
                EndAttackers = ArchitectAttackers.Both,
            });
        }
        __result.CharacterDialogues[key] = dialogues;
    }

    /// <summary>
    /// 没有对话时 WinRun 会读取空对话：直接让本地玩家进入结算。
    /// </summary>
    /// <param name="__instance">建筑师事件。</param>
    /// <param name="__result">替换后的任务。</param>
    /// <returns>有对话时为 true（走原逻辑）。</returns>
    [HarmonyPatch(typeof(TheArchitect), "WinRun")]
    [HarmonyPrefix]
    public static bool SkipWhenNoDialogue(TheArchitect __instance, ref Task __result)
    {
        if (Traverse.Create(__instance).Property("Dialogue").GetValue() != null)
        {
            return true;
        }
        if (__instance.Owner != null && LocalContext.IsMe(__instance.Owner))
        {
            RunManager.Instance.ActChangeSynchronizer.SetLocalPlayerReady();
        }
        __result = Task.CompletedTask;
        return false;
    }
}
