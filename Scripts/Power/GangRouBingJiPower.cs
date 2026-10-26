using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace YogSothothsYard.Scripts;

[RegisterPower]
public sealed class GangRouBingJiPower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/gang_rou_bing_ji_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/gang_rou_bing_ji_power.png";
    private int _attacks;
    private int _skills;
    private int _triggered;

    public override Task BeforeSideTurnStart(PlayerChoiceContext context, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState state)
    {
        if (participants.Contains(Owner)) _attacks = _skills = _triggered = 0;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Player.Creature != Owner || _triggered >= Amount) return;
        if (play.Card.Type == CardType.Attack) _attacks++;
        else if (play.Card.Type == CardType.Skill) _skills++;
        if (_attacks == 0 || _skills == 0) return;
        _attacks--;
        _skills--;
        _triggered++;
        Flash();
        await PlayerCmd.GainEnergy(1, play.Player);
    }
}
