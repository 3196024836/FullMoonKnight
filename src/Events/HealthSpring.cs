using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

using FullMoonKnight.Events.Data;

namespace FullMoonKnight.Events;

/// <summary>
/// 生命之泉（原作 2，地图事件卡）：喝光，回满生命。带荒芜乐章时先失去 8 点生命上限（原作 2 点 ×4）。
/// 原作 DealWithHealthSpring。
/// </summary>
public sealed class HealthSpring : KnightEvent
{
    /// <summary>荒芜乐章失去的生命上限（原作 2）。</summary>
    private static readonly int BarrenLoss = FullMoonNumbers.Hp(2);

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new MaxHpVar(BarrenLoss)];

    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        return [Choice(Drink, InitialPage, HasBarren ? "DRINK_BARREN" : "DRINK"), LeaveOption()];
    }

    /// <summary>
    /// 喝光：回满生命。
    /// </summary>
    /// <returns>异步任务。</returns>
    private async Task Drink()
    {
        if (HasBarren)
        {
            await LoseMaxHp(BarrenLoss);
        }
        await CreatureCmd.Heal(Owner!.Creature, Owner.Creature.MaxHp - Owner.Creature.CurrentHp);
        Finish("DRINK");
    }
}
