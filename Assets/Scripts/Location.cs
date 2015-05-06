using System;
using UnityEngine;

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

	public Vector3 ToPosition ()
	{
		return new Vector3 (x, -y, 0f);
	}

	public static Location Of (GameObject gameObject)
	{
		return Of (gameObject.transform);
	}

	public static Location Of (Transform transform)
	{
		Vector3 pos = transform.position;
		return new Location ((int)pos.x, -(int)pos.y);
	}

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
		return unchecked(x + y);
	}

	#endregion
}