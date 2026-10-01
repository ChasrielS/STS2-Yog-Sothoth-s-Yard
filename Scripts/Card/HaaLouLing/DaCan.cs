using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class DaCan : HaaLouLingCard
{
    public DaCan() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AllAllies) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 5)];
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    public override IEnumerable<CardKeyword> CanonicalKeywords => FormKeywords.Concat([CardKeyword.Exhaust]);

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        foreach (var player in CombatState!.Players.Where(player => player.Creature.IsAlive)) await CreatureCmd.Heal(player.Creature, DynamicVars["Amount"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Amount"].UpgradeValueBy(3);
    }
}
