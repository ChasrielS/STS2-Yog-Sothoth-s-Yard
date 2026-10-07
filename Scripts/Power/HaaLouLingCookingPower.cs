using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterPower]
public class HaaLouLingCookingPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;

    public override async Task BeforeHandDraw(
        Player player,
        PlayerChoiceContext choiceContext,
        ICombatState combatState
    )
    {
        if (player.Creature != Owner)
        {
            return;
        }

        if (Owner.GetPower<TianZaoPlusPower>() is { } early)
            await early.CheckForFullQueue(choiceContext);
        await HaaLouLingCookingService.Advance(choiceContext, player);
        if (Owner.GetPower<TianZaoPower>() is { } late)
            await late.CheckForFullQueue(choiceContext);
    }
}
