using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class DangHui : HaaLouLingCard
{
    public DangHui() : base(1, CardType.Skill, CardRarity.Common, TargetType.AllEnemies) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 20)];

    protected override bool UsesGuiYuan => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await HaaLouLingCombatRules.GiveGuiYuanToEnemies(context, Owner, DynamicVars["Amount"].IntValue, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Amount"].UpgradeValueBy(10);
    }
}
