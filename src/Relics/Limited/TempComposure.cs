using BaseLib.Utils;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

using FullMoonKnight.Character;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Relics;

/// <summary>
/// 限次·从容不迫：下次战斗，敌人的前 3 次行动无效。战斗开始时布置 3 个"从容"反制。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class TempComposure : KnightTemporaryRelic
{
    private const int CounterCount = 3;

    /// <inheritdoc />
    public override async Task BeforeCombatStart()
    {
        Flash();
        await ComposurePower.PlaceMany(new ThrowingPlayerChoiceContext(), Owner.Creature, CounterCount);
    }
}
