using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

/// <summary>
/// This class holds a heat value for each cell in your map;
/// critters can then move towards the hotter cells. This uses
/// a spare storage strategy, so you can set any cell's value and
/// not sweat the memory wastage.
/// </summary>
public sealed class Heatmap : LocationMap<Heatmap.Slot>
{
    public Heatmap()
    {
        this.name = "";
    }

    public Heatmap(IEnumerable<KeyValuePair<Location, Slot>> source) : base(source)
    {
        this.name = "";
    }

    /// <summary>
    /// This name has no effect on heatmap behavior, but
    /// can be used to idenitfy it.
    /// </summary>
    public string name { get; set; }

    /// <summary>
    /// Picks the best move from the candidates given; that is, it picks
    /// the cell with the largest heat value. If a tie for hottest cell is
    /// found, this picks one of the best randomly. This method will not
    /// pick a location whose heat is 0.
    /// 
    /// This method returns false if all candiates have a heat of 0, or if there
    /// are no candidates at all. Otherwise, it places the result in 'picked' and returns true.
    /// </summary>
    public bool TryPickMove(IEnumerable<Location> candidates, out Location picked)
    {
        IEnumerable<Location> moves =
            (from d in candidates
             let heat = this[d].heat
             where heat != 0
             group d by heat into g
             orderby g.Key descending
             select g).FirstOrDefault();

        if (moves != null)
        {
            int index = UnityEngine.Random.Range(0, moves.Count());
            picked = moves.ElementAt(index);
            return true;
        }
        else
        {
            picked = new Location();
            return false;
        }
    }

    /// <summary>
    /// This returns a crude string representation of the heatmap content;
    /// this is not very good or fast and should be used for debugging only.
    /// </summary>
    public override string ToString()
    {
        var byMap = Locations().GroupBy(l => l.mapIndex);
        var b = new StringBuilder();

        foreach (var grp in byMap)
        {
            b.AppendLine("Map #: " + grp.Key);

            int minX = grp.Min(loc => loc.x);
            int minY = grp.Min(loc => loc.y);
            int maxX = grp.Max(loc => loc.x);
            int maxY = grp.Max(loc => loc.y);

            for (int y = minY; y <= maxY; ++y)
            {
                for (int x = minX; x <= maxX; ++x)
                {
                    short heat = this[new Location(x, y, grp.Key)].heat;
                    b.AppendFormat("{0:X2}", Math.Abs(heat));
                }
                b.AppendLine();
            }
        }

        return b.ToString().Trim();
    }

    #region Heat and Cool

    /// <summary>
    /// TrimExcess() removes blocks from the heatmap that
    /// contain only 0 heat values.
    /// </summary>
    public void TrimExcess()
    {
        RemoveBlocksWhere(delegate (Slot[] slots)
        {
            foreach (Slot slot in slots)
                if (slot.heat != 0)
                    return false;

            return true;
        });
    }

    /// <summary>
    /// This method reduces every value by 'amount', but won't flip the sign
    /// of any value- it stops at 0. Cells that contain 0 will not be changed.
    /// 
    /// This method updates the heatmap in place.
    /// </summary>
    public void Reduce(int amount = 1)
    {
        ForEachBlock(delegate (Location upperLeft, Slot[] slots)
        {
            for (int i = 0; i < slots.Length; ++i)
                if (slots[i].heat != 0)
                    slots[i] = slots[i].ToReduced(amount);
        });
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
    public delegate void AdjacencySelector(Location where, ICollection<Location> adjacentLocations);

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
    public void Heat(AdjacencySelector adjacency)
    {
        var heater = new Heater(adjacency);
        heater.Heat(this);
    }

    /// <summary>
    /// This method applies multiple rounds of heat; as many as 'repeat' indicated.
    /// this returns a new heatmap containing the result.
    /// </summary>
    public void Heat(int repeats, AdjacencySelector adjacency)
    {
        var heater = new Heater(adjacency);

        for (int i = 0; i < repeats; ++i)
        {
            if (!heater.Heat(this))
            {
                // If heating did nothing, heating again won't either,
                // so we can just bail.
                break;
            }
        }
    }

    /// <summary>
    /// This structure is a utility to make heatmap heatings faster;
    /// this holds onto various buffers so they can be reused, and
    /// caches passability data.
    /// </summary>
    private sealed class Heater
    {
        private readonly AdjacencySelector adjacency;
        private readonly List<Location> adjacencyBuffer;

        public Heater(AdjacencySelector adjacency)
        {
            this.adjacency = adjacency;
            this.adjacencyBuffer = new List<Location>(6);
        }

        /// <summary>
        /// Heat() applies heat to the heatmap given. The resulting
        /// values are queued and applied only at the end, so
        /// the order of changes is not signficiant.
        /// 
        /// This method returns true if it found any changes to make,
        /// and false if it did nothing.
        /// </summary>
        public bool Heat(Heatmap heatmap)
        {
            bool changed = false;

            var original = new LocationMap<Slot>(heatmap);

            original.ForEachBlock(delegate (Location upperLeft, Slot[] slots)
            {
                int width = LocationMap<Slot>.blockSize;
                int height = LocationMap<Slot>.blockSize;

                int slotIndex = 0;

                for (int ly = 0; ly < height; ++ly)
                {
                    for (int lx = 0; lx < width; ++lx)
                    {
                        if (slots[slotIndex].heat != 0)
                        {
                            Slot min = slots[slotIndex].ToReduced(1);

                            if (min.heat != 0)
                            {
                                Location srcLoc = upperLeft.WithOffset(lx, ly);
                                adjacencyBuffer.Clear();
                                adjacency(srcLoc, adjacencyBuffer);

                                foreach (Location adj in adjacencyBuffer)
                                {
                                    Slot oldSlot = original[adj];
                                    Slot newSlot = Slot.Max(oldSlot, min);

                                    if (!oldSlot.Equals(newSlot))
                                    {
                                        heatmap[adj] = newSlot;
                                        changed = true;
                                    }
                                };
                            }
                        }

                        ++slotIndex;
                    }
                }
            });

            return changed;
        }
    }

    #endregion

    /// <summary>
    /// This structure describes the data held in one cell of
    /// the heatmap.
    /// </summary>
    public struct Slot : IEquatable<Slot>
    {
        public readonly SourceInfo source;
        public readonly short heat;

        public Slot(UnityEngine.GameObject source, short heat) :
            this(new SourceInfo(source), heat)
        {
        }

        public Slot(SourceInfo source, short heat)
        {
            this.source = source;
            this.heat = heat;
        }

        public override string ToString()
        {
            if (source == null)
                return heat.ToString();
            else
                return string.Format("{0} (from {1})", heat, source);
        }

        /// <summary>
        /// This method returns the slot (of the two given) that has the
        /// larger heat value. We use absolute value, so a large negatve
        /// value may be 'greater' than a small positive one.
        /// </summary>
        public static Slot Max(Slot left, Slot right)
        {
            if (Math.Abs(left.heat) < Math.Abs(right.heat))
                return right;
            else
                return left;
        }

        /// <summary>
        /// This method computes a new slot, by reduign this one.
        /// </summary>
        public Slot ToReduced(int amount)
        {
            short newHeat = heat;

            if (heat > 0)
                newHeat = (short)Math.Max(0, heat - amount);
            else if (heat < 0)
                newHeat = (short)Math.Min(0, heat + amount);
            else
                newHeat = 0;

            if (newHeat == 0)
                return new Slot();
            else
                return new Slot(source, newHeat);
        }

        #region IEquatable implementation

        public bool Equals(Slot other)
        {
            return
                this.heat == other.heat &&
                EqualityComparer<SourceInfo>.Default.Equals(this.source, other.source);
        }

        public override bool Equals(object obj)
        {
            return obj is Slot && Equals((Slot)obj);
        }

        public override int GetHashCode()
        {
            return heat.GetHashCode();
        }

        #endregion
    }

    /// <summary>
    /// This class holds the identifying data for the source of
    /// the heat in the heatmap; each cell contains a refertence to
    /// an immutable source-info, which can then be shared by many
    /// cells.
    /// 
    /// We cannot just use the GameObject directly here; it might be
    /// destroyed while the heatmap is still in use. So we copy what
    /// we need.
    /// </summary>
    public sealed class SourceInfo : IEquatable<SourceInfo>
    {
        public SourceInfo(UnityEngine.GameObject source)
        {
            this.Name = source.name;
            this.Tag = source.tag;

            var ic = source.GetComponent<ItemController>();
            CreatureController carrier;
            if (ic == null)
                Disposition = null;
            else if (!ic.TryGetCarrier(out carrier))
                Disposition = SourceDisposition.Exposed;
            else if (ic.isHeldItem)
                Disposition = SourceDisposition.Held;
            else
                Disposition = SourceDisposition.Carried;
        }

        public SourceInfo(string name, string tag, SourceDisposition? disposition)
        {
            this.Name = name;
            this.Tag = tag;
            this.Disposition = disposition;
        }

        public string Name { get; private set; }
        public string Tag { get; private set; }
        public SourceDisposition? Disposition { get; private set; }

        public override string ToString()
        {
            return string.Format("{0} {1}", Name, Disposition).Trim();
        }

        #region IEquatable implementation

        public bool Equals(SourceInfo other)
        {
            if (this == other)
            {
                return true;
            }

            return
                other != null &&
                this.Name == other.Name &&
                this.Tag == other.Tag;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SourceInfo);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        #endregion
    }

    /// <summary>
    /// SourceDisposition indicates whether the source item is
    /// being carried or held; does not apply to anything but items.
    /// </summary>
    public enum SourceDisposition
    {
        Exposed,
        Carried,
        Held
    }
}