using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefab")]
    public GameObject enemyPrefab;

    [Header("Spawn Point")]
    public Transform spawner1; // drag Spawner1 here

    void Start()
    {
        SpawnEnemy();
    }

    public void SpawnEnemy()
    {
        if (enemyPrefab == null || spawner1 == null)
        {
            Debug.LogWarning("EnemySpawner: missing enemyPrefab or spawner1!");
            return;
        }

        Instantiate(enemyPrefab, spawner1.position, spawner1.rotation);
        Debug.Log("Enemy spawned at Spawner1");
    }
}