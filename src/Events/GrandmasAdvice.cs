using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

using FullMoonKnight.Cards;

namespace FullMoonKnight.Events;

/// <summary>
/// 外婆的叮嘱（原作 32，GrandmaAdvicePanel）：获得"外婆的祝福"（每场战斗开始时在手牌里，打出后本场移除，
/// 从 3 张牌里选 1 张加入手牌）。必须拿。原作噩梦 VII 第 3 章；模组第 3 幕低概率。
/// </summary>
public sealed class GrandmasAdvice : KnightEvent
{

    /// <inheritdoc />
    protected override IReadOnlyList<EventOption> InitialOptions()
    {
        CardModel card = NewCard(ModelDb.Card<GrandmasBlessing>());
        return [CardOption(card, () => Take(card), InitialPage, "TAKE")];
    }

    /// <summary>
    /// 收下外婆的祝福。
    /// </summary>
    /// <param name="card">牌。</param>
    /// <returns>异步任务。</returns>
    private async Task Take(CardModel card)
    {
        await AddToDeck(card);
        Finish("DONE");
    }
}
