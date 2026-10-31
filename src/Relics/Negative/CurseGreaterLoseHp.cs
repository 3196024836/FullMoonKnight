using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 负面·失去生命 II：当场失去 20 点生命。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class CurseGreaterLoseHp : KnightCostRelic
{
    /// <summary>代价数值。</summary>
    private const int Amount = 20;

    /// <inheritdoc />
    public override bool Greater => true;

    /// <inheritdoc />
    public override bool CanPay(Player player)
    {
        return player.Creature.CurrentHp > Amount;
    }

    /// <inheritdoc />
    protected override async Task Pay()
    {
        await LoseHp(Amount);
    }
}
