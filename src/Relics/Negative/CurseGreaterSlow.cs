using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;

using FullMoonKnight.Cards;
using FullMoonKnight.Character;

namespace FullMoonKnight.Relics;

/// <summary>
/// 负面·获得减速 II：牌组加入 1 张"减速"诅咒。
/// </summary>
[Pool(typeof(KnightEventRelicPool))]
public sealed class CurseGreaterSlow : KnightCostRelic
{
    /// <inheritdoc />
    public override bool Greater => true;

    /// <inheritdoc />
    protected override async Task Pay()
    {
        await CardPileCmd.AddCurseToDeck<SlowCurse>(Owner);
    }
}
