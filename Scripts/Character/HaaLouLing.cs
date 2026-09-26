using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Data.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Godot;
using STS2RitsuLib.Scaffolding.Visuals.StateMachine;

namespace YogSothothsYard.Scripts;

[RegisterCharacter]
public sealed class HaaLouLing : ModCharacterTemplate<HaaLouLingCardPool, HaaLouLingRelicPool, HaaLouLingPotionPool>
{
    public override Color NameColor => new(0.18f, 0.70f, 0.62f);
    public override Color EnergyLabelOutlineColor => new(0.03f, 0.33f, 0.29f);
    public override Color MapDrawingColor => new(0.18f, 0.70f, 0.62f);
    public override CharacterGender Gender => CharacterGender.Feminine;

    public override int StartingHp => 72;
    public override int StartingGold => 99;

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
                CharacterSelectLockedIconPath: "res://images/packed/character_select/char_select_haa_lou_ling.png",
                CharacterSelectTransitionPath: "res://materials/transitions/silent_transition_mat.tres",
                MapMarkerPath: "res://images/ui/top_panel/character_icon_haa_lou_ling.png"
            ),
            Vfx: new(
                TrailPath: "res://scenes/vfx/card_trail_haa_lou_ling.tscn"
            ),
            Audio: new(
                // 攻击音效
                // AttackSfx: null,
                // 施法音效
                // CastSfx: null,
                // 死亡音效
                // DeathSfx: null,
                // 角色选择音效
                // CharacterSelectSfx: null,
                CharacterTransitionSfx: "event:/sfx/ui/wipe_silent"
            ),
            Multiplayer: new(
                ArmPointingTexturePath: "res://images/ui/hands/multiplayer_hand_haa_lou_ling_point.png",
                ArmRockTexturePath: "res://images/ui/hands/multiplayer_hand_haa_lou_ling_rock.png",
                ArmPaperTexturePath: "res://images/ui/hands/multiplayer_hand_haa_lou_ling_paper.png",
                ArmScissorsTexturePath: "res://images/ui/hands/multiplayer_hand_haa_lou_ling_scissors.png"
            ),
            VisualCues: HaaLouLingCombatVisualCues.Set
        ));

    public override float AttackAnimDelay => 0f;
    public override float CastAnimDelay => 0f;

    protected override NCreatureVisuals? TryCreateCreatureVisuals() =>
        RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.Scenes!.VisualsPath!);

    protected override IEnumerable<StartingDeckEntry> StartingDeckEntries => [
        new(typeof(StrikeHaaLouLing), 4),
        new(typeof(DefendHaaLouLing), 4),
            new(typeof(FindFood), 1)
    ];

    protected override ModAnimStateMachine? SetupCustomCombatAnimationStateMachine(Node visualsRoot, CharacterModel character) =>
        ModAnimStateMachines.StandardCue(
            visualsRoot,
            character,
            HaaLouLingCombatVisualCues.Idle,
            HaaLouLingCombatVisualCues.Dead,
            false,
            HaaLouLingCombatVisualCues.Hit,
            false,
            HaaLouLingCombatVisualCues.Attack,
            false,
            HaaLouLingCombatVisualCues.Cast,
            false,
            HaaLouLingCombatVisualCues.Relaxed,
            true,
            HaaLouLingCombatVisualCues.Set);

    protected override IEnumerable<Type> StartingRelicTypes => [
        typeof(Anchor)
    ];

    public override List<string> GetArchitectAttackVfx() => [
        "vfx/haa_lou_ling_strike_slash"
    ];
}
