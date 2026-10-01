using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class WeiJiaRenChuDao : HaaLouLingCard
{
    public WeiJiaRenChuDao() : base(3, CardType.Skill, CardRarity.Rare, TargetType.AllAllies) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Amount", 4)];
    protected override bool UsesKnife => true;
    public override KnifeForm Form => KnifeForm.Finish;
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    public override IEnumerable<CardKeyword> CanonicalKeywords => FormKeywords.Concat([CardKeyword.Exhaust]);

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var count = await FinishKnife(context);
        var turns = Math.Max(0, DynamicVars["Amount"].IntValue - count);
        if (turns > 0) await Apply<DaoShiJinDePower>(context, turns);
        foreach (var player in CombatState!.Players.Where(player => player.Creature.IsAlive))
            await PowerCmd.Apply<MianYiPower>(context, player.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Amount"].UpgradeValueBy(-1);
    }
}
