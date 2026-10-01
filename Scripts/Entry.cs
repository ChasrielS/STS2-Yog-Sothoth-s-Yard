using System;
using System.Reflection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.RuntimeInput;
using STS2RitsuLib.Scaffolding.Cards.HandOutline;
using STS2RitsuLib.Combat.CardTargeting;
using YogSothothsYard.Scripts.Patches;
using YogSothothsYard.Scripts.Cooking;
using YogSothothsYard.Scripts.UI;

namespace YogSothothsYard.Scripts;

[ModInitializer(nameof(Init))]
public class Entry
{
    public const string ModId = "Yog-Sothoth's Yard";
    public static readonly Logger Logger = RitsuLibFramework.CreateLogger(ModId);
    public static readonly ModPatcher Patcher = RitsuLibFramework.CreatePatcher(
        ModId,
        "YogSothothsYardPatcher",
        "yog_sothoths_yard_patch_log.txt"
    );

    public static PileType FoodPile;
    public static TargetType OtherPlayerTarget;

    private const string FoodPileIconPath = "res://images/packed/combat_ui/food_pile.png";
    private const string FoodPileEmptyMessageKey = "YOG_SOTHOTH_S_YARD_CARDPILE_FOOD_PILE.empty";
    private const string NoCookableDishesMessageKey = "YOG_SOTHOTH_S_YARD_COOKING_NO_COOKABLE_DISHES";
    private const string CannotCookMessageKey = "YOG_SOTHOTH_S_YARD_COOKING_CANNOT_COOK_NOW";
    private static IRuntimeHotkeyHandle? FoodPileHotkeyHandle;
    private static IRuntimeHotkeyHandle? CookingHotkeyHandle;
    private static IDisposable? CombatStartingSubscription;

    public static void Init()
    {
        var assembly = Assembly.GetExecutingAssembly();
        OtherPlayerTarget = CustomTargetType.RegisterSingleTargetType(ModId, "other_player",
            (creature, player) => creature.IsAlive && creature.IsPlayer && creature.Player != player);
        var registry = ModCardPileRegistry.For(ModId);
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
        Patcher.RegisterPatch<FoodPileRightClickPatch>();
        Patcher.RegisterPatch<DishRecipeHoverTipLayoutPatch>();
        Patcher.RegisterPatch<AncientGlimpseRemovalPatch>();
        Patcher.PatchAll();
        ModCardHandOutlineRegistry.Register<HaaLouLingCard>(
            ModCardHandOutlineRules.Fixed<HaaLouLingCard>(card => card.WantsGreenOutline, Godot.Colors.Green));
        NCookingQueueDisplay.Register();
        CombatStartingSubscription = RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(
            OnCombatStarting,
            replayCurrentState: false
        );

        ModContentRegistry.For(ModId).RegisterCardLibraryCompendiumSharedPoolFilter<HaaLouLingFoodPool>(
            "haa_lou_ling_food_pool",
            "res://images/packed/combat_ui/food_pile.png",
            [
                new CardLibraryCompendiumPlacementRule
                {
                    ModCharacterModelIdEntry = "YOG_SOTHOTH_S_YARD_CHARACTER_HAA_LOU_LING",
                    Relation = CardLibraryCompendiumFilterInsertRelation.After
                }
            ]
        );

        FoodPile = registry.RegisterOwned("food_pile", new ModCardPileSpec
        {
            Scope = ModCardPileScope.CombatOnly,
            Style = ModCardPileUiStyle.BottomLeft,
            Anchor = ModCardPileAnchor.Default,
            IconPath = FoodPileIconPath,
            OnOpen = ctx => HaaLouLingPileScreen.Show(ctx.Pile),
            VisibleWhen = ctx => ctx.Player?.Character is HaaLouLing,
        }).PileType;

        RegisterFoodPileHotkey();
        RegisterCookingHotkey();
    }

    private static void OnCombatStarting(CombatStartingEvent combatEvent)
    {
        if (combatEvent.CombatState == null)
        {
            return;
        }

        foreach (var player in combatEvent.CombatState.Players)
        {
            if (player.Character is not HaaLouLing ||
                player.Creature.GetPower<HaaLouLingCookingPower>() != null)
            {
                continue;
            }

            var cookingPower = ModelDb.Power<HaaLouLingCookingPower>().ToMutable();
            cookingPower.ApplyInternal(player.Creature, 1, silent: true);
            if (!player.Creature.HasPower<DaoShiPower>())
            {
                var gauge = ModelDb.Power<DaoShiGaugePower>().ToMutable();
                gauge.ApplyInternal(player.Creature, 1, silent: true);
            }
        }
    }

    private static void RegisterCookingHotkey()
    {
        CookingHotkeyHandle = RuntimeHotkeyService.Register(
            "C",
            OpenCookingScreen,
            new RuntimeHotkeyOptions
            {
                Id = "yog_sothoths_yard.open_cooking_screen",
                DisplayName = "打开烹饪界面",
                Description = "查看霞露零当前可烹饪的菜肴",
                Category = "Yog-Sothoth's Yard",
            }
        );
    }

    private static void RegisterFoodPileHotkey()
    {
        RuntimeHotkeyService.Initialize();
        FoodPileHotkeyHandle = RuntimeHotkeyService.Register(
            "F",
            OpenFoodPile,
            new RuntimeHotkeyOptions
            {
                Id = "yog_sothoths_yard.open_food_pile",
                DisplayName = "查看食材",
                Description = "查看霞露零拥有的食材",
                Category = "Yog-Sothoth's Yard",
            }
        );
    }

    private static void OpenFoodPile()
    {
        if (!CombatManager.Instance.IsInProgress)
        {
            return;
        }

        var player = LocalContext.GetMe(CombatManager.Instance.DebugOnlyGetState());
        if (player?.Character is not HaaLouLing)
        {
            return;
        }

        var pile = FoodPile.GetPile(player);
        var capstoneContainer = NCapstoneContainer.Instance;
        if (!CanUseCombatPileHotkey(screen => screen.Pile == pile))
        {
            return;
        }

        if (pile.IsEmpty)
        {
            ShowFoodPileEmptyMessage(player);
            return;
        }

        if (capstoneContainer?.CurrentCapstoneScreen is NCardPileScreen screen &&
            screen.Pile == pile)
        {
            capstoneContainer.Close();
            return;
        }

        HaaLouLingPileScreen.Show(pile);
    }

    private static void OpenCookingScreen()
    {
        if (!CombatManager.Instance.IsInProgress)
        {
            return;
        }

        var player = LocalContext.GetMe(CombatManager.Instance.DebugOnlyGetState());
        if (player?.Character is HaaLouLing &&
            CanUseCombatPileHotkey(screen => screen.Pile is Cooking.CookingRecipePile))
        {
            YogSothothsYard.Scripts.UI.CookingRecipeScreen.Show(player);
        }
    }

    private static bool CanUseCombatPileHotkey(Func<NCardPileScreen, bool> isOwnScreen)
    {
        var capstone = NCapstoneContainer.Instance;
        if (capstone?.InUse == true)
        {
            return capstone.CurrentCapstoneScreen is NCardPileScreen screen && isOwnScreen(screen);
        }

        var drawPile = NCombatRoom.Instance?.Ui.DrawPile;
        return drawPile is { IsEnabled: true } && drawPile.IsVisibleInTree();
    }

    public static void ShowFoodPileEmptyMessage(MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        ShowThoughtMessage(player, "static_hover_tips", FoodPileEmptyMessageKey);
    }

    public static void ShowNoCookableDishesMessage(MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        ShowThoughtMessage(player, "gameplay_ui", NoCookableDishesMessageKey);
    }

    public static void ShowCannotCookMessage(MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        ShowThoughtMessage(player, "gameplay_ui", CannotCookMessageKey);
    }

    private static void ShowThoughtMessage(
        MegaCrit.Sts2.Core.Entities.Players.Player player,
        string table,
        string key
    )
    {
        var capstoneContainer = NCapstoneContainer.Instance;
        if (capstoneContainer?.InUse == true)
        {
            capstoneContainer.Close();
        }

        var message = new LocString(table, key);
        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(
            NThoughtBubbleVfx.Create(message.GetFormattedText(), player.Creature, 2.0)
        );
    }
}
