using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Shadow;

public sealed class ShadowCardLibraryControlsPatch : IPatchMethod
{
    private static readonly ConditionalWeakTable<NCardLibraryGrid, Filters> Live = new();
    private static readonly Action<NCardLibrary, bool> UpdateFilter =
        AccessTools.MethodDelegate<Action<NCardLibrary, bool>>(AccessTools.Method(typeof(NCardLibrary), "UpdateFilter"));

    public static string PatchId => "yog_sothoths_yard_shadow_card_library_controls";
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NCardLibrary), "UpdateFilter", [typeof(bool)]),
        new(typeof(NCardLibrary), nameof(NCardLibrary.OnSubmenuClosed), Type.EmptyTypes)
    ];

    internal static int ActiveRank(NCardLibraryGrid grid) => Live.TryGetValue(grid, out var filters) ? filters.Rank : 0;

    public static void Prefix(NCardLibrary __instance, MethodBase __originalMethod, IRunState? ____runState,
        NCardLibraryGrid ____grid, NLibraryStatTickbox ____viewUpgrades, NCardPoolFilter ____colorlessFilter,
        Dictionary<CharacterModel, NCardPoolFilter> ____cardPoolFilters)
    {
        var show = __originalMethod.Name == "UpdateFilter" && ____runState is RunState run &&
            ShadowRun.Level(run) > 0 && (____colorlessFilter.IsSelected ||
                ____cardPoolFilters.Any(entry => entry.Key is HaaLouLing && entry.Value.IsSelected));
        if (show)
            Live.GetValue(____grid, _ => new Filters(__instance, ____viewUpgrades)).SetVisible(true);
        else if (Live.TryGetValue(____grid, out var filters)) filters.SetVisible(false);
    }

    private sealed class Filters
    {
        private readonly NLibraryStatTickbox _upgrades;
        private readonly NLibraryStatTickbox[] _options = new NLibraryStatTickbox[3];
        internal int Rank { get; private set; }

        internal Filters(NCardLibrary library, NLibraryStatTickbox upgrades)
        {
            _upgrades = upgrades;
            library.AddChild(new NMultiplayerCardFilterHover());
            var parent = upgrades.GetParent();
            var scene = GD.Load<PackedScene>("res://scenes/screens/card_library/card_library_tickbox.tscn");
            for (var i = 0; i < _options.Length; i++)
            {
                var rank = i + 1;
                var option = scene.Instantiate<NLibraryStatTickbox>();
                option.Name = "YardShadowTier" + rank;
                parent.AddChild(option);
                option.SetLabel(NShadowSelector.Loc("PREVIEW_" + new string('I', rank)).GetFormattedText());
                option.IsTicked = false;
                option.Toggled += _ =>
                {
                    Rank = option.IsTicked ? rank : 0;
                    for (var j = 0; j < _options.Length; j++) _options[j].IsTicked = Rank == j + 1;
                    UpdateFilter(library, false);
                };
                _options[i] = option;
            }
            for (var i = 0; i < _options.Length; i++)
            {
                var option = _options[i];
                option.FocusNeighborTop = option.GetPathTo(i == 0 ? upgrades : _options[i - 1]);
                option.FocusNeighborBottom = i == _options.Length - 1 ? new NodePath(".") : option.GetPathTo(_options[i + 1]);
            }
        }

        internal void SetVisible(bool visible)
        {
            if (!visible) Rank = 0;
            foreach (var option in _options)
            {
                option.Visible = visible;
                if (!visible) option.IsTicked = false;
            }
            _upgrades.FocusNeighborBottom = visible ? _upgrades.GetPathTo(_options[0]) : new NodePath(".");
        }
    }
}

internal sealed partial class NMultiplayerCardFilterHover : Node
{
    private Control _checkbox = null!;
    private Control _back = null!;
    private Control.MouseFilterEnum _backMouseFilter;
    private int _checkboxLayer;

    public override void _Ready()
    {
        var library = GetParent<NCardLibrary>();
        _checkbox = library.GetNode<NLibraryStatTickbox>("%MultiplayerCards");
        _back = library.GetNode<Control>("%BackButton");
        _backMouseFilter = _back.MouseFilter;
        _checkboxLayer = _checkbox.ZIndex;
        _checkbox.MouseEntered += () => _checkbox.ZIndex = _back.ZIndex + 1;
        _checkbox.MouseExited += () => _checkbox.ZIndex = _checkboxLayer;
        _checkbox.VisibilityChanged += () =>
        {
            if (_checkbox.IsVisibleInTree()) return;
            _checkbox.ZIndex = _checkboxLayer;
            _back.MouseFilter = _backMouseFilter;
        };
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouse || !_checkbox.IsVisibleInTree()) return;

        // ZIndex changes drawing order only. Let the checkbox receive clicks in the overlapping area.
        var overlaps = IsMouseOver(_checkbox) && IsMouseOver(_back);
        _back.MouseFilter = overlaps ? Control.MouseFilterEnum.Ignore : _backMouseFilter;
    }

    private static bool IsMouseOver(Control control) =>
        new Rect2(Vector2.Zero, control.Size).HasPoint(control.GetLocalMousePosition());
}

public sealed class ShadowCardLibraryFilterPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_card_library_filter";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(NCardLibraryGrid), nameof(NCardLibraryGrid.FilterCards), [typeof(Func<CardModel, bool>), typeof(List<SortingOrders>)])];

    public static bool Prefix(NCardLibraryGrid __instance, Func<CardModel, bool> filter,
        List<SortingOrders> sortingPriority, List<CardModel> ____allCards)
    {
        var rank = ShadowCardLibraryControlsPatch.ActiveRank(__instance);
        if (rank == 0) return true;

        var variants = new List<CardModel>();
        foreach (var card in ____allCards)
        {
            if (card.Pool is not (HaaLouLingCardPool or ColorlessCardPool) || !ShadowCardRules.TryGet(card, rank, out _)) continue;
            var variant = (CardModel)card.MutableClone();
            ShadowRanks.Set(variant, rank);
            if (filter(variant)) variants.Add(variant);
        }
        __instance.SetCards(variants, PileType.None, sortingPriority, Task.CompletedTask);
        return false;
    }
}

public sealed class ShadowCardLibraryVisibilityPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_card_library_visibility";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(NCardLibraryGrid), "GetCardVisibility", [typeof(CardModel)])];

    public static void Postfix(NCardLibraryGrid __instance, CardModel card, ref ModelVisibility __result)
    {
        var rank = ShadowCardLibraryControlsPatch.ActiveRank(__instance);
        if (rank > 0 && ShadowRanks.Of(card) == rank)
            __result = ShadowCardDiscovery.HasSeen(card, rank) ? ModelVisibility.Visible : ModelVisibility.NotSeen;
    }
}

public sealed class ShadowCardLibraryInspectPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_card_library_inspect";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(NCardLibrary), "ShowCardDetail", [typeof(NCardHolder)])];

    public static bool Prefix(NCardHolder holder, NCardLibraryGrid ____grid, NLibraryStatTickbox ____viewUpgrades,
        ref Control? ____lastHoveredControl)
    {
        if (holder.CardModel is not { } card || NGame.Instance is not { } game) return false;
        var rank = ShadowRanks.Of(card);
        if (rank == 0) return true;
        if (!ShadowCardDiscovery.HasSeen(card, rank)) return false;

        var visible = ____grid.VisibleCards.Where(candidate =>
            ShadowRanks.Of(candidate) == rank && ShadowCardDiscovery.HasSeen(candidate, rank)).ToList();
        var index = visible.IndexOf(card);
        if (index >= 0)
        {
            ____lastHoveredControl = holder;
            game.GetInspectCardScreen().Open(visible, index, ____viewUpgrades.IsTicked);
        }
        return false;
    }
}
