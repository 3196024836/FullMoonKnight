using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Powers;

/// <summary>
/// 反制·从容：敌人的任何行动都被无效，没有附带效果。由限次祝福"从容不迫"在战斗开始时布置。
/// </summary>
public sealed class ComposurePower : KnightCounterPower
{
    /// <inheritdoc />
    public override CounterCondition Condition => CounterCondition.Any;

    /// <inheritdoc />
    public override Task Resolve(CounterContext context)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 不经由反制牌，直接给生物布置若干个从容反制。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="owner">布置者。</param>
    /// <param name="count">布置个数。</param>
    /// <returns>异步任务。</returns>
    public static async Task PlaceMany(PlayerChoiceContext choiceContext, Creature owner, int count)
    {
        for (int i = 0; i < count; i++)
        {
            await PowerCmd.Apply<ComposurePower>(choiceContext, owner, 1m, owner, null);
        }
    }
}
