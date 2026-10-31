using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 迷你护盾：生命上限 +5，每次战斗开始时获得 5 格挡。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class MiniShield : KnightRelic
{
    private const decimal BlockAmount = 5m;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(BlockAmount, ValueProp.Move)];

    /// <inheritdoc />
    public override bool HasUponPickupEffect => true;

    /// <inheritdoc />
    public override async Task AfterObtained()
    {
        await CreatureCmd.GainMaxHp(Owner.Creature, 5);
    }

    /// <inheritdoc />
    public override async Task BeforeHandDraw(
        Player player,
        PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        if (player != Owner || player.PlayerCombatState?.TurnNumber != 1)
        {
            return;
        }

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, null);
    }
}
