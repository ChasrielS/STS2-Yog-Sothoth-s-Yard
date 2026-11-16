using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Shadow;

public sealed class ShadowHealthBarPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_shadow_health_bar";
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(NCreatureStateDisplay), nameof(NCreatureStateDisplay.SetCreature), [typeof(Creature)])];

    public static void Postfix(NCreatureStateDisplay __instance, Creature creature)
    {
        if (!creature.IsPlayer || creature.Player is not { } player || ShadowRun.Level(player) == 0) return;
        var hp = __instance.GetNode<NHealthBar>("%HealthBar").HpBarContainer;
        __instance.GetNode<Control>("%PowerContainer").Position += Vector2.Down * NShadowErosionBar.RowSpacing;
        __instance.GetNode<Control>("%NameplateContainer").Position += Vector2.Down * NShadowErosionBar.RowSpacing;
        var hitbox = __instance.GetNode<Control>("%HpBarHitbox");
        hp.AddChild(new NShadowErosionBar { Player = player, HealthHitbox = hitbox });
    }
}

public partial class NShadowErosionBar : Control
{
    internal const float RowSpacing = 34f;
    internal Player Player { get; init; } = null!;
    internal Control HealthHitbox { get; init; } = null!;
    private MegaLabel _number = null!;
    private ShaderMaterial _material = null!;
    private Tween? _fade;
    private int _erosion = -1;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        OffsetTop = RowSpacing - 6f;
        OffsetBottom = RowSpacing + 6f;

        var healthBar = GetParent();
        _material = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/shadow/erosion_bar.gdshader") };
        _material.SetShaderParameter("mist_noise", GD.Load<Texture2D>("res://shaders/shadow/smoke_noise.tres"));
        UpdateMistColors();
        Player.RelicObtained += OnRelicChanged;
        Player.RelicRemoved += OnRelicChanged;

        var fill = healthBar.GetNode<Control>("HpForegroundContainer");
        var mask = fill.GetNode<NinePatchRect>("Mask");
        var crystal = new NinePatchRect
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Material = _material,
            Texture = mask.Texture,
            TextureFilter = mask.TextureFilter,
            PatchMarginLeft = mask.PatchMarginLeft,
            PatchMarginRight = mask.PatchMarginRight
        };
        AddChild(crystal);
        crystal.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        crystal.OffsetLeft = fill.OffsetLeft;
        crystal.OffsetTop = fill.OffsetTop + 6f;
        crystal.OffsetRight = fill.OffsetRight;
        crystal.OffsetBottom = fill.OffsetBottom - 6f;
        crystal.Resized += () => _material.SetShaderParameter("bar_size", crystal.Size);
        _material.SetShaderParameter("bar_size", crystal.Size);

        _number = (MegaLabel)healthBar.GetNode("HpLabel").Duplicate((int)DuplicateFlags.Scripts);
        _number.UniqueNameInOwner = false;
        _number.Modulate = Colors.White;
        _number.MouseFilter = MouseFilterEnum.Ignore;
        _number.AddThemeColorOverride("font_outline_color", new Color("24181f"));
        AddChild(_number);
        _number.OffsetTop += 6f;
        _number.OffsetBottom -= 6f;
        MouseEntered += () => FadeNumber(0.1f);
        MouseExited += () => FadeNumber(1f);
        UpdateErosion();
    }

    public override void _Process(double delta)
    {
        MouseFilter = HealthHitbox.MouseFilter;
        UpdateErosion();
    }

    public override void _ExitTree()
    {
        Player.RelicObtained -= OnRelicChanged;
        Player.RelicRemoved -= OnRelicChanged;
    }

    private void OnRelicChanged(RelicModel relic)
    {
        if (relic is IShadowSoulstone) UpdateMistColors();
    }

    private void UpdateMistColors()
    {
        var (deep, light) = Player.Relics.OfType<IShadowSoulstone>().FirstOrDefault()?.ErosionMistColors
            ?? (new Color("858585"), new Color("e5e5e5"));
        _material.SetShaderParameter("mist_deep", deep);
        _material.SetShaderParameter("mist_light", light);
    }

    private void FadeNumber(float opacity)
    {
        _fade?.Kill();
        _fade = CreateTween();
        var tween = _fade.TweenProperty(_number, "modulate:a", opacity, 0.5);
        if (opacity < 1f) tween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
    }

    private void UpdateErosion()
    {
        var erosion = ShadowRun.Erosion(Player);
        if (erosion == _erosion) return;
        _erosion = erosion;
        _number.SetTextAutoSize($"{erosion}/{ShadowRun.ErosionLimit}");
        var fraction = Math.Clamp(_erosion / (float)ShadowRun.ErosionLimit, 0f, 1f);
        _material.SetShaderParameter("fill_fraction", fraction);
    }
}
