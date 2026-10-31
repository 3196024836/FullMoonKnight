using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Cards;
using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Relics;

/// <summary>
/// 创造之力（原作 14002，遗落的道具）：战斗中生成卡牌时，对随机敌人造成 3 点伤害。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class CreativePower : KnightRelic
{
    /// <summary>伤害。</summary>
    private const decimal Damage = 3m;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (card.Owner != Owner || KnightCards.RandomEnemy(Owner) is not { } enemy)
        {
            return;
        }
        Flash();
        await KnightDamage.Deal(new ThrowingPlayerChoiceContext(), enemy, Damage, ValueProp.Unpowered,
            Owner.Creature, null);
    }
}
