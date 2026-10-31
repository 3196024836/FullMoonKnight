using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Cards;
using FullMoonKnight.Events.Data;
using FullMoonKnight.Relics;

namespace FullMoonKnight.Events;

/// <summary>
/// 老猫商店一类事件（原作 StorePanel）：3 张卡，价格是卡的原作金币 ×2.5，买掉的位置不补货，可以买多张，离开结束。
/// 礼品卡：第 3 位半价；量产专家：买过 1 张后其余半价；凋零乐章：+50%。
/// 商品订单：刷新 3 张；卡牌包：获得 3 张随机牌。原作 GetShopCardId.GetShopCard。
/// </summary>
public abstract class CatShopBase : KnightEvent
{
    // ===== 状态 =====

    /// <summary>每个货位的原作卡（买走后为空）。</summary>
    private readonly OriginalCard?[] _entries = new OriginalCard?[OriginalCardDraw.ShopSize];

    /// <summary>每个货位展示的牌。</summary>
    private readonly CardModel?[] _cards = new CardModel?[OriginalCardDraw.ShopSize];

    /// <summary>本次买了几张。</summary>
    private int _bought;

    /// <summary>本店的卡牌等级范围。</summary>
    protected abstract (int Min, int Max) TierRange { get; }

    // ===== 选项 =====

    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        Restock();
        return ShopOptions();
    }

    /// <summary>
    /// 进货：3 个货位全部换成新抽的卡。
    /// </summary>
    private void Restock()
    {
        (int min, int max) = TierRange;
        List<OriginalCard> stock = OriginalCardDraw.Shop(Owner!, Rng, min, max);
        for (int i = 0; i < OriginalCardDraw.ShopSize; i++)
        {
            _entries[i] = i < stock.Count ? stock[i] : null;
            _cards[i] = _entries[i] == null ? null : OriginalCardDraw.Create(Owner!, _entries[i]!);
        }
    }

    /// <summary>
    /// 当前货架：每个还在的货位一个购买选项（钱不够锁定），加离开。
    /// </summary>
    /// <returns>选项。</returns>
    private EventOption[] ShopOptions()
    {
        List<EventOption> options = [];
        for (int i = 0; i < OriginalCardDraw.ShopSize; i++)
        {
            if (_cards[i] == null)
            {
                continue;
            }
            int slot = i;
            int price = PriceAt(slot);
            options.Add(CardOption(_cards[slot]!, Owner!.Gold >= price ? () => Buy(slot) : null,
                InitialPage, $"BUY{slot}", price, slot.ToString()));
        }
        options.Add(LeaveOption());
        return options.ToArray();
    }

    /// <summary>
    /// 货位的价格。
    /// </summary>
    /// <param name="slot">货位。</param>
    /// <returns>金币。</returns>
    private int PriceAt(int slot)
    {
        int price = OriginalCardDraw.PriceOf(_entries[slot]!);
        bool half = (slot == OriginalCardDraw.ShopSize - 1 && Owner!.GetRelic<GiftCard>() != null)
            || (_bought > 0 && Owner!.GetRelic<MassProduction>() != null);
        if (half)
        {
            price = Math.Max(1, price / 2);
        }
        return Price(price);
    }

    // ===== 购买 =====

    /// <summary>
    /// 买下一个货位：商品订单刷新货架，卡牌包给 3 张随机牌，其余加入牌组。
    /// </summary>
    /// <param name="slot">货位。</param>
    /// <returns>异步任务。</returns>
    private async Task Buy(int slot)
    {
        await Spend(PriceAt(slot));
        CardModel card = _cards[slot]!;
        _entries[slot] = null;
        _cards[slot] = null;
        _bought++;
        if (card is ShopOrder)
        {
            Restock();
        }
        else if (card is CardPack)
        {
            (int min, int max) = TierRange;
            for (int i = 0; i < CardPack.CardCount; i++)
            {
                if (OriginalCardDraw.Chest(Owner!, Rng, min, max) is { } entry)
                {
                    await AddToDeck(OriginalCardDraw.Create(Owner!, entry));
                }
            }
        }
        else
        {
            await AddToDeck(card);
        }
        ShowPage(InitialPage, ShopOptions());
    }
}

/// <summary>
/// 老猫商店（原作 1）：等级范围第 1/2/3 章 1–5、2–7、3–10。
/// </summary>
public sealed class OldCatShop : CatShopBase
{
    /// <inheritdoc />
    protected override (int Min, int Max) TierRange => Tiers(Owner!);
}

/// <summary>
/// 老猫的奇珍店（原作 51）：等级 6–10。原作第 3 位固定的忘忧草（线索剧情卡）不放。
/// </summary>
public sealed class CatsCuriosityShop : CatShopBase
{
    /// <inheritdoc />
    protected override (int Min, int Max) TierRange => (6, 10);
}
