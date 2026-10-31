using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace FullMoonKnight.Cards;

/// <summary>
/// 事件里展示用的原作物品牌（商品订单、卡牌包、红包）：只画在事件面板上，选中后由事件结算，不进牌组，不能打出。
/// </summary>
public abstract class EventDisplayCard : KnightCard
{
    /// <summary>
    /// 创建展示牌。
    /// </summary>
    protected EventDisplayCard() : base(-1, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    /// <inheritdoc />
    public override int MaxUpgradeLevel => 0;

    /// <inheritdoc />
    public override bool CanBeGeneratedInCombat => false;

    /// <inheritdoc />
    public override bool CanBeGeneratedByModifiers => false;

    /// <inheritdoc />
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    /// <inheritdoc />
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// 商品订单（原作 1072）：老猫商店里出售，买下后刷新商品。
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class ShopOrder : EventDisplayCard
{
}

/// <summary>
/// 卡牌包（原作 3222）：老猫商店里出售，买下后获得 3 张随机牌。
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class CardPack : EventDisplayCard
{
    /// <summary>获得的张数。</summary>
    public const int CardCount = 3;

    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(CardCount)];
}

/// <summary>
/// 红包（原作 9004，害羞的宝箱）：立即打开，获得金币。金额由事件写进 Gold。
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class RedEnvelope : EventDisplayCard
{
    /// <inheritdoc />
    protected override IEnumerable<DynamicVar> CanonicalVars => [new GoldVar(0)];
}
