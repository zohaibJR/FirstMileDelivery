using UnityEngine;

public class PauseScript : MonoBehaviour
{
    public GameObject PausePanel;
    public bool isgamePaused;

    void Start()
    {
        Time.timeScale = 1f;
        isgamePaused = false;
        PausePanel.SetActive(false);

        if (CursorManager.Instance != null)
            CursorManager.Instance.EnableCursor();   // show at start
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (!isgamePaused)
        {
            PausePanel.SetActive(true);
            Time.timeScale = 0f;

            if (CursorManager.Instance != null)
                CursorManager.Instance.EnableCursor();

            isgamePaused = true;
        }
        else
        {
            PausePanel.SetActive(false);
            Time.timeScale = 1f;

            if (CursorManager.Instance != null)
                CursorManager.Instance.DisableCursor();

            isgamePaused = false;
        }
    }

    public void QuitApplication()
    {
        Debug.Log("Quit button clicked. Application quitting...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;   // Stop play mode in Editor
#else
    Application.Quit();   // Quit in build
#endif
    }

}
