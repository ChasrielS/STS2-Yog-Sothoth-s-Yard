using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class DaNuoChiLing : HaaLouLingCard
{
    public DaNuoChiLing() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 10)];
    protected override bool UsesGuiYuan => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => FormKeywords.Concat([CardKeyword.Exhaust]);

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await HaaLouLingCombatRules.GiveGuiYuanToEnemies(context, Owner, DynamicVars["Amount"].IntValue, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); RemoveKeyword(CardKeyword.Exhaust);
    }
}
