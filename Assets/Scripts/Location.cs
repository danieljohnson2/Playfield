using System;
using UnityEngine;

/// <summary>
/// This structure holds a position in the game-world.
/// </summary>
[Serializable]
public struct Location : IEquatable<Location>
{
	public int x;
	public int y;

	public Location (int x, int y)
	{
		this.x = x;
		this.y = y;
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
	/// Of() retrurns the location of a gameobject.
	/// </summary>
	public static Location Of (GameObject gameObject)
	{
		return Of (gameObject.transform);
	}

	/// <summary>
	/// Of() retrurns the location of a transform.
	/// </summary>
	public static Location Of (Transform transform)
	{
		Vector3 pos = transform.position;
		return new Location ((int)pos.x, -(int)pos.y);
	}

	/// <summary>
	/// Returns an array containing the locations adjacent
	/// to this one, north, south, east and west.
	/// </summary>
	public Location[] GetAdjacent ()
	{
		return new[] 
		{
			new Location (x, y - 1),
			new Location (x + 1, y),
			new Location (x, y + 1),
			new Location (x - 1, y),
		};
	}

	/// <summary>
	/// Returna a textual representation of the location, useful
	/// for debugging.
	/// </summary>
	public override string ToString ()
	{
		return string.Format ("({0}, {1})", x, y);
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
		return this.x == other.x && this.y == other.y;
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