using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// This class holds a heat value for each cell in your map;
/// critters can then move towards the hotter cells. This uses
/// a spare storage strategy, so you can set any cell's value and
/// not sweat the memory wastage.
/// </summary>
public sealed class Heatmap : ICloneable
{
	private const int blockSize = 16;
	private const int locationLocalMask = 0xF;
	private const int locationKeyMask = ~locationLocalMask;
	private readonly Dictionary<Location, short[,]> blocks = new Dictionary<Location, short[,]> ();

	public Heatmap ()
	{
	}

	/// <summary>
	/// This indexer accesses an individual value for a location; you can
	/// set any location's value; unset values are 0 by default.
	/// </summary>
	public short this [Location location] {
		get { return GetCell (location, createIfMissing: false).Value; }

		set { 
			Cell cell = GetCell (location, createIfMissing: value != 0);

			if (cell.IsValid) {
				cell.Value = value;
			}
		}
	}

	/// <summary>
	/// Locations() yields every individual location that
	/// might have a non-zero value; this doesn't check each
	/// value, so some zero locations will be returned- but
	/// the set of locations returned is always finite.
	/// </summary>
	public IEnumerable<Location> Locations ()
	{
		foreach (Location key in blocks.Keys) {
			for (int ly = 0; ly < blockSize; ++ly) {
				for (int lx = 0; lx < blockSize; ++lx) {
					yield return key.WithOffset (lx, ly);
				}
			}
		}
	}

	/// <summary>
	/// Picks the best move from the candidates given; that is, it picks
	/// the cell with the largest heat value. If a tie for hottest cell is
	/// found, this picks one of the best randomly.
	/// 
	/// If 'candidates' is empty, this returns false. Otherwise, it places
	/// the result in 'picked' and returns true.
	/// </summary>
	public bool TryPickMove (IEnumerable<Location> candidates, out Location picked)
	{
		IEnumerable<Location> moves =
			(from d in candidates
			 group d by this [d] into g
			 orderby g.Key descending
			 select g).FirstOrDefault ();

		if (moves != null) {
			int index = UnityEngine.Random.Range (0, moves.Count ());
			picked = moves.ElementAt (index);
			return true;
		} else {
			picked = new Location ();
			return false;
		}
	}

	/// <summary>
	/// This returns a crude string representation of the heatmap content;
	/// this is not very good or fast and should be used for debugging only.
	/// </summary>
	public override string ToString ()
	{
		var byMap = Locations ().GroupBy (l => l.mapIndex);
		var b = new StringBuilder ();

		foreach (var grp in byMap) {
			b.AppendLine ("Map #: " + grp.Key);

			int minX = grp.Min (loc => loc.x);
			int minY = grp.Min (loc => loc.y);
			int maxX = grp.Max (loc => loc.x);
			int maxY = grp.Max (loc => loc.y);
		
			for (int y = minY; y <= maxY; ++y) {
				for (int x = minX; x <= maxX; ++x) {
					b.AppendFormat ("{0:X2}", Math.Abs (this [new Location (x, y, grp.Key)]));
				}
				b.AppendLine ();
			}
		}

		return b.ToString ().Trim ();
	}

	#region Heat and Cool

	/// <summary>
	/// This method reduces every value by 'amount', but won't flip the sign
	/// of any value- it stops at 0. Cells that contain 0 will not be changed.
	/// 
	/// This method updates the heatmap in place.
	/// </summary>
	public void Reduce (int amount = 1)
	{
		Location[] keys = Locations ().ToArray ();

		foreach (Location loc in keys) {
			GetCell (loc, createIfMissing: false).ReduceValue (amount);
		}
	}

	/// <summary>
	/// This returns a heatmap that has had its heat values propagated;
	/// each cell in the new heatmap has a value that is the maximum of
	/// its old value, and one less than the values of the adjacent cells.
	/// 
	/// We can't easily do this in place, so this returns a new heatmap that
	/// has been updated.
	/// 
	/// You provide a delegate that indicates which cells are passable;
	/// impassible cells don't get heated, which can block the spread of
	/// heat through the map.
	/// </summary>
	public Heatmap GetHeated (Func<Location, bool> passability)
	{
		Heatmap copy = Copy ();

		foreach (Location srcLoc in Locations ()) {
			short min = GetCell (srcLoc, createIfMissing: false).GetReducedValue (1);
			
			if (min != 0) {
				foreach (Location adj in srcLoc.GetAdjacent()) {
					if (passability (adj)) {
						copy.GetCell (adj, createIfMissing: true).IncreaseValue (min);
					}
				}
			}
		}

		return copy;
	}

	/// <summary>
	/// This method applies multiple rounds of heat; as many as 'repeat' indicated.
	/// this returns a new heatmap containing the result.
	/// </summary>
	public Heatmap GetHeated (int repeats, Func<Location, bool> passability)
	{
		if (repeats < 1) {
			return Copy ();
		}

		Heatmap h = this;

		for (int i = 0; i < repeats; ++i) {
			h = h.GetHeated (passability);
		}

		return h;
	}

	#endregion

	#region Cell Access

	/// <summary>
	/// Returns a cell structure that describes a specific location; if createIfMissing
	/// is true this will allocate storage for the location; if false it won't, and may return
	/// an invalid cell if there's no storage for the cell.
	/// </summary>
	private Cell GetCell (Location location, bool createIfMissing)
	{
		var key = new Location (location.x & locationKeyMask, location.y & locationKeyMask, location.mapIndex);
		short[,] block;
		
		if (!blocks.TryGetValue (key, out block) && createIfMissing) {
			block = new short[blockSize, blockSize];
			blocks.Add (key, block);
		}
		
		return new Cell (block, location.x & locationLocalMask, location.y & locationLocalMask);
	}

	/// <summary>
	/// This structure acts as a handle on a single location in the
	/// heapmap; it retains a refernce to the storage array and
	/// the position in that aray,.
	/// </summary>
	private struct Cell
	{
		private readonly short[,] block;
		private readonly int x, y;
		
		public Cell (short[,] block, int x, int y)
		{
			this.block = block;
			this.x = x;
			this.y = y;
		}

		/// <summary>
		/// IsValid is true if this cell refers to a locaiton, and false if it
		/// is an empty cell structure. If false the Value is 0, and cannot be set
		/// to any other value. An invalid cell is used to represent a cell whose
		/// storage is not allocated yet.
		/// </summary>
		public bool IsValid {
			get { return block != null; }
		}

		/// <summary>
		/// Value accesses the cell value. If IsValid is false, this value
		/// is 0 and can't be changed to anything else.
		/// </summary>
		public short Value {
			get { return block [x, y]; }
			set { 
				if (block != null)
					block [x, y] = value;
				else if (value != 0) {
					throw new InvalidOperationException ();
				}
			}
		}

		/// <summary>
		/// This increases Value so it is no less than 'minimum';
		/// this is judged on the absolute value, so a large negatve
		/// value may be 'greater' than a small positive one.
		/// </summary>
		public void IncreaseValue (short minimum)
		{
			if (Math.Abs (Value) < Math.Abs (minimum))
				Value = minimum;
		}
	
		/// <summary>
		/// This mnethod reduces Value by 'amount', but won't
		/// flip the sign; it will stop at 0. If the Value is
		/// zero already, this method does nothing.
		/// </summary>
		public void ReduceValue (int amount)
		{
			Value = GetReducedValue (amount);
		}

		/// <summary>
		/// GetReducedValue() computes the value that reducing this
		/// cell would produce, without actually updating the Value.
		/// </summary>
		public short GetReducedValue (int amount)
		{
			decimal current = Value;

			if (current > 0) {
				return (short)Math.Max (0, current - amount);
			} else if (current < 0) {
				return (short)Math.Min (0, current + amount);
			} else {
				return 0;
			}
		}
	}

	#endregion

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