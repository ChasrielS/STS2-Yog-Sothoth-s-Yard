using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.HoverTips;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class ZhanDuanEYuan : HaaLouLingCard
{
    public ZhanDuanEYuan() : base(2, CardType.Attack, CardRarity.Rare, TargetType.RandomEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var curses = new[] { PileType.Hand, PileType.Draw, PileType.Discard }
            .SelectMany(pile => pile.GetPile(Owner).Cards).Where(card => card.Type == CardType.Curse).ToList();
        var hits = 0;
        foreach (var card in curses)
            if (await CardCmd.Exhaust(context, card) != null) hits++;
        if (hits > 0) await Attack(context, play, hits, random: true);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4);
    }
}
