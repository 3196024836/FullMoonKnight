using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;

using FullMoonKnight.Cards;
using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 小丑戏法：每回合多抽 1 张牌；拾取时向牌组加入 4 张"恶作剧"诅咒。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class JesterTrick : KnightRelic
{
    /// <summary>加入的诅咒张数。</summary>
    private const int CurseCount = 4;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override bool HasUponPickupEffect => true;

    /// <inheritdoc />
    public override async Task AfterObtained()
    {
        for (int i = 0; i < CurseCount; i++)
        {
            await CardPileCmd.AddCurseToDeck<PrankCurse>(Owner);
        }
    }

    /// <inheritdoc />
    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        return player == Owner ? count + 1m : count;
    }
}
