using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Commands;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class TiQi : HaaLouLingCard
{
    public TiQi() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(4, ValueProp.Move), new EnergyVar(1)];

    protected override bool UsesKnife => true;
    public override bool GainsBlock => true;
    public override bool WantsConditionalHighlight => Owner?.PlayerCombatState != null &&
        HaaLouLingCookingService.GetState(Owner).KnifeGainedThisTurn;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var eligible = WantsConditionalHighlight;
        await Block(play);
        if (eligible) await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1);
    }
}
