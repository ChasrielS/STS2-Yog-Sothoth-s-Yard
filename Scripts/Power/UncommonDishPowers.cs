using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using YogSothothsYard.Scripts.Cooking;

namespace YogSothothsYard.Scripts;

public abstract class UncommonDishPower : ModPowerTemplate
{
    protected abstract string IconFileName { get; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string? CustomIconPath => $"res://images/powers/{IconFileName}_power_icon.png";
    public override string? CustomBigIconPath => $"res://images/powers/{IconFileName}_power.png";

    public void FlashEffect() => Flash();
}

public abstract class SingleUncommonDishPower : UncommonDishPower
{
    public override PowerStackType StackType => PowerStackType.Single;
}

public abstract class TurnStartBlockDishPower : UncommonDishPower
{
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
        await CreatureCmd.GainBlock(Owner, AmountOnTurnStart, ValueProp.Unpowered, null);
    }
}

[RegisterPower]
public sealed class MingLuShaoLuoPower : TurnStartBlockDishPower
{
    protected override string IconFileName => "ming_lu_shao_luo";
}

[RegisterPower]
public sealed class KaoRuZhuPower : UncommonDishPower
{
    protected override string IconFileName => "kao_ru_zhu";
}

[RegisterPower]
public sealed class FenZhengRouPower : UncommonDishPower
{
    protected override string IconFileName => "fen_zheng_rou";
}

[RegisterPower]
public sealed class YanJuJiPower : SingleUncommonDishPower
{
    protected override string IconFileName => "yan_ju_ji";
}

[RegisterPower]
public sealed class ZhangYuNingMengZhiPower : UncommonDishPower
{
    protected override string IconFileName => "zhang_yu_ning_meng_zhi";

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
        await PlayerCmd.GainEnergy(AmountOnTurnStart, player);
        await HaaLouLingCookingService.AddRandomIngredients(
            choiceContext,
            player,
            AmountOnTurnStart
        );
        await CardPileCmd.Draw(choiceContext, AmountOnTurnStart, player);
    }
}

[RegisterPower]
public sealed class KouShuiJiPower : UncommonDishPower
{
    protected override string IconFileName => "kou_shui_ji";

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

        var chickenCount = HaaLouLingCookingService.CountIngredients<JiRou>(player);
        if (chickenCount <= 0)
        {
            return;
        }

        Flash();
        await HaaLouLingCookingService.DamageAllEnemies(
            choiceContext,
            player,
            chickenCount * AmountOnTurnStart
        );
    }
}

[RegisterPower]
public sealed class GongBaoJiDingPower : UncommonDishPower
{
    protected override string IconFileName => "gong_bao_ji_ding";
}

[RegisterPower]
public sealed class LuoShiFenPower : TurnStartBlockDishPower
{
    protected override string IconFileName => "luo_shi_fen";
}

[RegisterPower]
public sealed class JiDiZhouPower : UncommonDishPower
{
    protected override string IconFileName => "ji_di_zhou";
}

[RegisterPower]
public sealed class ShaoLaShuangPinPower : UncommonDishPower
{
    protected override string IconFileName => "shao_la_shuang_pin";
}

[RegisterPower]
public sealed class ZhangChaYaPower : UncommonDishPower
{
    protected override string IconFileName => "zhang_cha_ya";
}

[RegisterPower]
public sealed class ZhuDuJiPower : UncommonDishPower
{
    protected override string IconFileName => "zhu_du_ji";
}

[RegisterPower]
public sealed class HuoBaoYaoHuaPower : UncommonDishPower
{
    protected override string IconFileName => "huo_bao_yao_hua";

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
        await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            Owner,
            AmountOnTurnStart,
            Owner,
            null
        );
    }
}
