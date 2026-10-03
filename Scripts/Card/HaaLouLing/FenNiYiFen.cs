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

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var recipient = play.Target!.Player!;
        var existing = Owner.Creature.Powers.OfType<FenNiYiFenPower>().FirstOrDefault(power => power.RecipientId == recipient.NetId.ToString());
        if (existing != null) await PowerCmd.ModifyAmount(context, existing, DynamicVars["Amount"].IntValue, Owner.Creature, this);
        else
        {
            var power = (FenNiYiFenPower)ModelDb.Power<FenNiYiFenPower>().ToMutable();
            power.RecipientId = recipient.NetId.ToString();
            await PowerCmd.Apply(context, power, Owner.Creature, DynamicVars["Amount"].IntValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Amount"].UpgradeValueBy(1);
    }
}
