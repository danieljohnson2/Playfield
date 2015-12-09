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
    private readonly Dictionary<Location, T[,]> blocks = new Dictionary<Location, T[,]>();

    public LocationMap()
    {
    }

    public LocationMap(IEnumerable<KeyValuePair<Location, T>> source)
    {
        var srcMap = source as LocationMap<T>;

        if (srcMap != null)
        {
            foreach (KeyValuePair<Location, T[,]> pair in srcMap.blocks)
                blocks.Add(pair.Key, (T[,])pair.Value.Clone());
        }
        else
        {
            foreach (KeyValuePair<Location, T> pair in source)
                this[pair.Key] = pair.Value;
        }
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
            T[,] block;

            if (blocks.TryGetValue(key, out block))
                return block[location.x & locationLocalMask, location.y & locationLocalMask];
            else
                return default(T);
        }

        set
        {
            Location key = new Location(location.x & locationKeyMask, location.y & locationKeyMask, location.mapIndex);
            T[,] block;

            if (!blocks.TryGetValue(key, out block))
            {
                block = new T[blockSize, blockSize];
                blocks.Add(key, block);
            }

            block[location.x & locationLocalMask, location.y & locationLocalMask] = value;
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

    public delegate void Transformer(ref T value);

    /// <summary>
    /// TransformValues()  applies a delegate to each value
    /// for which we have a key (ie, those corresponding to
    /// Locations(). the transformer can update the stored
    /// value directly.
    /// </summary>
    public void TransformValues(Transformer transformer)
    {
        foreach (T[,] array in blocks.Values)
        {
            for (int ly = 0; ly < blockSize; ++ly)
                for (int lx = 0; lx < blockSize; ++lx)
                    transformer(ref array[lx, ly]);
        }
    }

    /// <summary>
    /// ForEachBlock() provides a higher speed way to access the values
    /// of the map; this gives you each storage arrage and hte location
    /// of the upper-left cell only.
    /// </summary>
    public void ForEachBlock(Action<Location, T[,]> action)
    {
        foreach (KeyValuePair<Location, T[,]> pair in blocks)
            action(pair.Key, pair.Value);
    }

    /// <summary>
    /// This method finds any unused blocks and discards
    /// them. This is a slow method since it has to check
    /// each value, but short of Clear(), this is the only
    /// way to reclaim space.
    /// </summary>
    public void TrimExcess()
    {
        var keysToRemove = new List<Location>();
        bool anyKept = false;

        foreach (KeyValuePair<Location, T[,]> pair in blocks)
        {
            if (IsBlockEmpty(pair.Value))
            {
                keysToRemove.Add(pair.Key);
            }
            else
            {
                anyKept = true;
            }
        }

        if (!anyKept)
        {
            blocks.Clear();
        }
        else
        {
            foreach (Location key in keysToRemove)
            {
                blocks.Remove(key);
            }
        }
    }

    /// <summary>
    /// This method returns true if block contains only
    /// default values, using the default equality comparison
    /// to check.
    /// </summary>
    private static bool IsBlockEmpty(T[,] block)
    {
        var comparer = EqualityComparer<T>.Default;

        foreach (T t in block)
        {
            if (!comparer.Equals(t, default(T)))
            {
                return false;
            }
        }

        return true;
    }

    #region IEnumerable implementation

    public IEnumerator<KeyValuePair<Location, T>> GetEnumerator()
    {
        foreach (KeyValuePair<Location, T[,]> pair in blocks)
        {
            for (int ly = 0; ly < blockSize; ++ly)
            {
                for (int lx = 0; lx < blockSize; ++lx)
                {
                    Location key = pair.Key.WithOffset(lx, ly);
                    yield return new KeyValuePair<Location, T>(key, pair.Value[lx, ly]);
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