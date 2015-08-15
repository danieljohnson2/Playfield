using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

/// <summary>
/// This holds an entity's name or tag; or the default identifer which
/// means "all sources".
/// </summary>
public struct HeatSourceIdentifier : IEquatable<HeatSourceIdentifier>
{
	private readonly string text;
	private readonly bool isTag;

	private HeatSourceIdentifier (string text, bool isTag)
	{
		this.text = text;
		this.isTag = isTag;
	}
	
	public override string ToString ()
	{
		if (isTag) {
			return text;
		} else {
			return "\"" + text + "\"";
		}
	}

	/// <summary>
	/// MatchesAll is true if this is the default HeatSourceIdentifier,
	/// and in that case it matches any source.
	/// </summary>
	public bool MatchesAll {
		get { return string.IsNullOrEmpty (text); }
	}

	/// <summary>
	/// Matches() is true if the source info given corresponds
	/// to this identifier. This returns false if 'sourceInfo',
	/// and if 'MatchesAll' is true it true for any non-null
	/// 'sourceInfo'.
	/// </summary>
	public bool Matches (Heatmap.SourceInfo sourceInfo)
	{
		if (sourceInfo == null) {
			return false;
		}

		if (MatchesAll) {
			return true;
		} else if (isTag) {
			return sourceInfo.Tag == text;
		} else {
			return sourceInfo.Name == text;
		}
	}

	/// <summary>
	/// This returns the entities that exist in the world that matches
	/// this identifier.
	/// </summary>
	public IEnumerable<GameObject> GameObjects ()
	{
		if (MatchesAll) {
			return MapController.instance.entities.Entities ();
		} else if (isTag) {
			return MapController.instance.entities.byTag [text];
		} else {
			return MapController.instance.entities.byName [text];
		}
	}

	/// <summary>
	/// Parse() parses a string as a source identifier; if
	/// the text is blank, this returns a source identifier that
	/// matches everything. If it is in quotes, it is interpreted
	/// as a name. If not, a tag.
	/// </summary>
	public static HeatSourceIdentifier Parse (string text)
	{
		text = text.Trim ();

		if (text.StartsWith ("\"") && text.EndsWith ("\"")) {
			return new HeatSourceIdentifier (
				text.Substring (1, text.Length - 2).Trim (),
				isTag: false);
		} else {
			return new HeatSourceIdentifier (text, isTag: true);
		}
	}

	#region IEquatable implementation

	public bool Equals (HeatSourceIdentifier other)
	{
		return
			isTag == other.isTag &&
			text == other.text;
	}

	public override bool Equals (object obj)
	{
		return obj is HeatSourceIdentifier && Equals ((HeatSourceIdentifier)obj);
	}

	public override int GetHashCode ()
	{
		if (text == null) {
			return 0;
		} else {
			return text.GetHashCode ();
		}
	}

	#endregion
}