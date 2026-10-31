using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 负面·支付金币 II：当场支付 20 金币。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class CurseGreaterPayGold : KnightCostRelic
{
    /// <summary>代价数值。</summary>
    private const int Amount = 20;

    /// <inheritdoc />
    public override bool Greater => true;

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
