using UnityEngine;
using  System.Collections;
using TMPro;
public class TransitionSequence : MonoBehaviour
{
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialougeText;

    public void PlayAuto(LevelDataSO nextLevel, System.Action onComplete)
        => StartCoroutine(PlayRoutine(nextLevel, onComplete));

    private IEnumerator PlayRoutine(LevelDataSO nextLevel, System.Action onComplete)
    {
        dialoguePanel.SetActive(true);
        foreach (var line in nextLevel.monologueLines)
        {
            dialougeText.text = line;
            yield return new WaitForSeconds(nextLevel.secondPerLine);
        }
        dialoguePanel.SetActive(false);
        onComplete?.Invoke();
    }
}
