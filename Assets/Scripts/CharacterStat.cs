using UnityEngine;
using UnityEngine.UI; // needed for legacy Text component

public class CharacterStat : MonoBehaviour
{
    public int maxHp = 100;
    public int hp = 100;
    public int atk = 10;
    public int unique = 0; // was "= null" — int can't be null, fixed to 0
    public CharacterClass role;
    public bool isGuarding = false; 

    [Header("UI References (Legacy Text)")]
    public Text hpText;
    public Text atkText;
    public Text uniqueText;

    void Start()
    {
        UpdateStatText();
    }

    void Update()
    {
        // If these stats can change during gameplay (e.g. taking damage),
        // call UpdateStatText() again whenever they change instead of every frame.
        // Calling it every frame works but is wasteful if nothing changed.
        UpdateStatText();
    }

void UpdateStatText()
{
    if (hpText != null)
        hpText.text = hp.ToString();

    if (atkText != null)
        atkText.text = atk.ToString();

    if (uniqueText != null)
        uniqueText.text = unique.ToString();
}
}