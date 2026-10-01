using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class LianMianBuJue : HaaLouLingCard
{
    public LianMianBuJue() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(10, ValueProp.Move)];
    protected override bool UsesKnife => true;
    public override KnifeForm Form => KnifeForm.Follow;
    public override bool WantsGreenOutline => Knife > 0;

    public override bool GainsBlock => true;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var eligible = Knife > 0;
        await Block(play);
        if (eligible)
        {
            await CardPileCmd.Add(this, PileType.Draw, CardPilePosition.Top);
            await GainKnife(context, 1);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);
    }
}
