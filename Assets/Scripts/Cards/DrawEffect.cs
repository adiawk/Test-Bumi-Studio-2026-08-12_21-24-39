using UnityEngine;

/// <summary>
/// Draws cards from the player's deck.
/// </summary>
[CreateAssetMenu(fileName = "DrawEffect", menuName = "Deckbuilder/Effects/Draw")]
public class DrawEffect : CardEffect
{
    [SerializeField] int amount = 1;

    public int Amount => amount;

    public override void Resolve(EffectContext context)
    {
        if (context == null || context.Drawer == null)
            return;

        context.Drawer.DrawCards(amount);
    }
}
