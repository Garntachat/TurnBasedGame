using UnityEngine;

public class YouUnique : MonoBehaviour
{
    public HoverEffect mage;
    public HoverEffect healer;
    public HoverEffect tank;

    void Start()
    {
        
    }

    public void OnOrderButtonClick()
    {   
        Debug.Log("Button was clicked!");
        mage.SetAvailable(true);
        healer.SetAvailable(true);
        tank.SetAvailable(true);
    }
}
