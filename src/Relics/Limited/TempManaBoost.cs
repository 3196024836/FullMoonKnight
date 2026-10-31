using BaseLib.Utils;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Relics;

/// <summary>
/// 限次·魔力强化：下次战斗开始时获得 3 点法力（原作为第一回合法力消耗 -3，D23 下女骑士没有耗法力的牌，改为直接给法力）。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class TempManaBoost : KnightTemporaryRelic
{
    private const int ManaAmount = 3;

    /// <inheritdoc />
    public override Task BeforeCombatStart()
    {
        Flash();
        KnightMana.Gain(Owner, ManaAmount);
        return Task.CompletedTask;
    }
}
