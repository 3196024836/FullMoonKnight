using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Character;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 短兵相接：消耗，装备 1 把短剑和 1 个圆盾（升级后为升级版）。
/// </summary>
[Pool(typeof(KnightCardPool))]
public sealed class CloseQuarters : KnightCard
{
    /// <summary>
    /// 创建短兵相接。
    /// </summary>
    public CloseQuarters() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    /// <inheritdoc />
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<Shortsword>(IsUpgraded), HoverTipFactory.FromCard<RoundShield>(IsUpgraded)];

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 参照用的短剑和圆盾（取对应版本的数值）
        CardModel sword = ModelDb.Card<Shortsword>().ToMutable();
        CardModel shield = ModelDb.Card<RoundShield>().ToMutable();
        if (IsUpgraded)
        {
            sword.UpgradeInternal();
            shield.UpgradeInternal();
        }
        await KnightEquipmentPower.Equip<ShortswordPower>(choiceContext, Owner.Creature,
            sword.DynamicVars.Damage.BaseValue, this);
        await KnightEquipmentPower.Equip<RoundShieldPower>(choiceContext, Owner.Creature,
            shield.DynamicVars.Block.BaseValue, this);
    }

    /// <inheritdoc />
    protected override void OnUpgrade()
    {
    }
}
