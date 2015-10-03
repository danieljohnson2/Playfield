using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

using SourceDisposition = Heatmap.SourceDisposition;

/// <summary>
/// This holds an entity's name or tag; or the default identifer which
/// means "all sources".
/// </summary>
public struct HeatSourceIdentifier : IEquatable<HeatSourceIdentifier>
{
    private readonly string text;
    private readonly bool isTag;
    private readonly SourceDisposition? disposition;

    private HeatSourceIdentifier(string text, bool isTag, SourceDisposition? disposition)
    {
        this.text = text;
        this.isTag = isTag;
        this.disposition = disposition;
    }

    public override string ToString()
    {
        if (isTag)
            return text;
        else
            return "\"" + text + "\"";
    }

    /// <summary>
    /// MatchesAll is true if this is the default HeatSourceIdentifier,
    /// and in that case it matches any source.
    /// </summary>
    public bool MatchesAll
    {
        get { return string.IsNullOrEmpty(text); }
    }

    /// <summary>
    /// Matches() is true if the source info given corresponds
    /// to this identifier. This returns false if 'sourceInfo',
    /// and if 'MatchesAll' is true it true for any non-null
    /// 'sourceInfo'.
    /// </summary>
    public bool Matches(Heatmap.SourceInfo sourceInfo)
    {
        if (sourceInfo == null)
            return false;
        else if (MatchesAll)
            return true;
        else
        {
            if (disposition != null && sourceInfo.Disposition != null && disposition != sourceInfo.Disposition)
                return false;

            if (isTag)
                return sourceInfo.Tag == text;
            else
                return sourceInfo.Name == text;
        }
    }

    /// <summary>
    /// This returns the entities that exist in the world that matches
    /// this identifier.
    /// </summary>
    public IEnumerable<GameObject> GameObjects()
    {
        if (MatchesAll)
            return MapController.instance.entities.Entities();
        else if (isTag)
            return MapController.instance.entities.byTag[text];
        else
            return MapController.instance.entities.byName[text];
    }

    /// <summary>
    /// Parse() parses a string as a source identifier; if
    /// the text is blank, this returns a source identifier that
    /// matches everything. If it is in quotes, it is interpreted
    /// as a name. If not, a tag.
    /// </summary>
    public static HeatSourceIdentifier Parse(string text)
    {
        text = text.Trim();
        string namePart = text;
        SourceDisposition? disposition = null;

        if (text.EndsWith("]"))
        {
            int openBracket = text.LastIndexOf('[');

            if (openBracket >= 0)
            {
                string dispText = text.Substring(openBracket + 1, text.Length - openBracket - 2).Trim();

                disposition = (SourceDisposition)Enum.Parse(typeof(SourceDisposition), dispText, ignoreCase: true);
                namePart = text.Substring(0, openBracket).TrimEnd();
            }
        }

        if (namePart.StartsWith("\"") && namePart.EndsWith("\""))
        {
            return new HeatSourceIdentifier(
                namePart.Substring(1, namePart.Length - 2).Trim(),
                isTag: false,
                disposition: disposition);
        }
        else
        {
            return new HeatSourceIdentifier(namePart, isTag: true, disposition: disposition);
        }
    }

    #region IEquatable implementation

    public bool Equals(HeatSourceIdentifier other)
    {
        return
            isTag == other.isTag &&
            text == other.text;
    }

    public override bool Equals(object obj)
    {
        return obj is HeatSourceIdentifier && Equals((HeatSourceIdentifier)obj);
    }

    public override int GetHashCode()
    {
        if (text == null)
            return 0;
        else
            return text.GetHashCode();
    }

    #endregion
}