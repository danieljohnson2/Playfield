using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.SceneManagement;

/// <summary>
/// ResetGameController handles the 'reset game' screen, which
/// asks if you want to reset the game. If you do, this will
/// delete all the save game files.
/// </summary>
public class ResetGameController : MonoBehaviour
{
    public void ResetGame()
    {
        CharacterActivation.Reset();
        File.Delete(PlayableEntityController.GetSaveGamePath());

		SceneManager.LoadScene("Intro");
    }

    public void Cancel()
    {
		SceneManager.LoadScene("Intro");
    }
}
