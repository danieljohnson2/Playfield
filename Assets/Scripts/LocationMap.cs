using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This is a collection that stores a value for a location; this
/// allocates chunky storage to make this more efficient than a
/// straight directionary would be. On the eflip side, this class
/// does not record precisely which cell have been set; it knows
/// that cells have the default value for their type until set,
/// but allocation one cell allocations many neighboring cells,
/// so enumerating once of these maps hits many extra cells.
/// </summary>
public class LocationMap<T> : IEnumerable<KeyValuePair<Location, T>>
{	
	private const int blockSize = 16;
	private const int locationLocalMask = 0xF;
	private const int locationKeyMask = ~locationLocalMask;
	private readonly Dictionary<Location, T[,]> blocks = new Dictionary<Location, T[,]> ();
		
	public LocationMap ()
	{
	}

	public LocationMap (IEnumerable<KeyValuePair<Location,T>> source)
	{
		var srcMap = source as LocationMap<T>;

		if (srcMap != null) {
			foreach (KeyValuePair<Location, T[,]> pair in srcMap.blocks) {
				blocks.Add (pair.Key, (T[,])pair.Value.Clone ());
			}
		} else {
			foreach (KeyValuePair<Location, T> pair in source) {
				this [pair.Key] = pair.Value;
			}
		}
	}

	/// <summary>
	/// This indexer accesses an individual value for a location; you can
	/// set any location's value; unset values are 0 by default.
	/// </summary>
	public T this [Location location] {
		get { return GetCell (location, createIfMissing: false).Value; }
		
		set { 
			Cell cell = GetCell (location, createIfMissing: true);
			
			if (cell.IsValid) {
				cell.Value = value;
			}
		}
	}
	
	/// <summary>
	/// This method yields every individual location that
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
	/// This method removes all data from the map.
	/// </summary>
	public void Clear ()
	{
		blocks.Clear ();
	}

	/// <summary>
	/// This method finds any unused blocks and discards
	/// them. This is a slow method since it has to check
	/// each value, but short of Clear(), this is the only
	/// way to reclaim space.
	/// </summary>
	public void TrimExcess ()
	{
		var keysToRemove = new List<Location> ();
		bool anyKept = false;

		foreach (KeyValuePair<Location, T[,]> pair in blocks) {
			if (IsBlockEmpty (pair.Value)) {
				keysToRemove.Add (pair.Key);
			} else {
				anyKept = true;
			}
		}

		if (!anyKept) {
			blocks.Clear ();
		} else {
			foreach (Location key in keysToRemove) {
				blocks.Remove (key);
			}
		}
	}

	/// <summary>
	/// This method returns true if block contains only
	/// default values, using the default equality comparison
	/// to check.
	/// </summary>
	private static bool IsBlockEmpty (T[,] block)
	{
		var comparer = EqualityComparer<T>.Default;

		foreach (T t in block) {
			if (!comparer.Equals (t, default(T))) {
				return false;
			}
		}

		return true;
	}

	#region Cell Access
	
	/// <summary>
	/// Returns a cell structure that describes a specific location; if createIfMissing
	/// is true this will allocate storage for the location; if false it won't, and may return
	/// an invalid cell if there's no storage for the cell.
	/// </summary>
	private Cell GetCell (Location location, bool createIfMissing)
	{
		var key = new Location (location.x & locationKeyMask, location.y & locationKeyMask, location.mapIndex);
		T[,] block;
		
		if (!blocks.TryGetValue (key, out block) && createIfMissing) {
			block = new T[blockSize, blockSize];
			blocks.Add (key, block);
		}
		
		return new Cell (block, location.x & locationLocalMask, location.y & locationLocalMask);
	}
	
	/// <summary>
	/// This structure acts as a handle on a single location in the
	/// heapmap; it retains a refernce to the storage array and
	/// the position in that array; a default Cell has no array
	/// and is 'invalid'; it reads a default value but can't be
	/// written to.
	/// </summary>
	private struct Cell
	{
		private readonly T[,] block;
		private readonly int x, y;
		
		public Cell (T[,] block, int x, int y)
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
		public T Value {
			get { return IsValid ? block [x, y] : default(T); }
			set { 
				if (block != null)
					block [x, y] = value;
				else {
					throw new InvalidOperationException ();
				}
			}
		}
	}
	
	#endregion

	#region IEnumerable implementation

	public IEnumerator<KeyValuePair<Location, T>> GetEnumerator ()
	{
		foreach (KeyValuePair<Location, T[,]> pair in blocks) {
			for (int ly = 0; ly < blockSize; ++ly) {
				for (int lx = 0; lx < blockSize; ++lx) {
					Location key = pair.Key.WithOffset(lx, ly);
					yield return new KeyValuePair<Location, T> (key, pair.Value [lx, ly]);
				}
			}
		}
	}

	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
	{
		return GetEnumerator ();
	}

	#endregion
}