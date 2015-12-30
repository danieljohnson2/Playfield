using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/// <summary>
/// StartupController is used only on the title screen, and handles
/// the interactivity there. One the game starts we no longer use this.
/// </summary>
public class StartupController : MonoBehaviour
{
    public Button loadGameButton;
    public Text subtitleText;
    public Button[] characterButtons;
    private const string characterButtonSuffix = " Button";

    void Start()
    {
        if (loadGameButton != null && !PlayableEntityController.CanRestore)
            loadGameButton.gameObject.SetActive(false);

        var activation = CharacterActivation.instance;

        foreach (Button button in characterButtons ?? Enumerable.Empty<Button>())
        {
            string buttonName = button.name;
            if (buttonName.EndsWith(characterButtonSuffix))
            {
                string charName = buttonName.Substring(0, buttonName.Length - characterButtonSuffix.Length);
                button.interactable = activation[charName];
            }
        }

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

    public void StartGame(string playerName)
    {
        MapController.ReloadWithInitialization(delegate (MapController mapController)
        {
            GameObject player = mapController.entities.byName[playerName].Single();
            var pc = player.GetComponent<PlayableEntityController>();
            pc.isPlayerControlled = true;
        });

        // 'ReloadWithInitialization' doesn't load the level the first time,
        // so we do that.
        Application.LoadLevel("Playfield");
    }

    public void LoadSavedGame()
    {
        PlayableEntityController.Restore();
        Application.LoadLevel("Playfield");
    }
}
