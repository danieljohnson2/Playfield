using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine.SceneManagement;

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
    public Text startPrompt;

    private float startPromptTime;
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

        startPromptTime = Time.time + 5f;
    }

    void Update()
    {
        // after 5 seconds we'll fade in instructions on how to start.

        if (startPrompt != null && Time.time >= startPromptTime)
        {
            Color color = startPrompt.color;
            if (color.a < 1f)
            {
                if (color.a < 0.99f)
                    color.a = Mathf.Lerp(color.a, 1f, Time.deltaTime);
                else
                    color.a = 1f;

                startPrompt.color = color;
            }
        }
    }

    /// <summary>
    /// StartGame() loads the playfield scene, but places a delegate to be
    /// executed on game start; this will condigure the player named to be
    /// player controlled.
    /// </summary>
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
		SceneManager.LoadScene("Playfield");
    }

    /// <summary>
    /// ResetGame() doesn't immediately reset the saved game state, but
    /// instead goes to a scene where the user can do this.
    /// </summary>
    public void ResetGame()
    {
		SceneManager.LoadScene("Reset Game");
    }

    /// <summary>
    /// ExitGame() is a way out of here.
    /// </summary>
    public void ExitGame()
    {
        Application.Quit();
    }

    /// <summary>
    /// LoadSavedGame()triggers the loading of the Playfield
    /// scene, but places a delegate to cause it to load the
    /// saved game.
    /// </summary>
    public void LoadSavedGame()
    {
        PlayableEntityController.Restore();
		SceneManager.LoadScene("Playfield");
    }

    /// <summary>
    /// ShowCredits() transitions to our bragging scene.
    /// </summary>
    public void ShowCredits()
    {
        SceneManager.LoadScene("Credits");
    }
}