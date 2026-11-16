using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class FenNiYiFen : HaaLouLingCard
{
    public FenNiYiFen() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 1)];
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    private bool ShareWithAll => ErosionRank == 3 || ErosionRank == 2 && IsUpgraded;
    public override TargetType TargetType => ShareWithAll ? TargetType.AllAllies : base.TargetType;
    protected override bool IsPlayable => base.IsPlayable && (!ShareWithAll || CombatState is not { } combat ||
        combat.Players.Any(player => player != Owner && player.Creature.IsAlive));

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var recipientId = ShareWithAll ? "" : play.Target!.Player!.NetId.ToString();
        var existing = Owner.Creature.Powers.OfType<FenNiYiFenPower>().FirstOrDefault(power => power.RecipientId == recipientId);
        if (existing != null) await PowerCmd.ModifyAmount(context, existing, DynamicVars["Amount"].IntValue, Owner.Creature, this);
        else
        {
            var power = (FenNiYiFenPower)ModelDb.Power<FenNiYiFenPower>().ToMutable();
            power.RecipientId = recipientId;
            await PowerCmd.Apply(context, power, Owner.Creature, DynamicVars["Amount"].IntValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Amount"].UpgradeValueBy(1);
    }
}
