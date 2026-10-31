using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

using FullMoonKnight.Events.Data;

namespace FullMoonKnight.Events;

/// <summary>
/// 搞错的礼物（原作 30，WrongGiftPanel）：3 张"别的角色"的牌（普通、罕见），免费拿 1 张。必须拿。
/// 只在本幕遇到过圣诞老人的疑惑之后出现，本幕最多 3 个。原作 GetShopCardId.GetWrongGiftCard。
/// </summary>
public sealed class LostGift : KnightEvent
{
    /// <summary>展示的牌数。</summary>
    private const int OfferCount = 3;

    /// <inheritdoc />
    protected override bool CanAppear(IRunState runState)
    {
        return FullMoonEventDeck.SeenCount<SantasDoubt>(runState) > 0;
    }

    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        List<CardModel> gifts = OriginalCardDraw.WrongGift(Owner!, Rng, OfferCount);
        List<EventOption> options = [];
        for (int i = 0; i < gifts.Count; i++)
        {
            CardModel card = gifts[i];
            options.Add(CardOption(card, () => Take(card), InitialPage, $"TAKE{i}", -1, i.ToString()));
        }
        if (options.Count == 0)
        {
            options.Add(LeaveOption());
        }
        return options;
    }

    /// <summary>
    /// 拿走一份礼物。
    /// </summary>
    /// <param name="card">牌。</param>
    /// <returns>异步任务。</returns>
    private async Task Take(CardModel card)
    {
        await AddToDeck(card);
        Finish("DONE");
    }
}
