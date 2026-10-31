using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Mechanics;
using FullMoonKnight.Powers;

namespace FullMoonKnight.Cards;

/// <summary>
/// 装备牌基类：0 费能力牌，打出后给自己装备对应的装备能力，整场战斗有效，同名可叠加。
/// </summary>
/// <typeparam name="TPower">装备能力类型。</typeparam>
public abstract class KnightEquipmentCard<TPower> : KnightCard where TPower : KnightEquipmentPower
{
    /// <summary>
    /// 创建装备牌。
    /// </summary>
    /// <param name="rarity">稀有度。</param>
    protected KnightEquipmentCard(CardRarity rarity) : base(0, CardType.Power, rarity, TargetType.Self)
    {
    }

    /// <inheritdoc />
    public override IEnumerable<CardKeyword> CanonicalKeywords => [KnightKeywords.Equip];

    /// <inheritdoc />
    protected override string TypeTextKey => KnightKeywords.EquipTitleKey;

    /// <summary>本件装备给装备能力增加的层数（效果数值）。</summary>
    protected abstract decimal EquipAmount { get; }

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await KnightEquipmentPower.Equip<TPower>(choiceContext, Owner.Creature, EquipAmount, this);
    }
}

/// <summary>
/// 反制牌基类：1 费技能牌，打出后布置对应的反制。
/// </summary>
/// <typeparam name="TPower">反制能力类型。</typeparam>
public abstract class KnightCounterCard<TPower> : KnightCard where TPower : KnightCounterPower
{
    /// <summary>
    /// 创建反制牌。
    /// </summary>
    /// <param name="rarity">稀有度。</param>
    protected KnightCounterCard(CardRarity rarity) : base(1, CardType.Skill, rarity, TargetType.Self)
    {
    }

    /// <inheritdoc />
    public override IEnumerable<CardKeyword> CanonicalKeywords => [KnightKeywords.Counter];

    /// <inheritdoc />
    protected override string TypeTextKey => KnightKeywords.CounterTitleKey;

    /// <inheritdoc />
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await KnightCounterPower.Place<TPower>(choiceContext, this);
    }
}

/// <summary>
/// 抉择用的选项牌（嗒啦啦噗噜噜、特效药）：只用来展示和选择，选中后由 <see cref="Resolve" /> 结算效果，不会进入牌堆。
/// </summary>
public abstract class KnightOptionCard : KnightCard
{
    /// <summary>
    /// 创建选项牌。
    /// </summary>
    protected KnightOptionCard() : base(-1, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    /// <inheritdoc />
    public override bool CanBeGeneratedInCombat => false;

    /// <inheritdoc />
    public override bool CanBeGeneratedByModifiers => false;

    /// <summary>
    /// 结算选项效果。
    /// </summary>
    /// <param name="choiceContext">玩家选择上下文。</param>
    /// <param name="source">打出的抉择牌。</param>
    /// <returns>异步任务。</returns>
    public abstract Task Resolve(PlayerChoiceContext choiceContext, CardModel source);

    /// <inheritdoc />
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return Resolve(choiceContext, this);
    }
}
