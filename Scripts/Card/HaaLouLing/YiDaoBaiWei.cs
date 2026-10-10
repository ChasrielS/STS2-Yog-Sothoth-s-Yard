using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class YiDaoBaiWei : HaaLouLingCard
{
    public YiDaoBaiWei() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 2)];
    protected override bool UsesKnife => true;
    public override KnifeForm Form => KnifeForm.Finish;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var count = await FinishKnife(context);
        await HaaLouLingCookingService.RandomDishEffects(context, Owner,
            count * DynamicVars["Amount"].IntValue, uncommonOnly: true);
    }

    protected override void OnUpgrade() => DynamicVars["Amount"].UpgradeValueBy(1);
}
