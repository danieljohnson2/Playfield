﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// GameOverController controls the game over screen, and
/// hosts variables that hold the text to display there
/// whilst we load the game over screen.
/// </summary>
public class GameOverController : MonoBehaviour
{
    public UnityEngine.UI.Text messageText;
    public UnityEngine.UI.Text lockedCharactersText;
    public UnityEngine.UI.Text unlockedCharactersText;

    /// <summary>
    /// gameOverMessage is a second message to show
    /// on the game over screen. This is set before we load
    /// the game over scene.
    /// </summary>
    public static string gameOverMessage { get; set; }

    void Start()
    {
        if (messageText != null)
            messageText.text = (gameOverMessage ?? "").Trim();

        var activation = CharacterActivation.instance;

        if (lockedCharactersText != null)
            lockedCharactersText.text = GetLockMessage("Characters locked", activation.RecentlyLocked());

        if (unlockedCharactersText != null)
            unlockedCharactersText.text = GetLockMessage("Characters unlocked", activation.RecentlyUnlocked());

        activation.ResetRecentChanges();
    }

    /// <summary>
    /// GetLockMessage() returns the message to show on the screen so the user
    /// can see what character he has locked, or unlocked.
    /// </summary>
    private static string GetLockMessage(string prefix, IEnumerable<string> characters)
    {
        if (characters.Any())
            return string.Format("{0}: {1}", prefix, string.Join(", ", characters.ToArray()));
        else
            return "";
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void RestartGame()
    {
        Application.LoadLevel("Intro");
    }

    public static void GameOver()
    {
        Application.LoadLevel("Game Over");
    }
}
