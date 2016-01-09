using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// This class provides a transcript, a series of messages
/// shown to the player. You can add lines to it, but there's
/// a size limit and old lines are discarded to make room.
/// </summary>
public class TranscriptController : MonoBehaviour
{
    public int maxLines = 8;
    public Text transcriptText;
    public Text playerStatusText;
    public Text instructionText;

    private readonly List<string> lines = new List<string>();

    /// <summary>
    /// isInstructionTextVisible is used to show or hide the instruction text.
    /// </summary>
    public bool isInstructionTextVisible
    {
        get { return instructionText != null && instructionText.gameObject.activeInHierarchy; }

        set
        {
            if (instructionText != null && instructionText.gameObject.activeSelf != value)
                instructionText.gameObject.SetActive(value);
        }
    }

    public void AddLine(string format, params object[] parameters)
    {
        AddLine(string.Format(format, parameters));
    }

    public void SetPlayerStatus(string text)
    {
        playerStatusText.text = text;
    }

    public void AddLine(string text)
    {
        lines.Add(text);
        while (lines.Count > maxLines)
            lines.RemoveAt(0);

        transcriptText.text = string.Join(
            System.Environment.NewLine,
            lines.ToArray());
    }

    /// <summary>
    /// Lines() reteturns the collection of
    /// lines being displayed.
    /// </summary>
    public IEnumerable<string> Lines()
    {
        return lines;
    }
}
