using System;
using System.Collections.Generic;
using System.Linq;

public sealed class Heatmap : ICloneable
{
	private const int blockSize = 16;
	private const int locationLocalMask = 0xF;
	private const int locationKeyMask = ~locationLocalMask;
	private readonly Dictionary<Location, short[,]> blocks = new Dictionary<Location, short[,]> ();

	public Heatmap ()
	{
	}

	private void ResolveBlock (Location location, out short[,] block, out Location blockLocation, bool createIfMissing)
	{
		var key = new Location (location.x & locationKeyMask, location.y & locationKeyMask);
		blockLocation = new Location (location.x & locationLocalMask, location.y & locationLocalMask);

		if (!blocks.TryGetValue (key, out block) && createIfMissing) {
			block = new short[blockSize, blockSize];
			blocks.Add (key, block);
		}
	}

	public short this [Location location] {
		get {
			short[,] block;
			Location local;
			ResolveBlock (location, out block, out local, createIfMissing: false);

			if (block != null) {
				return block [local.x, local.y];
			} else {
				return 0;
			}
		}

		set { 
			short[,] block;
			Location local;
			ResolveBlock (location, out block, out local, createIfMissing: value != 0);

			if (block != null) {
				block [local.x, local.y] = value;
			}
		}
	}

	public IEnumerable<Location> Locations ()
	{
		foreach (Location key in blocks.Keys) {
			for (int ly = 0; ly < blockSize; ++ly) {
				for (int lx = 0; lx < blockSize; ++lx) {
					yield return new Location (key.x + lx, key.y + ly);
				}
			}
		}
	}

	public void Reduce (int amount = 1)
	{
		Location[] keys = Locations ().ToArray ();

		foreach (Location loc in keys) {
			this [loc] = ReduceShort (this [loc], amount);
		}
	}

	public Heatmap GetHeated (Func<Location, bool> passability)
	{
		Heatmap copy = Copy ();

		foreach (Location srcLoc in Locations ()) {
			short min = ReduceShort (this [srcLoc], 1);

			if (min != 0) {
				foreach (Location adj in srcLoc.GetAdjacent()) {
					if (passability (adj)) {
						short[,] block;
						Location local;
						copy.ResolveBlock (adj, out block, out local, createIfMissing: true);

						block [local.x, local.y] = AbsMax (min, block [local.x, local.y]);
					}
				}
			}
		}

		return copy;
	}

	private static short ReduceShort (short value, int amount)
	{
		if (value > 0) {
			return (short)Math.Max (0, value - amount);
		} else if (value < 0) {
			return (short)Math.Min (0, value + amount);
		} else {
			return 0;
		}
	}

	private static short AbsMax (short left, short right)
	{
		if (Math.Abs (left) > Math.Abs (right))
			return left;
		else
			return right;
	}

	public override string ToString ()
	{
		int minX = Locations ().Min (loc => loc.x);
		int minY = Locations ().Min (loc => loc.y);
		int maxX = Locations ().Max (loc => loc.x);
		int maxY = Locations ().Max (loc => loc.y);

		var b = new System.Text.StringBuilder ();

		for (int y = minY; y <= maxY; ++y) {
			for (int x = minX; x <= maxX; ++x) {
				b.AppendFormat ("{0:X2}", Math.Abs (this [new Location (x, y)]));
			}
			b.AppendLine ();
		}

		return b.ToString ().Trim ();
	}

	#region ICloneable implementation

	public Heatmap (Heatmap source)
	{
		foreach (var pair in source.blocks) {
			blocks.Add (pair.Key, (short[,])pair.Value.Clone ());
		}
	}

	public Heatmap Copy ()
	{
		return new Heatmap (this);
	}

	public object Clone ()
	{
		return Copy ();
	}

	#endregion
}