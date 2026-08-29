using UnityEngine;

[CreateAssetMenu(fileName = "LevelDataSO", menuName = "Scriptable Objects/LevelDataSO")]
public class LevelDataSO : ScriptableObject
{
    public string levelName;
    public EnemySpawnData[] enemies;
    [TextArea] public string[] monologueLines;
    public float secondPerLine = 2f;
    public bool isCampPhase ; // true for level 2->3 and 5->6

}

[System.Serializable]
public class EnemySpawnData
{
    public GameObject enemyPrefab;
    public int spawnPointIndex;
}

