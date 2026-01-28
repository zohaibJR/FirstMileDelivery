using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance;

    public bool forceCursor = false;   // true = always visible

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        EnableCursor();   // Start with cursor visible
        forceCursor = true;
    }

    void LateUpdate()
    {
        // This prevents other scripts from overriding cursor
        if (forceCursor)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void EnableCursor()
    {
        Debug.Log("Cursor Enabled");
        forceCursor = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void DisableCursor()
    {
        Debug.Log("Cursor Disabled");
        forceCursor = false;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
