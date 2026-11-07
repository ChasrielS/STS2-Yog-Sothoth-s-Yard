using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Combat;

namespace YogSothothsYard.Scripts;

[RegisterCard(typeof(HaaLouLingCardPool))]
public sealed class YiQiHeCheng : HaaLouLingCard
{
    public YiQiHeCheng() : base(4, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(15, ValueProp.Move), new EnergyVar(1)];

    private int SkillsPlayed => Owner?.Creature.CombatState is not { } state ? 0 :
        CombatManager.Instance.History.CardPlaysFinished.Count(entry => entry.HappenedThisTurn(state) &&
            entry.CardPlay.Player == Owner && entry.CardPlay.Card.Type == CardType.Skill);
    public override bool WantsConditionalHighlight => SkillsPlayed > 0;

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal original, out decimal modified)
    {
        modified = card == this ? Math.Max(0, original - SkillsPlayed) : original;
        return modified != original;
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Attack(context, play);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4);
    }
}
