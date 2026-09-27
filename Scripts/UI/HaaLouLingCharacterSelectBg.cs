using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;

namespace YogSothothsYard.Scripts;

public partial class HaaLouLingCharacterSelectBg : Control
{
    public override void _Ready()
    {
        var sprite = new MegaSprite(GetNode<Node2D>("BaseCg"));
        this.RunWhenSpineReady(sprite, state => state.SetAnimation("idle_loop", true));
    }
}
