using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace YogSothothsYard.Scripts;

public partial class HaaLouLingStrikeSlashVfx : Node2D
{
    private const string ScenePath = "res://scenes/vfx/haa_lou_ling_strike_slash.tscn";
    private const float Duration = 0.14f;
    private static readonly StringName ProgressParameter = new("progress");
    private Vector2 _spawnPosition;
    private ShaderMaterial _material = null!;
    private float _elapsed;

    public static Node2D? Create(Creature target)
    {
        NCreature? creatureNode = NCombatRoom.Instance?.GetCreatureNode(target);
        if (creatureNode == null)
        {
            return null;
        }

        PackedScene? scene = GD.Load<PackedScene>(ScenePath);
        if (scene == null)
        {
            return null;
        }

        HaaLouLingStrikeSlashVfx vfx = scene.Instantiate<HaaLouLingStrikeSlashVfx>();

        float scale = Mathf.Clamp(creatureNode.Hitbox.Size.X / 260.0f, 0.82f, 1.22f);
        vfx._spawnPosition = creatureNode.VfxSpawnPosition + new Vector2(0.0f, -creatureNode.Hitbox.Size.Y * 0.06f);
        vfx.RotationDegrees = -18.0f;
        vfx.Scale = new Vector2(scale, scale);

        return vfx;
    }

    public override void _Ready()
    {
        GlobalPosition = _spawnPosition;
        var sprite = GetNode<Sprite2D>("SlashSprite");
        _material = (ShaderMaterial)sprite.Material.Duplicate();
        sprite.Material = _material;
        _material.SetShaderParameter(ProgressParameter, 0f);
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        if (_elapsed >= Duration)
        {
            this.QueueFreeSafely();
            return;
        }
        _material.SetShaderParameter(ProgressParameter, _elapsed / Duration);
    }
}
