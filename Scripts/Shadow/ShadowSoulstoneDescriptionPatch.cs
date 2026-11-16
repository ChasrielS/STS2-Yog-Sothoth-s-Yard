using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Shadow;

public sealed class ShadowSoulstoneDescriptionPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_soulstone_description";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(RelicModel), "get_DynamicDescription", Type.EmptyTypes)];

    public static void Postfix(RelicModel __instance, ref LocString __result)
    {
        if (__instance is not IShadowSoulstone || !__instance.IsMutable || __instance.Owner is not { } owner ||
            ShadowRun.Level(owner) == 0) return;
        var erosion = ShadowRun.Erosion(owner);
        var suffix = erosion >= ShadowRun.ErosionLimit ? ".brokenDescription" : ".shadowDescription";
        if (LocString.GetIfExists("relics", __instance.Id.Entry + suffix) is not { } description) return;
        __result = description;
        __result.Add("Erosion", erosion);
    }
}
