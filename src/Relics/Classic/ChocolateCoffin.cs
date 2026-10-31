using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 巧克力之棺：每打出一张攻击牌时获得 1 点格挡。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class ChocolateCoffin : KnightRelic
{
    private const decimal BlockAmount = 1m;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(BlockAmount, ValueProp.Move)];

    /// <inheritdoc />
    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner)
        {
            return;
        }

        if (cardPlay.Card.Type == CardType.Attack)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, null);
        }
    }
}
