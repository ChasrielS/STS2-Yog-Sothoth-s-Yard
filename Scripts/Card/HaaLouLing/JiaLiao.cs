using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.ValueProps;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class JiaLiao : HaaLouLingCard
{
    public JiaLiao() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6, ValueProp.Move), new ExtraDamageVar(2), new CardsVar(3)];
    protected override bool ShouldGlowRedInternal => Owner?.PlayerCombatState != null && Entry.FoodPile.GetPile(Owner).IsEmpty;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var bonus = 0;
        if (!Entry.FoodPile.GetPile(Owner).IsEmpty)
        {
            var selected = (await HaaLouLingCardChoices.SelectOwnedIngredients(context, this, DynamicVars.Cards.IntValue)).ToList();
            bonus = selected.Count * DynamicVars.ExtraDamage.IntValue;
            await CardPileCmd.RemoveFromCombat(selected, false);
        }
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue + bonus).FromCard(this, play).Targeting(play.Target!)
            .WithHitVfxNode(HaaLouLingStrikeSlashVfx.Create).Execute(context);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        DynamicVars.ExtraDamage.UpgradeValueBy(1);
    }
}
