using UnityEngine;

/// <summary>
/// Combat enemy with a telegraphing intent. No AI beyond a two-step pattern.
/// Lives on the character root; sprites live under Visual.
/// </summary>
public class Enemy : MonoBehaviour, IEffectTarget
{
    readonly Combatant body = new Combatant();

    EnemyData data;
    bool nextIsAttack = true;
    CharacterVisual visual;

    public int Hp => body.Hp;
    public int MaxHp => body.MaxHp;
    public int Block => body.Block;
    public bool IsAlive => body.IsAlive;
    public EnemyIntent CurrentIntent { get; private set; }
    public string IntentLabel => CurrentIntent.Label;
    public string DisplayName => data != null ? data.DisplayName : "Enemy";
    public CharacterVisual Visual => visual;

    void Awake()
    {
        visual = GetComponent<CharacterVisual>();
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
        if (visual != null && data != null)
            visual.ApplyLayers(data.VisualLayers);
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

    public void ExecuteIntent(IEffectTarget player)
    {
        if (CurrentIntent.Type == IntentType.Attack)
        {
            if (player != null)
                player.TakeDamage(CurrentIntent.Value);
            return;
        }

        body.GainBlock(CurrentIntent.Value);
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

