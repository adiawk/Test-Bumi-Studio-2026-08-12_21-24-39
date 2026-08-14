using System.Collections;
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
    [SerializeField] Transform formationCenter;
    [SerializeField] float enemySpacing = 2.1f;
    [SerializeField] int maxEnemies = 4;
    [SerializeField] DeckManager deckManager;
    [SerializeField] TurnManager turnManager;
    [SerializeField] EncounterData fallbackEncounter;
    [SerializeField] int playerMaxHp = 50;
    [SerializeField] int playerMaxEnergy = 3;
    [SerializeField] CardTargetIndicator targetIndicatorPrefab;
    [SerializeField] CardTargetPointer targetPointerPrefab;

    readonly List<Enemy> spawnedEnemies = new List<Enemy>();
    Enemy selectedEnemy;
    RuntimeCard pendingCard;
    CardTargetIndicator activeIndicator;
    CardTargetPointer activePointer;
    bool isDraggingCard;
    IEffectTarget dragHoverTarget;

    public bool IsCombatActive { get; private set; }
    public Player Player => player;
    public IReadOnlyList<Enemy> Enemies => spawnedEnemies;
    public Enemy SelectedEnemy => selectedEnemy;
    public RuntimeCard PendingCard => pendingCard;
    public DeckManager Deck => deckManager;
    public bool IsDraggingCard => isDraggingCard;

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
            CancelCardDrag();
            CancelPending();
            return;
        }

        // Drag-to-target owns input while a card is being dragged.
        if (isDraggingCard)
            return;

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

        // After turn start so StartBlock survives ClearBlock and ExtraEnergy survives ResetEnergy.
        if (run != null)
            run.ApplyPendingCombatModifiers(player, spawnedEnemies);
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

    public bool BeginCardDrag(RuntimeCard card)
    {
        if (!CanPlayCard(card))
            return false;

        pendingCard = card;
        isDraggingCard = true;
        dragHoverTarget = null;
        HideTargetIndicator();
        HideTargetPointer();
        RefreshSelectionVisuals();
        return true;
    }

    public void UpdateCardDragHover(Vector2 screenPosition)
    {
        if (!isDraggingCard || pendingCard == null || pendingCard.Data == null)
        {
            HideTargetIndicator();
            return;
        }

        IEffectTarget hit = FindTargetAtScreen(screenPosition);
        IEffectTarget valid = ResolveValidDragTarget(pendingCard, hit, 0f, float.PositiveInfinity);
        if (valid == dragHoverTarget)
            return;

        dragHoverTarget = valid;
        ShowTargetIndicator(valid as MonoBehaviour);
        if (valid is Enemy enemy)
            SelectEnemy(enemy);
        else
            RefreshSelectionVisuals();
    }

    public void SetTargetPointer(Transform canvasParent, Vector2 fromCanvasLocal, Vector2 toCanvasLocal, bool visible)
    {
        if (!visible)
        {
            HideTargetPointer();
            return;
        }

        EnsureTargetPointer(canvasParent);
        if (activePointer == null)
            return;

        activePointer.transform.SetParent(canvasParent, false);
        activePointer.transform.SetAsLastSibling();
        activePointer.Show(fromCanvasLocal, toCanvasLocal);
    }

    public void HideTargetPointer()
    {
        if (activePointer != null)
            activePointer.Hide();
    }

    public bool TryConfirmCardDrag(RuntimeCard card, IEffectTarget hoveredTarget, float liftAmount, float playLiftThreshold)
    {
        if (!isDraggingCard || card == null || card != pendingCard)
        {
            CancelCardDrag();
            return false;
        }

        IEffectTarget target = ResolveValidDragTarget(card, hoveredTarget, liftAmount, playLiftThreshold);
        isDraggingCard = false;
        dragHoverTarget = null;
        HideTargetIndicator();
        HideTargetPointer();

        if (target == null)
        {
            CancelPending();
            return false;
        }

        PlayPendingOn(target);
        return true;
    }

    public void CancelCardDrag()
    {
        if (!isDraggingCard && pendingCard == null)
        {
            HideTargetIndicator();
            HideTargetPointer();
            return;
        }

        isDraggingCard = false;
        dragHoverTarget = null;
        HideTargetIndicator();
        HideTargetPointer();
        CancelPending();
    }

    public IEffectTarget FindTargetAtScreen(Vector2 screenPosition)
    {
        Camera cam = Camera.main;
        if (cam == null)
            return null;

        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -cam.transform.position.z));
        Collider2D hit = Physics2D.OverlapPoint(world);
        if (hit == null)
            return null;

        Enemy enemy = hit.GetComponent<Enemy>();
        if (enemy == null)
            enemy = hit.GetComponentInParent<Enemy>();
        if (enemy != null && enemy.IsAlive)
            return enemy;

        Player hitPlayer = hit.GetComponent<Player>();
        if (hitPlayer == null)
            hitPlayer = hit.GetComponentInParent<Player>();
        return hitPlayer;
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
        StartCoroutine(EndTurnRoutine());
    }

    IEnumerator EndTurnRoutine()
    {
        yield return turnManager.ResolveEnemyTurn();
        if (FinishIfOver())
            yield break;

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
        {
            HideTargetIndicator();
            HideTargetPointer();
            return;
        }

        pendingCard = null;
        isDraggingCard = false;
        dragHoverTarget = null;
        HideTargetIndicator();
        HideTargetPointer();
        RefreshSelectionVisuals();
    }

    void PlayPendingOn(IEffectTarget target)
    {
        RuntimeCard card = pendingCard;
        pendingCard = null;
        isDraggingCard = false;
        dragHoverTarget = null;
        HideTargetIndicator();
        HideTargetPointer();
        PlayCard(card, target);
    }

    IEffectTarget ResolveValidDragTarget(RuntimeCard card, IEffectTarget hoveredTarget, float liftAmount, float playLiftThreshold)
    {
        if (card == null || card.Data == null)
            return null;

        CardTargetType targetType = card.Data.TargetType;
        if (targetType == CardTargetType.Enemy)
        {
            Enemy enemy = hoveredTarget as Enemy;
            if (enemy != null && enemy.IsAlive)
                return enemy;
            return null;
        }

        if (targetType == CardTargetType.Self)
        {
            if (hoveredTarget is Player)
                return player;
            if (liftAmount >= playLiftThreshold)
                return player;
            return null;
        }

        // None: play when dragged up far enough, or dropped on the player.
        if (hoveredTarget is Player || liftAmount >= playLiftThreshold)
            return player;

        return null;
    }

    void ShowTargetIndicator(MonoBehaviour targetBehaviour)
    {
        if (targetBehaviour == null)
        {
            HideTargetIndicator();
            return;
        }

        EnsureTargetIndicator();
        if (activeIndicator == null)
            return;

        activeIndicator.Show(targetBehaviour.transform);
    }

    void HideTargetIndicator()
    {
        if (activeIndicator != null)
            activeIndicator.Hide();
    }

    void EnsureTargetIndicator()
    {
        if (activeIndicator != null || targetIndicatorPrefab == null)
            return;

        activeIndicator = Instantiate(targetIndicatorPrefab);
        activeIndicator.name = "CardTargetIndicator";
        activeIndicator.Hide();
    }

    void EnsureTargetPointer(Transform canvasParent)
    {
        if (activePointer != null || targetPointerPrefab == null)
            return;

        Transform parent = canvasParent != null ? canvasParent : transform;
        activePointer = Instantiate(targetPointerPrefab, parent);
        activePointer.name = "CardTargetPointer";
        activePointer.Hide();
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
        int count = 0;
        for (int i = 0; i < encounter.Enemies.Count; i++)
        {
            if (encounter.Enemies[i] != null)
                count++;
        }

        count = Mathf.Min(count, Mathf.Max(0, maxEnemies));
        int spawned = 0;
        for (int i = 0; i < encounter.Enemies.Count && spawned < count; i++)
        {
            EnemyData data = encounter.Enemies[i];
            if (data == null)
                continue;

            float offsetX = (spawned - (count - 1) * 0.5f) * enemySpacing;
            Enemy instance = Instantiate(enemyPrefab, parent);
            instance.name = data.DisplayName;
            instance.transform.position = FormationWorldPosition(offsetX);
            instance.Initialize(data);
            spawnedEnemies.Add(instance);
            spawned++;
        }

        EnsureValidSelection();
    }

    Vector3 FormationWorldPosition(float offsetX)
    {
        if (formationCenter != null)
            return formationCenter.TransformPoint(new Vector3(offsetX, 0f, 0f));

        Transform parent = enemiesRoot != null ? enemiesRoot : transform;
        return parent.TransformPoint(new Vector3(offsetX, 0f, 0f));
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
        isDraggingCard = false;
        dragHoverTarget = null;
        HideTargetIndicator();
        HideTargetPointer();
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
        isDraggingCard = false;
        dragHoverTarget = null;
        HideTargetIndicator();
        HideTargetPointer();
    }
}

