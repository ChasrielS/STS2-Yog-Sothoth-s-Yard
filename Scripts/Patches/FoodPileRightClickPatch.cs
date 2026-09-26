using Godot;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Patching.Models;
using YogSothothsYard.Scripts;
using YogSothothsYard.Scripts.UI;

namespace YogSothothsYard.Scripts.Patches;

public sealed class FoodPileRightClickPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_food_pile_right_click";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NModCardPileButton>(nameof(NModCardPileButton._GuiInput))
    ];

    public static bool Prefix(NModCardPileButton __instance, InputEvent @event)
    {
        if (__instance.Definition?.PileType != Entry.FoodPile ||
            @event is not InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: false })
        {
            return true;
        }

        __instance.AcceptEvent();
        CookingRecipeScreen.ShowForLocalPlayer();
        return false;
    }
}
