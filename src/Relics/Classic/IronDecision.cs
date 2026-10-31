using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 斩钉截铁：从牌组中选择最多 2 张牌并删除它们。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class IronDecision : KnightRelic
{
    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override bool HasUponPickupEffect => true;

    /// <inheritdoc />
    public override async Task AfterObtained()
    {
        await RemoveDeckCards(2);
    }
}
