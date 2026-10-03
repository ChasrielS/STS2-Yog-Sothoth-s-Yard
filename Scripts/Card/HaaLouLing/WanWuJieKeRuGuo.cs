using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class WanWuJieKeRuGuo : HaaLouLingCard
{
    public WanWuJieKeRuGuo() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(10, ValueProp.Move)];
    public override bool GainsBlock => true;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Block(play);
        var cards = new[] { PileType.Hand, PileType.Draw, PileType.Discard }.SelectMany(pile => pile.GetPile(Owner).Cards).ToList();
        await HaaLouLingCardChoices.TransformCombatCards(context, this, cards);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(4);
}
