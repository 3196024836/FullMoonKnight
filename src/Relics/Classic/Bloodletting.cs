using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 放血：复制牌组中的 1 张牌，生命上限 -12。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class Bloodletting : KnightRelic
{
    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override bool HasUponPickupEffect => true;

    /// <inheritdoc />
    public override async Task AfterObtained()
    {
        await LoseMaxHp(12);
        await CopyDeckCards(1);
    }
}
