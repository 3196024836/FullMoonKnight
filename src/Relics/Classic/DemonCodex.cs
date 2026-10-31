using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 恶魔法典：每回合多抽 1 张牌，每回合能量 -1；拾取时生命上限 -8。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class DemonCodex : KnightRelic
{
    /// <summary>拾取时失去的生命上限。</summary>
    private const decimal MaxHpPenalty = 8m;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override bool HasUponPickupEffect => true;

    /// <inheritdoc />
    public override async Task AfterObtained()
    {
        await LoseMaxHp(MaxHpPenalty);
    }

    /// <inheritdoc />
    public override decimal ModifyHandDraw(Player player, decimal amount)
    {
        return player == Owner ? amount + 1m : amount;
    }

    /// <inheritdoc />
    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        return player == Owner ? Math.Max(0m, amount - 1m) : amount;
    }
}
