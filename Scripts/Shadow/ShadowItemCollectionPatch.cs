using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.PotionLab;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Shadow;

public sealed class ShadowItemCollectionControlsPatch : IPatchMethod
{
    private static readonly ConditionalWeakTable<NSubmenu, RankPicker> Pickers = new();
    private static readonly AccessTools.FieldRef<NRelicCollection, List<RelicModel>> RelicList =
        AccessTools.FieldRefAccess<NRelicCollection, List<RelicModel>>("_relics");

    public static string PatchId => "yog_sothoths_yard_shadow_item_collection_controls";
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NRelicCollection), nameof(NSubmenu.OnSubmenuOpened), Type.EmptyTypes),
        new(typeof(NPotionLab), nameof(NSubmenu.OnSubmenuOpened), Type.EmptyTypes),
        new(typeof(NRelicCollection), nameof(NSubmenu.OnSubmenuClosed), Type.EmptyTypes),
        new(typeof(NPotionLab), nameof(NSubmenu.OnSubmenuClosed), Type.EmptyTypes)
    ];

    internal static int Rank(NSubmenu screen) => Pickers.TryGetValue(screen, out var picker) ? picker.Rank : 0;

    public static void Prefix(NSubmenu __instance, MethodBase __originalMethod, ref Control? ____lastFocusedControl)
    {
        var run = RunManager.Instance.DebugOnlyGetState();
        if (__originalMethod.Name == nameof(NSubmenu.OnSubmenuClosed) || NRun.Instance == null || ShadowRun.Level(run) == 0)
        {
            if (Pickers.TryGetValue(__instance, out var picker)) picker.Reset();
            return;
        }

        Pickers.GetValue(__instance, static screen => new RankPicker(screen)).Panel.Show();
        ____lastFocusedControl = null;
        if (LocalContext.GetMe(run) is not { } player) return;
        foreach (var relic in player.Relics) ShadowItemDiscovery.Observe(relic);
        foreach (var potion in player.Potions) ShadowItemDiscovery.Observe(potion);
    }

    public static void Postfix(NSubmenu __instance, MethodBase __originalMethod)
    {
        if (__originalMethod.Name != nameof(NSubmenu.OnSubmenuOpened) || !Pickers.TryGetValue(__instance, out var picker)) return;
        var rows = new List<IReadOnlyList<Control>>();
        foreach (var category in Categories(__instance))
        {
            if (category is NRelicCollectionCategory relics)
            {
                foreach (var subcategory in relics.GetChildren().OfType<NRelicCollectionCategory>())
                    subcategory.Visible = picker.Rank == 0 || subcategory.GetGridItems().Count > 0;
            }
            var categoryRows = category switch
            {
                NRelicCollectionCategory relicCategory => relicCategory.GetGridItems(),
                NPotionLabCategory potionCategory => potionCategory.GetGridItems(),
                _ => []
            };
            category.Visible = picker.Rank == 0 || categoryRows.Count > 0;
            rows.AddRange(categoryRows);
        }
        if (picker.Rank != 0 && __instance is NRelicCollection collection)
        {
            var list = RelicList(collection);
            list.Clear();
            list.AddRange(rows.SelectMany(row => row).OfType<NRelicCollectionEntry>().Select(entry => entry.relic));
        }
        if (picker.Panel.Visible) picker.ConnectFocus(rows);
    }

    internal static Control? InitialFocus(NSubmenu screen, Control? native)
    {
        if (!Pickers.TryGetValue(screen, out var picker) || !picker.Panel.Visible) return native;
        var focused = screen.GetViewport().GuiGetFocusOwner();
        return focused != null && picker.Panel.IsAncestorOf(focused) ? focused : native ?? picker.DefaultFocus;
    }

    private static IEnumerable<Control> Categories(NSubmenu screen) => screen.GetNode(screen is NRelicCollection
        ? "%Relics" : "%ScreenContents/Content/PotionsContainer").GetChildren().OfType<Control>();

    private sealed class RankPicker
    {
        private readonly NLibraryStatTickbox[] _buttons = new NLibraryStatTickbox[3];
        internal VBoxContainer Panel { get; }
        internal Control? DefaultFocus { get; private set; }
        internal int Rank { get; private set; }

        internal RankPicker(NSubmenu screen)
        {
            Panel = new VBoxContainer { Name = "YardShadowRanks", MouseFilter = Control.MouseFilterEnum.Ignore };
            Panel.AddThemeConstantOverride("separation", 0);
            screen.AddChild(Panel);
            Panel.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
            Panel.OffsetLeft = -344f;
            Panel.OffsetRight = -104f;
            Panel.OffsetTop = -154f;
            Panel.OffsetBottom = -28f;

            var scene = GD.Load<PackedScene>("res://scenes/screens/card_library/card_library_tickbox.tscn");
            for (var i = 0; i < _buttons.Length; i++)
            {
                var rank = i + 1;
                var button = scene.Instantiate<NLibraryStatTickbox>();
                Panel.AddChild(button);
                button.SetLabel(NShadowSelector.Loc("PREVIEW_" + new string('I', rank)).GetFormattedText());
                button.IsTicked = false;
                _buttons[i] = button;
                button.Toggled += _ =>
                {
                    Rank = button.IsTicked ? rank : 0;
                    for (var j = 0; j < _buttons.Length; j++) _buttons[j].IsTicked = Rank == j + 1;
                    Reload(screen);
                };
            }
        }

        internal void Reset()
        {
            Rank = 0;
            Panel.Hide();
            foreach (var button in _buttons) button.IsTicked = false;
        }

        internal void ConnectFocus(IReadOnlyList<IReadOnlyList<Control>> rows)
        {
            DefaultFocus = rows.FirstOrDefault()?.FirstOrDefault() ?? _buttons[0];
            var selected = _buttons[Math.Max(0, Rank - 1)];
            foreach (var row in rows) row[^1].FocusNeighborRight = row[^1].GetPathTo(selected);
            for (var i = 0; i < _buttons.Length; i++)
            {
                var button = _buttons[i];
                button.FocusNeighborLeft = button.GetPathTo(rows.FirstOrDefault()?.LastOrDefault() ?? button);
                button.FocusNeighborRight = new NodePath(".");
                button.FocusNeighborTop = button.GetPathTo(_buttons[Math.Max(0, i - 1)]);
                button.FocusNeighborBottom = button.GetPathTo(_buttons[Math.Min(_buttons.Length - 1, i + 1)]);
            }
        }

        private static void Reload(NSubmenu screen)
        {
            NHoverTipSet.Clear();
            foreach (var category in Categories(screen))
            {
                if (category is NRelicCollectionCategory)
                {
                    foreach (var child in category.GetChildren().OfType<NRelicCollectionCategory>().ToArray())
                        Remove(category, child);
                }
                var grid = category.GetNode<GridContainer>(category is NRelicCollectionCategory ? "%RelicsContainer" : "%PotionsContainer");
                foreach (var child in grid.GetChildren()) Remove(grid, child);
            }
            screen.OnSubmenuOpened();
        }

        private static void Remove(Node parent, Node child)
        {
            parent.RemoveChild(child);
            child.QueueFree();
        }
    }
}

public sealed class ShadowItemCollectionFocusPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_item_collection_focus";
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NRelicCollection), "get_InitialFocusedControl", Type.EmptyTypes),
        new(typeof(NPotionLab), "get_InitialFocusedControl", Type.EmptyTypes)
    ];

    public static void Postfix(NSubmenu __instance, ref Control? __result) =>
        __result = ShadowItemCollectionControlsPatch.InitialFocus(__instance, __result);
}

public sealed class ShadowRelicCollectionFilterPatch : IPatchMethod
{
    private static readonly ConditionalWeakTable<RelicModel, Preview> Previews = new();

    public static string PatchId => "yog_sothoths_yard_shadow_relic_collection_filter";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(NRelicCollectionCategory), "LoadRelicNodes", [typeof(IEnumerable<RelicModel>), typeof(HashSet<RelicModel>), typeof(HashSet<RelicModel>)])];

    public static void Prefix(NRelicCollection ____collection, ref IEnumerable<RelicModel> relics,
        ref HashSet<RelicModel> seenRelics, ref HashSet<RelicModel> unlockedRelics)
    {
        var rank = ShadowItemCollectionControlsPatch.Rank(____collection);
        if (rank == 0) return;

        var variants = new List<RelicModel>();
        var seen = new HashSet<RelicModel>();
        var unlocked = new HashSet<RelicModel>();
        foreach (var relic in relics)
        {
            if (!ShadowRelicRules.Supports(relic, rank)) continue;
            var variant = ShadowRelicRules.CreatePreview(relic.CanonicalInstance, rank);
            ShadowTreasureRelicClonePatch.MarkPreview(variant);
            variants.Add(variant);
            var isUnlocked = unlockedRelics.Contains(relic);
            var isSeen = isUnlocked && ShadowItemDiscovery.HasSeen(relic, rank);
            if (isUnlocked) unlocked.Add(variant);
            if (isSeen) seen.Add(variant);
            Previews.Add(variant, new Preview(isSeen));
        }
        relics = variants;
        seenRelics = seen;
        unlockedRelics = unlocked;
    }

    internal static bool? Seen(RelicModel relic) => Previews.TryGetValue(relic, out var preview) ? preview.Seen : null;

    private sealed record Preview(bool Seen);
}

public sealed class ShadowRelicCollectionInspectPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_relic_collection_inspect";
    public static ModPatchTarget[] GetTargets() => [new(typeof(SaveManager), nameof(SaveManager.IsRelicSeen), [typeof(RelicModel)])];

    public static void Postfix(RelicModel relic, ref bool __result) =>
        __result = ShadowRelicCollectionFilterPatch.Seen(relic) ?? __result;
}

public sealed class ShadowPotionLabFilterPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_potion_lab_filter";
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NPotionLabCategory), nameof(NPotionLabCategory.LoadPotions),
            [typeof(PotionRarity), typeof(LocString), typeof(HashSet<PotionModel>), typeof(UnlockState), typeof(HashSet<PotionModel>), typeof(PotionRarity?)])
    ];

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var allPotions = AccessTools.PropertyGetter(typeof(ModelDb), nameof(ModelDb.AllPotions));
        var createHolder = AccessTools.Method(typeof(NLabPotionHolder), nameof(NLabPotionHolder.Create));
        var filteredPotions = AccessTools.DeclaredMethod(typeof(ShadowPotionLabFilterPatch), nameof(FilterPotions));
        var createVariant = AccessTools.DeclaredMethod(typeof(ShadowPotionLabFilterPatch), nameof(CreateHolder));
        foreach (var code in instructions)
        {
            var replacement = code.Calls(allPotions) ? filteredPotions : code.Calls(createHolder) ? createVariant : null;
            if (replacement != null)
            {
                var category = new CodeInstruction(OpCodes.Ldarg_0);
                category.labels.AddRange(code.labels);
                category.blocks.AddRange(code.blocks);
                yield return category;
                yield return new CodeInstruction(OpCodes.Call, replacement);
            }
            else yield return code;
        }
    }

    private static int Rank(NPotionLabCategory category) =>
        category.GetNodeOrNull<NPotionLab>("../../../..") is { } lab ? ShadowItemCollectionControlsPatch.Rank(lab) : 0;

    private static IEnumerable<PotionModel> FilterPotions(NPotionLabCategory category)
    {
        var rank = Rank(category);
        return rank == 0 ? ModelDb.AllPotions : ModelDb.AllPotions.Where(potion => ShadowPotionRules.Supports(potion, rank));
    }

    private static NLabPotionHolder CreateHolder(PotionModel potion, ModelVisibility visibility, NPotionLabCategory category)
    {
        var rank = Rank(category);
        if (rank == 0 || visibility == ModelVisibility.Locked) return NLabPotionHolder.Create(potion, visibility);
        ShadowRanks.Set(potion, rank);
        return NLabPotionHolder.Create(potion,
            ShadowItemDiscovery.HasSeen(potion, rank) ? ModelVisibility.Visible : ModelVisibility.NotSeen);
    }
}
