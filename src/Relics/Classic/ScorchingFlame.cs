using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Relics;

/// <summary>
/// 炙热火焰：敌人每行动一次（包括被反制无效的行动），受到 1 点火焰伤害。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class ScorchingFlame : KnightRelic, IEnemyMoveListener
{
    /// <summary>每次的火焰伤害。</summary>
    private const decimal FireDamage = 1m;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public async Task AfterEnemyMove(PlayerChoiceContext choiceContext, Creature monster)
    {
        if (monster.IsDead)
        {
            return;
        }
        Flash();
        await KnightDamage.Deal(choiceContext, monster, FireDamage, KnightDamage.Fire | ValueProp.Unpowered,
            Owner.Creature, null);
    }
}
