using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class GaiDao : HaaLouLingCard
{
    public GaiDao() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    public override KnifeForm Form => KnifeForm.Turn;
    protected override bool UsesKnife => true;
    public override bool WantsGreenOutline => Knife > 0;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(15, ValueProp.Move), new CardsVar(2)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat([HoverTipFactory.Static(StaticHoverTip.Transform)]);

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var eligible = Knife > 0;
        await Attack(context, play);
        if (!eligible) return;
        await SpendKnife(context, 1);
        var selected = await CardSelectCmd.FromHand(context, Owner,
            new CardSelectorPrefs(new LocString("cards", Id.Entry + ".selectionScreenPrompt"), 1),
            null, this);
        foreach (var card in selected)
        {
            await CardPileCmd.RemoveFromCombat(card, false);
            await HaaLouLingCookingService.AddRandomIngredients(context, Owner, DynamicVars.Cards.IntValue, CardRarity.Uncommon);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(8);
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}
