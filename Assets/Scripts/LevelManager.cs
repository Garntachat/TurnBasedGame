using UnityEngine;
using  System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private LevelDataSO[] levels;
    [SerializeField] private Transform[] enemySpawnPoints;

    [SerializeField] private TransitionSequence transitionSequence;

    private int currentLevelIndex = 0;
    private List<GameObject> spawnEnemeis = new();

    public void OnLevelCompleted() // when the player cleared current level
    {
        ClearEnemies();
        currentLevelIndex++;

        if(currentLevelIndex > levels.Length) return; //player has played all 6 levels 

        transitionSequence.PlayAuto(levels[currentLevelIndex], () => LoadLevel(currentLevelIndex));
    }

    private void ClearEnemies()
    {
        foreach(var e in spawnEnemeis) Destroy(e);
        spawnEnemeis.Clear();
    }

    private void LoadLevel(int index)
    {
        foreach(var e in levels[index].enemies)
            spawnEnemeis.Add(Instantiate(e.enemyPrefab,enemySpawnPoints[e.spawnPointIndex]));
    }
}
