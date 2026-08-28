using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class Game : MonoBehaviour
{
    [Header("Characters")]
    public List<HoverEffect> characters;

    [Header("Attack Buttons")]
    public List<Button> attackButtons;

    [Header("Take Damage Buttons")]
    public List<Button> takeDamageButtons;

    [Header("Enemies")]
    public List<HoverEffect> enemies; // enemies that will randomly attack on EndTurn

    [Header("Ally Auto-Attack Settings")]
    [Range(0f, 1f)]
    public float allyFriendlyFireChance = 0.2f; // 20% chance to hit an ally instead of an enemy

    [Header("Normal Attack Settings")]
    [Range(0f, 1f)]
    public float normalAttackMissTargetChance = 0.15f;
    private CharacterStat currentAttacker;
    private int currentAttackerIndex = -1;
    private List<bool> previousAvailability = new List<bool>();
    private bool isProcessing = false;

    void Start()
    {
        for (int i = 0; i < attackButtons.Count; i++)
        {
            int index = i;
            attackButtons[i].onClick.AddListener(() => OnAttackButtonClick(index));
        }

        for (int i = 0; i < takeDamageButtons.Count; i++)
        {
            int index = i;
            takeDamageButtons[i].onClick.AddListener(() => OnTakeDamageButtonClick(index));
        }
    }

    public void OnAttackButtonClick(int attackerIndex)
    {
        if (isProcessing) return;

        if (attackerIndex < 0 || attackerIndex >= characters.Count)
        {
            Debug.LogWarning("Invalid attacker index: " + attackerIndex);
            return;
        }

        currentAttackerIndex = attackerIndex;
        currentAttacker = characters[attackerIndex].GetComponent<CharacterStat>();

        Debug.Log(characters[attackerIndex].name + " attacked!");

        previousAvailability.Clear();
        foreach (HoverEffect character in characters)
        {
            previousAvailability.Add(character.available);
        }

        foreach (HoverEffect character in characters)
        {
            character.SetAvailable(false);
        }

        for (int i = 0; i < takeDamageButtons.Count; i++)
        {
            takeDamageButtons[i].gameObject.SetActive(i != currentAttackerIndex);
        }
    }

public void OnTakeDamageButtonClick(int characterIndex)
{
    if (isProcessing) return;

    if (characterIndex < 0 || characterIndex >= characters.Count)
    {
        Debug.LogWarning("Invalid character index: " + characterIndex);
        return;
    }

    if (currentAttacker == null)
    {
        Debug.LogWarning("No attacker selected yet!");
        return;
    }

    int finalTargetIndex = characterIndex;
    bool attackerIsPlayer = (currentAttackerIndex == 0);

    // Only non-player attackers have a chance to hit the wrong target
    if (!attackerIsPlayer && Random.value < normalAttackMissTargetChance)
    {
        List<int> alternateIndices = new List<int>();
        for (int i = 0; i < characters.Count; i++)
        {
            if (i == currentAttackerIndex) continue;
            if (i == characterIndex) continue;

            CharacterStat stat = characters[i].GetComponent<CharacterStat>();
            if (stat != null && stat.hp > 0)
                alternateIndices.Add(i);
        }

        if (alternateIndices.Count > 0)
        {
            finalTargetIndex = alternateIndices[Random.Range(0, alternateIndices.Count)];
            Debug.Log(currentAttacker.name + " missed and hit " + characters[finalTargetIndex].name + " instead!");
        }
    }

    CharacterStat target = characters[finalTargetIndex].GetComponent<CharacterStat>();

    if (target == null)
    {
        Debug.LogWarning("Target has no CharacterStat component!");
        return;
    }

    target.hp -= currentAttacker.atk;
    Debug.Log(target.name + " took " + currentAttacker.atk + " damage! Remaining HP: " + target.hp);

    if (target.hp <= 0)
    {
        target.hp = 0;
        Debug.Log(target.name + " has been defeated!");
    }

    StartCoroutine(PauseThenResolve());
}

private IEnumerator PauseThenResolve()
{
    isProcessing = true;

    foreach (Button btn in takeDamageButtons)
    {
        btn.gameObject.SetActive(false);
    }

    yield return new WaitForSeconds(1f);

    for (int i = 0; i < characters.Count; i++)
    {
        if (i < previousAvailability.Count)
            characters[i].SetAvailable(previousAvailability[i]);
    }

    bool attackerWasPlayer = (currentAttackerIndex == 0); // assumes Player is index 0

    currentAttacker = null;
    currentAttackerIndex = -1;
    isProcessing = false;

    if (attackerWasPlayer)
    {
        StartCoroutine(AlliesAutoAttack());
    }
}

private IEnumerator AlliesAutoAttack()
{
    isProcessing = true;
    Debug.Log("Mage, Healer, and Tank attack automatically!");

    // allyFriendlyFireChance is now a class field — no longer declared here

    for (int i = 1; i <= 3; i++)
    {
        if (i >= characters.Count) break;

        CharacterStat allyStat = characters[i].GetComponent<CharacterStat>();

        if (allyStat == null || allyStat.hp <= 0)
            continue;

        HoverEffect chosenTargetHover = null;
        CharacterStat chosenTarget = null;

        bool attackAlly = Random.value < allyFriendlyFireChance;

        if (attackAlly)
        {
            List<HoverEffect> validAllyTargets = new List<HoverEffect>();
            for (int j = 1; j <= 3; j++)
            {
                if (j >= characters.Count || j == i) continue;

                CharacterStat otherAllyStat = characters[j].GetComponent<CharacterStat>();
                if (otherAllyStat != null && otherAllyStat.hp > 0)
                    validAllyTargets.Add(characters[j]);
            }

            if (validAllyTargets.Count > 0)
            {
                chosenTargetHover = validAllyTargets[Random.Range(0, validAllyTargets.Count)];
                chosenTarget = chosenTargetHover.GetComponent<CharacterStat>();
                Debug.Log(characters[i].name + " got confused and attacked an ally!");
            }
        }

        if (chosenTarget == null)
        {
            List<HoverEffect> validEnemyTargets = new List<HoverEffect>();
            foreach (HoverEffect enemyHover in enemies)
            {
                CharacterStat enemyStat = enemyHover.GetComponent<CharacterStat>();
                if (enemyStat != null && enemyStat.hp > 0)
                    validEnemyTargets.Add(enemyHover);
            }

            if (validEnemyTargets.Count == 0)
            {
                Debug.Log("No enemies left to attack!");
                break;
            }

            chosenTargetHover = validEnemyTargets[Random.Range(0, validEnemyTargets.Count)];
            chosenTarget = chosenTargetHover.GetComponent<CharacterStat>();
        }

        chosenTarget.hp -= allyStat.atk;
        Debug.Log(characters[i].name + " attacked " + chosenTargetHover.name + " for " + allyStat.atk + " damage! Remaining HP: " + chosenTarget.hp);

        if (chosenTarget.hp <= 0)
        {
            chosenTarget.hp = 0;
            Debug.Log(chosenTargetHover.name + " has been defeated!");
        }

        yield return new WaitForSeconds(1f);
    }

    isProcessing = false;
}
    // ---------- END TURN ----------

    public void EndTurn()
    {
        if (isProcessing) return;
        StartCoroutine(EndTurnRoutine());
    }

private IEnumerator EndTurnRoutine()
{
    isProcessing = true;

    for (int i = 0; i < characters.Count; i++)
    {
        characters[i].SetAvailable(i == 0);
    }

    foreach (HoverEffect enemyHover in enemies)
    {
        CharacterStat enemyStat = enemyHover.GetComponent<CharacterStat>();

        if (enemyStat == null || enemyStat.hp <= 0)
            continue;

        // Build target list from party ONLY — exclude any enemies that might be in "characters"
        List<HoverEffect> validTargets = new List<HoverEffect>();
        foreach (HoverEffect character in characters)
        {
            if (enemies.Contains(character)) 
                continue; // skip anything that's actually an enemy

            CharacterStat charStat = character.GetComponent<CharacterStat>();
            if (charStat != null && charStat.hp > 0)
                validTargets.Add(character);
        }

        if (validTargets.Count == 0)
        {
            Debug.Log("No valid targets for " + enemyHover.name);
            continue;
        }

        HoverEffect chosenTargetHover = validTargets[Random.Range(0, validTargets.Count)];
        CharacterStat chosenTarget = chosenTargetHover.GetComponent<CharacterStat>();

        chosenTarget.hp -= enemyStat.atk;
        Debug.Log(enemyHover.name + " attacked " + chosenTargetHover.name + " for " + enemyStat.atk + " damage! Remaining HP: " + chosenTarget.hp);

        if (chosenTarget.hp <= 0)
        {
            chosenTarget.hp = 0;
            Debug.Log(chosenTargetHover.name + " has been defeated!");
        }

        yield return new WaitForSeconds(1f);
    }

    isProcessing = false;
}
}