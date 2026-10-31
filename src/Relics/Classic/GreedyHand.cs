using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 贪婪之手：拾取时获得 80 金币；每进入新的章节，失去所有金币。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class GreedyHand : KnightRelic
{
    /// <summary>拾取时获得的金币。</summary>
    private const int GoldBonus = 80;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override bool HasUponPickupEffect => true;

    /// <inheritdoc />
    public override async Task AfterObtained()
    {
        await PlayerCmd.GainGold(GoldBonus, Owner);
    }

    /// <inheritdoc />
    public override async Task AfterActEntered()
    {
        if (Owner.Gold <= 0)
        {
            return;
        }
        Flash();
        await PlayerCmd.LoseGold(Owner.Gold, Owner);
    }
}
