using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class ZhaoHouYouZhao : HaaLouLingCard
{
    public ZhaoHouYouZhao() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2), new IntVar("ExtraCards", 2)];

    protected override bool UsesKnife => true;
    public override bool WantsConditionalHighlight => Owner?.PlayerCombatState != null &&
        HaaLouLingCookingService.GetState(Owner).KnifeSpentThisTurn;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var eligible = WantsConditionalHighlight;
        await CardPileCmd.Draw(context, DynamicVars.Cards.IntValue + (eligible ? DynamicVars["ExtraCards"].IntValue : 0), Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["ExtraCards"].UpgradeValueBy(1);
    }
}
