using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;

using FullMoonKnight.Cards;
using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 负面·获得减速 I：牌组加入 1 张"减速"诅咒。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class CurseLesserSlow : KnightCostRelic
{
    /// <inheritdoc />
    protected override async Task Pay()
    {
        await CardPileCmd.AddCurseToDeck<SlowCurse>(Owner);
    }
}
