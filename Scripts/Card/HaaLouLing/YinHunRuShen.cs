using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class YinHunRuShen : HaaLouLingCard
{
    public YinHunRuShen() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override bool UsesGuiYuan => true;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var target = play.Target!;
        var amount = HaaLouLingCombatRules.GuiYuan(target);
        if (amount <= 0) return;
        await CreatureCmd.Damage(context, target, amount, ValueProp.Unblockable | ValueProp.Unpowered, this, play);
        var recipient = ErosionRank == 3 ? FindRecipient(target) : Owner.Creature;
        await HaaLouLingCombatRules.RemoveGuiYuan(target);
        if (recipient != null)
            await HaaLouLingCombatRules.ReceiveTransferredGuiYuan(context, recipient, amount, this);
    }

    private Creature? FindRecipient(Creature original)
    {
        var combat = CombatState;
        if (combat == null) return null;
        var candidates = combat.HittableEnemies.Where(enemy => enemy != original).ToList();
        if (candidates.Count == 0) candidates.AddRange(combat.Players.Select(player => player.Creature).Where(creature => creature.IsAlive));
        return candidates.Count == 0 ? null : Owner.RunState.Rng.CombatTargets.NextItem(candidates);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
