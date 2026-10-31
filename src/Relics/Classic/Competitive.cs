using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Cards;
using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 争强好胜：每回合开始时，把 1 张随机的临时女骑士攻击牌加入手牌。
/// </summary>
[Pool(typeof(KnightRelicPool))]
public sealed class Competitive : KnightRelic
{
    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Rare;

    /// <inheritdoc />
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }
        Flash();
        foreach (CardModel card in KnightCards.RandomKnightCards(Owner, 1, c => c.Type == CardType.Attack))
        {
            await KnightCards.AddTemporary(card, PileType.Hand, Owner);
        }
    }
}
