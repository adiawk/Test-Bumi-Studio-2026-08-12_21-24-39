using System.Collections.Generic;
using UnityEngine;

public enum TurnPhase
{
    PlayerTurn,
    EnemyTurn
}

/// <summary>
/// Player and enemy turns. Resets energy, draws, then runs each living enemy intent.
/// </summary>
public class TurnManager : MonoBehaviour
{
    [SerializeField] Player player;
    [SerializeField] DeckManager deckManager;
    [SerializeField] int cardsPerTurn = 5;

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
        }

        if (deckManager != null)
            deckManager.DrawCards(cardsPerTurn);
    }

    public void ResolveEnemyTurn()
    {
        if (CurrentPhase != TurnPhase.PlayerTurn)
            return;

        CurrentPhase = TurnPhase.EnemyTurn;

        if (deckManager != null)
            deckManager.DiscardHand();

        if (enemies == null)
            return;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive)
                continue;

            enemy.ClearBlock();
            enemy.ExecuteIntent(player);
            enemy.ChooseNextIntent();

            if (player != null && !player.IsAlive)
                break;
        }
    }
}

