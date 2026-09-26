using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts.GameActions;

public sealed class StartCookingDishAction : GameAction
{
    public override ulong OwnerId => Player.NetId;
    public override GameActionType ActionType => GameActionType.CombatPlayPhaseOnly;

    public Player Player { get; }
    public ModelId DishModelId { get; }
    public bool IsUpgraded { get; }

    public StartCookingDishAction(Player player, ModelId dishModelId, bool isUpgraded)
    {
        Player = player;
        DishModelId = dishModelId;
        IsUpgraded = isUpgraded;
    }

    protected override async Task ExecuteAction()
    {
        var choiceContext = new GameActionPlayerChoiceContext(this);
        await HaaLouLingCookingService.StartCooking(choiceContext, Player, DishModelId, IsUpgraded);
    }

    public override INetAction ToNetAction() =>
        new NetStartCookingDishAction
        {
            DishModelId = DishModelId,
            IsUpgraded = IsUpgraded
        };
}
