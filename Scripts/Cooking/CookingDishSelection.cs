using MegaCrit.Sts2.Core.Models;
using YogSothothsYard.Scripts.Card.HaaLouLingDish;

namespace YogSothothsYard.Scripts.Cooking;

public sealed class CookingDishSelection
{
    private readonly List<HaaLouLingDishCard> _selected = [];

    public IReadOnlyList<HaaLouLingDishCard> Selected => _selected;
    public int Version { get; private set; }

    public void Toggle(HaaLouLingDishCard card, bool wasSelected, bool canSelect)
    {
        if (wasSelected) _selected.Remove(card);
        else if (canSelect) _selected.Add(card);
        else return;
        Version++;
    }

    public async Task<IEnumerable<CardModel>> InSelectionOrder(Task<IEnumerable<CardModel>> result)
    {
        var selected = (await result).ToHashSet();
        return _selected.Where(card => selected.Remove(card)).Cast<CardModel>().Concat(selected).ToList();
    }
}
