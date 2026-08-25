public interface ICombatant
{
    int CurrentHp {get; }
    void TakeDamage(int amount);
    void Heal(int amount);


}
