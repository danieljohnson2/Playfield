using UnityEngine;
using System.Collections;

/// <summary>
/// GameOverController controls the game over screen, and
/// hosts variables that hold the text to display there
/// whilst we load the game over screen.
/// </summary>
public class GameOverController : MonoBehaviour
{
    public UnityEngine.UI.Text messageText;

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
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public static void GameOver()
    {
        Application.LoadLevel("Game Over");
    }
}
