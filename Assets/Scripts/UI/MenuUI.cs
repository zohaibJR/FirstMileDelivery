using UnityEngine;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject ContinueButton;
    public InputField nameInput;

    void Start()
    {
        mainMenuPanel.SetActive(true);

        if (SaveSystem.HasSave())
        {
            ContinueButton.SetActive(true);
            PlayerData data = SaveSystem.Load();
            Debug.Log("Welcome back " + data.playerName);
        }
        else
        {
            ContinueButton.SetActive(false);
            Debug.Log("New Name");
        }
    }

    public void StartNewGame()
    {
        if (nameInput.text == "") return;

        // Delete old save if exists
        SaveSystem.DeleteSave();

        PlayerData data = new PlayerData();
        data.playerName = nameInput.text;
        data.coins = 3000;

        SaveSystem.Save(data);
        ContinueButton.SetActive(false);

        Debug.Log("Game Started for " + data.playerName);
    }

    public void ContinueGame()
    {
        PlayerData data = SaveSystem.Load();
        if (data != null)
            Debug.Log("Welcome back " + data.playerName);

        ContinueButton.SetActive(false);
    }
}
