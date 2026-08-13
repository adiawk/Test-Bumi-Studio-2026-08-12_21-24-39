using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player combat stats. Deck piles stay on DeckManager.
/// Lives on the character root; sprites live under Visual.
/// </summary>
public class Player : MonoBehaviour, IEffectTarget
{
    readonly Combatant body = new Combatant();

    [SerializeField] List<Sprite> visualLayers = new List<Sprite>();

    CharacterVisual visual;

    public int Hp => body.Hp;
    public int MaxHp => body.MaxHp;
    public int Block => body.Block;
    public int Energy { get; private set; }
    public int MaxEnergy { get; private set; }
    public bool IsAlive => body.IsAlive;
    public DeckManager Deck { get; private set; }

    void Awake()
    {
        visual = GetComponent<CharacterVisual>();
    }

    public void Initialize(int maxHp, int maxEnergy, DeckManager deck, int currentHp)
    {
        body.Initialize(maxHp);
        body.SetHp(currentHp);
        MaxEnergy = Mathf.Max(0, maxEnergy);
        Energy = MaxEnergy;
        Deck = deck;

        if (visual == null)
            visual = GetComponent<CharacterVisual>();
        if (visual != null)
            visual.ApplyLayers(visualLayers);
    }

    public void ResetEnergy()
    {
        Energy = MaxEnergy;
    }

    public bool TrySpendEnergy(int cost)
    {
        if (cost < 0 || Energy < cost)
            return false;

        Energy -= cost;
        return true;
    }

    public void ClearBlock()
    {
        body.ClearBlock();
    }

    public void TakeDamage(int amount) => body.TakeDamage(amount);
    public void GainBlock(int amount) => body.GainBlock(amount);
    public void Heal(int amount) => body.Heal(amount);

    void LateUpdate()
    {
        if (visual != null)
            visual.SetAlive(IsAlive);
    }
}

