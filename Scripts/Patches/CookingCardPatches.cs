using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens;
using STS2RitsuLib.Patching.Models;
using YogSothothsYard.Scripts.Card.HaaLouLingDish;

namespace YogSothothsYard.Scripts.Patches;

internal static class CookingCardPileRules
{
    internal static bool CanEnter(CardModel card, CardPile pile)
    {
        if (card is not (IHaaLouLingDishCard or IHaaLouLingIngredientCard)) return true;
        // 烹饪菜单等展示用牌堆不属于玩家的实际牌堆，不限制预览卡入堆。
        if (!card.Owner.Piles.Contains(pile)) return true;
        return card is IHaaLouLingDishCard ? pile.Type == PileType.Play : pile.Type == Entry.FoodPile;
    }

    internal static Task Remove(CardModel card, bool skipVisuals)
    {
        if (card.Pile is { IsCombatPile: true }) return CardPileCmd.RemoveFromCombat(card, skipVisuals);
        card.RemoveFromState();
        return Task.CompletedTask;
    }
}

public sealed class CookingRecipePreviewPilePatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_cooking_recipe_preview_pile";
    public static ModPatchTarget[] GetTargets() => [PatchTarget.Getter<CardModel>(nameof(CardModel.Pile))];

    public static bool Prefix(CardModel __instance, ref CardPile? __result)
    {
        if (__instance is not HaaLouLingDishCard { IsRecipePreview: true }) return true;
        __result = null;
        return false;
    }
}

public sealed class CookingRecipeOverlayPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_cooking_recipe_overlay";
    public static ModPatchTarget[] GetTargets() => [new(typeof(NCard), "ReloadOverlay", Type.EmptyTypes)];

    public static void Postfix(NCard __instance, ref Control? ____cardOverlay)
    {
        if (____cardOverlay != null || __instance.Model is not HaaLouLingDishCard { IsRecipePreview: true } dish ||
            dish.Owner?.Creature.CombatState is not { } combat) return;

        AfflictionModel? affliction = dish.Type switch
        {
            CardType.Skill when combat.Enemies.Any(enemy => enemy.HasPower<VitalSparkPower>()) => ModelDb.Affliction<Tainted>(),
            CardType.Power when combat.Enemies.Any(enemy => enemy.HasPower<GalvanicPower>()) => ModelDb.Affliction<Galvanized>(),
            _ => null
        };
        if (affliction == null) return;

        ____cardOverlay = affliction.CreateOverlay();
        __instance.OverlayContainer.AddChildSafely(____cardOverlay);
    }
}

public sealed class DishInspectUpgradePatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_dish_inspect_upgrade";
    public static ModPatchTarget[] GetTargets() => [PatchTarget.Method<NCard>(nameof(NCard.ShowUpgradePreview))];

    public static void Prefix(NCard __instance)
    {
        if (__instance.GetParent() is not NInspectCardScreen || __instance.Model is not HaaLouLingDishCard
            { IsUpgraded: true, UpgradePreviewType: CardUpgradePreviewType.None } dish) return;

        // An already upgraded dish has no pending upgrade flags on the inspect screen's clone.
        var upgraded = ModelDb.GetById<CardModel>(dish.Id).ToMutable();
        upgraded.UpgradeInternal();
        foreach (var (name, variable) in upgraded.DynamicVars)
            if (variable.WasJustUpgraded && dish.DynamicVars.TryGetValue(name, out var value))
                value.UpgradeValueBy(0);
    }
}

public sealed class CookingCardPlayPermissionPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_cooking_card_play_permission";
    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method(typeof(Hook), nameof(Hook.ShouldPlay), typeof(ICombatState), typeof(CardModel),
            typeof(AbstractModel).MakeByRefType(), typeof(AutoPlayType))
    ];

    public static bool Prefix(CardModel card, ref AbstractModel? preventer, ref bool __result)
    {
        if (card is not IHaaLouLingIngredientCard) return true;
        preventer = card;
        __result = false;
        return false;
    }
}

public sealed class CookingCardPileAddPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_cooking_card_pile_add";
    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method(typeof(CardPileCmd), nameof(CardPileCmd.Add), typeof(IEnumerable<CardModel>),
            typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool), typeof(bool))
    ];

    public static bool Prefix(ref IEnumerable<CardModel> cards, CardPile newPile, CardPilePosition position,
        AbstractModel? clonedBy, bool skipVisuals, bool isChangingOwners,
        ref Task<IReadOnlyList<CardPileAddResult>> __result)
    {
        var snapshot = cards as IReadOnlyList<CardModel> ?? cards.ToList();
        cards = snapshot;
        if (snapshot.All(card => CookingCardPileRules.CanEnter(card, newPile))) return true;
        __result = RouteCards(snapshot, newPile, position, clonedBy, skipVisuals, isChangingOwners);
        return false;
    }

    private static async Task<IReadOnlyList<CardPileAddResult>> RouteCards(IReadOnlyList<CardModel> cards, CardPile newPile,
        CardPilePosition position, AbstractModel? clonedBy, bool skipVisuals, bool isChangingOwners)
    {
        var results = new Dictionary<CardModel, CardPileAddResult>();
        var allowed = cards.Where(card => CookingCardPileRules.CanEnter(card, newPile)).ToList();
        var added = await CardPileCmd.Add(allowed, newPile, position, clonedBy, skipVisuals, isChangingOwners);
        for (var i = 0; i < allowed.Count; i++) results[allowed[i]] = added[i];

        foreach (var card in cards.Where(card => !CookingCardPileRules.CanEnter(card, newPile)))
        {
            var oldPile = card.Pile;
            var targetPile = PileType.None;
            if (card is IHaaLouLingIngredientCard && card.Owner.PlayerCombatState != null)
            {
                targetPile = Entry.FoodPile;
                if (oldPile?.Type != targetPile)
                {
                    results[card] = (await CardPileCmd.Add([card], targetPile.GetPile(card.Owner), position,
                        clonedBy, skipVisuals, isChangingOwners))[0];
                    continue;
                }
            }
            else await CookingCardPileRules.Remove(card, skipVisuals);
            results[card] = new CardPileAddResult
            {
                cardAdded = card,
                oldPile = oldPile,
                targetPile = targetPile,
                success = false
            };
        }
        return cards.Select(card => results[card]).ToList();
    }
}

public sealed class CookingCardPileInternalAddPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_cooking_card_pile_internal_add";
    public static ModPatchTarget[] GetTargets() =>
        [PatchTarget.Method<CardPile>(nameof(CardPile.AddInternal), typeof(CardModel), typeof(int), typeof(bool))];

    public static bool Prefix(CardPile __instance, CardModel card, bool silent)
    {
        if (CookingCardPileRules.CanEnter(card, __instance)) return true;
        // 覆盖初始化、变牌等直接入堆的路径，不让卡牌先进入非法牌堆。
        if (card is IHaaLouLingIngredientCard && card.Owner.PlayerCombatState != null)
        {
            var foodPile = Entry.FoodPile.GetPile(card.Owner);
            if (card.Pile != foodPile)
            {
                card.RemoveFromCurrentPile(silent);
                foodPile.AddInternal(card, silent: silent);
            }
        }
        else card.RemoveFromState();
        return false;
    }
}

public sealed class CookingCardExhaustPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_cooking_card_exhaust";
    public static ModPatchTarget[] GetTargets() =>
        [PatchTarget.Method(typeof(CardCmd), nameof(CardCmd.Exhaust),
            typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool), typeof(bool))];

    public static bool Prefix(CardModel card, bool skipVisuals, ref Task<CardPileAddResult?> __result)
    {
        if (card is not (IHaaLouLingDishCard or IHaaLouLingIngredientCard)) return true;
        __result = RemoveWithoutExhaust(card, skipVisuals);
        return false;
    }

    private static async Task<CardPileAddResult?> RemoveWithoutExhaust(CardModel card, bool skipVisuals)
    {
        if (card is IHaaLouLingDishCard) await CookingCardPileRules.Remove(card, skipVisuals);
        else if (card.Owner.PlayerCombatState != null && card.Pile?.Type != Entry.FoodPile)
            await CardPileCmd.Add(card, Entry.FoodPile, skipVisuals: skipVisuals);
        return null;
    }
}
