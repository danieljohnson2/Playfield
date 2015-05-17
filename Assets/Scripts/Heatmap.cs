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
public sealed class Heatmap : LocationMap<short>
{
	public Heatmap ()
	{
	}
	
	public Heatmap (IEnumerable<KeyValuePair<Location, short>> source) : base(source)
	{
	}

	/// <summary>
	/// Picks the best move from the candidates given; that is, it picks
	/// the cell with the largest heat value. If a tie for hottest cell is
	/// found, this picks one of the best randomly. This method will not
	/// pick a location whose heat is 0.
	/// 
	/// This method returns false if all candiates have a heat of 0, or if there
	/// are no candidates at all. Otherwise, it places the result in 'picked' and returns true.
	/// </summary>
	public bool TryPickMove (IEnumerable<Location> candidates, out Location picked)
	{
		IEnumerable<Location> moves =
			(from d in candidates
			 let heat = this [d]
			 where heat != 0
			 group d by heat into g
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
			short value = this [loc];

			if (value != 0) {
				this [loc] = GetReducedValue (value, amount);
			}
		}
	}

	/// <summary>
	/// This delegate type is used to for the function that determines
	/// which cells are 'next to' a location; we spread head into these
	/// cells.
	/// 
	/// As an optimization, this function places its results in a collection
	/// you provide; we can then reuse this collection efficiently and avoid
	/// allocations.
	/// </summary>
	public delegate void AdjacencySelector (Location where, ICollection<Location> adjacentLocations);

	/// <summary>
	/// This returns a heatmap that has had its heat values propagated;
	/// each cell in the new heatmap has a value that is the maximum of
	/// its old value, and one less than the values of the adjacent cells.
	/// 
	/// We can't easily do this in place, so this returns a new heatmap that
	/// has been updated.
	/// 
	/// You provide a delegate that provides the locaitons 'adjacent' to 
	/// any given position; we call this to figure out where to spread heat to.
	/// 
	/// As an optimization, you can return a shared buffer from this call;
	/// we will not hold into the array or use it again after making a second
	/// call to 'adjacency'. The the vast majority of locaitons have 4
	/// neighbors, but occasional 'door' cells have more.
	///
	/// Cells like walls may be omitted from the adjacency result; we return
	/// an ArraySegment so the buffer can still be shared.
	/// 
	/// You also provide a delegate that indicates which cells are passable;
	/// impassible cells don't get heated, which can block the spread of
	/// heat through the map. This lets us skep locations returned by
	/// 'adjacency' without needing to allocate a smaller array.
	/// </summary>
	public void Heat (AdjacencySelector adjacency)
	{
		var heater = new Heater (adjacency);
		heater.Heat (this);
	}

	/// <summary>
	/// This method applies multiple rounds of heat; as many as 'repeat' indicated.
	/// this returns a new heatmap containing the result.
	/// </summary>
	public void Heat (int repeats, AdjacencySelector adjacency)
	{
		var heater = new Heater (adjacency);

		for (int i = 0; i < repeats; ++i) {
			if (!heater.Heat (this)) {
				// If heating did nothing, heating again won't either,
				// so we can just bail.
				return;
			}
		}
	}

	/// <summary>
	/// This structure is a utility to make heatmap heatings faster;
	/// this holds onto various buffers so they can be reused, and
	/// caches passability data.
	/// </summary>
	private struct Heater
	{
		private readonly AdjacencySelector adjacency;
		private readonly List<Location> adjacencyBuffer;
		private readonly List<KeyValuePair<Location, short>> updates;

		public Heater (AdjacencySelector adjacency)
		{
			this.adjacency = adjacency;
			this.adjacencyBuffer = new List<Location> (6);
			updates = new List<KeyValuePair<Location, short>> ();
		}
	
		/// <summary>
		/// Heat() applies heat to the heatmap given. The resulting
		/// values are queued and applied only at the end, so
		/// the order of changes is not signficiant.
		/// 
		/// This method returns true if it found any changes to make,
		/// and false if it did nothing.
		/// </summary>
		public bool Heat (Heatmap heatmap)
		{
			updates.Clear ();
		
			foreach (Location srcLoc in heatmap.Locations ()) {
				short min = GetReducedValue (heatmap [srcLoc], 1);
			
				if (min != 0) {
					adjacencyBuffer.Clear ();
					adjacency (srcLoc, adjacencyBuffer);

					foreach (Location adj in adjacencyBuffer) {
						short oldValue = heatmap [adj];
						short newValue = IncreaseValue (oldValue, min);
						
						if (oldValue != newValue) {
							updates.Add (new KeyValuePair<Location, short> (adj, newValue));
						}
					}
				}
			}
		
			foreach (KeyValuePair<Location, short> update in updates)
				heatmap [update.Key] = update.Value;

			return updates.Count > 0;
		}
	}

	#endregion

	#region Value Arithmetic

	/// <summary>
	/// This increases Value so it is no less than 'minimum';
	/// this is judged on the absolute value, so a large negatve
	/// value may be 'greater' than a small positive one.
	/// </summary>
	private static short IncreaseValue (short current, short minimum)
	{
		if (Math.Abs (current) < Math.Abs (minimum))
			return  minimum;
		else
			return current;
	}

	/// <summary>
	/// GetReducedValue() computes the value that reducing this
	/// cell would produce, without actually updating the Value.
	/// </summary>
	private static short GetReducedValue (short current, int amount)
	{
		if (current > 0) {
			return (short)Math.Max (0, current - amount);
		} else if (current < 0) {
			return (short)Math.Min (0, current + amount);
		} else {
			return 0;
		}
	}

	#endregion
}