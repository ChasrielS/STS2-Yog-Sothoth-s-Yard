using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using YogSothothsYard.Scripts.GameActions;

namespace YogSothothsYard.Scripts.Cooking;

public struct NetStartCookingDishAction : INetAction, IPacketSerializable
{
    public ModelId DishModelId;
    public bool IsUpgraded;

    public GameAction ToGameAction(Player player) =>
        new StartCookingDishAction(player, DishModelId, IsUpgraded);

    public void Serialize(PacketWriter writer)
    {
        writer.WriteModelEntry(DishModelId);
        writer.WriteBool(IsUpgraded);
    }

    public void Deserialize(PacketReader reader)
    {
        DishModelId = reader.ReadModelIdAssumingType<CardModel>();
        IsUpgraded = reader.ReadBool();
    }
}
