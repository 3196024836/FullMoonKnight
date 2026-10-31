using BaseLib.Utils;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Relics;

/// <summary>
/// 限次·无尽魔力：下次战斗开始时获得 6 点法力（原作为第一回合法力消耗 -6，处理方式同魔力强化）。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class TempEndlessMana : KnightTemporaryRelic
{
    private const int ManaAmount = 6;

    /// <inheritdoc />
    public override Task BeforeCombatStart()
    {
        Flash();
        KnightMana.Gain(Owner, ManaAmount);
        return Task.CompletedTask;
    }
}
