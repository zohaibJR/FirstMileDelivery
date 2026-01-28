using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public void AddCoins(int amount)
    {
        PlayerData data = SaveSystem.Load();
        if (data == null) return;

        data.coins += amount;
        SaveSystem.Save(data);

        Debug.Log("Coins Added: " + amount + " | Total: " + data.coins);
    }

    public bool SubtractCoins(int amount)
    {
        PlayerData data = SaveSystem.Load();
        if (data == null) return false;

        if (data.coins >= amount)
        {
            data.coins -= amount;
            SaveSystem.Save(data);
            Debug.Log("Coins Spent: " + amount + " | Left: " + data.coins);
            return true;
        }
        else
        {
            Debug.Log("Not enough coins!");
            return false;
        }
    }

}
