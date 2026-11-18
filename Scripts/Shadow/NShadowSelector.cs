using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.Fonts;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Ftue;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Networking.Sidecar;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Godot.NodeAttachments;

namespace YogSothothsYard.Scripts.Shadow;

public partial class NShadowSelector : Control
{
    private const string LocPrefix = "YOG_SOTHOTH_S_YARD_SHADOW_";
    private const string LeftArrow = "HBoxContainer/LeftArrowContainer/LeftArrow";
    private const string RightArrow = "HBoxContainer/RightArrowContainer/RightArrow";
    private static readonly AccessTools.FieldRef<NAscensionPanel, bool> ArrowsVisible =
        AccessTools.FieldRefAccess<NAscensionPanel, bool>("_arrowsVisible");
    private static readonly RitsuLibSidecarMessageDescriptor<LobbyDifficulty> DifficultyMessage = new(
        Entry.ModId, "shadow_lobby_difficulty",
        static difficulty => [(byte)difficulty.Level, (byte)difficulty.MaxLevel],
        static data => data.Length == 2 ? new LobbyDifficulty(data[0], data[1]) : new LobbyDifficulty(-1, -1));
    private static readonly ConditionalWeakTable<StartRunLobby, Preview> ReceivedPreviews = new();

    private NAscensionPanel _panel = null!;
    private NButton _nativeLeft = null!;
    private NButton _nativeRight = null!;
    private NButton _left = null!;
    private NButton _right = null!;
    private bool _refreshing;
    private bool? _isClient;
    private int _displayedMaxLevel;
    private (StartRunLobby Lobby, string? Character)? _selectionOwner;
    private readonly Dictionary<(StartRunLobby Lobby, string Character), int> _singleplayerSelections = [];
    private readonly Dictionary<ulong, LobbyDifficulty> _sentPreviews = [];
    private IDisposable? _previewSubscription;
    private StartRunLobby? _hostingLobby;
    private double _previewRetry;
    private LobbyDifficulty _lastPreview;

    internal static void Register() => ModNodeAttachmentRegistry.For(Entry.ModId)
        .RegisterReadyChild<NCharacterSelectScreen, NShadowSelector>(
            "shadow_difficulty", static _ => new NShadowSelector());

    internal static LocString Loc(string key) => new("gameplay_ui", LocPrefix + key);

    internal static int DisplayedLobbyLevel(StartRunLobby lobby) => lobby.NetService.Type == NetGameType.Client
        ? GetPreview(lobby).Level : ShadowRun.SelectedLevel(lobby);

    private static LobbyDifficulty GetPreview(StartRunLobby lobby) =>
        ReceivedPreviews.TryGetValue(lobby, out var preview) ? preview.Difficulty : default;

    public override void _EnterTree() =>
        _previewSubscription = RitsuLibSidecarTypedMessageRegistry.Subscribe(DifficultyMessage, ReceivePreview);

    public override void _ExitTree()
    {
        _previewSubscription?.Dispose();
        SetHostingLobby(null);
    }

    public override void _Ready()
    {
        var ascension = GetParent().GetNode<NAscensionPanel>("%AscensionPanel");
        _nativeLeft = ascension.GetNode<NButton>(LeftArrow);
        _nativeRight = ascension.GetNode<NButton>(RightArrow);
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        Callable.From(() =>
        {
            if (IsInsideTree()) GetParent().MoveChild(this, ascension.GetIndex());
        }).CallDeferred();

        _panel = GD.Load<PackedScene>("res://scenes/screens/ascension_panel.tscn").Instantiate<NAscensionPanel>();
        _left = _panel.GetNode<NButton>(LeftArrow);
        _right = _panel.GetNode<NButton>(RightArrow);
        _left.FocusMode = FocusModeEnum.All;
        _right.FocusMode = FocusModeEnum.All;
        AddChild(_panel);
        // Reuse the native panel's buttons without binding the ascension shortcuts a second time.
        _panel.Initialize(MultiplayerUiMode.Client, GameMode.Standard);
        _panel.GetNode<Control>("%LeftTabIcon").Hide();
        _panel.GetNode<Control>("%RightTabIcon").Hide();
        _panel.AnchorLeft = ascension.AnchorLeft;
        _panel.AnchorRight = ascension.AnchorRight;
        _panel.AnchorTop = ascension.AnchorTop;
        _panel.AnchorBottom = ascension.AnchorBottom;
        _panel.OffsetLeft = ascension.OffsetLeft;
        _panel.OffsetRight = ascension.OffsetRight;
        _panel.OffsetBottom = ascension.OffsetTop - 12f;
        _panel.OffsetTop = _panel.OffsetBottom - (ascension.OffsetBottom - ascension.OffsetTop);
        _panel.MouseFilter = MouseFilterEnum.Pass;
        _panel.GetNode<Control>("Background").MouseFilter = MouseFilterEnum.Ignore;
        var icon = _panel.GetNodeOrNull<TextureRect>("%AscensionIconStandard")
            ?? _panel.GetNode<TextureRect>("%AscensionIcon");
        var levelLabel = icon.GetParent().GetNodeOrNull<MegaLabel>("AscensionLevel")
            ?? icon.GetNode<MegaLabel>("AscensionLevel");
        BlackFlame(icon);
        levelLabel.AddThemeColorOverride("font_outline_color", new Color("24181f"));
        _panel.Connect(NAscensionPanel.SignalName.AscensionLevelChanged, Callable.From(OnLevelChanged));
        _panel.MouseEntered += ShowTip;
        _panel.MouseExited += HideTip;
        _panel.VisibilityChanged += () =>
        {
            if (!_panel.IsVisibleInTree()) HideTip();
        };
        _panel.Hide();
    }

    public override void _Process(double delta)
    {
        if (GetParent() is not NCharacterSelectScreen { Lobby: { } lobby } screen || !screen.IsVisibleInTree())
        {
            _selectionOwner = null;
            _singleplayerSelections.Clear();
            SetHostingLobby(null);
            _panel.Hide();
            return;
        }

        var localMaxLevel = ShadowRun.SyncLocalMaxLevel(lobby);
        var isClient = lobby.NetService.Type == NetGameType.Client;
        var maxLevel = isClient ? GetPreview(lobby).MaxLevel : ShadowRun.LobbyMaxLevel(lobby);
        if (!isClient) RestoreSelection(lobby, maxLevel);
        var level = DisplayedLobbyLevel(lobby);
        var seenIntro = SaveManager.Instance.SeenPopup(ShadowRun.IntroPopup);

        SetHostingLobby(lobby.NetService.Type == NetGameType.Host ? lobby : null);
        if (_hostingLobby != null) SendPreview(_hostingLobby, new LobbyDifficulty(level, maxLevel), delta);

        if (localMaxLevel == 0 || maxLevel == 0 || !seenIntro)
        {
            _panel.Hide();
            if (localMaxLevel > 0 && !seenIntro && NModalContainer.Instance is { OpenModal: null } modal)
                ShadowUnlockPopup.Show(modal);
            return;
        }

        if (!_panel.Visible || _panel.Ascension != level || _displayedMaxLevel != maxLevel || _isClient != isClient)
        {
            var animate = !_panel.Visible;
            _refreshing = true;
            _isClient = isClient;
            _displayedMaxLevel = maxLevel;
            ArrowsVisible(_panel) = !isClient;
            _panel.SetMaxAscension(maxLevel);
            _panel.SetAscensionLevel(level);
            _refreshing = false;
            HideTip();
            if (animate) _panel.AnimIn();
        }

        if (isClient || (!_nativeLeft.IsEnabled && !_nativeRight.IsEnabled)) _panel.Disable();
        else
        {
            _left.Enable();
            _right.Enable();
        }
    }

    private void ReceivePreview(RitsuLibSidecarTypedDispatchContext<LobbyDifficulty> message)
    {
        var difficulty = message.Message;
        if (message.IsHostIngest || difficulty.Level < 0 || difficulty.Level > difficulty.MaxLevel ||
            difficulty.MaxLevel > ShadowRun.MaxLevel) return;
        if (GetParent() is NCharacterSelectScreen { Lobby: { NetService: NetClientGameService { IsConnected: true } client } lobby }
            && message.SenderNetId == client.HostNetId)
            ReceivedPreviews.GetValue(lobby, static _ => new Preview()).Difficulty = difficulty;
    }

    private void SetHostingLobby(StartRunLobby? lobby)
    {
        if (_hostingLobby == lobby) return;
        if (_hostingLobby != null) _hostingLobby.PlayerDisconnected -= ForgetPreview;
        _hostingLobby = lobby;
        if (_hostingLobby != null) _hostingLobby.PlayerDisconnected += ForgetPreview;
        _sentPreviews.Clear();
        _previewRetry = 0;
    }

    private void ForgetPreview(StartRunLobbyPlayer player) => _sentPreviews.Remove(player.id);

    private void SendPreview(StartRunLobby lobby, LobbyDifficulty difficulty, double delta)
    {
        _previewRetry -= delta;
        if (difficulty == _lastPreview && _previewRetry > 0) return;
        _lastPreview = difficulty;
        _previewRetry = 0.2;
        foreach (var player in lobby.Players)
        {
            if (player.id == lobby.NetService.NetId ||
                (_sentPreviews.TryGetValue(player.id, out var sent) && sent == difficulty)) continue;
            if (RitsuLibSidecarTypedMessageRegistry.SendToPeer(lobby.NetService, player.id, DifficultyMessage, difficulty))
                _sentPreviews[player.id] = difficulty;
        }
    }

    private readonly record struct LobbyDifficulty(int Level, int MaxLevel);

    private sealed class Preview
    {
        internal LobbyDifficulty Difficulty;
    }

    private void RestoreSelection(StartRunLobby lobby, int maxLevel)
    {
        var character = lobby.NetService.Type == NetGameType.Singleplayer ? lobby.LocalPlayer.character?.Id.Entry : null;
        var owner = (lobby, character);
        if (_selectionOwner != owner)
        {
            if (maxLevel == 0)
            {
                _selectionOwner = null;
                if (ShadowRun.SelectedLevel(lobby) > 0) ShadowRun.Select(lobby, 0);
                return;
            }

            _selectionOwner = owner;
            var preferred = character != null && _singleplayerSelections.TryGetValue((lobby, character), out var selection)
                ? selection : ShadowRun.PreferredLevel(lobby);
            ShadowRun.Select(lobby, preferred);
        }
        else if (ShadowRun.SelectedLevel(lobby) > maxLevel)
        {
            ShadowRun.Select(lobby, maxLevel);
        }
    }

    private void OnLevelChanged()
    {
        if (!_refreshing && GetParent() is NCharacterSelectScreen { Lobby: { } lobby })
        {
            ShadowRun.Select(lobby, _panel.Ascension);
            if (lobby.NetService.Type == NetGameType.Singleplayer)
                _singleplayerSelections[(lobby, lobby.LocalPlayer.character.Id.Entry)] = ShadowRun.SelectedLevel(lobby);
        }
        HideTip();
    }

    private void ShowTip()
    {
        if (_panel.Ascension == 0 || NModalContainer.Instance?.OpenModal != null) return;
        HideTip();
        var tip = NHoverTipSet.CreateAndShow(_panel, ShadowDifficultyUi.HoverTip());
        if (tip == null) return;

        var (flow, box) = ShadowDifficultyUi.FitHoverTip(tip);

        void Place()
        {
            var panel = _panel.GetGlobalRect();
            var viewport = GetViewportRect().Size;
            var size = box.GetCombinedMinimumSize().Max(box.Size);
            const float gap = 12f;
            var right = panel.End.X + gap;
            var beside = right + size.X <= viewport.X - gap;
            var x = beside ? right : panel.GetCenter().X - size.X * 0.5f;
            var y = beside ? panel.Position.Y : panel.Position.Y - size.Y - gap;
            flow.GlobalPosition = new Vector2(
                Math.Clamp(x, gap, Math.Max(gap, viewport.X - size.X - gap)),
                Math.Clamp(y, gap, Math.Max(gap, viewport.Y - size.Y - gap)));
            tip.SetFollowOwner();
        }

        box.Resized += Place;
        Place();
    }

    private void HideTip() => NHoverTipSet.Remove(_panel);

    internal static void BlackFlame(TextureRect icon)
    {
        var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/hsv.gdshader") };
        material.SetShaderParameter("h", 1f);
        material.SetShaderParameter("s", 0f);
        material.SetShaderParameter("v", 0.24f);
        icon.Material = material;
    }
}

public sealed class ShadowAscensionTextPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_ascension_text";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(NAscensionPanel), "RefreshAscensionText", Type.EmptyTypes)];

    public static void Postfix(NAscensionPanel __instance, MegaRichTextLabel ____info)
    {
        if (__instance.GetParent() is not NShadowSelector) return;
        var key = __instance.Ascension == 0 ? "ZERO" : "ONE";
        var title = NShadowSelector.Loc(key + "_TITLE").GetFormattedText();
        var description = NShadowSelector.Loc(key + "_DESCRIPTION").GetFormattedText();
        ____info.Text = $"[b][gold]{title}[/gold][/b]\n{description}";
    }
}

internal static class ShadowUnlockPopup
{
    internal static readonly StringName Marker = new("yard_shadow_unlock");

    internal static void Show(NModalContainer modal)
    {
        var popup = NAscensionSingleplayerFtue.Create();
        if (popup == null) return;
        popup.SetMeta(Marker, true);
        modal.Add(popup);
    }
}

public sealed class ShadowUnlockPopupReadyPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_unlock_popup_ready";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(NAscensionSingleplayerFtue), nameof(NAscensionSingleplayerFtue._Ready), Type.EmptyTypes)];

    public static void Postfix(NAscensionSingleplayerFtue __instance)
    {
        if (!__instance.HasMeta(ShadowUnlockPopup.Marker)) return;
        var title = __instance.GetNode<MegaLabel>("%Header");
        title.ApplyLocaleFontSubstitution(FontType.Bold, "font");
        title.AddThemeColorOverride("font_color", ShadowDifficultyUi.TextColor);
        title.AddThemeConstantOverride("outline_size", 0);
        title.SetTextAutoSize(NShadowSelector.Loc("UNLOCK_TITLE").GetFormattedText());
        __instance.GetNode<MegaRichTextLabel>("%Description")
            .SetTextAutoSize(NShadowSelector.Loc("UNLOCK_BODY").GetFormattedText());
        __instance.GetNode<MegaRichTextLabel>("%Disclaimer")
            .SetTextAutoSize(NShadowSelector.Loc("UNLOCK_NOTE").GetFormattedText());
        NShadowSelector.BlackFlame(__instance.GetNode<TextureRect>("VerticalPopup/AscensionIcon"));
    }
}

public sealed class ShadowUnlockPopupConfirmPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_unlock_popup_confirm";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(NAscensionSingleplayerFtue), "CloseFtue", [typeof(NButton)])];

    public static bool Prefix(NAscensionSingleplayerFtue __instance)
    {
        if (!__instance.HasMeta(ShadowUnlockPopup.Marker)) return true;
        SaveManager.Instance.MarkFtueAsComplete(ShadowRun.IntroPopup);
        SaveManager.Instance.SaveProgressFile();
        NModalContainer.Instance?.Clear();
        return false;
    }
}
