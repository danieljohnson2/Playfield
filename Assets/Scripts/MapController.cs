using UnityEngine;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/// <summary>
/// This class loads the map and places all game-objects; it keeps track of
/// what it creates so we can find them again more efficiently, without
/// depending upon tags.
/// 
/// There can be only one of these for the entire game; it loads and
/// tracks the maps and active entities.
/// </summary>
public class MapController : MonoBehaviour
{
    public TranscriptController transcript;
    public InventoryDisplayController inventoryDisplay;

    void Start()
    {
        foreach (Map map in maps.Maps())
        {
            terrain.Load(map);
        }

        activeMap = maps[0];

        if (nextLevelInitializer != null)
        {
            Action<MapController> init = nextLevelInitializer;
            nextLevelInitializer = null;
            init(this);
        }

        entities.ProcessRemovals();
        entities.ActivateEntities();
        entities.ActivateMapContainers();

        foreach (var cc in entities.Components<CreatureController>())
            cc.UpdateHeldItem();

        StartCoroutine(ExecuteTurns());
    }

    /// <summary>
    /// This co-routine runs while the game plays and lets each
    /// creature take its turn- including the player.
    /// </summary>
    private IEnumerator ExecuteTurns()
    {
        var hasMoved = new HashSet<GameObject>();
        bool playerFound;
        do
        {
            entities.ActivateEntities();

            playerFound = false;

            foreach (var cc in entities.Components<CreatureController>().ToArray())
            {
                bool isPlayer = cc is PlayerController;

                if (isPlayer && inventoryDisplay != null)
                {
                    inventoryDisplay.UpdateInventoryFrom(cc.gameObject);
                }

                if (!playerFound && isPlayer)
                {
                    playerFound = true;
                    activeMap = maps[Location.Of(cc.gameObject).mapIndex];
                }

                if (cc.CheckTurn())
                {
                    if (isPlayer)
                    {
                        hasMoved.Clear();
                    }
                    else if (Location.Of(cc.gameObject).mapIndex == activeMap.mapIndex)
                    {
                        // When a creature moves where we can see it, and it double-moves
                        // the player, we add a short delay to make it not look flickery.
                        if (!hasMoved.Add(cc.gameObject))
                        {
                            // we never want to double up delays though.
                            hasMoved.Clear();
                            yield return new WaitForSeconds(1f / 16f);
                        }
                    }

                    yield return StartCoroutine(cc.DoTurnAsync());

                    if (nextLevelInitializer != null)
                        break;
                }
            }

            entities.ProcessRemovals();
        } while (playerFound && nextLevelInitializer == null);

        if (nextLevelInitializer != null)
        {
            lazyInstance = null;
            Application.LoadLevel(Application.loadedLevel);
        }
    }

    #region Singleton Instance

    private static MapController lazyInstance;
    private static Action<MapController> nextLevelInitializer;

    /// <summary>
    /// This property returns the map controller; this
    /// is a singleton that is cached here on first
    /// access.
    /// </summary>
    public static MapController instance
    {
        get
        {
            return Lazy.Init(ref lazyInstance, delegate
            {
                var go = GameObject.FindGameObjectWithTag("GameController");

                if (go == null)
                {
                    throw new System.InvalidOperationException("GameController could not be found.");
                }

                var mc = go.GetComponent<MapController>();

                if (mc == null)
                {
                    throw new System.InvalidOperationException("MapController could not be found.");
                }

                return mc;
            });
        }
    }

    /// <summary>
    /// ReloadWithInitialization() reloads the level, but you provide a delegate
    /// to run on the new map before the game starts. We use this to load saved
    /// games.
    /// 
    /// Calling this will cause the ExecuteTurn loop to exit first, before the reload,
    /// so none of the old entities remain active.
    /// </summary>
    public static void ReloadWithInitialization(Action<MapController> initializer)
    {
        nextLevelInitializer = initializer;
    }

    #endregion

    #region Game Over

    /// <summary>
    /// GameOver() ends the game over a short display; if you supply
    /// a message we will display it, but if you don't we display
    /// the last line of the transcript.
    /// </summary>
    public void GameOver(string message = null, float delay = 2.0f)
    {
        GameOverController.gameOverMessage = message ?? transcript.Lines().LastOrDefault();
    
        Invoke("ExecuteGameOver", delay);
    }

    void ExecuteGameOver()
    {
        GameOverController.GameOver();
    }

    #endregion

    #region Terrain and Movement

    private Map storedActiveMap;

    /// <summary>
    /// This is the map displayed in the game. This
    /// should be the map containing the player; setting
    /// this loads new maps and activates the entities
    /// within those maps.
    /// </summary>
    public Map activeMap
    {
        get { return storedActiveMap; }

        set
        {
            if (storedActiveMap != value)
            {
                storedActiveMap = value;
                entities.ActivateMapContainers();
            }
        }
    }

    /// <summary>
    /// ComponentsInCell() returns the components fo the type
    /// indicated of all the objects in the location, including
    /// terrain. Warning: the terrain object may be a prefab; we
    /// do not activate a map just to access the real object
    /// for the terrain.
    /// </summary>
    public IEnumerable<T> ComponentsInCell<T>(Location location)
        where T : Component
    {
        GameObject terrainObject = terrain.GetTerrain(location);
        T terrainComponent = terrainObject != null ? terrainObject.GetComponent<T>() : null;

        if (terrainComponent != null)
        {
            return new[] { terrainComponent }.Concat(entities.ComponentsAt<T>(location));
        }
        else
        {
            return entities.ComponentsAt<T>(location);
        }
    }

    /// <summary>
    /// IsPassable is true if the cell indicated by 'where' may be
    /// walked through; it is false if lcoations outside hte map,
    /// but true for cells that contain no movement-blocker objects.
    /// </summary>
    public bool IsPassable(Location where)
    {
        if (terrain.GetTerrain(where) == null)
        {
            return false;
        }

        return ComponentsInCell<MovementBlocker>(where).All(mb => mb.passable);
    }

    /// <summary>
    /// IsPathableFor determines whether heatmaps can make a path through
    /// this cell; it is false outside the map, and may be false for cells
    /// inside depending on what movement blockers are found there.
    /// </summary>
    public bool IsPathableFor(GameObject mover, Location where)
    {
        if (terrain.GetTerrain(where) == null)
        {
            return false;
        }

        return ComponentsInCell<MovementBlocker>(where).All(mb => mb.IsPathableFor(mover));
    }

    #endregion

    #region Terrain Objects

    private TerrainTracker lazyTerrainTracker;

    public TerrainTracker terrain
    {
        get { return Lazy.Init(ref lazyTerrainTracker, () => new TerrainTracker(this)); }
    }

    /// <summary>
    /// This tracker holds the game objects for the terrain; all
    /// terrain cells should be pre-loaded at start up, and this
    /// tracker keeps track of them.
    /// </summary>
    public sealed class TerrainTracker
    {
        private readonly MapController mapController;
        private readonly List<GameObject[,]> terrainMaps = new List<GameObject[,]>();

        public TerrainTracker(MapController mapController)
        {
            this.mapController = mapController;
        }

        public GameObject[,] this[int mapIndex]
        {
            get
            {
                if (mapIndex >= 0 || mapIndex < terrainMaps.Count)
                {
                    GameObject[,] objects = terrainMaps[mapIndex];

                    if (objects != null)
                    {
                        return objects;
                    }
                }

                throw new ArgumentOutOfRangeException("mapIndex", "Invalid map index");
            }
        }

        public GameObject[,] this[Map map]
        {
            get { return this[map.mapIndex]; }
        }

        /// <summary>
        /// Returns the terrain object for a cell, or null if
        /// the location is outside the map entirely.
        /// </summary>
        public GameObject GetTerrain(Location location)
        {
            if (location.mapIndex >= 0)
            {
                GameObject[,] objects = this[location.mapIndex];
                if (location.x >= 0 && location.x < objects.GetLength(0) &&
                    location.y >= 0 && location.y < objects.GetLength(1))
                {
                    return objects[location.x, location.y];
                }
            }

            return null;
        }

        /// <summary>
        /// This method loads a map's data into the tracker; we load
        /// them all at start up.
        /// </summary>
        public void Load(Map map)
        {
            EntityTracker entities = mapController.entities;

            var terrain = new GameObject[map.width, map.height];

            for (int y = 0; y < map.height; ++y)
            {
                for (int x = 0; x < map.width; ++x)
                {
                    Location location = new Location(x, y, map);

                    terrain[x, y] =
                        entities.InstantiateEntities(location, map);
                }
            }

            while (terrainMaps.Count <= map.mapIndex)
            {
                terrainMaps.Add(null);
            }

            terrainMaps[map.mapIndex] = terrain;
        }
    }

    #endregion

    #region Entity Objects

    private EntityTracker lazyEntityTracker;

    public EntityTracker entities
    {
        get { return Lazy.Init(ref lazyEntityTracker, () => new EntityTracker(this)); }
    }

    /// <summary>
    /// This object tracks the active entities, which are created as
    /// we enter rooms and destroyed by components. The player is
    /// one of these.
    /// </summary>
    public sealed class EntityTracker
    {
        private readonly MapController mapController;
        private readonly List<GameObject> entities = new List<GameObject>();
        private readonly Queue<GameObject> pendingRemoval = new Queue<GameObject>();
        private readonly LazyList<GameObject> lazyMapContainers = new LazyList<GameObject>();
        private readonly HashSet<Location> loadedEntityLocations = new HashSet<Location>();
        private ILookup<string, GameObject> lazyByTag, lazyByName;

        public EntityTracker(MapController mapController)
        {
            this.mapController = mapController;
        }

        /// <summary>
        /// This method returns the game object that contains the entities that
        /// came from the map given (even if they later move elsewhere).
        /// </summary>
        public GameObject GetMapContainer(Map map)
        {
            return lazyMapContainers.GetOrCreate(map.mapIndex, delegate
            {
                var c = new GameObject(map.name);
                c.transform.parent = mapController.transform;
                return c;
            });
        }

        /// <summary>
        /// InstantiateEntities() places new entities into the world
        /// and records them in the tracker; we use this when we load rooms.
        /// The tracker won't load the same entity twice, except for terrain-
        /// it doesn't know what to do with terrain, so it creates the
        /// terrain game object and returns it to you to record.
        /// 
        /// This method can return null only if the cell indicated is empty.
        /// 
        /// You provide the map to avoid loading it again, but it must be
        /// the map that contains the location.
        /// </summary>
        public GameObject InstantiateEntities(Location location, Map map)
        {
            if (location.mapIndex != map.mapIndex)
            {
                throw new ArgumentException("InstantiateEntities() must be given the correct map for the location.");
            }

            Map.Cell cell = map[location.x, location.y];
            GameObject mapContainer = GetMapContainer(map);
            GameObject terrain = cell.InstantiateTerrain(location, mapContainer);

            if (loadedEntityLocations.Add(location))
            {
                entities.AddRange(cell.InstantiateEntities(location, mapContainer));
                ClearEntityCaches();
            }

            return terrain;
        }

        /// <summary>
        /// This creates an entity by instantiating its prefab; it's placed
        /// in the location indicated and registered with this tracker.
        /// </summary>
        public GameObject InstantiateEntity(GameObject prefab, Location location)
        {
            Map map = mapController.maps[location.mapIndex];

            GameObject created = GameObject.Instantiate(prefab);
            created.transform.parent = GetMapContainer(map).transform;
            created.transform.localPosition = location.ToPosition();
            entities.Add(created);
            ClearEntityCaches();
            return created;
        }

        /// <summary>
        /// RemoveEntity() causes the entity given to be removed
        /// from the game at end of turn; it is destroyed and
        /// also unregistered at tha time.
        /// </summary>
        public void RemoveEntity(GameObject toRemove)
        {
            if (toRemove != null)
            {
                pendingRemoval.Enqueue(toRemove);
            }
        }

        /// <summary>
        /// Actually performs the removals RemoveEntity() schedules;
        /// this is called at end of turn.
        /// </summary>
        public void ProcessRemovals()
        {
            if (pendingRemoval.Count > 0)
            {
                while (pendingRemoval.Count > 0)
                {
                    GameObject toRemove = pendingRemoval.Dequeue();

                    mapController.adjacencyGenerator.InvalidatePathability(toRemove);
                    mapController.adjacencyGenerator.InvalidatePathability(Location.Of(toRemove));

                    entities.Remove(toRemove);
                    Destroy(toRemove);
                }

                ClearEntityCaches();
            }
        }

        /// <summary>
        /// Entities() yields all entities.
        /// </summary>
        public IEnumerable<GameObject> Entities()
        {
            return entities;
        }

        /// <summary>
        /// This property returns a lookup containing every
        /// entity game object, keyed by their tag. This
        /// lookup is replaced when entities are instantiated
        /// or removed.
        /// </summary>
        public ILookup<string, GameObject> byTag
        {
            get { return Lazy.Init(ref lazyByTag, () => entities.ToLookup(e => e.tag)); }
        }

        /// <summary>
        /// This property returns a lookup containing every
        /// entity game object, keyed by their name. This
        /// lookup is replaced when entities are instantiated
        /// or removed.
        /// </summary>
        public ILookup<string, GameObject> byName
        {
            get { return Lazy.Init(ref lazyByName, () => entities.ToLookup(e => e.name)); }
        }

        /// <summary>
        /// This method yields the component of type 'T'
        /// every active entity.
        /// </summary>
        public IEnumerable<T> Components<T>()
            where T : Component
        {
            return
                from go in entities
                select go.GetComponent<T>() into c
                where c != null
                select c;
        }

        /// <summary>
        /// This yields the components of the type 'T' of the
        /// entties, but only entities in the locaiton given
        /// are considered; terrain is also not considered.
        /// </summary>
        public IEnumerable<T> ComponentsAt<T>(Location location)
            where T : Component
        {
            return
                from go in entities
                where location == Location.Of(go)
                select go.GetComponent<T>() into c
                where c != null
                select c;
        }

        /// <summary>
        /// ActivateEntities() places each entity into the conrrect
        /// map container.
        /// 
        /// This is called at end of turn, but also when an entity
        /// might have moved between maps.
        /// </summary>
        public void ActivateEntities()
        {
            foreach (GameObject go in entities)
            {
                Location loc = Location.Of(go);

                if (loc.mapIndex >= 0)
                {
                    Map map = mapController.maps[loc.mapIndex];
                    GameObject mapContainer = GetMapContainer(map);
                    go.transform.parent = mapContainer.transform;
                    go.SetActive(true);
                }
                else
                {
                    go.SetActive(false);
                }
            }
        }

        /// <summary>
        /// This sets the active flags on the map container
        /// objects; we call this when the active map changes.
        /// </summary>
        public void ActivateMapContainers()
        {
            Map activeMap = mapController.activeMap;

            foreach (Map map in mapController.maps.Maps())
            {
                GameObject mapContainer = GetMapContainer(map);
                mapContainer.SetActive(map == activeMap);
            }
        }

        /// <summary>
        /// ClearEntityCaches() discards the cached lookups we keep
        /// in case they have changed; they'll be regenerated on demand.
        /// </summary>
        private void ClearEntityCaches()
        {
            lazyByTag = null;
            lazyByName = null;
        }
    }

    #endregion

    #region Map Tracking

    public MapTracker maps;

    /// <summary>
    /// This class holds onto map data; it loads maps as needed
    /// but must be configured in the Unity editor to have the text
    /// of each map as all the prefabs.
    /// </summary>
    [Serializable]
    public class MapTracker
    {
        public TextAsset[] mapTexts;
        public GameObject[] prefabs;
        private readonly LazyList<Map> lazyMaps = new LazyList<Map>();

        /// <summary>
        /// Maps() yields each map in turn, in map index order.
        /// </summary>
        public IEnumerable<Map> Maps()
        {
            if (mapTexts != null)
            {
                for (int i = 0; i < mapTexts.Length; ++i)
                {
                    yield return this[i];
                }
            }
        }

        /// <summary>
        /// This indexer retreives a map by name, or throws
        /// KeyNotFoundException if the map name is not valid.
        /// </summary>
        public Map this[string mapName]
        {
            get
            {
                for (int i = 0; i < mapTexts.Length; ++i)
                {
                    if (mapTexts[i].name == mapName)
                    {
                        return this[i];
                    }
                }

                throw new KeyNotFoundException(string.Format(
                    "The map '{0}' could not be found.",
                    mapName));
            }
        }

        /// <summary>
        /// This indexer retreives a map by map index, or throws
        /// KeyNotFoundException if the map index is not valid.
        /// </summary>
        public Map this[int mapIndex]
        {
            get
            {
                if (mapTexts != null &&
                    mapIndex >= 0 && mapIndex < mapTexts.Length)
                {
                    return lazyMaps.GetOrCreate(mapIndex, ReadMap);
                }

                throw new KeyNotFoundException(string.Format(
                    "Map index '{0}' could not be found.",
                    mapIndex));
            }
        }

        /// <summary>
        /// This indexer returns a cell given its location in the
        /// world; if the location is not part of any valid map this
        /// returns an empty cell rather than throwing an exception.
        /// </summary>
        public Map.Cell this[Location location]
        {
            get
            {
                if (location.mapIndex >= 0 && location.mapIndex < mapTexts.Length)
                    return this[location.mapIndex][location.x, location.y];
                else
                    return Map.Cell.empty;
            }
        }

        /// <summary>
        /// This methood retreives the prefab whose name is given;
        /// if this name can't be found it throws an exception.
        /// </summary>
        public GameObject GetPrefabByName(string name)
        {
            GameObject prefab = prefabs.FirstOrDefault(p => p.name == name);

            if (prefab == null)
            {
                throw new KeyNotFoundException(string.Format(
                    "The prefab named '{0}' could not be found.",
                    name));
            }

            return prefab;
        }

        private static readonly Location[] noLocations = new Location[0];

        /// <summary>
        /// This method returns an array containing all door destinations for
        /// the door indicated by name and location. This returns an empty array
        /// if the name is null, or if no destination can be found.
        /// </summary>
        public Location[] FindDestinations(string doorName, Location source)
        {
            if (doorName != null)
            {
                IEnumerable<Location> found =
                    (from map in Maps()
                     orderby map.mapIndex == source.mapIndex // other maps before source map!
                     select map.FindDoors(doorName).Where(loc => loc != source) into doorLocs
                     where doorLocs.Any()
                     select doorLocs).
                     FirstOrDefault();

                if (found != null)
                    return found.ToArray();
            }

            return noLocations;
        }

        /// <summary>
        /// This returns an array of the destinations assigned to the map
        /// cell given, or an empty array if it has none.
        /// </summary>
        public Location[] FindDestinations(Map.Cell cell, Location source)
        {
            return FindDestinations(cell.doorName, source);
        }

        /// <summary>
        /// Selects the destination for door given; this picks only one
        /// location, and if more than one destination is available, chooses
        /// randomly.
        /// 
        /// You provide a source and a name to indicate the door being entered;
        /// this lets us avoid exiting the door at its entrance.
        /// 
        /// This method returns false if the name is null, or if no destination can be
        /// found (other than the source).
        /// </summary>
        public bool TryFindDestination(string doorName, Location source, out Location destination)
        {
            Location[] targets = FindDestinations(doorName, source);
            if (targets.Length > 0)
            {
                int index = UnityEngine.Random.Range(0, targets.Length);
                destination = targets[index];
                return true;
            }

            destination = default(Location);
            return false;
        }

        /// <summary>
        /// Selects the destination for the cell given; this picks only one
        /// location, and if more than one destination is available, chooses
        /// randomly.
        /// 
        /// This method returns false if the cell has no destinations at all.
        /// </summary>
        public bool TryFindDestination(Map.Cell cell, Location source, out Location destination)
        {
            return TryFindDestination(cell.doorName, source, out destination);
        }

        /// <summary>
        /// Reads the text of a map into a Map object; used
        /// to implement one of the indexers, which caches
        /// the map objects.
        /// </summary>
        private Map ReadMap(int mapIndex)
        {
            TextAsset textAsset = mapTexts[mapIndex];
            using (var reader = new StringReader(textAsset.text))
            {
                return Map.Load(mapIndex, textAsset.name, reader, prefabs);
            }
        }
    }

    #endregion

    #region Adjancency

    private AdjacencyGenerator lazyAdjacencyGenerator;

    /// <summary>
    /// Returns a lazy-allocated adjance generator that caches
    /// pathability information for you; this is discarded
    /// at end of turn so the pathability can be refreshed,
    /// but caching this information is a major speed boost
    /// for the heatmap code.
    /// </summary>
    public AdjacencyGenerator adjacencyGenerator
    {
        get { return Lazy.Init(ref lazyAdjacencyGenerator, () => new AdjacencyGenerator(this)); }
    }

    /// <summary>
    /// This gemerates the lists of adjacent cells, and it understands
    /// adjancency through doors and pathability. This maintains a pathability
    /// cache so it need not look up game-objects so much.
    /// </summary>
    public sealed class AdjacencyGenerator
    {
        private readonly Location[] adjacencyBuffer = new Location[4];
        private readonly MapController mapController;
        private readonly Dictionary<GameObject, LocationMap<Flag>> pathabilityCache =
            new Dictionary<GameObject, LocationMap<Flag>>();

        public AdjacencyGenerator(MapController mapController)
        {
            this.mapController = mapController;
        }

        /// <summary>
        /// InvalidatePathability() discards all cached pathability
        /// data, so it must be recomputed.
        /// </summary>
        public void InvalidatePathability()
        {
            pathabilityCache.Clear();
        }

        /// <summary>
        /// InvalidatePathability() discards the cached pathability
        /// data for a specific game-object; this movers data
        /// will be recomputed as needed.
        /// </summary>
        public void InvalidatePathability(GameObject mover)
        {
            pathabilityCache.Remove(mover);
        }

        /// <summary>
        /// InvalidatePathability() discards the cached pathability
        /// data for a location; each mover will recompute a value
        /// for this as needed.
        /// </summary>
        public void InvalidatePathability(Location location)
        {
            foreach (LocationMap<Flag> cache in pathabilityCache.Values)
                cache[location] = Flag.Unknown;
        }

        /// <summary>
        /// This finds locations adjacent to 'where', including
        /// those that are adjacent through doors, but only pathable
        /// locations are returned.
        /// 
        /// These locations are added to 'adjacentLocations'; this way you
        /// can reuse the same List object many times and avoid allocating
        /// lots of collections. Don't forget to clear your collection when
        /// needed; this method won't do that for you.
        /// </summary>
        public void GetAdjacentLocationsInto(GameObject mover, Location where, ICollection<Location> adjacentLocations)
        {
            where.GetAdjacentInto(adjacencyBuffer);

            Map.Cell cell = mapController.maps[where];
            Location[] destinations = mapController.maps.FindDestinations(cell, where);

            if (destinations.Length == 0)
            {
                // Fast path - we know there are no duplicates in 'adjacencyBuffer'
                // so we can just iterate and test. This is allocation-free
                // if IsPathable()'s caches are hot and 'adjacentLocation' is a
                // list with sufficient capaciity.

                foreach (Location loc in adjacencyBuffer)
                {
                    if (IsPathableFor(mover, loc))
                    {
                        adjacentLocations.Add(loc);
                    }
                }
            }
            else
            {
                // Slow path- we must make sure we don't introduce duplicate
                // locations so we must do a Union() and iterate the iterator
                // produced by that. This alwys allocates, but should only happen
                // to the cells that have doors in them.

                IEnumerable<Location> unioned = adjacencyBuffer.Union(destinations);

                foreach (Location loc in unioned)
                {
                    if (IsPathableFor(mover, loc))
                    {
                        adjacentLocations.Add(loc);
                    }
                }
            }
        }

        /// <summary>
        /// IsPathable tests if the locaiton given is pathable,
        /// and uses cached results whenever it can.
        /// </summary>
        public bool IsPathableFor(GameObject mover, Location where)
        {
            LocationMap<Flag> cache;

            if (!pathabilityCache.TryGetValue(mover, out cache))
            {
                cache = new LocationMap<Flag>();
                pathabilityCache.Add(mover, cache);
            }

            Flag flag = cache[where];

            switch (flag)
            {
                case Flag.Unknown:
                    bool c = mapController.IsPathableFor(mover, where);
                    cache[where] = c ? Flag.Pathable : Flag.Impathable;
                    return c;

                case Flag.Pathable: return true;
                case Flag.Impathable: return false;
                default: return false;
            }
        }

        /// <summary>
        /// Flag is essentially a nullable boolean, but is only
        /// a byte wide; since we have big arrays fo these, this
        /// saves memory.
        /// </summary>
        private enum Flag : byte
        {
            Unknown,
            Impathable,
            Pathable
        }
    }

    #endregion
}