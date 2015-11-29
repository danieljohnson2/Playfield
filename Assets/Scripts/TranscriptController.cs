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
    public Text transcriptShadowText;
    private readonly List<string> lines = new List<string>();

    public void AddLine(string format, params object[] parameters)
    {
        AddLine(string.Format(format, parameters));
    }

    public void AddLine(string text)
    {
        lines.Add(text);
        while (lines.Count > maxLines)
            lines.RemoveAt(0);
        UpdateText();
    }

    private void UpdateText()
    {
        string text = string.Join(
            System.Environment.NewLine,
            lines.ToArray());

        transcriptText.text = text;
        transcriptShadowText.text = text;
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
