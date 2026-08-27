using UnityEngine;

public class Character : MonoBehaviour
{
    public string characterName;
    public int maxHp;
    public int hp;
    public CharacterClass role;
    public int atk;
    public int def;

    public bool IsAlive => hp > 0;

    private void Awake() {
        hp = maxHp;
    }
    
    public void TakeDamage(int damage) {
        int lastDamage = damage - def;
        if (lastDamage > 0) {
            if (hp - lastDamage < 0) {
                hp = 0;
            } else {
                hp -= lastDamage;
            }
        } 
    }

    public void Heal(int healing) {
        hp += healing;
        if (hp > maxHp) hp = maxHp;
    }
}
