using UnityEngine;

public class PlayerPickupManager : MonoBehaviour
{
    public bool isPlayerHoldingPakcage;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isPlayerHoldingPakcage = false;
    }

    // Update is called once per frame
    void Update()
    {
        CheckPlayerHand();
    }

    public void CheckPlayerHand()
    {
        if(isPlayerHoldingPakcage == true)
        {
            Debug.Log("PLayer is Holding Package");
        }
        else{
            Debug.Log("No Package in Player Hand");
        }
    }

    public void ObjectPickedUp()
    {
        isPlayerHoldingPakcage = true;
        Debug.Log("***********************");
    }

        public void ObjectDropped()
    {
        isPlayerHoldingPakcage = false;
        Debug.Log("------------------");
    }
}
