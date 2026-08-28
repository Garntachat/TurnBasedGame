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
    private bool isUniqueAction = false; // true = กำลังเลือกเป้าหมายให้สกิล unique อยู่ (ไม่ใช่ attack ปกติ)

    [Header("Unique Buttons")]
    public List<Button> uniqueButtons;

    [Header("Betrayal Settings")]
    [Range(0f, 1f)] public float betrayalChance = 0.3f;
    [Range(0f, 1f)] public float betrayalUseUniqueChance = 0.5f;
    private int traitorIndex = -1;

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
        for (int i = 0; i < uniqueButtons.Count; i++)
        {
            int index = i;
            uniqueButtons[i].onClick.AddListener(() => OnUniqueButtonClick(index));
        }

        List<int> eligibleTraitorIndices = new List<int>();
        for (int i = 1; i <= 3; i++)
        {
            if (i >= characters.Count) continue;
            eligibleTraitorIndices.Add(i);
        }
        if (eligibleTraitorIndices.Count > 0)
        {
            traitorIndex = eligibleTraitorIndices[Random.Range(0, eligibleTraitorIndices.Count)];
            Debug.Log("[DEBUG] Traitor this run: " + characters[traitorIndex].name);
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

        CharacterStat attackerStat = characters[attackerIndex].GetComponent<CharacterStat>();
        if (attackerStat == null || attackerStat.hp <= 0) return;
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

        SetTakeDamageButtonLabels("Attack");
    }

    private void SetTakeDamageButtonLabels(string label)
    {
        foreach (Button btn in takeDamageButtons)
        {
            Text btnText = btn.GetComponentInChildren<Text>();
            if (btnText != null) btnText.text = label;
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

    if (isUniqueAction)
    {
        CharacterStat uniqueTarget = characters[characterIndex].GetComponent<CharacterStat>();
        if (uniqueTarget == null)
        {
            Debug.LogWarning("Target has no CharacterStat component!");
            return;
        }

        ApplyUniqueEffect(currentAttacker, uniqueTarget);

        StartCoroutine(PauseThenResolve());
        return;
    }

    if (TryResolveBetrayal()) return;

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

public void OnUniqueButtonClick(int casterIndex)
{
    if (isProcessing) return;
    if (casterIndex < 0 || casterIndex >= characters.Count) return;

    CharacterStat caster = characters[casterIndex].GetComponent<CharacterStat>();
    if (caster == null || caster.hp <= 0) return;

    switch(caster.role)
    {
        case CharacterClass.Leader:
            // TODO: skill ของ Leader
            break;

        case CharacterClass.Mage:
        case CharacterClass.Healer:
            // เข้าโหมดเลือกเป้าหมายเดียว ผลลัพธ์จริงไปเกิดที่ OnTakeDamageButtonClick
            BeginUniqueTargetSelect(casterIndex, caster);
            break;

        case CharacterClass.Tank:
            ApplyUniqueEffect(caster, null);
            break;
    }
}

private void BeginUniqueTargetSelect(int casterIndex, CharacterStat caster)
{
    isUniqueAction = true;
    currentAttackerIndex = casterIndex;
    currentAttacker = caster;

    Debug.Log(characters[casterIndex].name + " is using a unique skill! Choose a target.");

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

    string label = (caster.role == CharacterClass.Healer) ? "Heal" : "Attack";
    SetTakeDamageButtonLabels(label);
}

private void ApplyUniqueEffect(CharacterStat caster, CharacterStat target)
    {
        switch (caster.role)
        {
            case CharacterClass.Healer:
                target.hp += caster.unique;
                if (target.hp > target.maxHp) target.hp = target.maxHp;
                Debug.Log(caster.name + " healed " + target.name + " for " + caster.unique + "! Current HP: " + target.hp);
                break;

            case CharacterClass.Mage:
                target.hp -= caster.unique;
                Debug.Log(target.name + " was hit by " + caster.name + "'s unique skill for " + caster.unique + " damage! Remaining HP: " + target.hp);
                if (target.hp <= 0)
                {
                    target.hp = 0;
                    Debug.Log(target.name + " has been defeated!");
                }
                break;

            case CharacterClass.Tank:
                caster.isGuarding = true; // target ไม่ได้ใช้ — Tank guard ตัวเองเสมอ
                Debug.Log(caster.name + " activated Guard for this round!");
                break;
        }
    }

private bool TryResolveBetrayal()
{
    if (currentAttackerIndex != traitorIndex) return false;
    if (Random.value >= betrayalChance) return false;

    bool useUnique = Random.value < betrayalUseUniqueChance;

    if (useUnique && currentAttacker.role == CharacterClass.Tank)
        {
            Debug.Log(currentAttacker.name + " ignored the order and guarded instead!");
            ApplyUniqueEffect(currentAttacker, null);
            StartCoroutine(PauseThenResolve());
            return true;
        }        
    
    List<HoverEffect> validTargets = new List<HoverEffect>();
    for (int i = 0; i < characters.Count; i++)
        {
            if (i == currentAttackerIndex) continue;
            CharacterStat stat = characters[i].GetComponent<CharacterStat>();
            if (stat != null && stat.hp > 0)
                validTargets.Add(characters[i]);
        }
    
    if (validTargets.Count == 0) return false;

    HoverEffect betrayTargetHover = validTargets[Random.Range(0, validTargets.Count)];
    CharacterStat betrayTarget = betrayTargetHover.GetComponent<CharacterStat>();

    if (useUnique){
        Debug.Log(currentAttacker.name + " ignored the order and used their unique skill on " + betrayTargetHover.name + " instead!");
        ApplyUniqueEffect(currentAttacker, betrayTarget);
    } else {
        Debug.Log(currentAttacker.name + " ignored the order and attacked " + betrayTargetHover.name + " instead!");
        betrayTarget.hp -= currentAttacker.atk;
        Debug.Log(betrayTargetHover.name + " took " + currentAttacker.atk + " damage! Remaining HP: " + betrayTarget.hp);

        if (betrayTarget.hp <= 0)
        {
            betrayTarget.hp = 0;
            Debug.Log(betrayTargetHover.name + " has been defeated!");
        }
    }

    StartCoroutine(PauseThenResolve());
    return true;
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
    isUniqueAction = false;
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
    EndTurn();
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

        foreach (HoverEffect guardHover in characters)
        {
            if (enemies.Contains(guardHover)) continue;
            CharacterStat guardStat = guardHover.GetComponent<CharacterStat>();
            if (guardStat != null && guardStat.isGuarding && guardStat.hp > 0)
                {
                    if (chosenTargetHover != guardHover)
                    {
                        Debug.Log(guardHover.name + " jumped in front of " + chosenTargetHover.name + " to take the hit!");
                        chosenTargetHover = guardHover;
                        chosenTarget = guardStat;
                        break;
                    }
                }        
        }

        chosenTarget.hp -= enemyStat.atk;
        Debug.Log(enemyHover.name + " attacked " + chosenTargetHover.name + " for " + enemyStat.atk + " damage! Remaining HP: " + chosenTarget.hp);

        if (chosenTarget.hp <= 0)
        {
            chosenTarget.hp = 0;
            Debug.Log(chosenTargetHover.name + " has been defeated!");
        }

        yield return new WaitForSeconds(1f);
    }

    foreach (HoverEffect character in characters)
        {
            CharacterStat stat = character.GetComponent<CharacterStat>();
            if (stat != null) stat.isGuarding = false;
        }

    isProcessing = false;
}
}