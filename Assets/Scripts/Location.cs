using System;
using UnityEngine;

/// <summary>
/// This structure holds a position in the game-world;
/// the map is identified by its index in the mapController's
/// map array. The entire location can be encoded into 
/// </summary>
[Serializable]
public struct Location : IEquatable<Location>
{
	public readonly int x;
	public readonly int y;
	public readonly int mapIndex;

	public Location (int x, int y, int mapIndex)
	{
		this.x = x;
		this.y = y;
		this.mapIndex = mapIndex;
	}

	/// <summary>
	/// This method applies a delta to the x and y co-ordinates
	/// of this location, and returns the updated result.
	/// </summary>
	public Location WithOffset (int deltaX, int deltaY)
	{
		return new Location (x + deltaX, y + deltaY, mapIndex);
	}

	/// <summary>
	/// This method converts the position to a Unity scene-space
	/// position. The z co-ordinate is always 0, and the y is
	/// inverted (typically negatie) because Unity's co-ordinates
	/// have (0,0) in the bottom left.
	/// </summary>
	public Vector3 ToPosition ()
	{
		return new Vector3 (x, -y, mapIndex);
	}

	/// <summary>
	/// FromPosition() turns a Unity position it its locaiton, reversing
	/// the effect of ToPosition().
	/// </summary>
	public static Location FromPosition (Vector3 position)
	{
		return new Location ((int)position.x, -(int)position.y, (int)position.z);
	}

	/// <summary>
	/// This method extracts the location of a game object
	/// for you.
	/// </summary>
	public static Location Of (GameObject gameObject)
	{
		return FromPosition (gameObject.transform.position);
	}

	/// <summary>
	/// Returns an array containing the locations adjacent
	/// to this one, north, south, east and west.
	/// </summary>
	public Location[] GetAdjacent ()
	{
		var buffer = new Location[4];
		GetAdjacent (buffer);
		return buffer;
	}

	/// <summary>
	/// Populates the first four elements of 'buffer'
	/// with the adjacent locations next to this one;
	/// with this method you can reuse the buffer array
	/// many times, saving allocations.
	/// </summary>
	public void GetAdjacent(Location[] buffer)
	{
		buffer [0] = new Location (x, y - 1, mapIndex);
		buffer [1] = new Location (x + 1, y, mapIndex);
		buffer [2] = new Location (x, y + 1, mapIndex);
		buffer [3] = new Location (x - 1, y, mapIndex);
	}

	/// <summary>
	/// Returna a textual representation of the location, useful
	/// for debugging.
	/// </summary>
	public override string ToString ()
	{
		return string.Format ("({0}, {1} in map #{2})", x, y, mapIndex);
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
		return this.x == other.x && this.y == other.y && this.mapIndex == other.mapIndex;
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