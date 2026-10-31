using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;

using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Relics;

/// <summary>
/// 亡灵之花：每回合少抽 1 张牌；敌方回合中，敌人每让你失去生命，就对它造成等量伤害。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class UndeadFlower : KnightRelic
{
    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        return player == Owner ? count - 1m : count;
    }

    /// <inheritdoc />
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner.Creature || result.UnblockedDamage <= 0 || dealer is not { Side: CombatSide.Enemy }
            || dealer.IsDead || Owner.Creature.CombatState?.CurrentSide != CombatSide.Enemy)
        {
            return;
        }
        Flash();
        await KnightDamage.Deal(choiceContext, dealer, result.UnblockedDamage, ValueProp.Unpowered, Owner.Creature,
            null);
    }
}
