using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class YiZhanYiWu : HaaLouLingCard
{
    public YiZhanYiWu() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CalculationBaseVar(0), new ExtraDamageVar(4), new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (card, _) => HaaLouLingCombatRules.KnifeForCard(card)), new IntVar("Increase", 1)];
    protected override bool UsesKnife => true;
    public override KnifeForm Form => KnifeForm.Finish;
    public override bool CanBeGeneratedInCombat => false;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        base.AdditionalHoverTips.Concat([HoverTipFactory.Static(StaticHoverTip.Fatal)]);
    private int _increasedDamage;

    [SavedProperty]
    public int IncreasedDamage
    {
        get => _increasedDamage;
        set
        {
            AssertMutable();
            _increasedDamage = value;
            DynamicVars.ExtraDamage.BaseValue = 4 + value;
        }
    }
    protected override void AfterDowngraded() => DynamicVars.ExtraDamage.BaseValue = 4 + IncreasedDamage;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var fatal = play.Target!.Powers.All(power => power.ShouldOwnerDeathTriggerFatal());
        await FinishKnife(context);
        var attack = await Attack(context, play, calculated: true);
        if (fatal && attack.Results.SelectMany(hit => hit).Any(result => result.WasTargetKilled))
        {
            var increase = DynamicVars["Increase"].IntValue;
            IncreasedDamage += increase;
            if (DeckVersion is YiZhanYiWu deck) deck.IncreasedDamage += increase;
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Increase"].UpgradeValueBy(1);
    }
}
