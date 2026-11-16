using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Shadow;

internal static class ShadowErosionTips
{
    private const string LocKey = "YOG_SOTHOTH_S_YARD_EROSION";

    internal static HoverTip Create() => new HoverTip(
        new LocString("static_hover_tips", LocKey + ".title"),
        new LocString("static_hover_tips", LocKey + ".description"),
        ShadowErosionIcons.Small);

    internal static bool MentionsErosion(string? text) => text?.Contains("侵蚀度", StringComparison.Ordinal) == true;

    internal static bool MentionsErosion(CardModel card) =>
        ShadowRanks.Of(card) > 0 && MentionsErosion(card.Description.GetRawText());
}

public sealed class ShadowErosionModelTipsPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_erosion_model_tips";
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(CardModel), "get_HoverTips", Type.EmptyTypes),
        new(typeof(RelicModel), "get_HoverTipsExcludingRelic", Type.EmptyTypes)
    ];

    public static void Postfix(AbstractModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        var mentioned = __instance switch
        {
            CardModel card => ShadowErosionTips.MentionsErosion(card),
            RelicModel relic when relic is IShadowSoulstone || ShadowRanks.Of(relic) > 0 =>
                ShadowErosionTips.MentionsErosion(relic.DynamicDescription.GetFormattedText()),
            _ => false
        };
        if (mentioned) __result = __result.Append(ShadowErosionTips.Create());
    }
}

public sealed class ShadowErosionTipSetPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_erosion_tip_set";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(NHoverTipSet), "Init", [typeof(Control), typeof(IEnumerable<IHoverTip>)])];

    public static void Prefix(Control owner, ref IEnumerable<IHoverTip> hoverTips) =>
        hoverTips = AppendErosionTip(owner, hoverTips);

    private static IEnumerable<IHoverTip> AppendErosionTip(Control owner, IEnumerable<IHoverTip> tips)
    {
        var mentioned = owner is NEventOptionButton { Option: { } option } &&
            ShadowErosionTips.MentionsErosion(option.Description.GetFormattedText());
        foreach (var tip in tips)
        {
            mentioned |= tip switch
            {
                HoverTip text => ShadowErosionTips.MentionsErosion(text.Description),
                CardHoverTip card => ShadowErosionTips.MentionsErosion(card.Card),
                _ => false
            };
            yield return tip;
        }
        // The native tip set removes duplicates by the ID generated from the localization keys.
        if (mentioned) yield return ShadowErosionTips.Create();
    }
}
