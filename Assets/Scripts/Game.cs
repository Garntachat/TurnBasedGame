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
    public List<HoverEffect> enemies;

    [Header("Ally Auto-Attack Settings")]
    [Range(0f, 1f)]
    public float allyFriendlyFireChance = 0.2f;

    [Header("Normal Attack Settings")]
    [Range(0f, 1f)]
    public float normalAttackMissTargetChance = 0.15f;

    private CharacterStat currentAttacker;
    private int currentAttackerIndex = -1;
    private List<bool> previousAvailability = new List<bool>();
    private bool isProcessing = false;
    private bool isUniqueAction = false;

    [Header("Unique Buttons")]
    public List<Button> uniqueButtons;

    [Header("Betrayal Settings")]
    [Range(0f, 1f)]
    public float betrayalChance = 0.3f;

    [Range(0f, 1f)]
    public float betrayalUseUniqueChance = 0.5f;

    private int traitorIndex = -1;

    // ============================================================
    // PARTICLES
    // ============================================================

    [Header("Attack Particles")]
    public GameObject hitParticlePrefab;
    public GameObject allyHitParticlePrefab;
    public GameObject attackerHitParticlePrefab;

    [Header("Unique Skill Particle")]
    public GameObject uniqueActiveParticlePrefab;

    // ============================================================
    // START
    // ============================================================

    void Start()
    {
        for (int i = 0; i < attackButtons.Count; i++)
        {
            int index = i;
            attackButtons[i].onClick.AddListener(
                () => OnAttackButtonClick(index)
            );
        }

        for (int i = 0; i < takeDamageButtons.Count; i++)
        {
            int index = i;
            takeDamageButtons[i].onClick.AddListener(
                () => OnTakeDamageButtonClick(index)
            );
        }

        for (int i = 0; i < uniqueButtons.Count; i++)
        {
            int index = i;
            uniqueButtons[i].onClick.AddListener(
                () => OnUniqueButtonClick(index)
            );
        }

        // Pick random traitor from allies
        List<int> eligibleTraitorIndices = new List<int>();

        for (int i = 1; i <= 3; i++)
        {
            if (i >= characters.Count)
                continue;

            eligibleTraitorIndices.Add(i);
        }

        if (eligibleTraitorIndices.Count > 0)
        {
            traitorIndex =
                eligibleTraitorIndices[
                    Random.Range(
                        0,
                        eligibleTraitorIndices.Count
                    )
                ];

            Debug.Log(
                "[DEBUG] Traitor this run: " +
                characters[traitorIndex].name
            );
        }
    }

    // ============================================================
    // ATTACK BUTTON
    // ============================================================

    public void OnAttackButtonClick(int attackerIndex)
    {
        if (isProcessing)
            return;

        if (attackerIndex < 0 ||
            attackerIndex >= characters.Count)
        {
            Debug.LogWarning(
                "Invalid attacker index: " +
                attackerIndex
            );

            return;
        }

        CharacterStat attackerStat =
            characters[attackerIndex]
                .GetComponent<CharacterStat>();

        if (attackerStat == null ||
            attackerStat.hp <= 0)
            return;

        currentAttackerIndex =
            attackerIndex;

        currentAttacker =
            attackerStat;

        Debug.Log(
            characters[attackerIndex].name +
            " attacked!"
        );

        previousAvailability.Clear();

        foreach (HoverEffect character in characters)
        {
            previousAvailability.Add(
                character.available
            );
        }

        foreach (HoverEffect character in characters)
        {
            character.SetAvailable(false);
        }

        for (int i = 0;
             i < takeDamageButtons.Count;
             i++)
        {
            takeDamageButtons[i]
                .gameObject
                .SetActive(
                    i != currentAttackerIndex
                );
        }

        SetTakeDamageButtonLabels("Attack");
    }

    // ============================================================
    // BUTTON LABEL
    // ============================================================

    private void SetTakeDamageButtonLabels(string label)
    {
        foreach (Button btn in takeDamageButtons)
        {
            Text btnText =
                btn.GetComponentInChildren<Text>();

            if (btnText != null)
                btnText.text = label;
        }
    }

    // ============================================================
    // TAKE DAMAGE / NORMAL ATTACK
    // ============================================================

    public void OnTakeDamageButtonClick(int characterIndex)
    {
        if (isProcessing)
            return;

        if (characterIndex < 0 ||
            characterIndex >= characters.Count)
        {
            Debug.LogWarning(
                "Invalid character index: " +
                characterIndex
            );

            return;
        }

        if (currentAttacker == null)
        {
            Debug.LogWarning(
                "No attacker selected yet!"
            );

            return;
        }

        // ========================================================
        // UNIQUE SKILL
        // ========================================================

        if (isUniqueAction)
        {
            CharacterStat uniqueTarget =
                characters[characterIndex]
                    .GetComponent<CharacterStat>();

            if (uniqueTarget == null)
            {
                Debug.LogWarning(
                    "Target has no CharacterStat component!"
                );

                return;
            }

            ApplyUniqueEffect(
                currentAttacker,
                uniqueTarget
            );

            StartCoroutine(
                PauseThenResolve()
            );

            return;
        }

        // ========================================================
        // BETRAYAL
        // ========================================================

        if (TryResolveBetrayal())
            return;

        // ========================================================
        // NORMAL ATTACK
        // ========================================================

        int finalTargetIndex =
            characterIndex;

        bool attackerIsPlayer =
            (currentAttackerIndex == 0);

        // Only non-player attackers can miss
        if (!attackerIsPlayer &&
            Random.value < normalAttackMissTargetChance)
        {
            List<int> alternateIndices =
                new List<int>();

            for (int i = 0;
                 i < characters.Count;
                 i++)
            {
                if (i == currentAttackerIndex)
                    continue;

                if (i == characterIndex)
                    continue;

                CharacterStat stat =
                    characters[i]
                        .GetComponent<CharacterStat>();

                if (stat != null &&
                    stat.hp > 0)
                {
                    alternateIndices.Add(i);
                }
            }

            if (alternateIndices.Count > 0)
            {
                finalTargetIndex =
                    alternateIndices[
                        Random.Range(
                            0,
                            alternateIndices.Count
                        )
                    ];

                Debug.Log(
                    currentAttacker.name +
                    " missed and hit " +
                    characters[finalTargetIndex].name +
                    " instead!"
                );
            }
        }

        CharacterStat target =
            characters[finalTargetIndex]
                .GetComponent<CharacterStat>();

        if (target == null)
        {
            Debug.LogWarning(
                "Target has no CharacterStat component!"
            );

            return;
        }

        // ========================================================
        // ATTACKER PARTICLE
        // ========================================================

        SpawnParticle(
            attackerHitParticlePrefab,
            currentAttacker.gameObject
        );

        // ========================================================
        // DAMAGE
        // ========================================================

        target.hp -= currentAttacker.atk;

        // ========================================================
        // TARGET PARTICLE
        // ========================================================

        if (enemies.Contains(
                characters[finalTargetIndex]))
        {
            // Enemy was damaged
            SpawnParticle(
                hitParticlePrefab,
                target.gameObject
            );
        }
        else
        {
            // Ally was damaged
            SpawnParticle(
                allyHitParticlePrefab,
                target.gameObject
            );
        }

        Debug.Log(
            target.name +
            " took " +
            currentAttacker.atk +
            " damage! Remaining HP: " +
            target.hp
        );

        if (target.hp <= 0)
        {
            target.hp = 0;

            Debug.Log(
                target.name +
                " has been defeated!"
            );
        }

        StartCoroutine(
            PauseThenResolve()
        );
    }

    // ============================================================
    // UNIQUE BUTTON
    // ============================================================

    public void OnUniqueButtonClick(int casterIndex)
    {
        if (isProcessing)
            return;

        if (casterIndex < 0 ||
            casterIndex >= characters.Count)
            return;

        CharacterStat caster =
            characters[casterIndex]
                .GetComponent<CharacterStat>();

        if (caster == null ||
            caster.hp <= 0)
            return;

        switch (caster.role)
        {
            case CharacterClass.Leader:

                // TODO: Leader unique skill
                break;

            case CharacterClass.Mage:
            case CharacterClass.Healer:

                // Unique activation particle
                SpawnParticle(
                    uniqueActiveParticlePrefab,
                    caster.gameObject
                );

                // Enter target selection
                BeginUniqueTargetSelect(
                    casterIndex,
                    caster
                );

                break;

            case CharacterClass.Tank:

                // Unique activation particle
                SpawnParticle(
                    uniqueActiveParticlePrefab,
                    caster.gameObject
                );

                ApplyUniqueEffect(
                    caster,
                    null
                );

                break;
        }
    }

    // ============================================================
    // BEGIN UNIQUE TARGET SELECTION
    // ============================================================

    private void BeginUniqueTargetSelect(
        int casterIndex,
        CharacterStat caster)
    {
        isUniqueAction = true;

        currentAttackerIndex =
            casterIndex;

        currentAttacker =
            caster;

        Debug.Log(
            characters[casterIndex].name +
            " is using a unique skill! Choose a target."
        );

        previousAvailability.Clear();

        foreach (HoverEffect character in characters)
        {
            previousAvailability.Add(
                character.available
            );
        }

        foreach (HoverEffect character in characters)
        {
            character.SetAvailable(false);
        }

        for (int i = 0;
             i < takeDamageButtons.Count;
             i++)
        {
            takeDamageButtons[i]
                .gameObject
                .SetActive(
                    i != currentAttackerIndex
                );
        }

        string label =
            (caster.role == CharacterClass.Healer)
                ? "Heal"
                : "Attack";

        SetTakeDamageButtonLabels(label);
    }

    // ============================================================
    // UNIQUE EFFECT
    // ============================================================

    private void ApplyUniqueEffect(
        CharacterStat caster,
        CharacterStat target)
    {
        switch (caster.role)
        {
            case CharacterClass.Healer:

                if (target == null)
                    return;

                target.hp += caster.unique;

                if (target.hp > target.maxHp)
                    target.hp = target.maxHp;

                Debug.Log(
                    caster.name +
                    " healed " +
                    target.name +
                    " for " +
                    caster.unique +
                    "! Current HP: " +
                    target.hp
                );

                break;

            case CharacterClass.Mage:

                if (target == null)
                    return;

                target.hp -= caster.unique;

                Debug.Log(
                    target.name +
                    " was hit by " +
                    caster.name +
                    "'s unique skill for " +
                    caster.unique +
                    " damage! Remaining HP: " +
                    target.hp
                );

                // Enemy/ally particle for Unique damage
                if (enemies.Contains(
                        target.GetComponent<HoverEffect>()))
                {
                    SpawnParticle(
                        hitParticlePrefab,
                        target.gameObject
                    );
                }
                else
                {
                    SpawnParticle(
                        allyHitParticlePrefab,
                        target.gameObject
                    );
                }

                if (target.hp <= 0)
                {
                    target.hp = 0;

                    Debug.Log(
                        target.name +
                        " has been defeated!"
                    );
                }

                break;

            case CharacterClass.Tank:

                caster.isGuarding = true;

                Debug.Log(
                    caster.name +
                    " activated Guard for this round!"
                );

                break;
        }
    }

    // ============================================================
    // BETRAYAL
    // ============================================================

    private bool TryResolveBetrayal()
    {
        if (currentAttackerIndex != traitorIndex)
            return false;

        if (Random.value >= betrayalChance)
            return false;

        bool useUnique =
            Random.value <
            betrayalUseUniqueChance;

        // ========================================================
        // TRAITOR TANK UNIQUE
        // ========================================================

        if (useUnique &&
            currentAttacker.role == CharacterClass.Tank)
        {
            Debug.Log(
                currentAttacker.name +
                " ignored the order and guarded instead!"
            );

            // Unique particle
            SpawnParticle(
                uniqueActiveParticlePrefab,
                currentAttacker.gameObject
            );

            ApplyUniqueEffect(
                currentAttacker,
                null
            );

            StartCoroutine(
                PauseThenResolve()
            );

            return true;
        }

        // ========================================================
        // FIND BETRAYAL TARGET
        // ========================================================

        List<HoverEffect> validTargets =
            new List<HoverEffect>();

        for (int i = 0;
             i < characters.Count;
             i++)
        {
            if (i == currentAttackerIndex)
                continue;

            CharacterStat stat =
                characters[i]
                    .GetComponent<CharacterStat>();

            if (stat != null &&
                stat.hp > 0)
            {
                validTargets.Add(
                    characters[i]
                );
            }
        }

        if (validTargets.Count == 0)
            return false;

        HoverEffect betrayTargetHover =
            validTargets[
                Random.Range(
                    0,
                    validTargets.Count
                )
            ];

        CharacterStat betrayTarget =
            betrayTargetHover
                .GetComponent<CharacterStat>();

        // ========================================================
        // BETRAYAL UNIQUE
        // ========================================================

        if (useUnique)
        {
            Debug.Log(
                currentAttacker.name +
                " ignored the order and used their unique skill on " +
                betrayTargetHover.name +
                " instead!"
            );

            // Unique activation particle
            SpawnParticle(
                uniqueActiveParticlePrefab,
                currentAttacker.gameObject
            );

            ApplyUniqueEffect(
                currentAttacker,
                betrayTarget
            );
        }
        else
        {
            // ====================================================
            // BETRAYAL NORMAL ATTACK
            // ====================================================

            Debug.Log(
                currentAttacker.name +
                " ignored the order and attacked " +
                betrayTargetHover.name +
                " instead!"
            );

            // Attacker particle
            SpawnParticle(
                attackerHitParticlePrefab,
                currentAttacker.gameObject
            );

            betrayTarget.hp -=
                currentAttacker.atk;

            // Target particle
            if (enemies.Contains(
                    betrayTargetHover))
            {
                SpawnParticle(
                    hitParticlePrefab,
                    betrayTarget.gameObject
                );
            }
            else
            {
                SpawnParticle(
                    allyHitParticlePrefab,
                    betrayTarget.gameObject
                );
            }

            Debug.Log(
                betrayTargetHover.name +
                " took " +
                currentAttacker.atk +
                " damage! Remaining HP: " +
                betrayTarget.hp
            );

            if (betrayTarget.hp <= 0)
            {
                betrayTarget.hp = 0;

                Debug.Log(
                    betrayTargetHover.name +
                    " has been defeated!"
                );
            }
        }

        StartCoroutine(
            PauseThenResolve()
        );

        return true;
    }

    // ============================================================
    // PARTICLE HELPER
    // ============================================================

    private void SpawnParticle(
        GameObject prefab,
        GameObject target)
    {
        if (prefab == null)
        {
            Debug.LogWarning(
                "Particle prefab is not assigned!"
            );

            return;
        }

        if (target == null)
        {
            Debug.LogWarning(
                "Particle target is null!"
            );

            return;
        }

        Instantiate(
            prefab,
            target.transform.position,
            Quaternion.identity
        );
    }

    // ============================================================
    // PAUSE THEN RESOLVE
    // ============================================================

    private IEnumerator PauseThenResolve()
    {
        isProcessing = true;

        foreach (Button btn in takeDamageButtons)
        {
            btn.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(1f);

        for (int i = 0;
             i < characters.Count;
             i++)
        {
            if (i < previousAvailability.Count)
            {
                characters[i].SetAvailable(
                    previousAvailability[i]
                );
            }
        }

        bool attackerWasPlayer =
            (currentAttackerIndex == 0);

        currentAttacker = null;
        currentAttackerIndex = -1;
        isUniqueAction = false;
        isProcessing = false;

        if (attackerWasPlayer)
        {
            StartCoroutine(
                AlliesAutoAttack()
            );
        }
    }

    // ============================================================
    // ALLIES AUTO ATTACK
    // ============================================================

    private IEnumerator AlliesAutoAttack()
    {
        isProcessing = true;

        Debug.Log(
            "Mage, Healer, and Tank attack automatically!"
        );

        for (int i = 1; i <= 3; i++)
        {
            if (i >= characters.Count)
                break;

            CharacterStat allyStat =
                characters[i]
                    .GetComponent<CharacterStat>();

            if (allyStat == null ||
                allyStat.hp <= 0)
                continue;

            HoverEffect chosenTargetHover = null;
            CharacterStat chosenTarget = null;

            bool attackAlly =
                Random.value <
                allyFriendlyFireChance;

            // ====================================================
            // FRIENDLY FIRE
            // ====================================================

            if (attackAlly)
            {
                List<HoverEffect> validAllyTargets =
                    new List<HoverEffect>();

                for (int j = 1; j <= 3; j++)
                {
                    if (j >= characters.Count ||
                        j == i)
                        continue;

                    CharacterStat otherAllyStat =
                        characters[j]
                            .GetComponent<CharacterStat>();

                    if (otherAllyStat != null &&
                        otherAllyStat.hp > 0)
                    {
                        validAllyTargets.Add(
                            characters[j]
                        );
                    }
                }

                if (validAllyTargets.Count > 0)
                {
                    chosenTargetHover =
                        validAllyTargets[
                            Random.Range(
                                0,
                                validAllyTargets.Count
                            )
                        ];

                    chosenTarget =
                        chosenTargetHover
                            .GetComponent<CharacterStat>();

                    Debug.Log(
                        characters[i].name +
                        " got confused and attacked an ally!"
                    );
                }
            }

            // ====================================================
            // ENEMY TARGET
            // ====================================================

            if (chosenTarget == null)
            {
                List<HoverEffect> validEnemyTargets =
                    new List<HoverEffect>();

                foreach (
                    HoverEffect enemyHover
                    in enemies)
                {
                    CharacterStat enemyStat =
                        enemyHover
                            .GetComponent<CharacterStat>();

                    if (enemyStat != null &&
                        enemyStat.hp > 0)
                    {
                        validEnemyTargets.Add(
                            enemyHover
                        );
                    }
                }

                if (validEnemyTargets.Count == 0)
                {
                    Debug.Log(
                        "No enemies left to attack!"
                    );

                    break;
                }

                chosenTargetHover =
                    validEnemyTargets[
                        Random.Range(
                            0,
                            validEnemyTargets.Count
                        )
                    ];

                chosenTarget =
                    chosenTargetHover
                        .GetComponent<CharacterStat>();
            }

            // ====================================================
            // ATTACKER PARTICLE
            // ====================================================

            SpawnParticle(
                attackerHitParticlePrefab,
                allyStat.gameObject
            );

            // ====================================================
            // DAMAGE
            // ====================================================

            chosenTarget.hp -= allyStat.atk;

            // ====================================================
            // TARGET PARTICLE
            // ====================================================

            if (characters.Contains(
                    chosenTargetHover))
            {
                // Ally damaged
                SpawnParticle(
                    allyHitParticlePrefab,
                    chosenTarget.gameObject
                );
            }
            else
            {
                // Enemy damaged
                SpawnParticle(
                    hitParticlePrefab,
                    chosenTarget.gameObject
                );
            }

            Debug.Log(
                characters[i].name +
                " attacked " +
                chosenTargetHover.name +
                " for " +
                allyStat.atk +
                " damage! Remaining HP: " +
                chosenTarget.hp
            );

            if (chosenTarget.hp <= 0)
            {
                chosenTarget.hp = 0;

                Debug.Log(
                    chosenTargetHover.name +
                    " has been defeated!"
                );
            }

            yield return new WaitForSeconds(1f);
        }

        isProcessing = false;

        EndTurn();
    }

    // ============================================================
    // END TURN
    // ============================================================

    public void EndTurn()
    {
        if (isProcessing)
            return;

        StartCoroutine(
            EndTurnRoutine()
        );
    }

    // ============================================================
    // ENEMY ATTACK ROUTINE
    // ============================================================

    private IEnumerator EndTurnRoutine()
    {
        isProcessing = true;

        for (int i = 0;
             i < characters.Count;
             i++)
        {
            characters[i].SetAvailable(
                i == 0
            );
        }

        foreach (
            HoverEffect enemyHover
            in enemies)
        {
            CharacterStat enemyStat =
                enemyHover
                    .GetComponent<CharacterStat>();

            if (enemyStat == null ||
                enemyStat.hp <= 0)
                continue;

            // ====================================================
            // VALID PLAYER/ALLY TARGETS
            // ====================================================

            List<HoverEffect> validTargets =
                new List<HoverEffect>();

            foreach (
                HoverEffect character
                in characters)
            {
                if (enemies.Contains(character))
                    continue;

                CharacterStat charStat =
                    character
                        .GetComponent<CharacterStat>();

                if (charStat != null &&
                    charStat.hp > 0)
                {
                    validTargets.Add(
                        character
                    );
                }
            }

            if (validTargets.Count == 0)
            {
                Debug.Log(
                    "No valid targets for " +
                    enemyHover.name
                );

                continue;
            }

            // ====================================================
            // CHOOSE TARGET
            // ====================================================

            HoverEffect chosenTargetHover =
                validTargets[
                    Random.Range(
                        0,
                        validTargets.Count
                    )
                ];

            CharacterStat chosenTarget =
                chosenTargetHover
                    .GetComponent<CharacterStat>();

            // ====================================================
            // GUARD
            // ====================================================

            foreach (
                HoverEffect guardHover
                in characters)
            {
                if (enemies.Contains(guardHover))
                    continue;

                CharacterStat guardStat =
                    guardHover
                        .GetComponent<CharacterStat>();

                if (guardStat != null &&
                    guardStat.isGuarding &&
                    guardStat.hp > 0)
                {
                    if (chosenTargetHover != guardHover)
                    {
                        Debug.Log(
                            guardHover.name +
                            " jumped in front of " +
                            chosenTargetHover.name +
                            " to take the hit!"
                        );

                        chosenTargetHover =
                            guardHover;

                        chosenTarget =
                            guardStat;

                        break;
                    }
                }
            }

            // ====================================================
            // ENEMY ATTACKER PARTICLE
            // ====================================================

            SpawnParticle(
                attackerHitParticlePrefab,
                enemyStat.gameObject
            );

            // ====================================================
            // DAMAGE
            // ====================================================

            chosenTarget.hp -=
                enemyStat.atk;

            // ====================================================
            // ALLY HIT PARTICLE
            // ====================================================

            SpawnParticle(
                allyHitParticlePrefab,
                chosenTarget.gameObject
            );

            Debug.Log(
                enemyHover.name +
                " attacked " +
                chosenTargetHover.name +
                " for " +
                enemyStat.atk +
                " damage! Remaining HP: " +
                chosenTarget.hp
            );

            if (chosenTarget.hp <= 0)
            {
                chosenTarget.hp = 0;

                Debug.Log(
                    chosenTargetHover.name +
                    " has been defeated!"
                );
            }

            yield return new WaitForSeconds(1f);
        }

        // ========================================================
        // RESET GUARD
        // ========================================================

        foreach (HoverEffect character in characters)
        {
            CharacterStat stat =
                character.GetComponent<CharacterStat>();

            if (stat != null)
                stat.isGuarding = false;
        }

        isProcessing = false;
    }
}