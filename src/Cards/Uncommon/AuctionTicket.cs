using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Character;
using FullMoonKnight.Mechanics;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 拍卖券：抽牌；你每有 1 件装备，费用 -1。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class AuctionTicket : KnightCard
{
    /// <summary>
    /// 创建拍卖券。
    /// </summary>
    public AuctionTicket() : base(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(KnightKeywords.Equip)];

    /// <summary>
    /// 自己的费用按装备件数降低。
    /// </summary>
    /// <param name="card">要计算费用的牌。</param>
    /// <param name="originalCost">原费用。</param>
    /// <param name="modifiedCost">修改后的费用。</param>
    /// <returns>修改了费用时为 true。</returns>
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        // 装备件数
        int equipment = card == this ? KnightEquipmentPower.CountEquipment(Owner.Creature) : 0;
        modifiedCost = Math.Max(0m, originalCost - equipment);
        return equipment > 0;
    }

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
