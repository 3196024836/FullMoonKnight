using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Relics;

/// <summary>
/// 悔恨（原作 14001，遗落的道具）：回合开始时失去生命上限 10% 的生命，本场战斗伤害 +2（力量 +2，可叠加）。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class Remorse : KnightRelic
{
    /// <summary>每回合失去生命上限的百分比。</summary>
    private const int LossPercent = 10;

    /// <summary>每回合获得的力量。</summary>
    private const decimal StrengthGain = 2m;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner.Creature.IsDead)
        {
            return;
        }
        Flash();
        // 失去的生命
        int loss = Owner.Creature.MaxHp * LossPercent / 100;
        if (loss > 0)
        {
            await KnightDamage.Deal(choiceContext, Owner.Creature, loss, ValueProp.Unblockable | ValueProp.Unpowered,
                null, null);
        }
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, StrengthGain, Owner.Creature, null);
    }
}
