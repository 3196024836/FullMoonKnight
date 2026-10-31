using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 血的代偿（原作 1085，恶魔交易）：获得 66 金币，失去 10 点生命上限。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class BloodCompensation : KnightRelic
{
    /// <summary>金币。</summary>
    private const int Gold = 66;

    /// <summary>失去的生命上限。</summary>
    private const int MaxHpLoss = 10;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override bool HasUponPickupEffect => true;

    /// <inheritdoc />
    public override async Task AfterObtained()
    {
        await PlayerCmd.GainGold(Gold, Owner);
        await LoseMaxHp(MaxHpLoss);
    }
}
