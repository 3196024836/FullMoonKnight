using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 冥想（原作 1089，妖精低语）：复制牌组中 1 张牌，然后基础行动力 -1（能量上限 -1）。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class Meditation : KnightRelic
{
    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override bool HasUponPickupEffect => true;

    /// <inheritdoc />
    public override async Task AfterObtained()
    {
        await CopyDeckCards(1);
    }

    /// <inheritdoc />
    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        return player == Owner ? amount - 1 : amount;
    }
}
