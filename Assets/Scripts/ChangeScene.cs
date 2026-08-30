using UnityEngine;
// 1. You must include this namespace to manage scenes
using UnityEngine.SceneManagement; 

public class SceneChanger : MonoBehaviour
{
    // Load a scene by its exact name
    public void ChangeSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // Load a scene by its build index number
    public void ChangeSceneByIndex(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }
}
