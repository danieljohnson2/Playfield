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
    public Button resetGameButton;
    public Text subtitleText;
    public Button[] characterButtons;
    private const string characterButtonSuffix = " Button";

    void Start()
    {
        if (loadGameButton != null && !PlayableEntityController.CanRestore)
            loadGameButton.gameObject.SetActive(false);
        
        if (resetGameButton != null &&
            CharacterActivation.isDefault &&
            !File.Exists(PlayableEntityController.GetSaveGamePath()))
        {
            resetGameButton.gameObject.SetActive(false);
        }

        HashSet<string> activeCharacters = CharacterActivation.GetActivatedCharacters();

        foreach (Button button in characterButtons ?? Enumerable.Empty<Button>())
        {
            string buttonName = button.name;
            if (buttonName.EndsWith(characterButtonSuffix))
            {
                string charName = buttonName.Substring(0, buttonName.Length - characterButtonSuffix.Length);
                button.interactable = activeCharacters.Contains(charName);
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
                "With a Vengeance",
                "The Quest for Subtitle",
                "The Quickening",
				"More Play, More Field",
				"If There's Play On The Field",
				"Diagonal Combat Exploit",
				"Inexplicably Luminous",
				"Jewels Glow?",
				"Paint The Key",
				"You Dirty Rat",
				"Inconceivable!"
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

    public void ResetGame()
    {
        Application.LoadLevel("Reset Game");
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void LoadSavedGame()
    {
        PlayableEntityController.Restore();
        Application.LoadLevel("Playfield");
    }
}
