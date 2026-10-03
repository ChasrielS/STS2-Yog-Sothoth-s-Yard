using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Patches;

public sealed class HaaLouLingPotionLargeImagePatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_potion_large_image";
    public static ModPatchTarget[] GetTargets() => [PatchTarget.Getter<PotionModel>(nameof(PotionModel.LargeImagePath))];
    public static bool Prefix(PotionModel __instance, ref string __result)
    {
        if (__instance is not HaaLouLingPotion potion) return true;
        __result = potion.BigImagePath;
        return false;
    }
}
