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
	public void Heat (Func<Location, bool> passability)
	{
		var heater = new Heater (passability);
		heater.Heat (this);
	}

	/// <summary>
	/// This method applies multiple rounds of heat; as many as 'repeat' indicated.
	/// this returns a new heatmap containing the result.
	/// </summary>
	public void Heat (int repeats, Func<Location, bool> passability)
	{
		var heater = new Heater (passability);

		for (int i = 0; i < repeats; ++i) {
			heater.Heat (this);
		}
	}

	/// <summary>
	/// This structure is a utility to make heatmap heatings faster;
	/// this holds onto various buffers so they can be reused, and
	/// caches passability data.
	/// </summary>
	private struct Heater
	{
		private readonly Location[] adjacentBuffer;
		private readonly LocationPredicateCache passability;
		private readonly List<KeyValuePair<Location, short>> updates;

		public Heater (Func<Location, bool> passability)
		{
			this.adjacentBuffer = new Location[4];
			this.passability = new LocationPredicateCache (passability);
			this.updates = new List<KeyValuePair<Location, short>> ();
		}
	
		/// <summary>
		/// Heat() applies heat to the heatmap given. The resulting
		/// values are queued and applied only at the end, so
		/// the order of changes is not signficiant.
		/// </summary>
		public void Heat (Heatmap heatmap)
		{
			updates.Clear ();
		
			foreach (Location srcLoc in heatmap.Locations ()) {
				short min = GetReducedValue (heatmap [srcLoc], 1);
			
				if (min != 0) {
					srcLoc.GetAdjacent (adjacentBuffer);
					foreach (Location adj in adjacentBuffer) {
						if (passability.GetOrCreate (adj)) {
							short oldValue = heatmap [adj];
							short newValue = IncreaseValue (oldValue, min);
						
							if (oldValue != newValue) {
								updates.Add (new KeyValuePair<Location, short> (adj, newValue));
							}
						}
					}
				}
			}
		
			foreach (KeyValuePair<Location, short> update in updates)
				heatmap [update.Key] = update.Value;
		}
	}

	/// <summary>
	/// This acts as a cache for the results of the passability
	/// predicate; this makes a big difference as we query each
	/// location's passability many times and it is slow to
	/// access the underlying game objects.
	/// </summary>
	private sealed class LocationPredicateCache : LocationMap<bool?>
	{
		private readonly Func<Location, bool> predicate;

		public LocationPredicateCache (Func<Location, bool> predicate)
		{
			this.predicate = predicate;
		}

		public bool GetOrCreate (Location where)
		{
			bool? b = this [where];

			if (b == null) {
				bool c = predicate (where);
				this [where] = c;
				return c;
			} else {
				return b.Value;
			}
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