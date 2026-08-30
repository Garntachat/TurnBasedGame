using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CampPhaseManager : MonoBehaviour
{
    [Header("References")]
    public Game game;

    [Header("Main Panel")]
    public GameObject campPhasePanel;
    public Button upgradeTeamButton;
    public Button upgradeOneButton;
    public Button killTeammateButton;

    [Header("Character Select Panel")]
    public GameObject characterSelectPanel;
    public List<Button> characterSelectButtons; // 3 ปุ่ม แทน char2, char3, char4
    public Button selectSelfButton; 

    [Header("Upgrade Amounts")]
    public int teamHpBonus = 10;
    public int teamAtkBonus = 2;
    public int soloHpBonus = 25;
    public int soloAtkBonus = 5;

    private System.Action onDone;
    private bool selectingForKill;

    void Awake()
    {
        campPhasePanel.SetActive(false);
        characterSelectPanel.SetActive(false);

        upgradeTeamButton.onClick.AddListener(OnUpgradeTeamClicked);
        upgradeOneButton.onClick.AddListener(OnUpgradeOneClicked);
        killTeammateButton.onClick.AddListener(OnKillTeammateClicked);

        for (int i = 0; i < characterSelectButtons.Count; i++)
        {
            int allyIndex = i + 1; // ปุ่มที่ 0 = characters[1], ปุ่มที่ 1 = characters[2], ...
            characterSelectButtons[i].onClick.AddListener(() => OnCharacterSelected(allyIndex));
        }

        if (selectSelfButton != null)
            selectSelfButton.onClick.AddListener(() => OnCharacterSelected(0));
    }

    public void ShowCampPhase(bool allowKillTeammate, System.Action onComplete)
    {
        onDone = onComplete;
        campPhasePanel.SetActive(true);
        killTeammateButton.gameObject.SetActive(allowKillTeammate);
    }

    private void OnUpgradeTeamClicked()
    {
        game.UpgradeWholeTeam(teamHpBonus, teamAtkBonus);
        Finish();
    }

    private void OnUpgradeOneClicked()
    {
        selectingForKill = false;
        OpenCharacterSelect();
    }

    private void OnKillTeammateClicked()
    {
        selectingForKill = true;
        OpenCharacterSelect();
    }

    private void OpenCharacterSelect()
    {
        campPhasePanel.SetActive(false);
        characterSelectPanel.SetActive(true);

        List<int> alive = game.GetAliveTeammateIndices();

        for (int i = 0; i < characterSelectButtons.Count; i++)
        {
            int allyIndex = i + 1;
            characterSelectButtons[i].gameObject.SetActive(alive.Contains(allyIndex));
        }

        // ปุ่มเลือกตัวเอง โชว์เฉพาะตอน "อัพคนเดียว" เท่านั้น ไม่โชว์ตอน "ฆ่าเพื่อน" (ฆ่าตัวเองไม่ได้)
        if (selectSelfButton != null)
            selectSelfButton.gameObject.SetActive(!selectingForKill);
    }

    private void OnCharacterSelected(int allyIndex)
    {
        if (selectingForKill)
            game.KillTeammate(allyIndex);
        else
            game.UpgradeOneCharacter(allyIndex, soloHpBonus, soloAtkBonus);

        characterSelectPanel.SetActive(false);
        Finish();
    }

    private void Finish()
    {
        campPhasePanel.SetActive(false);
        characterSelectPanel.SetActive(false);

        System.Action callback = onDone;
        onDone = null;
        callback?.Invoke();
    }
}