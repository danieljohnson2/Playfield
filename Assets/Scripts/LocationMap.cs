using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This is a collection that stores a value for a location; this
/// allocates chunky storage to make this more efficient than a
/// straight directionary would be. On the flip side, this class
/// does not record precisely which cell have been set; it knows
/// that cells have the default value for their type until set,
/// but allocation one cell allocations many neighboring cells,
/// so enumerating once of these maps hits many extra cells.
/// 
/// This class stores the cell data in 64x64 chunks, but these are
/// kept in 1 dimensional arrays for performance reasons.
/// </summary>
public class LocationMap<T> : IEnumerable<KeyValuePair<Location, T>>
{
    public const int blockSize = 64;
    private const int locationYShift = 6;
    private const int locationLocalMask = 0x3F;
    private const int locationKeyMask = ~locationLocalMask;
    private readonly Dictionary<Location, T[]> blocks = new Dictionary<Location, T[]>();

    public LocationMap()
    {
    }

    public LocationMap(IEnumerable<KeyValuePair<Location, T>> source)
    {
        var srcMap = source as LocationMap<T>;

        if (srcMap != null)
        {
            foreach (KeyValuePair<Location, T[]> pair in srcMap.blocks)
                blocks.Add(pair.Key, (T[])pair.Value.Clone());
        }
        else
        {
            foreach (KeyValuePair<Location, T> pair in source)
                this[pair.Key] = pair.Value;
        }
    }

    private static int GetBlockIndex(Location location)
    {
        int bx = location.x & locationLocalMask;
        int by = location.y & locationLocalMask;

        return by << locationYShift | bx;
    }

    /// <summary>
    /// This indexer accesses an individual value for a location; you can
    /// set any location's value; unset values are 0 by default.
    /// </summary>
    public T this[Location location]
    {
        get
        {
            Location key = new Location(location.x & locationKeyMask, location.y & locationKeyMask, location.mapIndex);
            T[] block;

            if (blocks.TryGetValue(key, out block))
                return block[GetBlockIndex(location)];
            else
                return default(T);
        }

        set
        {
            Location key = new Location(location.x & locationKeyMask, location.y & locationKeyMask, location.mapIndex);
            T[] block;

            if (!blocks.TryGetValue(key, out block))
            {
                block = new T[blockSize * blockSize];
                blocks.Add(key, block);
            }

            block[GetBlockIndex(location)] = value;
        }
    }

    /// <summary>
    /// This method yields every individual location that
    /// might have a non-zero value; this doesn't check each
    /// value, so some zero locations will be returned- but
    /// the set of locations returned is always finite.
    /// </summary>
    public IEnumerable<Location> Locations()
    {
        foreach (Location key in blocks.Keys)
        {
            for (int ly = 0; ly < blockSize; ++ly)
            {
                for (int lx = 0; lx < blockSize; ++lx)
                {
                    yield return key.WithOffset(lx, ly);
                }
            }
        }
    }

    /// <summary>
    /// This method removes all data from the map.
    /// </summary>
    public void Clear()
    {
        blocks.Clear();
    }
    
    /// <summary>
    /// ForEachBlock() provides a higher speed way to access the values
    /// of the map; this gives you each storage arrage and the location
    /// of the upper-left cell only.
    /// 
    /// Mono seems to be slow for 2d arrays, so we use a 1 day array
    /// arranged in row-major order. This means to move one column right
    /// you just go to the next index; to go one row down, you must
    /// add 'blockSize', to skip over a whole row.
    /// </summary>
    public void ForEachBlock(Action<Location, T[]> action)
    {
        foreach (KeyValuePair<Location, T[]> pair in blocks)
            action(pair.Key, pair.Value);
    }

    /// <summary>
    /// This method finds any blocks and discards it if it
    /// passes a predicate. This can be used to discard unused
    /// section of data and can reclaim space.
    /// </summary>
    public void RemoveBlocksWhere(Func<T[], bool> predicate)
    {
        var keysToRemove = new List<Location>();
        bool anyKept = false;

        foreach (KeyValuePair<Location, T[]> pair in blocks)
        {
            if (predicate(pair.Value))
                keysToRemove.Add(pair.Key);
            else
                anyKept = true;
        }
        
        if (!anyKept)
        {
            blocks.Clear();
        }
        else
        {
            foreach (Location key in keysToRemove)
                blocks.Remove(key);
        }
    }
    
    #region IEnumerable implementation

    public IEnumerator<KeyValuePair<Location, T>> GetEnumerator()
    {
        foreach (KeyValuePair<Location, T[]> pair in blocks)
        {
            int index = 0;
            for (int ly = 0; ly < blockSize; ++ly)
            {
                for (int lx = 0; lx < blockSize; ++lx)
                {
                    Location key = pair.Key.WithOffset(lx, ly);
                    yield return new KeyValuePair<Location, T>(key, pair.Value[index]);
                    ++index;
                }
            }
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion
}