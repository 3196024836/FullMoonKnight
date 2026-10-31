using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 神奇雕像：将每次战斗使用的第 1 张牌的复制置入手牌。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class MagicStatue : KnightRelic
{
    private bool _triggered;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override Task BeforeCombatStart()
    {
        _triggered = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (_triggered || cardPlay.Card.Owner != Owner)
        {
            return;
        }

        _triggered = true;
        Flash();

        var copy = cardPlay.Card.CreateClone();
        await CardPileCmd.Add(copy, PileType.Hand, clonedBy: this);
    }
}
