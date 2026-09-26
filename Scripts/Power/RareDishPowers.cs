using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

public abstract class RareDishPower : ModPowerTemplate
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
        var targets = combatState.Players
            .Select(candidate => candidate.Creature)
            .Concat(combatState.Enemies)
            .Where(creature => creature.IsAlive)
            .ToList();
        foreach (var target in targets)
        {
            var poison = target.CurrentHp * AmountOnTurnStart / 100;
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
}

[RegisterPower]
public sealed class BaoZaiFanPower : RareDishPower
{
    protected override string IconFileName => "bao_zai_fan";
}

[RegisterPower]
public sealed class ShengYanPower : RareDishPower
{
    protected override string IconFileName => "sheng_yan";

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
        await HaaLouLingCookingService.ApplyPowerToEnemies<WeakPower>(
            choiceContext,
            player,
            AmountOnTurnStart
        );
        await HaaLouLingCookingService.ApplyPowerToEnemies<VulnerablePower>(
            choiceContext,
            player,
            AmountOnTurnStart
        );

        var mangle = HaaLouLingCookingService.GetShengYanMangle(player);
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
        await PowerCmd.Apply<PlatingPower>(
            choiceContext,
            Owner,
            AmountOnTurnStart,
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
