using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;

using FullMoonKnight.Cards;
using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 负面·瘟疫乐章：拾取时向牌组加入 1 张"恶作剧"诅咒。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class CursePlagueMovement : KnightRelic
{
    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override bool HasUponPickupEffect => true;

    /// <inheritdoc />
    public override async Task AfterObtained()
    {
        await CardPileCmd.AddCurseToDeck<PrankCurse>(Owner);
    }
}
