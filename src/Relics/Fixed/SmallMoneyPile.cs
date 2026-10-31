using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 小钱堆（原作 1040）：获得时立即得到 15 金币。祝福抽不够时用它补。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class SmallMoneyPile : KnightRelic
{
    /// <summary>金币。</summary>
    private const int Gold = 15;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override bool HasUponPickupEffect => true;

    /// <inheritdoc />
    public override async Task AfterObtained()
    {
        await PlayerCmd.GainGold(Gold, Owner);
    }
}
