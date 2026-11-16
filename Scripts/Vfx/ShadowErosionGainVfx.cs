using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace YogSothothsYard.Scripts.Shadow;

public partial class ShadowErosionGainVfx : Node2D
{
    private const float Duration = 1.2f;
    private static readonly ConditionalWeakTable<NCreature, ShadowErosionGainVfx> Active = new();
    private static readonly StringName Progress = new("progress");
    private static Shader? _shader;

    private NCreature _target = null!;
    private ShaderMaterial _material = null!;
    private ColorRect _mist = null!;
    private float _elapsed;

    internal static void Play(Creature creature)
    {
        if (creature.CombatState == null || NCombatRoom.Instance is not { } room ||
            room.GetCreatureNode(creature) is not { } target) return;
        if (Active.TryGetValue(target, out var existing) && !existing.IsQueuedForDeletion())
        {
            existing._elapsed = 0f;
            return;
        }

        var vfx = new ShadowErosionGainVfx { _target = target };
        Active.Remove(target);
        Active.Add(target, vfx);
        room.CombatVfxContainer.AddChildSafely(vfx);
    }

    public override void _Ready()
    {
        _material = new ShaderMaterial
        {
            Shader = _shader ??= GD.Load<Shader>("res://shaders/vfx/shadow_erosion_gain.gdshader")
        };
        _material.SetShaderParameter("smoke_noise", GD.Load<Texture2D>("res://shaders/shadow/smoke_noise.tres"));
        _mist = new ColorRect { Material = _material, MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_mist);
        NCombatVfxVisibility.Attach(this);
        UpdateVisuals();
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        if (_elapsed >= Duration || !GodotObject.IsInstanceValid(_target) || !_target.IsInsideTree())
        {
            this.QueueFreeSafely();
            return;
        }
        UpdateVisuals();
    }

    public override void _ExitTree()
    {
        if (Active.TryGetValue(_target, out var active) && ReferenceEquals(active, this))
            Active.Remove(_target);
    }

    private void UpdateVisuals()
    {
        var bounds = _target.Hitbox.Size;
        GlobalPosition = _target.GetBottomOfHitbox();
        _mist.Size = bounds * new Vector2(1.25f, 1.1f);
        _mist.Position = -_mist.Size * new Vector2(0.5f, 0.93f);
        _material.SetShaderParameter(Progress, _elapsed / Duration);
    }
}
