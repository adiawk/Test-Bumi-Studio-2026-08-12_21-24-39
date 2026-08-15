using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TurnPhase
{
    PlayerTurn,
    EnemyTurn
}

/// <summary>
/// Player and enemy turns. Resets energy, draws, then runs each living enemy intent in order.
/// </summary>
public class TurnManager : MonoBehaviour
{
    [SerializeField] Player player;
    [SerializeField] DeckManager deckManager;
    [SerializeField] int cardsPerTurn = 5;
    [SerializeField] float delayBetweenEnemyActions = 0.75f;

    IReadOnlyList<Enemy> enemies;

    public TurnPhase CurrentPhase { get; private set; } = TurnPhase.PlayerTurn;

    public void Bind(Player combatPlayer, IReadOnlyList<Enemy> combatEnemies)
    {
        player = combatPlayer;
        enemies = combatEnemies;
    }

    public void BeginPlayerTurn()
    {
        CurrentPhase = TurnPhase.PlayerTurn;

        if (player != null)
        {
            player.ClearBlock();
            player.ResetEnergy();
            player.Statuses.ActivateDelayedStatuses();
        }

        ActivateEnemyDelayedStatuses();

        if (deckManager != null)
            deckManager.DrawCards(cardsPerTurn);
    }

    public IEnumerator ResolveEnemyTurn()
    {
        if (CurrentPhase != TurnPhase.PlayerTurn)
            yield break;

        CurrentPhase = TurnPhase.EnemyTurn;

        if (player != null)
            player.Statuses.ExpireTimedStatuses();

        if (deckManager != null)
            deckManager.DiscardHand();

        if (enemies == null)
            yield break;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive)
                continue;

            enemy.ClearBlock();
            enemy.ExecuteIntent(player);
            enemy.ChooseNextIntent();
            enemy.Statuses.ExpireTimedStatuses();

            yield return new WaitForSeconds(delayBetweenEnemyActions);

            if (player != null && !player.IsAlive)
                break;
        }
    }

    void ActivateEnemyDelayedStatuses()
    {
        if (enemies == null)
            return;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy != null && enemy.IsAlive)
                enemy.Statuses.ActivateDelayedStatuses();
        }
    }
}
