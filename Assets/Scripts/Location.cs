using System;
using UnityEngine;

/// <summary>
/// This structure holds a position in the game-world.
/// </summary>
[Serializable]
public struct Location : IEquatable<Location>
{
	public readonly int x;
	public readonly int y;
	public readonly Map map;

	public Location (int x, int y, Map map)
	{
		this.x = x;
		this.y = y;
		this.map = map;
	}

	public Location WithOffset (int deltaX, int deltaY)
	{
		return new Location (x + deltaX, y + deltaY, map);
	}

	/// <summary>
	/// This method converts the position to a Unity scene-space
	/// position. The z co-ordinate is always 0, and the y is
	/// inverted (typically negatie) because Unity's co-ordinates
	/// have (0,0) in the bottom left.
	/// </summary>
	public Vector3 ToPosition ()
	{
		return new Vector3 (x, -y, 0f);
	}

	/// <summary>
	/// FromPosition() turns a Unity position it its locaiton, reversing
	/// the effect of ToPosition(); we need the map given explicitly
	/// since Untity does not track that.
	/// 
	/// Normally you should use MapController.GetLocation() on a GameObject,
	/// not this directly.
	/// </summary>
	public static Location FromPosition (Vector3 position, Map map)
	{
		return new Location ((int)position.x, -(int)position.y, map);
	}

	/// <summary>
	/// Returns an array containing the locations adjacent
	/// to this one, north, south, east and west.
	/// </summary>
	public Location[] GetAdjacent ()
	{
		return new[] 
		{
			new Location (x, y - 1, map),
			new Location (x + 1, y, map),
			new Location (x, y + 1, map),
			new Location (x - 1, y, map),
		};
	}

	/// <summary>
	/// Returna a textual representation of the location, useful
	/// for debugging.
	/// </summary>
	public override string ToString ()
	{
		if (map != null) {
			return string.Format ("({0}, {1} in {2})", x, y, map.name);
		} else {
			return string.Format ("({0}, {1})", x, y);
		}
	}

	#region IEquatable implementation

	public static bool operator== (Location left, Location right)
	{
		return left.Equals (right);
	}
	
	public static bool operator!= (Location left, Location right)
	{
		return !left.Equals (right);
	}

	public bool Equals (Location other)
	{
		return this.x == other.x && this.y == other.y && this.map == other.map;
	}

	public override bool Equals (object other)
	{
		return other is Location && Equals ((Location)other);
	}

	public override int GetHashCode ()
	{
		return unchecked(x ^ (y << 16));
	}

	#endregion
}