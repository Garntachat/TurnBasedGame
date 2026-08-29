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

    [Header("HP Bar")]
    public Image hpBarFill;

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
    if (hpBarFill != null && maxHp > 0){
        RectTransform rt = hpBarFill.rectTransform;
        Vector3 scale = rt.localScale;
        scale.x = Mathf.Clamp01((float)hp / maxHp);
        rt.localScale = scale;
    }
}
}