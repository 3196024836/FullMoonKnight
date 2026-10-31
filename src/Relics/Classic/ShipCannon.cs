using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Cards;
using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;

namespace FullMoonKnight.Relics;

/// <summary>
/// 船载火炮：每打出 1 张装备牌，对随机敌人造成 6 点火焰伤害。
/// </summary>
[Pool(typeof(KnightRelicPool))]
public sealed class ShipCannon : KnightRelic
{
    /// <summary>火焰伤害。</summary>
    private const decimal FireDamage = 6m;

    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    /// <inheritdoc />
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner || !KnightCards.IsEquipment(cardPlay.Card)
            || KnightCards.RandomEnemy(Owner) is not { } enemy)
        {
            return;
        }
        Flash();
        await KnightDamage.Deal(choiceContext, enemy, FireDamage, KnightDamage.Fire | ValueProp.Unpowered,
            Owner.Creature, null);
    }
}
