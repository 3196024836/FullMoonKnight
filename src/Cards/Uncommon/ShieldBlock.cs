using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

using FullMoonKnight.Character;

namespace FullMoonKnight.Cards;

/// <summary>
/// 盾牌格挡：获得格挡；手牌最左边的攻击牌变成这张牌的复制。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class ShieldBlock : KnightCard
{
    /// <summary>
    /// 创建盾牌格挡。
    /// </summary>
    public ShieldBlock() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5m, ValueProp.Move)];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        // 手牌最左边的攻击牌
        CardModel? leftmost = PileType.Hand.GetPile(Owner).Cards.FirstOrDefault(c => c.Type == CardType.Attack);
        if (leftmost != null)
        {
            await CardCmd.Transform(leftmost, CreateClone());
        }
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}
