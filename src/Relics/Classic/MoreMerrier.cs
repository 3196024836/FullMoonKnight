using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 多多益善：复制牌组中的 1 张牌。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class MoreMerrier : KnightRelic
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
}
