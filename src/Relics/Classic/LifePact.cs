using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 生命契约：每场战斗第 1 个回合开始时恢复已损失生命的 20%。
/// 原来持续 3 回合，残血进战斗每场能回近 20 点；改为只在第 1 回合。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class LifePact : KnightRelic
{
    /// <summary>每场战斗回复的回合数。</summary>
    private const int MaxTurns = 1;
    private const decimal HealPercent = 0.20m;

    private int _turnsHealed;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override Task BeforeCombatStart()
    {
        _turnsHealed = 0;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        // 只看自己的第 1 回合；这一回合判定过就算用掉，满血也不留到后面的回合
        if (player != Owner || _turnsHealed >= MaxTurns)
        {
            return;
        }

        _turnsHealed = MaxTurns;
        var missing = Owner.Creature.MaxHp - Owner.Creature.CurrentHp;
        if (missing <= 0m)
        {
            return;
        }

        var healAmount = (int)Math.Floor(missing * HealPercent);
        if (healAmount <= 0)
        {
            return;
        }

        await CreatureCmd.Heal(Owner.Creature, healAmount);
    }
}
