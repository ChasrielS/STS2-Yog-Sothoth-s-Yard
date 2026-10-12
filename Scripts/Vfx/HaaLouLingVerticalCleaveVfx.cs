using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace YogSothothsYard.Scripts;

public partial class HaaLouLingVerticalCleaveVfx : Node2D
{
    private const string ScenePath = "res://scenes/vfx/haa_lou_ling_vertical_cleave.tscn";
    private const float Duration = 0.50f;
    private static readonly StringName ProgressParameter = new("progress");
    private static PackedScene? _scene;
    private NCreature _target = null!;
    private Vector2 _position;
    private Vector2 _extent;
    private float _elapsed;
    private ShaderMaterial _material = null!;
    private Sprite2D _flash = null!;
    private readonly List<(Sprite2D Sprite, Vector2 Origin, Vector2 Velocity, float Delay, float Life, float Size)> _fragments = [];

    public static Node2D? Create(Creature target)
    {
        var node = NCombatRoom.Instance?.GetCreatureNode(target);
        if (node == null) return null;
        _scene ??= GD.Load<PackedScene>(ScenePath);
        if (_scene == null) return null;
        var vfx = _scene.Instantiate<HaaLouLingVerticalCleaveVfx>();
        vfx._target = node;
        var bounds = node.Hitbox.Size;
        vfx._position = node.VfxSpawnPosition + new Vector2(0f, -bounds.Y * 0.06f);
        vfx._extent = new Vector2(Mathf.Clamp(bounds.X * 0.65f + 160f, 220f, 340f),
            Mathf.Clamp(bounds.Y * 1.25f + 110f, 320f, 560f));
        return vfx;
    }

    public override void _Ready()
    {
        var sheet = GetNode<Sprite2D>("Sheet");
        _material = (ShaderMaterial)sheet.Material.Duplicate();
        sheet.Material = _material;
        sheet.Scale = _extent * new Vector2(0.8f, 1.25f) / sheet.Texture.GetSize();
        _flash = GetNode<Sprite2D>("ImpactFlash");
        var chip = GetNode<Sprite2D>("ChipTemplate");
        var scale = _extent.Y / 440f;
        _flash.Scale = new Vector2(0.62f, 1.05f) * scale;
        for (var i = 0; i < 10; i++)
        {
            var side = i % 2 == 0 ? -1f : 1f;
            var origin = new Vector2(side * 12f, (i / 2 - 2f) * _extent.Y * 0.09f);
            var fragment = new Sprite2D
            {
                Texture = i % 3 == 0 ? _flash.Texture : chip.Texture,
                UseParentMaterial = true,
                TextureFilter = TextureFilterEnum.Linear,
                Visible = false
            };
            AddChild(fragment);
            _fragments.Add((fragment, origin,
                new Vector2(side * (320f + i * 47 % 160), -90f + i * 37 % 180) * scale,
                0.004f + i % 3 * 0.006f, 0.12f + i % 4 * 0.012f,
                (i % 3 == 0 ? 0.27f : 0.19f + i % 3 * 0.035f) * scale));
        }
        chip.QueueFree();
        UpdateVisuals();
        NGame.Instance?.ScreenShake(ShakeStrength.Medium, ShakeDuration.Short, 88f);
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        if (_elapsed >= Duration)
        {
            this.QueueFreeSafely();
            return;
        }
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (GodotObject.IsInstanceValid(_target) && _target.IsInsideTree())
            _position = _target.VfxSpawnPosition + new Vector2(0f, -_target.Hitbox.Size.Y * 0.06f);
        GlobalPosition = _position;
        _material.SetShaderParameter(ProgressParameter, _elapsed / Duration);
        _flash.Modulate = new Color(1f, 1f, 0.86f, 0.85f * (1f - Mathf.SmoothStep(0f, 1f, _elapsed / 0.065f)));
        for (var i = 0; i < _fragments.Count; i++)
        {
            var fragment = _fragments[i];
            var age = _elapsed - fragment.Delay;
            var progress = age / fragment.Life;
            fragment.Sprite.Visible = progress >= 0f && progress < 1f;
            if (!fragment.Sprite.Visible) continue;
            fragment.Sprite.Position = fragment.Origin + fragment.Velocity * age * (1f - progress * 0.25f);
            fragment.Sprite.Rotation = i * 1.71f + (i % 2 == 0 ? -4f : 4f) * age;
            fragment.Sprite.Scale = Vector2.One * fragment.Size * (1f - progress * 0.45f);
            var alpha = Mathf.SmoothStep(0f, 1f, age / 0.012f) * (1f - Mathf.SmoothStep(0f, 1f, progress));
            fragment.Sprite.Modulate = i % 3 == 0
                ? new Color(1f, 0.97f, 0.80f, alpha)
                : new Color(0.80f, 1f, 0.88f, alpha);
        }
    }
}
