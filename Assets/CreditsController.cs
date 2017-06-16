using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// CreditsController does little; just provides the handler
/// for the 'main menu' button.
/// </summary>
public class CreditsController : MonoBehaviour
{
    public void ReturnToIntro()
    {
		SceneManager.LoadScene("Intro");
    }
}
