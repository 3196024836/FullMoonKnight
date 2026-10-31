using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Runs;

using FullMoonKnight.Events.Data;
using FullMoonKnight.Relics;

namespace FullMoonKnight.Events;

/// <summary>
/// 遗失的苹果（原作 8，地图事件卡）：咬一口，随机效果（女巫的疑惑时就定好了顺序）。只能咬，不能离开。
/// 苹果只在本幕有效。原作 EventApple.txt：
/// 健康 生命 +12/16/24，毒 生命 −12/16/24（最低 1），硕大 上限 +8/8/12，剧毒 上限 −8/8/12，
/// 魔法 初始法力 +1/1/2，残缺 初始法力 −1/1/2（最低 0），保鲜 经验 +2/1/1，腐烂 经验 −2/1/1（不降级）。
/// </summary>
public sealed class LostApple : KnightEvent
{
    /// <summary>生命类苹果每章原作数值（健康、毒）。</summary>
    private static readonly int[] HpValues = [3, 4, 6];

    /// <summary>生命上限类苹果每章原作数值（硕大、剧毒）。</summary>
    private static readonly int[] MaxHpValues = [2, 2, 3];

    /// <summary>法力类苹果每章数值（魔法、残缺）。</summary>
    private static readonly int[] ManaValues = [1, 1, 2];

    /// <summary>经验类苹果每章数值（保鲜、腐烂；模组经验折算）。</summary>
    private static readonly int[] XpValues = [2, 1, 1];

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Amount", 0)];

    /// <inheritdoc />
    protected override bool CanAppear(IRunState runState)
    {
        return runState.Players.Any(p => FullMoonLedger.Peek(p)?.ApplesLeft(runState.CurrentActIndex) > 0);
    }

    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        bool hasApple = Ledger.ApplesLeft(Owner!.RunState.CurrentActIndex) > 0;
        return [hasApple ? Choice(Bite, InitialPage, "BITE") : Choice(Nothing, InitialPage, "NOTHING")];
    }

    /// <summary>
    /// 咬一口：按苹果种类结算。
    /// </summary>
    /// <returns>异步任务。</returns>
    private async Task Bite()
    {
        int kind = Ledger.EatApple();
        int chapter = Chapter - 1;
        KnightOathBase? oath = Owner!.GetRelic<KnightOathBase>();
        // 本次效果的数值（填进结果页）
        int amount;
        switch (kind)
        {
            case 1001:
                amount = FullMoonNumbers.Hp(HpValues[chapter]);
                await CreatureCmd.Heal(Owner.Creature, amount);
                break;
            case 1002:
                amount = FullMoonNumbers.Hp(HpValues[chapter]);
                await LoseHp(amount, keepOne: true);
                break;
            case 1003:
                amount = FullMoonNumbers.Hp(MaxHpValues[chapter]);
                await CreatureCmd.GainMaxHp(Owner.Creature, amount);
                break;
            case 1004:
                amount = FullMoonNumbers.Hp(MaxHpValues[chapter]);
                await LoseMaxHp(amount);
                break;
            case 1005:
                amount = ManaValues[chapter];
                Ledger.FmkBaseMana += amount;
                break;
            case 1006:
                amount = ManaValues[chapter];
                Ledger.FmkBaseMana = Math.Max(0, Ledger.FmkBaseMana - amount);
                break;
            case 1007:
                amount = XpValues[chapter];
                if (oath != null)
                {
                    await oath.GainXp(amount);
                }
                break;
            default:
                amount = XpValues[chapter];
                oath?.LoseXp(amount);
                break;
        }
        DynamicVars["Amount"].BaseValue = amount;
        Finish($"APPLE_{kind}");
    }

    /// <summary>
    /// 联机时别人的苹果：什么也没有。
    /// </summary>
    /// <returns>异步任务。</returns>
    private Task Nothing()
    {
        Finish("NOTHING");
        return Task.CompletedTask;
    }
}
