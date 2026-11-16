using Godot;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Scaffolding.Godot.NodeAttachments;

namespace YogSothothsYard.Scripts.Shadow;

public partial class NShadowScreenEdges : ColorRect
{
    private Control? _mapTopBarBackground;

    internal static void Register()
    {
        var registry = ModNodeAttachmentRegistry.For(Entry.ModId);
        registry.RegisterReadyChild<NCharacterSelectScreen, NShadowScreenEdges>(
            "shadow_select_edges", static _ => new NShadowScreenEdges());
        registry.RegisterReadyChild<NMapScreen, NShadowScreenEdges>(
            "shadow_map_edges", static _ => new NShadowScreenEdges());
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/shadow/screen_edges.gdshader") };
        material.SetShaderParameter("smoke_noise", GD.Load<Texture2D>("res://shaders/shadow/smoke_noise.tres"));
        material.SetShaderParameter("top_edge", GetParent() is not NMapScreen);
        Material = material;
        Hide();
        Callable.From(() =>
        {
            if (GetParent() is NMapScreen map)
                map.MoveChild(this, map.GetNode("TheMap").GetIndex() + 1);
            else if (GetParent() is NCharacterSelectScreen screen)
                screen.MoveChild(this, screen.GetNode("AnimatedBg").GetIndex() + 1);
        }).CallDeferred();
    }

    public override void _Process(double delta)
    {
        var level = GetParent() switch
        {
            NCharacterSelectScreen { Lobby: { } lobby } when lobby.LocalPlayer.character is HaaLouLing =>
                ShadowRun.SelectedLevel(lobby),
            NMapScreen { IsOpen: true } => ShadowRun.Level(RunManager.Instance.DebugOnlyGetState()),
            _ => 0
        };
        Visible = level == 1;
        if (!Visible || GetParent() is not NMapScreen map) return;
        _mapTopBarBackground ??= NRun.Instance?.GlobalUi?.TopBar?.GetNodeOrNull<Control>("BgImage");
        if (_mapTopBarBackground == null) return;
        var artBottom = _mapTopBarBackground.GetGlobalTransformWithCanvas() * new Vector2(0, _mapTopBarBackground.Size.Y);
        var mapTop = map.GetGlobalTransformWithCanvas().AffineInverse() * artBottom;
        OffsetTop = Math.Clamp(mapTop.Y, 0f, map.Size.Y);
    }
}
