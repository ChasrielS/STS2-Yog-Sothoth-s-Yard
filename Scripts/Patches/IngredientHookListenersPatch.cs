using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Patching.Models;

namespace YogSothothsYard.Scripts.Patches;

public sealed class IngredientHookListenersPatch : IPatchMethod
{
    public static string PatchId => "yog_sothoths_yard_ingredient_hook_listeners";
    public static ModPatchTarget[] GetTargets() =>
        [PatchTarget.EnumeratorMethod<CombatState>(nameof(CombatState.IterateHookListeners))];

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var getPiles = AccessTools.PropertyGetter(typeof(PlayerCombatState), nameof(PlayerCombatState.AllPiles));
        var getListenerPiles = AccessTools.Method(typeof(IngredientHookListenersPatch), nameof(GetListenerPiles));
        var matched = false;
        foreach (var instruction in instructions)
        {
            if (instruction.Calls(getPiles))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = getListenerPiles;
                matched = true;
            }
            yield return instruction;
        }
        if (!matched) throw new InvalidOperationException("Cannot locate the combat hook listener pile collection.");
    }

    private static IReadOnlyList<CardPile> GetListenerPiles(PlayerCombatState state)
    {
        var piles = state.AllPiles;
        if (!piles.Any(pile => pile.Type == Entry.FoodPile && !pile.IsEmpty)) return piles;
        // 在逐张收集卡牌及其附属监听对象前跳过食材堆，避免开销随食材数量增长。
        return piles.Where(pile => pile.Type != Entry.FoodPile).ToArray();
    }
}
