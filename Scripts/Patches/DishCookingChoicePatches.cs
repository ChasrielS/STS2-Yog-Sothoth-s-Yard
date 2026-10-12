using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using STS2RitsuLib.Patching.Models;
using YogSothothsYard.Scripts.Card.HaaLouLingDish;
using YogSothothsYard.Scripts.UI;

namespace YogSothothsYard.Scripts.Patches;

public sealed class DishCookingChoiceClickPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_dish_cooking_choice_click";
    public static ModPatchTarget[] GetTargets() =>
        [PatchTarget.Method<NSimpleCardSelectScreen>("OnCardClicked", typeof(CardModel))];

    public static void Prefix(NSimpleCardSelectScreen __instance, CardModel card,
        HashSet<CardModel> ____selectedCards, CardSelectorPrefs ____prefs)
    {
        if (card is not HaaLouLingDishCard { CookingSelection: { } selection } dish) return;
        // 先记录最后一次点击，原版可能在本次点击中直接完成选择。
        selection.Toggle(dish, ____selectedCards.Contains(card), ____selectedCards.Count < ____prefs.MaxSelect);
        if (NDishCookingChoiceOutline.TryGet(__instance, out var display)) display.RefreshPreviews();
    }
}

public sealed class DishCookingChoiceResultPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_dish_cooking_choice_result";
    public static ModPatchTarget[] GetTargets() =>
        [PatchTarget.Method<NCardGridSelectionScreen>(nameof(NCardGridSelectionScreen.CardsSelected))];

    public static void Postfix(IReadOnlyList<CardModel> ____cards, ref Task<IEnumerable<CardModel>> __result)
    {
        // 返回选择顺序后，原版仍负责发送联机选择结果。
        if (____cards.FirstOrDefault() is HaaLouLingDishCard { CookingSelection: { } selection })
            __result = selection.InSelectionOrder(__result);
    }
}
