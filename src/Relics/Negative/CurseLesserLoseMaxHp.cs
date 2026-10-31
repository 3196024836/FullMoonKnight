using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 负面·失去生命上限 I：当场失去 2 点生命上限。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class CurseLesserLoseMaxHp : KnightCostRelic
{
    /// <summary>代价数值。</summary>
    private const int Amount = 2;

    /// <inheritdoc />
    public override bool CanPay(Player player)
    {
        return player.Creature.MaxHp > Amount;
    }

    /// <inheritdoc />
    protected override async Task Pay()
    {
        await LoseMaxHp(Amount);
    }
}
