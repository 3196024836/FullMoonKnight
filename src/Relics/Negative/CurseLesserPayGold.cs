using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 负面·支付金币 I：当场支付 10 金币。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class CurseLesserPayGold : KnightCostRelic
{
    /// <summary>代价数值。</summary>
    private const int Amount = 10;

    /// <inheritdoc />
    public override bool CanPay(Player player)
    {
        return player.Gold >= Amount;
    }

    /// <inheritdoc />
    protected override async Task Pay()
    {
        await PlayerCmd.LoseGold(Amount, Owner);
    }
}
