using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Cards;
using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Relics;

/// <summary>
/// 邪恶祭祀：回合开始时，随机敌人失去 2 点生命，自己回复 2 点生命。
/// 原作还会吸收生命上限；每回合永久增加生命上限在整局里会滚雪球，这里只吸生命。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class EvilSacrifice : KnightRelic
{
    private const decimal DrainAmount = 2m;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }

        Creature? enemy = KnightCards.RandomEnemy(Owner);
        if (enemy == null)
        {
            return;
        }

        Flash();
        await KnightDamage.Deal(choiceContext, enemy, DrainAmount, ValueProp.Unblockable | ValueProp.Unpowered,
            Owner.Creature, null);
        await CreatureCmd.Heal(Owner.Creature, DrainAmount);
    }
}
