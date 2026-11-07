using System;
using System.Reflection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib;
using STS2RitsuLib.Audio;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.RuntimeInput;
using STS2RitsuLib.Unlocks;
using STS2RitsuLib.Combat.CardTargeting;
using YogSothothsYard.Scripts.Patches;
using YogSothothsYard.Scripts.Cooking;
using YogSothothsYard.Scripts.UI;

namespace YogSothothsYard.Scripts;

[ModInitializer(nameof(Init))]
public class Entry
{
    public const string ModId = "Yog-Sothoth's Yard";
    public const string HaaLouLingTransitionSfx = "event:/yog_sothoths_yard/sfx/character_transition";
    public static readonly Logger Logger = RitsuLibFramework.CreateLogger(ModId);
    public static readonly ModPatcher Patcher = RitsuLibFramework.CreatePatcher(
        ModId,
        "YogSothothsYardPatcher",
        "yog_sothoths_yard_patch_log.txt"
    );

    public static PileType FoodPile;
    public static TargetType OtherPlayerTarget;

    internal const string FoodPileIconPath = "res://images/packed/combat_ui/food_pile.png";
    private const string FoodPileEmptyMessageKey = "YOG_SOTHOTH_S_YARD_CARDPILE_FOOD_PILE.empty";
    private const string CannotCookMessageKey = "YOG_SOTHOTH_S_YARD_COOKING_CANNOT_COOK_NOW";
    private const string NoCookableDishesMessageKey = "YOG_SOTHOTH_S_YARD_COOKING_NO_COOKABLE_DISHES";
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
        VirtualFmodEventRegistry.RegisterOneShots(new Dictionary<string, string>
        {
            [HaaLouLingTransitionSfx] = "res://audio/sfx/haa_lou_ling/character_transition.wav"
        }, busPath: FmodStudioRouting.SfxBus);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
        YardStory.RegisterTimelineLayout();
        CookingMenuPreferences.Register();
        ModUnlockRegistry.For(ModId).RegisterPostRunRule(PostRunEpochUnlockRule.Create(
            XunRenQiChengEpoch.EpochId,
            "用铁甲战士结束一局普通游戏后获得寻人启程。",
            context => context.CharacterId == ModelDb.Character<Ironclad>().Id
        ));
        Patcher.RegisterPatch<FoodPileRightClickPatch>();
        Patcher.RegisterPatch<DishRecipeHoverTipLayoutPatch>();
        Patcher.RegisterPatch<AncientGlimpseRemovalPatch>();
        Patcher.RegisterPatch<DaoShiPersistencePatch>();
        Patcher.RegisterPatch<HaaLouLingPotionLargeImagePatch>();
        Patcher.RegisterPatch<YardTimelineRunUnlockPatch>();
        Patcher.RegisterPatch<CookingCardPlayPermissionPatch>();
        Patcher.RegisterPatch<IngredientHookListenersPatch>();
        Patcher.RegisterPatch<CookingRecipePreviewPilePatch>();
        Patcher.RegisterPatch<CookingCardPileAddPatch>();
        Patcher.RegisterPatch<CookingCardPileInternalAddPatch>();
        Patcher.RegisterPatch<CookingCardExhaustPatch>();
        Patcher.RegisterPatch<DishCookingChoiceClickPatch>();
        Patcher.RegisterPatch<DishCookingChoiceResultPatch>();
        Patcher.RegisterPatch<IngredientSelectionClickPatch>();
        Patcher.RegisterPatch<SheZhangResourcePermissionPatch>();
        Patcher.RegisterPatch<SheZhangEnergyPaymentPatch>();
        Patcher.PatchAll();
        NCookingQueueDisplay.Register();
        NFoodPileMotion.Register();
        NDishEffectChoicePrompt.Register();
        NDishCookingChoiceOutline.Register();
        NIngredientSelection.Register();
        NMultiplayerIngredientCount.Register();
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
                var knife = ModelDb.Power<DaoShiPower>().ToMutable();
                // 原版不允许以零层添加能力，先静默绑定，再保留真实的零层计数。
                knife.ApplyInternal(player.Creature, 1, silent: true);
                knife.SetAmount(0, silent: true);
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
        if (ActiveScreenContext.Instance.GetCurrentScreen() is NCardPileScreen screen && screen.Pile.Type == FoodPile)
        {
            NCapstoneContainer.Instance?.Close();
            return;
        }
        FindFoodPileButton()?.TriggerOpen();
    }

    internal static void OpenCookingScreen()
    {
        if (ActiveScreenContext.Instance.GetCurrentScreen() is NCardPileScreen { Pile: CookingRecipePile } ||
            FindFoodPileButton() != null) CookingRecipeScreen.ShowForLocalPlayer();
    }

    private static NModCardPileButton? FindFoodPileButton()
    {
        if (!CombatManager.Instance.IsInProgress) return null;
        var screen = ActiveScreenContext.Instance.GetCurrentScreen() as Godot.Node;
        return screen?.FindChildren("*", recursive: true, owned: false).OfType<NModCardPileButton>()
            .FirstOrDefault(button => button.Definition?.PileType == FoodPile && button.IsVisibleInTree());
    }

    public static void ShowFoodPileEmptyMessage(MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        ShowThoughtMessage(player, "static_hover_tips", FoodPileEmptyMessageKey);
    }

    public static void ShowCannotCookMessage(MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        ShowThoughtMessage(player, "gameplay_ui", CannotCookMessageKey);
    }

    public static void ShowNoCookableDishesMessage(MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        ShowThoughtMessage(player, "gameplay_ui", NoCookableDishesMessageKey);
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
        var vfx = NThoughtBubbleVfx.Create(message.GetFormattedText(), player.Creature, 2.0);
        if (vfx == null) return;
        NCombatVfxVisibility.Attach(vfx);
        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfx);
    }
}
