using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace FullMoonKnight.Cards;

/// <summary>
/// 恶作剧（诅咒）：无法打出。小丑戏法、瘟疫乐章加入牌组。不进随机诅咒池。
/// </summary>
[Pool(typeof(CurseCardPool))]
public sealed class PrankCurse : KnightCard
{
    /// <summary>
    /// 创建恶作剧。
    /// </summary>
    public PrankCurse() : base(-1, CardType.Curse, CardRarity.Curse, TargetType.None)
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
}
