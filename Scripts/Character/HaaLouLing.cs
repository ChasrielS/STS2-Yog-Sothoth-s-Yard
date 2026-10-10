using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Data.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Godot;
using STS2RitsuLib.Scaffolding.Visuals.StateMachine;

namespace YogSothothsYard.Scripts;

[RegisterCharacter]
[RequireEpoch(typeof(XunRenQiChengEpoch))]
public sealed class HaaLouLing : ModCharacterTemplate<HaaLouLingCardPool, HaaLouLingRelicPool, HaaLouLingPotionPool>
{
    public override Color NameColor => new(0.18f, 0.70f, 0.62f);
    public override Color EnergyLabelOutlineColor => new(0.03f, 0.33f, 0.29f);
    public override Color MapDrawingColor => new(0.24f, 0.70f, 0.36f);
    public override CharacterGender Gender => CharacterGender.Feminine;

    public override int StartingHp => 75;
    public override int StartingGold => 99;
    protected override Type? UnlocksAfterRunAsType => typeof(Ironclad);

    public override CharacterAssetProfile AssetProfile => CharacterAssetProfiles.Merge(
        CharacterAssetProfiles.Ironclad(),
        new(
            Scenes: new(
                VisualsPath: "res://scenes/creature_visuals/haa_lou_ling.tscn",
                EnergyCounterPath: "res://scenes/combat/energy_counters/haa_lou_ling_energy_counter.tscn",
                MerchantAnimPath: "res://scenes/merchant/characters/haa_lou_ling_merchant.tscn",
                RestSiteAnimPath: "res://scenes/rest_site/characters/haa_lou_ling_rest_site.tscn"
            ),
            Ui: new(
                IconTexturePath: "res://images/ui/top_panel/character_icon_haa_lou_ling.png",
                IconPath: "res://scenes/ui/character_icons/haa_lou_ling_icon.tscn",
                CharacterSelectBgPath: "res://scenes/screens/char_select/char_select_bg_haa_lou_ling.tscn",
                CharacterSelectIconPath: "res://images/packed/character_select/char_select_haa_lou_ling.png",
                CharacterSelectLockedIconPath: "res://images/packed/character_select/char_select_haa_lou_ling_locked.png",
                CharacterSelectTransitionPath: "res://materials/transitions/haa_lou_ling_transition_mat.tres",
                MapMarkerPath: "res://images/packed/map/icons/map_marker_haa_lou_ling.svg"
            ),
            Vfx: new(
                TrailPath: "res://scenes/vfx/card_trail_haa_lou_ling.tscn"
            ),
            Spine: new(
                // 场景已绑定骨骼；再次替换会清空动画轨道。空串同时阻止继承占位角色的路径。
                CombatSkeletonDataPath: ""
            ),
            Audio: CharacterAssetProfiles.Silent().Audio! with
            {
                CharacterTransitionSfx = Entry.HaaLouLingTransitionSfx
            },
            Multiplayer: new(
                ArmPointingTexturePath: "res://images/ui/hands/multiplayer_hand_haa_lou_ling_point.tres",
                ArmRockTexturePath: "res://images/ui/hands/multiplayer_hand_haa_lou_ling_rock.png",
                ArmPaperTexturePath: "res://images/ui/hands/multiplayer_hand_haa_lou_ling_paper.png",
                ArmScissorsTexturePath: "res://images/ui/hands/multiplayer_hand_haa_lou_ling_scissors.png"
            ),
            VanillaRelicVisualOverrides: [
                new(CharacterOwnedVanillaRelicModelId.YummyCookie, new(
                    IconPath: "res://images/relics/mei_wei_bing_gan_icon.png",
                    IconOutlinePath: "res://images/relics/mei_wei_bing_gan_icon_outline.png",
                    BigIconPath: "res://images/relics/mei_wei_bing_gan.png"
                ))
            ]
        ));

    public override float AttackAnimDelay => 0.15f;
    public override float CastAnimDelay => 0.25f;

    protected override NCreatureVisuals? TryCreateCreatureVisuals() =>
        RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.Scenes!.VisualsPath!);

    protected override IEnumerable<CardModel> LocalStartingDeck =>
        Enumerable.Repeat<CardModel>(ModelDb.Card<StrikeHaaLouLing>(), 4)
            .Concat(Enumerable.Repeat<CardModel>(ModelDb.Card<DefendHaaLouLing>(), 4))
            .Concat([ModelDb.Card<FindFood>(), ModelDb.Card<ZhanLingGuiYuan>()]);

    public override CreatureAnimator GenerateAnimator(MegaSprite controller, Creature creature)
    {
        var idle = new AnimState("idle_loop", true);
        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Relaxed", idle);
        animator.AddAnyState("Attack", new AnimState("attack") { NextState = idle });
        animator.AddAnyState("Hit", new AnimState("hurt") { NextState = idle });
        animator.AddAnyState("Dead", new AnimState("die"));
        return animator;
    }

    protected override IEnumerable<RelicModel> LocalStartingRelics =>
        [ModelDb.Relic<ZhanLingTangDao>(), ModelDb.Relic<LingHunShiQuSuiZhiQing>()];

    protected override ModAnimStateMachine? SetupCustomMerchantAnimationStateMachine(Node merchantRoot, CharacterModel character)
    {
        var sprite = new MegaSprite(merchantRoot.GetChild(0).GetNode("SpineSprite"));
        var machine = ModAnimStateMachineBuilder.Create()
            .AddState("idle_loop", true).AsInitial().Done()
            .AddAnyState("Relaxed", "idle_loop")
            .AddAnyState("Idle", "idle_loop")
            .BuildSpine(sprite);
        merchantRoot.RunWhenSpineReady(sprite, _ => machine.SetTrigger("Relaxed"));
        return machine;
    }

    protected override ModAnimStateMachine? SetupCustomRestSiteAnimationStateMachine(Node restSiteRoot, CharacterModel character) => null;

    public override List<string> GetArchitectAttackVfx() => [
        "vfx/haa_lou_ling_strike_slash"
    ];
}
