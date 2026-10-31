using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 着魔（原作 1088，妖精低语）：复制牌组中 1 张牌，然后失去 18 点生命上限。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class Obsession : KnightRelic
{
    /// <summary>失去的生命上限。</summary>
    private const int MaxHpLoss = 18;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override bool HasUponPickupEffect => true;

    /// <inheritdoc />
    public override async Task AfterObtained()
    {
        await CopyDeckCards(1);
        await LoseMaxHp(MaxHpLoss);
    }
}
