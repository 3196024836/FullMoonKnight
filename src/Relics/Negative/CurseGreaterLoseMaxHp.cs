using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 负面·失去生命上限 II：当场失去 5 点生命上限。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class CurseGreaterLoseMaxHp : KnightCostRelic
{
    /// <summary>代价数值。</summary>
    private const int Amount = 5;

    /// <inheritdoc />
    public override bool Greater => true;

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
