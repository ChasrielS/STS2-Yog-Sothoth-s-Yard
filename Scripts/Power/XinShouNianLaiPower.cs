using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace YogSothothsYard.Scripts;

[RegisterPower]
public sealed class XinShouNianLaiPower : HaaLouLingPower
{
    public override string? CustomIconPath => "res://images/powers/xin_shou_nian_lai_power_icon.png";
    public override string? CustomBigIconPath => "res://images/powers/xin_shou_nian_lai_power.png";
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => new[] { HaaLouLingKeywords.KeywordId("DaoShu") }.ToHoverTips().Concat([HoverTipFactory.FromKeyword(CardKeyword.Retain)]);

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState combatState)
    {
        if (player.Creature != Owner || AmountOnTurnStart <= 0) return;
        Flash();
        var pool = ModelDb.CardPool<HaaLouLingCardPool>().GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .OfType<HaaLouLingCard>().Where(card => card.Form != KnifeForm.None && card.CanBeGeneratedInCombat);
        var cards = CardFactory.GetForCombat(player, pool, AmountOnTurnStart, player.RunState.Rng.CombatCardGeneration).ToList();
        foreach (var card in cards)
        {
            CardCmd.ApplyKeyword(card, CardKeyword.Retain);
            card.EnergyCost.SetThisTurn(0);
        }
        var results = await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, player);
        CardCmd.PreviewCardPileAdd(results);
    }
}
