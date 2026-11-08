using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Tracks whether the player is using keyboard/mouse or a gamepad,
/// so prompts can show the right button label.
/// </summary>
public static class InputGlyphs
{
    private static int lastPollFrame = -1;

    public static bool UsingGamepad { get; private set; }

    /// <summary>Call once per frame (safe to call many times).</summary>
    public static void Poll()
    {
        if (lastPollFrame == Time.frameCount)
            return;
        lastPollFrame = Time.frameCount;

        Gamepad pad = Gamepad.current;
        if (pad != null && GamepadActive(pad))
        {
            UsingGamepad = true;
            return;
        }

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        bool keyboardActive = keyboard != null && keyboard.anyKey.wasPressedThisFrame;
        bool mouseActive = mouse != null &&
            (mouse.leftButton.wasPressedThisFrame ||
             mouse.rightButton.wasPressedThisFrame ||
             mouse.delta.ReadValue().sqrMagnitude > 4f);

        if (keyboardActive || mouseActive)
            UsingGamepad = false;
    }

    private static bool GamepadActive(Gamepad pad)
    {
        foreach (InputControl control in pad.allControls)
        {
            if (control is ButtonControl button && button.wasPressedThisFrame)
                return true;
        }

        return pad.leftStick.ReadValue().sqrMagnitude > 0.25f ||
               pad.rightStick.ReadValue().sqrMagnitude > 0.25f;
    }

    public static string KeyName(Key key)
    {
        switch (key)
        {
            case Key.Escape: return "Esc";
            case Key.LeftShift: return "Shift";
            case Key.LeftCtrl: return "Ctrl";
            default: return key.ToString();
        }
    }

    public static string Badge(string label) => $"<color=#FFD54A>[{label}]</color>";

    /// <summary>Badge for a keyboard key, or the given gamepad label when a gamepad is in use.</summary>
    public static string Pick(Key key, string gamepadLabel) =>
        Badge(UsingGamepad ? gamepadLabel : KeyName(key));

    public static string Pick(string keyboardLabel, string gamepadLabel) =>
        Badge(UsingGamepad ? gamepadLabel : keyboardLabel);
}