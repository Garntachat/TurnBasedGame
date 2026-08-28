using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoverEffect : MonoBehaviour
{
    public GameObject AttackButton;     // leave empty for enemies
    public GameObject UniqueButton;     // leave empty for enemies
    public bool available = true;
    public GameObject availableStatus;  // leave empty for enemies

    void Start()
    {
        UpdateAvailableStatus();
    }

    public void OnHoverEnterEffect(GameObject go)
    {
        go.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
        if (available)
        {
            if (AttackButton != null) AttackButton.SetActive(true);
            if (UniqueButton != null) UniqueButton.SetActive(true);
        }
    }

    public void OnHoverExitEffect(GameObject go)
    {
        go.transform.localScale = Vector3.one;
        if (AttackButton != null) AttackButton.SetActive(false);
        if (UniqueButton != null) UniqueButton.SetActive(false);
    }

    public void OnButtonClick()
    {   
        Debug.Log("Button was clicked!");
        available = false;
        if (AttackButton != null) AttackButton.SetActive(false);
        if (UniqueButton != null) UniqueButton.SetActive(false);
        UpdateAvailableStatus();
    }

    public void SetAvailable(bool value)
    {
        available = value;
        UpdateAvailableStatus();
    }

    private void UpdateAvailableStatus()
    {
        if (availableStatus != null)
            availableStatus.SetActive(available);
    }
}