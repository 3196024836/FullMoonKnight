using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;

using FullMoonKnight.Cards;
using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 复制幻象：复制牌组中的 1 张牌，并向牌组加入 2 张"减速"诅咒。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class CopyIllusion : KnightRelic
{
    /// <summary>加入的诅咒张数。</summary>
    private const int CurseCount = 2;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override bool HasUponPickupEffect => true;

    /// <inheritdoc />
    public override async Task AfterObtained()
    {
        await CopyDeckCards(1);
        for (int i = 0; i < CurseCount; i++)
        {
            await CardPileCmd.AddCurseToDeck<SlowCurse>(Owner);
        }
    }
}
