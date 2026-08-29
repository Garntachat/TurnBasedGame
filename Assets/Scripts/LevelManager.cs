using UnityEngine;
using  System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private LevelDataSO[] levels;
    [SerializeField] private Transform[] enemySpawnPoints;

    [SerializeField] private TransitionSequence transitionSequence;
    [SerializeField] private Game game;

    private int currentLevelIndex = 0;
    private List<GameObject> spawnEnemeis = new();

    private void Start() => LoadLevel(0);

    public void OnLevelCompleted() // when the player cleared current level
    {
        ClearEnemies();
        currentLevelIndex++;

        if (currentLevelIndex >= levels.Length) return; //player has played all 6 levels

        transitionSequence.PlayAuto(levels[currentLevelIndex], () => LoadLevel(currentLevelIndex));
    }

    private void ClearEnemies()
    {
        foreach(var e in spawnEnemeis) Destroy(e);
        spawnEnemeis.Clear();
    }

    private void LoadLevel(int index)
    {
        foreach (var e in levels[index].enemies)
        {
            if (e.enemyPrefab != null)
                spawnEnemeis.Add(Instantiate(e.enemyPrefab, enemySpawnPoints[e.spawnPointIndex]));

            if (game != null)
                game.ApplyEnemyStats(e.maxHp, e.atk, e.unique);
        }

        if (game != null)
            game.ResetForNewLevel();
    }
}