using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 忍耐力：受到伤害时，有 10% 的概率抽 1 张牌。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class Endurance : KnightRelic
{
    private const float ProcChance = 0.10f;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner.Creature || result.UnblockedDamage <= 0)
        {
            return;
        }

        if (Owner.RunState.Rng.Niche.NextFloat() < ProcChance)
        {
            await CardPileCmd.Draw(choiceContext, 1m, Owner);
        }
    }
}
