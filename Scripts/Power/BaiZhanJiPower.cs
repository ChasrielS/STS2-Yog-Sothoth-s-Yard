using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterPower]
public class BaiZhanJiPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string? CustomIconPath => "res://images/powers/bai_zhan_ji_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/bai_zhan_ji_power.png";

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<JiRou>()
    ];

    public override async Task BeforeHandDraw(
        Player player,
        PlayerChoiceContext choiceContext,
        ICombatState combatState
    )
    {
        if (player.Creature != Owner || AmountOnTurnStart <= 0)
        {
            return;
        }

        Flash();

        var chicken = CardFactory.GetForCombat(
            player,
            [ModelDb.Card<JiRou>()],
            2,
            player.RunState.Rng.CombatCardGeneration
        ).ToList();

        if (chicken.Count > 0)
        {
            await HaaLouLingCookingService.AddGeneratedIngredients(choiceContext, player, chicken);
        }

        await PowerCmd.Decrement(this);
    }
}
