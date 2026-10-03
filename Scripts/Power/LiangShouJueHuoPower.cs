using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

[RegisterPower]
public sealed class LiangShouJueHuoPower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/liang_shou_jue_huo_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/liang_shou_jue_huo_power.png";
    public override PowerStackType StackType => PowerStackType.Single;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource) => Synchronize(new ThrowingPlayerChoiceContext());

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power, decimal amount,
        Creature? applier, CardModel? cardSource)
    {
        if (power.Owner == Owner && power is GuoWanPiaoPenPower or DaoFaJingJinPower) await Synchronize(context);
    }

    public async Task Synchronize(PlayerChoiceContext context)
    {
        var capacity = HaaLouLingCookingState.DefaultCapacity + Owner.GetPowerAmount<GuoWanPiaoPenPower>();
        var knifeLimit = 3 + Owner.GetPowerAmount<DaoFaJingJinPower>();
        HaaLouLingCookingService.RefreshCapacity(Owner.Player!);
        if (capacity < knifeLimit)
            await PowerCmd.Apply<GuoWanPiaoPenPower>(context, Owner, knifeLimit - capacity, Owner, null);
        else if (knifeLimit < capacity)
            await PowerCmd.Apply<DaoFaJingJinPower>(context, Owner, capacity - knifeLimit, Owner, null);
    }
}
