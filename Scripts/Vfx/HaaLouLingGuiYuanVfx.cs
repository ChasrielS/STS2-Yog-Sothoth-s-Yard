using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace YogSothothsYard.Scripts;

public partial class HaaLouLingGuiYuanVfx : Node2D
{
    private const string ScenePath = "res://scenes/vfx/haa_lou_ling_gui_yuan.tscn";
    private static readonly StringName ProgressParameter = new("progress");
    private static readonly StringName PhaseParameter = new("phase");
    private static readonly (float Start, float Life, Vector2 Spawn, float Rise, float Drift, float Size, float Phase)[] Souls =
    [
        (0.02f, 0.88f, new(-0.318f, 0.07f), 116f, -23f, 0.67f, 0.5f),
        (0.12f, 0.94f, new(-0.116f, -0.077f), 134f, -13f, 0.91f, 2.1f),
        (0.21f, 1.02f, new(0.046f, 0.112f), 145f, 9f, 1.03f, 4.1f),
        (0.30f, 0.84f, new(0.220f, -0.084f), 112f, 25f, 0.61f, 5.2f),
        (0.40f, 0.91f, new(0.347f, 0.119f), 121f, 33f, 0.76f, 3.2f)
    ];

    private NCreature _target = null!;
    private Sprite2D _pulse = null!;
    private readonly Sprite2D[] _souls = new Sprite2D[Souls.Length];
    private readonly ShaderMaterial[] _materials = new ShaderMaterial[Souls.Length];
    private Vector2 _span;
    private float _scale;
    private float _elapsed;

    public static void Show(Creature target)
    {
        var room = NCombatRoom.Instance;
        var creatureNode = room?.GetCreatureNode(target);
        if (creatureNode == null) return;

        var vfx = GD.Load<PackedScene>(ScenePath).Instantiate<HaaLouLingGuiYuanVfx>();
        vfx._target = creatureNode;
        room!.CombatVfxContainer.AddChildSafely(vfx);
    }

    public override void _Ready()
    {
        _pulse = GetNode<Sprite2D>("Pulse");
        var template = GetNode<Sprite2D>("SoulTemplate");
        var shader = ((ShaderMaterial)template.Material).Shader;
        for (var i = 0; i < Souls.Length; i++)
        {
            var material = new ShaderMaterial { Shader = shader };
            material.SetShaderParameter(PhaseParameter, Souls[i].Phase);
            _materials[i] = material;
            _souls[i] = new Sprite2D
            {
                Texture = template.Texture,
                Offset = template.Offset,
                TextureFilter = TextureFilterEnum.Linear,
                Material = material,
                Visible = false
            };
            AddChild(_souls[i]);
        }
        template.QueueFree();
        UpdateVisuals();
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        if (_elapsed >= 1.35f || !GodotObject.IsInstanceValid(_target) || !_target.IsInsideTree())
        {
            this.QueueFreeSafely();
            return;
        }
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        GlobalPosition = _target.VfxSpawnPosition;
        _span = _target.Hitbox.Size;
        _scale = Mathf.Clamp(Mathf.Min(_span.X / 173f, _span.Y / 143f), 0.35f, 1.5f);
        var pulse = Smooth(_elapsed / 0.09f) * (1f - Smooth((_elapsed - 0.12f) / 0.35f));
        _pulse.Scale = new Vector2(_span.X * 0.83f, _span.Y) / 128f;
        _pulse.Modulate = new Color(1f, 1f, 1f, pulse);

        for (var i = 0; i < Souls.Length; i++)
        {
            var soul = Souls[i];
            var p = (_elapsed - soul.Start) / soul.Life;
            var sprite = _souls[i];
            sprite.Visible = p >= 0f && p <= 1f;
            if (!sprite.Visible) continue;

            var rise = Mathf.Pow(p, 0.82f);
            var sway = (Mathf.Sin(soul.Phase + p * 6f) - Mathf.Sin(soul.Phase)) * 6f;
            sprite.Position = soul.Spawn * _span + new Vector2(
                (soul.Drift * rise + sway) * _scale, -soul.Rise * rise * _scale);
            sprite.Rotation = Mathf.Sin(soul.Phase + p * 5f) * 0.13f;
            var size = soul.Size * _scale * (0.48f + 0.52f * Smooth(p / 0.19f));
            sprite.Scale = new Vector2(size * (1f - p * 0.19f), size * (0.84f + p * 0.18f));
            sprite.Modulate = new Color(1f, 1f, 1f,
                Smooth(p / 0.13f) * (1f - Smooth((p - 0.57f) / 0.43f)) * 0.88f);
            _materials[i].SetShaderParameter(ProgressParameter, p);
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        for (var i = 0; i < 13; i++)
        {
            var p = (_elapsed - 0.03f - i * 0.025f) / (0.51f + i % 4 * 0.08f);
            if (p < 0f || p > 1f) continue;
            var alpha = Smooth(p / 0.12f) * (1f - Smooth((p - 0.3f) / 0.7f)) * 0.49f;
            var position = new Vector2(
                (-61f + i * 43 % 127) / 173f * _span.X + Mathf.Sin(p * 4f + i) * (7f + p * 4f) * _scale,
                (7f - i * 13 % 35) / 143f * _span.Y - p * (46f + i % 3 * 17f) * _scale);
            DrawCircle(position, (1.1f - p * 0.45f) * _scale, new Color(0.796f, 0.976f, 0.914f, alpha));
        }
    }

    private static float Smooth(float value)
    {
        var p = Mathf.Clamp(value, 0f, 1f);
        return p * p * (3f - 2f * p);
    }
}
