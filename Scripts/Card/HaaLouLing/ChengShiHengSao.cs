using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class ChengShiHengSao : HaaLouLingCard
{
    public ChengShiHengSao() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(15, ValueProp.Move)];
    protected override bool UsesKnife => true;
    public override KnifeForm Form => KnifeForm.Follow;
    public override IEnumerable<CardKeyword> CanonicalKeywords => FormKeywords.Concat([CardKeyword.Ethereal]);
    public override bool WantsGreenOutline => Knife > 0;

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal original, out decimal modified)
    {
        modified = card == this && Knife > 0 ? 0 : original;
        return modified != original;
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var eligible = Knife > 0;
        await Attack(context, play);
        if (eligible) await GainKnife(context, 1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5);
    }
}
