using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Cards;
using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 限次·突破技能（原作"立即重置战斗技能"）：下次战斗开始时，把 1 张临时的已升级"嗒啦啦噗噜噜"加入手牌。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class TempSkillBreakthrough : KnightTemporaryRelic
{
    /// <inheritdoc />
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!IsFirstTurnOf(player))
        {
            return;
        }
        Flash();
        // 已升级的嗒啦啦噗噜噜
        CardModel card = Owner.Creature.CombatState!.CreateCard<Tarara>(Owner);
        CardCmd.Upgrade(card);
        await KnightCards.AddTemporary(card, PileType.Hand, Owner);
    }
}
