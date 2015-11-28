using UnityEngine;
using System.Collections;

/// <summary>
/// StartupController is used only on the title screen, and handles
/// the interactivity there. One the game starts we no longer use this.
/// </summary>
public class StartupController : MonoBehaviour
{
    public UnityEngine.UI.Button loadGameButton;
    public UnityEngine.UI.Text subtitleText;

    void Start()
    {
        if (loadGameButton != null && !PlayerController.CanRestore)
            loadGameButton.gameObject.SetActive(false);

        if (subtitleText != null)
        {
            string[] subtitles =
                {
                    "The Search for Subtitle",
                    "This Time It's Personal",
                    "Forever",
                    "I Want to Subtitle",
                    "With a Vengeange",
                    "The Quest for Subtitle",
                    "The Quickening"
                };

            subtitleText.text = subtitles[Random.Range(0, subtitles.Length)];
        }
    }

    public void StartGame()
    {
        Application.LoadLevel("Playfield");
    }

    public void LoadSavedGame()
    {
        PlayerController.Restore();
        Application.LoadLevel("Playfield");
    }
}
