using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

public abstract class RareDishPower : DishPower
{
    protected abstract string IconFileName { get; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string? CustomIconPath => $"res://images/powers/{IconFileName}_power_icon.png";
    public override string? CustomBigIconPath => $"res://images/powers/{IconFileName}_power.png";

    public void FlashEffect() => Flash();
}

public abstract class SingleRareDishPower : RareDishPower
{
    public override PowerStackType StackType => PowerStackType.Single;
}

[RegisterPower]
public sealed class YangWangXingKongPower : RareDishPower
{
    protected override string IconFileName => "yang_wang_xing_kong";

    protected override bool HasTurnStartEffect => true;

    protected override async Task OnTurnStart(Player player, PlayerChoiceContext choiceContext, ICombatState combatState, int amount)
    {
        var targets = combatState.Players
            .Select(candidate => candidate.Creature)
            .Concat(combatState.Enemies)
            .Where(creature => creature.IsAlive)
            .ToList();
        foreach (var target in targets)
        {
            var poison = target.CurrentHp * amount / 100;
            if (poison <= 0)
            {
                continue;
            }

            await PowerCmd.Apply<PoisonPower>(
                choiceContext,
                target,
                poison,
                Owner,
                null
            );
        }
    }
}

[RegisterPower]
public sealed class ZhangYuWanZiPower : RareDishPower
{
    protected override string IconFileName => "zhang_yu_wan_zi";
    public override LocString Description
    {
        get
        {
            var description = base.Description;
            var state = IsMutable ? HaaLouLingCookingService.GetState(Owner.Player!) : null;
            description.Add("Progress", state?.OctopusBallIngredientProgress ?? 0);
            description.Add("Charges", state?.BonusDishEffectCharges ?? 0);
            return description;
        }
    }
}

[RegisterPower]
public sealed class BaoZaiFanPower : RareDishPower
{
    protected override string IconFileName => "bao_zai_fan";
}

[RegisterPower]
public sealed class ShengYanPower : RareDishPower
{
    private int _mangleTurn;
    private int _appliedMangle;
    protected override string IconFileName => "sheng_yan";

    public override LocString Description
    {
        get
        {
            var text = base.Description;
            text.Add("Mangle", IsMutable ? HaaLouLingCookingService.GetShengYanMangle(Owner.Player!) : 0);
            return text;
        }
    }

    protected override bool HasTurnStartEffect => true;

    public override Task AfterRemoved(Creature oldOwner)
    {
        HaaLouLingCookingService.GetState(oldOwner.Player!).ShengYanManglePerTurn = 0;
        return Task.CompletedTask;
    }

    protected override async Task OnTurnStart(Player player, PlayerChoiceContext choiceContext, ICombatState combatState, int amount)
    {
        await HaaLouLingCookingService.ApplyPowerToEnemies<WeakPower>(
            choiceContext,
            player,
            amount
        );
        await HaaLouLingCookingService.ApplyPowerToEnemies<VulnerablePower>(
            choiceContext,
            player,
            amount
        );

        if (_mangleTurn != player.PlayerCombatState!.TurnNumber)
        {
            _mangleTurn = player.PlayerCombatState.TurnNumber;
            _appliedMangle = 0;
        }
        var totalMangle = HaaLouLingCookingService.GetShengYanMangle(player);
        var mangle = totalMangle - _appliedMangle;
        _appliedMangle = totalMangle;
        if (mangle > 0)
        {
            await HaaLouLingCookingService.ApplyPowerToEnemies<ManglePower>(
                choiceContext,
                player,
                mangle
            );
        }
    }
}

[RegisterPower]
public sealed class MaoXueWangPower : RareDishPower
{
    protected override string IconFileName => "mao_xue_wang";

    public override async Task AfterBlockGained(
        Creature creature,
        decimal amount,
        ValueProp props,
        CardModel? cardSource
    )
    {
        if (creature != Owner || amount <= 0 || Amount <= 0 || Owner.Player == null)
        {
            return;
        }

        Flash();
        await HaaLouLingCookingService.DamageRandomEnemy(
            new ThrowingPlayerChoiceContext(),
            Owner.Player,
            (int)amount * Amount
        );
    }
}

[RegisterPower]
public sealed class KaiShuiBaiCaiPower : SingleRareDishPower
{
    protected override string IconFileName => "kai_shui_bai_cai";

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount
    )
    {
        modifiedAmount = amount;
        if (target != Owner ||
            canonicalPower is GuShenDeYiPiePower ||
            canonicalPower.GetTypeForAmount(amount) != PowerType.Debuff)
        {
            return false;
        }

        modifiedAmount = 0;
        return true;
    }
}

[RegisterPower]
public sealed class ShenQianTangFenPower : RareDishPower
{
    protected override string IconFileName => "shen_qian_tang_fen";

    protected override bool HasTurnStartEffect => true;

    protected override async Task OnTurnStart(Player player, PlayerChoiceContext choiceContext, ICombatState combatState, int amount)
    {
        await PowerCmd.Apply<PlatingPower>(
            choiceContext,
            Owner,
            amount,
            Owner,
            null
        );
    }
}

[RegisterPower]
public sealed class PenCaiPower : RareDishPower
{
    protected override string IconFileName => "pen_cai";
}

[RegisterPower]
public sealed class MoYuBaoPower : SingleRareDishPower
{
    protected override string IconFileName => "mo_yu_bao";
}
