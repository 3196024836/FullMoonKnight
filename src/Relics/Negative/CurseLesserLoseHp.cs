using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 负面·失去生命 I：当场失去 10 点生命。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class CurseLesserLoseHp : KnightCostRelic
{
    /// <summary>代价数值。</summary>
    private const int Amount = 10;

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
