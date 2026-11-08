using System;

/// <summary>
/// Tiny global message bus for on-screen toasts. Anything can call Notifier.Show("text").
/// </summary>
public static class Notifier
{
    public static event Action<string> Message;

    public static void Show(string message)
    {
        if (!string.IsNullOrEmpty(message))
            Message?.Invoke(message);
    }
}