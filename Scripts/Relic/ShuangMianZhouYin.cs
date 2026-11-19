using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Interop.AutoRegistration;
using YogSothothsYard.Scripts.Shadow;

namespace YogSothothsYard.Scripts;

[RegisterRelic(typeof(HaaLouLingRelicPool))]
public sealed class ShuangMianZhouYin : HaaLouLingRelic, IModRightClickableRelic
{
    private int _uses;

    protected override string ImageName => "shuang_mian_zhou_yin";
    public override RelicRarity Rarity => RelicRarity.Rare;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Uses", 1)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.Static(StaticHoverTip.Transform),
        HoverTipFactory.FromCard<CanZhou>()
    ];
    public override bool ShowCounter => IsMutable && CombatManager.Instance.IsInProgress && Owner?.Creature.CombatState != null;
    public override int DisplayAmount => _uses;

    public override Task BeforeCombatStart()
    {
        SetUses(DynamicVars["Uses"].IntValue);
        return Task.CompletedTask;
    }

    public override Task AfterObtained() =>
        CombatManager.Instance.IsInProgress ? BeforeCombatStart() : Task.CompletedTask;

    public override Task AfterCombatEnd(CombatRoom room)
    {
        SetUses(0);
        return Task.CompletedTask;
    }

    public bool CanHandleRightClickLocal(ModRightClickContext context) =>
        CanUse(context.Player) && !CombatManager.Instance.IsPlayerReadyToEndTurn(context.Player);

    public bool CanExecuteRightClick(ModRightClickExecutionContext context) => CanUse(context.Player);

    private bool CanUse(Player player) =>
        _uses > 0 && !IsMelted && player == Owner && player.Creature.IsAlive &&
        !CombatManager.Instance.IsOverOrEnding && player.PlayerCombatState?.Phase == PlayerTurnPhase.Play &&
        PileType.Hand.GetPile(player).Cards.Count > 0;

    public async Task OnRightClick(ModRightClickExecutionContext context)
    {
        if (context.PlayerChoiceContext is not { } choiceContext) return;
        var hand = PileType.Hand.GetPile(Owner);
        var prompt = new LocString("gameplay_ui", "YOG_SOTHOTH_S_YARD_SHUANG_MIAN_ZHOU_YIN_CHOOSE");
        var cards = (await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(prompt, 0, hand.Cards.Count), null, this)).ToArray();

        SetUses(_uses - 1);
        Flash();
        foreach (var card in cards)
        {
            if (card.Pile != hand || CombatManager.Instance.IsOverOrEnding) continue;
            if (card.Type == CardType.Curse) await CardCmd.Exhaust(choiceContext, card);
            else await CardCmd.Transform(card, Owner.Creature.CombatState!.CreateCard(ModelDb.Card<CanZhou>(), Owner));
        }
        if (ShadowRelicRules.TryGet(this, out var rule)) await ShadowRelicRules.Charge(this, rule.Amount, choiceContext);
    }

    private void SetUses(int uses)
    {
        _uses = uses;
        Status = uses > 0 ? RelicStatus.Active : RelicStatus.Normal;
        InvokeDisplayAmountChanged();
    }
}
