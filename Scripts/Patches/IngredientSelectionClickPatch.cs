using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using STS2RitsuLib.Patching.Models;
using YogSothothsYard.Scripts.UI;

namespace YogSothothsYard.Scripts.Patches;

public sealed class IngredientSelectionClickPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_ingredient_quantity_selection";
    public static ModPatchTarget[] GetTargets() =>
        [PatchTarget.Method<NSimpleCardSelectScreen>("OnCardClicked", typeof(CardModel))];

    public static bool Prefix(NSimpleCardSelectScreen __instance, CardModel card,
        HashSet<CardModel> ____selectedCards, CardSelectorPrefs ____prefs)
    {
        if (!NIngredientSelection.TryGet(__instance, out var selection)) return true;
        selection.Select(card, ____selectedCards, ____prefs);
        return false;
    }
}
