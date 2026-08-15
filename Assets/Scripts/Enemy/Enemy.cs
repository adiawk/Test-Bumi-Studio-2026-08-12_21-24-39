using System.Collections;
using UnityEngine;

/// <summary>
/// Combat enemy with a telegraphing intent. No AI beyond a two-step pattern.
/// Lives on the character root; the animated body is spawned under Visual from EnemyData.
/// </summary>
public class Enemy : MonoBehaviour, IEffectTarget, ICombatFeedback
{
    readonly Combatant body = new Combatant();

    EnemyData data;
    bool nextIsAttack = true;
    CharacterVisual visual;
    CharacterFeedback feedback;
    bool hidingDeadVisual;

    public int Hp => body.Hp;
    public int MaxHp => body.MaxHp;
    public int Block => body.Block;
    public bool IsAlive => body.IsAlive;
    public StatusBag Statuses => body.Statuses;
    public EnemyIntent CurrentIntent { get; private set; }
    public string IntentLabel => Statuses.GetStacks(StatusId.Stun) > 0 ? "Stunned" : CurrentIntent.Label;
    public string DisplayName => data != null ? data.DisplayName : "Enemy";
    public CharacterVisual Visual => visual;

    void Awake()
    {
        visual = GetComponent<CharacterVisual>();
        feedback = GetComponent<CharacterFeedback>();
    }

    public void Initialize(EnemyData enemyData)
    {
        data = enemyData;
        int maxHp = data != null ? data.MaxHp : 40;
        body.Initialize(maxHp);
        nextIsAttack = true;
        ChooseNextIntent();

        if (visual == null)
            visual = GetComponent<CharacterVisual>();
        if (feedback == null)
            feedback = GetComponent<CharacterFeedback>();
        if (visual != null && data != null)
            visual.ApplyVisualPrefab(data.VisualPrefab);
    }

    public void ChooseNextIntent()
    {
        int attack = data != null ? data.AttackDamage : 8;
        int defend = data != null ? data.DefendBlock : 5;

        if (nextIsAttack)
            CurrentIntent = new EnemyIntent { Type = IntentType.Attack, Value = attack };
        else
            CurrentIntent = new EnemyIntent { Type = IntentType.Defend, Value = defend };

        nextIsAttack = !nextIsAttack;
    }

    public void ApplyStatus(StatusId id, int stacks)
    {
        body.ApplyStatus(id, stacks);
    }

    public void ExecuteIntent(IEffectTarget player)
    {
        if (Statuses.GetStacks(StatusId.Stun) > 0)
            return;

        if (CurrentIntent.Type == IntentType.Attack)
        {
            if (feedback != null)
                feedback.PlayAttackFeedback();
            if (player != null)
            {
                int damage = Statuses.ModifyOutgoingAttack(CurrentIntent.Value);
                Player playerActor = player as Player;
                if (playerActor != null)
                    damage = playerActor.Statuses.ModifyIncomingAttack(damage);
                player.TakeDamage(damage);
                ICombatFeedback playerFx = player as ICombatFeedback;
                if (playerFx != null)
                    playerFx.PlayHitFeedback();
            }
            return;
        }

        body.GainBlock(CurrentIntent.Value);
        if (feedback != null)
            feedback.PlayBlockFeedback();
    }

    public void ClearBlock()
    {
        body.ClearBlock();
    }

    public void TakeDamage(int amount) => body.TakeDamage(amount);
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
        if (IsAlive)
            return;

        if (feedback != null)
            feedback.PlayDeathFeedback();
        if (hidingDeadVisual)
            return;

        hidingDeadVisual = true;
        StartCoroutine(HideVisualAfterDeath());
    }

    IEnumerator HideVisualAfterDeath()
    {
        float delay = feedback != null ? feedback.DeathHideDelay : 0.4f;
        yield return new WaitForSeconds(delay);
        if (visual != null)
            visual.HideBody();
    }
}
