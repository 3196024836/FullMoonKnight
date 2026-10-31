using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 干净利落：从牌组中选择 1 张牌删除。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class CleanSlate : KnightRelic
{
    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override bool HasUponPickupEffect => true;

    /// <inheritdoc />
    public override async Task AfterObtained()
    {
        await RemoveDeckCards(1);
    }
}
