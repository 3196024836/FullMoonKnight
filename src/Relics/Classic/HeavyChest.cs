using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 沉重宝箱：每回合开始时额外抽 1 张牌，生命上限 -8。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class HeavyChest : KnightRelic
{
    /// <inheritdoc />
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <inheritdoc />
    public override bool HasUponPickupEffect => true;

    /// <inheritdoc />
    public override async Task AfterObtained()
    {
        await LoseMaxHp(8m);
    }

    /// <inheritdoc />
    public override async Task BeforeHandDraw(
        Player player,
        PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        if (player != Owner)
        {
            return;
        }

        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }
}
