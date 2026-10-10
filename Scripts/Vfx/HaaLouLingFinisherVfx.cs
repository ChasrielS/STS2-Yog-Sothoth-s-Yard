using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace YogSothothsYard.Scripts;

public partial class HaaLouLingFinisherVfx : Node2D
{
    private enum Stage { Gather, Cut }
    private const string ScenePath = "res://scenes/vfx/haa_lou_ling_finisher.tscn";
    private const string KnifeHandBone = "bone13";
    private static readonly StringName ElapsedParameter = new("elapsed");
    private Stage _stage;
    private NCreature? _anchor;
    private Vector2 _position;
    private Vector2 _extent;
    private int _knife;
    private bool _breaking;
    private bool _primary;
    private float _duration;
    private float _elapsed;
    private Sprite2D _sheet = null!;
    private Sprite2D? _impactFlash;
    private ShaderMaterial _material = null!;
    private readonly List<(Sprite2D Sprite, Vector2 Origin, Vector2 Velocity, float Delay, float Life, float Size, float Spin)> _particles = [];
    private float Strength => _knife / 3f;
    private float Anticipation => _breaking ? 0.75f : 0.55f;

    public static Task Anticipate(Creature source, int knife, bool breaking)
    {
        var room = NCombatRoom.Instance;
        var node = room?.GetCreatureNode(source);
        if (node == null) return Task.CompletedTask;
        var vfx = Create(Stage.Gather, knife, breaking, true);
        vfx._anchor = node;
        room!.CombatVfxContainer.AddChildSafely(vfx);
        return Cmd.Wait(vfx.Anticipation);
    }

    public static Node2D? CreateImpact(Creature target, int knife, bool breaking, bool primary)
    {
        var node = NCombatRoom.Instance?.GetCreatureNode(target);
        if (node == null) return null;
        var vfx = Create(Stage.Cut, knife, breaking, primary);
        vfx._anchor = node;
        return vfx;
    }

    public static Task ImpactPause(bool breaking) => NCombatRoom.Instance == null
        ? Task.CompletedTask : Cmd.Wait(breaking ? 0.26f : 0.22f);

    private static HaaLouLingFinisherVfx Create(Stage stage, int knife, bool breaking, bool primary)
    {
        var vfx = GD.Load<PackedScene>(ScenePath).Instantiate<HaaLouLingFinisherVfx>();
        vfx._stage = stage;
        vfx._knife = Math.Clamp(knife, 0, 3);
        vfx._breaking = breaking;
        vfx._primary = primary;
        vfx._duration = stage == Stage.Gather ? vfx.Anticipation * 1.3f : breaking ? 1.05f : 0.82f;
        return vfx;
    }

    public override void _Ready()
    {
        _sheet = GetNode<Sprite2D>("Sheet");
        _material = (ShaderMaterial)_sheet.Material.Duplicate();
        _sheet.Material = _material;
        _material.SetShaderParameter("stage", (int)_stage);
        _material.SetShaderParameter("strength", Strength);
        _material.SetShaderParameter("breaking", _breaking);
        _material.SetShaderParameter("anticipation", Anticipation);
        _material.SetShaderParameter("duration", _duration);
        UpdateAnchor();
        _sheet.Scale = _extent / _sheet.Texture.GetSize();
        _sheet.RotationDegrees = _stage == Stage.Cut ? -18f : 0f;
        _sheet.Modulate = new Color(1f, 1f, 1f, _primary ? 1f : 0.58f);

        var chipTemplate = GetNode<Sprite2D>("ChipTemplate");
        var glintTemplate = GetNode<Sprite2D>("GlintTemplate");
        if (_stage == Stage.Cut)
        {
            _impactFlash = new Sprite2D
            {
                Texture = glintTemplate.Texture,
                UseParentMaterial = true,
                Scale = Vector2.One * (0.72f + Strength * 0.32f) * (_primary ? 1f : 0.58f),
                Rotation = _sheet.Rotation,
                TextureFilter = TextureFilterEnum.Linear
            };
            AddChild(_impactFlash);
            CreateParticles(chipTemplate.Texture, glintTemplate.Texture);
            for (var i = 0; i < Math.Max(0, _knife - 1); i++)
            {
                var echo = new Sprite2D
                {
                    Texture = _sheet.Texture,
                    Material = _material,
                    Scale = _sheet.Scale * new Vector2(0.96f - i * 0.06f, 0.60f - i * 0.10f),
                    Rotation = _sheet.Rotation + (i == 0 ? -0.032f : 0.025f),
                    Position = new Vector2(-10f - i * 7f, 14f + i * 9f).Rotated(_sheet.Rotation),
                    Modulate = new Color(0.78f, 1f, 0.86f, (_primary ? 0.32f : 0.18f) / (1f + i * 0.7f)),
                    TextureFilter = TextureFilterEnum.Linear
                };
                AddChild(echo);
                MoveChild(echo, 0);
            }
            if (_primary)
                NGame.Instance?.ScreenShake(_breaking && _knife >= 2 ? ShakeStrength.Strong : ShakeStrength.Medium,
                    ShakeDuration.Short, 342f);
        }
        chipTemplate.QueueFree();
        glintTemplate.QueueFree();
        UpdateVisuals();
    }

    private void UpdateAnchor()
    {
        if (_anchor != null && GodotObject.IsInstanceValid(_anchor) && _anchor.IsInsideTree())
        {
            var bounds = _anchor.Hitbox.Size;
            if (_stage == Stage.Gather)
            {
                _position = _anchor.Entity.Player?.Character is HaaLouLing
                    ? _anchor.Visuals.SpineBody?.GetGlobalBoneTransform(KnifeHandBone)?.Origin ?? _anchor.VfxSpawnPosition
                    : _anchor.VfxSpawnPosition;
                _extent = new Vector2(Mathf.Clamp(bounds.X * 2f + 80f, 300f, 620f),
                    Mathf.Clamp(bounds.Y * 1.6f + 80f, 320f, 660f));
            }
            else
            {
                _position = _anchor.VfxSpawnPosition + new Vector2(0f, -bounds.Y * 0.06f);
                var size = _primary ? 1f : 0.78f;
                _extent = new Vector2(Mathf.Clamp(bounds.X * 1.7f + 190f, 430f, 730f),
                    Mathf.Clamp(bounds.Y * 0.24f + 80f, 120f, 190f)) * size
                    * new Vector2(0.86f + Strength * 0.52f, 0.88f + Strength * 0.54f);
            }
        }
        GlobalPosition = _position;
    }

    private void CreateParticles(Texture2D chip, Texture2D glint)
    {
        var count = (_breaking ? 12 : 7) + _knife * (_breaking ? 5 : 3);
        if (!_primary) count = Math.Max(6, count / 2);
        var scale = Mathf.Clamp(_extent.X / 560f, 0.75f, 1.3f);
        for (var i = 0; i < count; i++)
        {
            var pane = _breaking && i % 3 != 0;
            var size = (pane ? 0.27f + i % 3 * 0.045f : 0.19f + i % 4 * 0.032f) * scale;
            var origin = new Vector2(((i * 37 % 19) / 18f - 0.5f) * _extent.X * 0.48f,
                _extent.Y * 0.055f + Mathf.Sin(i * 0.87f) * 5f).Rotated(_sheet.Rotation);
            var angle = _sheet.Rotation + (i % 2 == 0 ? -Mathf.Pi / 2f : Mathf.Pi / 2f) + Mathf.Sin(i * 2.37f) * 0.5f;
            var velocity = Vector2.FromAngle(angle) * (74f + i * 41 % 130) * scale;
            var sprite = new Sprite2D
            {
                Texture = pane ? chip : glint,
                UseParentMaterial = true,
                Visible = false,
                TextureFilter = TextureFilterEnum.Linear
            };
            AddChild(sprite);
            _particles.Add((sprite, origin, velocity, 0.02f + i % 5 * 0.015f,
                0.46f + i % 6 * 0.055f + (_breaking ? 0.16f : 0f), size, (i % 2 == 0 ? -1f : 1f) * (1.2f + i % 4)));
        }
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        if (_elapsed >= _duration)
        {
            this.QueueFreeSafely();
            return;
        }
        UpdateAnchor();
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        _material.SetShaderParameter(ElapsedParameter, _elapsed);
        if (_stage == Stage.Gather)
            _sheet.Scale = _extent / _sheet.Texture.GetSize();
        if (_impactFlash != null)
            _impactFlash.Modulate = new Color(1f, 1f, 0.88f, (1f - Smooth(_elapsed / 0.18f)) * 0.88f);
        for (var i = 0; i < _particles.Count; i++)
        {
            var particle = _particles[i];
            var age = _elapsed - particle.Delay;
            var p = age / particle.Life;
            particle.Sprite.Visible = p >= 0f && p <= 1f;
            if (!particle.Sprite.Visible) continue;
            var travel = Mathf.Max(0f, age - (_breaking ? 0.065f : 0f));
            particle.Sprite.Position = particle.Origin + particle.Velocity * travel + new Vector2(0f, 58f * travel * travel);
            particle.Sprite.Rotation = i * 2.12f + particle.Spin * travel;
            particle.Sprite.Scale = Vector2.One * particle.Size * (0.82f + Smooth(age / 0.06f) * 0.18f) * (1f - p * 0.28f);
            var alpha = Smooth(age / 0.035f) * (1f - Smooth((p - 0.22f) / 0.78f)) * (_primary ? 0.9f : 0.56f);
            particle.Sprite.Modulate = i % 3 == 0 ? new Color(1f, 0.96f, 0.78f, alpha) : new Color(0.80f, 1f, 0.88f, alpha);
        }
    }

    private static float Smooth(float value)
    {
        var p = Mathf.Clamp(value, 0f, 1f);
        return p * p * (3f - 2f * p);
    }
}
