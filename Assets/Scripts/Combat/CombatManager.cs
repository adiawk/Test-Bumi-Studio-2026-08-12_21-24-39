using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Starts a combat from the current encounter, then the first player turn.
/// </summary>
public class CombatManager : MonoBehaviour
{
    [SerializeField] Player player;
    [SerializeField] Enemy enemyPrefab;
    [SerializeField] Transform enemiesRoot;
    [SerializeField] Vector3 firstEnemyPosition = new Vector3(2.2f, 0.4f, 0f);
    [SerializeField] float enemySpacing = 2.1f;
    [SerializeField] DeckManager deckManager;
    [SerializeField] TurnManager turnManager;
    [SerializeField] EncounterData fallbackEncounter;
    [SerializeField] int playerMaxHp = 50;
    [SerializeField] int playerMaxEnergy = 3;

    readonly List<Enemy> spawnedEnemies = new List<Enemy>();
    Enemy selectedEnemy;
    RuntimeCard pendingCard;

    public bool IsCombatActive { get; private set; }
    public Player Player => player;
    public IReadOnlyList<Enemy> Enemies => spawnedEnemies;
    public Enemy SelectedEnemy => selectedEnemy;
    public RuntimeCard PendingCard => pendingCard;
    public DeckManager Deck => deckManager;

    void Start()
    {
        StartCombat();
    }

    void Update()
    {
        if (!IsCombatActive)
            return;

        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        if (mouse.rightButton.wasPressedThisFrame)
        {
            CancelPending();
            return;
        }

        if (!mouse.leftButton.wasPressedThisFrame)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Vector2 screen = mouse.position.ReadValue();
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
        Collider2D hit = Physics2D.OverlapPoint(world);

        Enemy clickedEnemy = null;
        Player clickedPlayer = null;
        if (hit != null)
        {
            clickedEnemy = hit.GetComponent<Enemy>();
            if (clickedEnemy == null)
                clickedEnemy = hit.GetComponentInParent<Enemy>();
            clickedPlayer = hit.GetComponent<Player>();
            if (clickedPlayer == null)
                clickedPlayer = hit.GetComponentInParent<Player>();
        }

        if (pendingCard != null && pendingCard.Data != null && pendingCard.Data.TargetType == CardTargetType.Enemy)
        {
            if (clickedEnemy != null && clickedEnemy.IsAlive)
            {
                SelectEnemy(clickedEnemy);
                PlayPendingOn(clickedEnemy);
            }
            return;
        }

        if (pendingCard != null && pendingCard.Data != null && pendingCard.Data.TargetType == CardTargetType.Self)
        {
            if (clickedPlayer != null)
                PlayPendingOn(clickedPlayer);
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (clickedEnemy != null && clickedEnemy.IsAlive)
            SelectEnemy(clickedEnemy);
    }

    public void StartCombat()
    {
        IsCombatActive = true;
        pendingCard = null;
        RunManager run = GameManager.Instance != null ? GameManager.Instance.Run : null;

        int hp = run != null ? run.CurrentHp : playerMaxHp;
        if (player != null)
            player.Initialize(playerMaxHp, playerMaxEnergy, deckManager, hp);

        EncounterData encounter = run != null ? run.GetCurrentEncounter() : fallbackEncounter;
        SpawnEncounter(encounter);

        if (deckManager != null)
        {
            if (run != null)
                deckManager.SetupCombatDeck(run.RunDeck);
            else
                deckManager.SetupCombatDeck();
        }

        if (turnManager != null)
        {
            turnManager.Bind(player, spawnedEnemies);
            turnManager.BeginPlayerTurn();
        }
    }

    public bool CanPlayCard(RuntimeCard card)
    {
        if (!IsCombatActive || card == null || card.Data == null || player == null)
            return false;
        if (turnManager == null || turnManager.CurrentPhase != TurnPhase.PlayerTurn)
            return false;
        if (!player.IsAlive || !HasLivingEnemy())
            return false;
        if (card.Data.TargetType == CardTargetType.Enemy && FirstLivingEnemy() == null)
            return false;
        return player.Energy >= card.Cost;
    }

    public void BeginPlay(RuntimeCard card)
    {
        if (pendingCard == card)
        {
            CancelPending();
            return;
        }

        if (!CanPlayCard(card))
            return;

        CardTargetType targetType = card.Data.TargetType;
        if (targetType == CardTargetType.None || targetType == CardTargetType.Self)
        {
            PlayCard(card, player);
            return;
        }

        int living = CountLivingEnemies();
        if (living <= 1)
        {
            PlayCard(card, FirstLivingEnemy());
            return;
        }

        pendingCard = card;
        RefreshSelectionVisuals();
    }

    public void TryPlayCard(RuntimeCard card)
    {
        BeginPlay(card);
    }

    public void RequestEndTurn()
    {
        if (!IsCombatActive || turnManager == null)
            return;
        if (turnManager.CurrentPhase != TurnPhase.PlayerTurn)
            return;

        CancelPending();
        turnManager.ResolveEnemyTurn();
        if (FinishIfOver())
            return;

        turnManager.BeginPlayerTurn();
    }

    public void SelectEnemy(Enemy enemy)
    {
        if (enemy == null || !enemy.IsAlive)
            return;

        selectedEnemy = enemy;
        RefreshSelectionVisuals();
    }

    public void CancelPending()
    {
        if (pendingCard == null)
            return;

        pendingCard = null;
        RefreshSelectionVisuals();
    }

    void PlayPendingOn(IEffectTarget target)
    {
        RuntimeCard card = pendingCard;
        pendingCard = null;
        PlayCard(card, target);
    }

    void PlayCard(RuntimeCard card, IEffectTarget target)
    {
        pendingCard = null;
        if (!CanPlayCard(card) || deckManager == null || target == null)
        {
            RefreshSelectionVisuals();
            return;
        }
        if (!player.TrySpendEnergy(card.Cost))
        {
            RefreshSelectionVisuals();
            return;
        }

        card.Resolve(new EffectContext(player, target, deckManager));
        deckManager.Discard(card);
        EnsureValidSelection();
        FinishIfOver();
    }

    void SpawnEncounter(EncounterData encounter)
    {
        ClearSpawnedEnemies();

        if (enemyPrefab == null || encounter == null || encounter.Enemies == null)
        {
            EnsureValidSelection();
            return;
        }

        Transform parent = enemiesRoot != null ? enemiesRoot : transform;
        int spawned = 0;
        for (int i = 0; i < encounter.Enemies.Count; i++)
        {
            EnemyData data = encounter.Enemies[i];
            if (data == null)
                continue;

            Enemy instance = Instantiate(enemyPrefab, parent);
            instance.name = data.DisplayName;
            instance.transform.localPosition = firstEnemyPosition + new Vector3(spawned * enemySpacing, 0f, 0f);
            instance.Initialize(data);
            spawnedEnemies.Add(instance);
            spawned++;
        }

        EnsureValidSelection();
    }

    void ClearSpawnedEnemies()
    {
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            if (spawnedEnemies[i] != null)
                Destroy(spawnedEnemies[i].gameObject);
        }

        spawnedEnemies.Clear();
        selectedEnemy = null;
        pendingCard = null;
    }

    Enemy FirstLivingEnemy()
    {
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            if (spawnedEnemies[i] != null && spawnedEnemies[i].IsAlive)
                return spawnedEnemies[i];
        }

        return null;
    }

    int CountLivingEnemies()
    {
        int count = 0;
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            if (spawnedEnemies[i] != null && spawnedEnemies[i].IsAlive)
                count++;
        }

        return count;
    }

    bool HasLivingEnemy()
    {
        return FirstLivingEnemy() != null;
    }

    void EnsureValidSelection()
    {
        if (selectedEnemy == null || !selectedEnemy.IsAlive)
            selectedEnemy = FirstLivingEnemy();
        RefreshSelectionVisuals();
    }

    void RefreshSelectionVisuals()
    {
        bool pickingEnemy = pendingCard != null && pendingCard.Data != null && pendingCard.Data.TargetType == CardTargetType.Enemy;
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            Enemy enemy = spawnedEnemies[i];
            if (enemy == null || enemy.Visual == null)
                continue;

            bool highlight = enemy.IsAlive && (pickingEnemy || enemy == selectedEnemy);
            enemy.Visual.SetSelected(highlight);
        }
    }

    bool FinishIfOver()
    {
        pendingCard = null;
        if (!HasLivingEnemy())
        {
            PersistHp();
            EndCombat();
            if (GameManager.Instance != null)
                GameManager.Instance.NotifyCombatWon();
            return true;
        }

        if (player != null && !player.IsAlive)
        {
            PersistHp();
            EndCombat();
            if (GameManager.Instance != null)
                GameManager.Instance.NotifyCombatLost();
            return true;
        }

        return false;
    }

    void PersistHp()
    {
        if (GameManager.Instance != null && GameManager.Instance.Run != null && player != null)
            GameManager.Instance.Run.SetHp(player.Hp);
    }

    public void EndCombat()
    {
        IsCombatActive = false;
        pendingCard = null;
    }
}

