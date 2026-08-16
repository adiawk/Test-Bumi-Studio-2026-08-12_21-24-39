using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player combat stats. Deck piles stay on DeckManager.
/// Lives on the character root; sprites live under Visual.
/// </summary>
public class Player : MonoBehaviour, IEffectTarget, ICombatFeedback
{
    readonly Combatant body = new Combatant();

    [SerializeField] List<Sprite> visualLayers = new List<Sprite>();

    CharacterVisual visual;
    CharacterFeedback feedback;

    public int Hp => body.Hp;
    public int MaxHp => body.MaxHp;
    public int Block => body.Block;
    public int Energy { get; private set; }
    public int MaxEnergy { get; private set; }
    public bool IsAlive => body.IsAlive;
    public StatusBag Statuses => body.Statuses;
    public DeckManager Deck { get; private set; }

    void Awake()
    {
        visual = GetComponent<CharacterVisual>();
        feedback = GetComponent<CharacterFeedback>();
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
        if (feedback == null)
            feedback = GetComponent<CharacterFeedback>();
        if (visual != null)
            visual.ApplyLayers(visualLayers);
    }

    public void ResetEnergy()
    {
        Energy = MaxEnergy;
    }

    public void AddMaxEnergy(int amount)
    {
        if (amount <= 0)
            return;

        MaxEnergy += amount;
        Energy += amount;
    }

    public bool TrySpendEnergy(int cost)
    {
        if (cost < 0 || Energy < cost)
            return false;

        Energy -= cost;
        return true;
    }

    public void ApplyStatus(StatusId id, int stacks)
    {
        if (id == StatusId.ExtraEnergy)
        {
            AddMaxEnergy(stacks);
            return;
        }

        body.ApplyStatus(id, stacks);
    }

    public void ClearBlock()
    {
        body.ClearBlock();
    }

    public void TakeDamage(int amount)
    {
        DamageResult result = body.ApplyDamage(amount);
        if (feedback != null)
            feedback.PlayIncomingHit(result.Blocked, result.HpDamage);
    }

    public void GainBlock(int amount) => body.GainBlock(amount);
    public void Heal(int amount) => body.Heal(amount);

    public void PlayAttackFeedback()
    {
        if (feedback != null)
            feedback.PlayAttackFeedback();
    }

    public void PlayHitFeedback()
    {
        if (feedback != null)
            feedback.PlayHitFeedback();
    }

    public void PlayBlockFeedback()
    {
        if (feedback != null)
            feedback.PlayBlockFeedback();
    }

    public void PlayHealFeedback()
    {
        if (feedback != null)
            feedback.PlayHealFeedback();
    }

    void LateUpdate()
    {
        if (visual != null)
            visual.SetAlive(IsAlive);
        if (!IsAlive && feedback != null)
            feedback.PlayDeathFeedback();
    }
}
