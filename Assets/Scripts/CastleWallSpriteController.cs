using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// There are 15 sprites named with bitwise naming: in theory it should be possible to get the sprite with the
/// correct name by looking at adjacent spots on the map. Bits are true (1) if the location does NOT have more
/// wall. Form is CastleWall_0000 and the bits are Top,Bottom,Left,Right.
/// </summary>
public class CastleWallSpriteController : MonoBehaviour
{
    public Sprite Wall_0000;
    public Sprite Wall_0001;
    public Sprite Wall_0010;
    public Sprite Wall_0011;
    public Sprite Wall_0100;
    public Sprite Wall_0101;
    public Sprite Wall_0110;
    public Sprite Wall_0111;
    public Sprite Wall_1000;
    public Sprite Wall_1001;
    public Sprite Wall_1010;
    public Sprite Wall_1011;
    public Sprite Wall_1100;
    public Sprite Wall_1101;
    public Sprite Wall_1110;
    public Sprite Wall_1111;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        this.spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        Sprite newSprite = PickSprite();

        if (newSprite != null)
            spriteRenderer.sprite = newSprite;
    }

    /// <summary>
    /// I would totally do this by grabbing the sprite by name but we're not doing anything like that
    /// </summary>
    private Sprite PickSprite()
    {
        MapController mapController = MapController.instance;

        Location loc = Location.Of(gameObject);
        Location[] buffer = loc.Adjacent().ToArray(); // order is n, e, s, w

        GameObject north = mapController.terrain.GetTerrain(buffer[0]);
        GameObject east = mapController.terrain.GetTerrain(buffer[1]);
        GameObject south = mapController.terrain.GetTerrain(buffer[2]);
        GameObject west = mapController.terrain.GetTerrain(buffer[3]);

        //mmmm! thog like grinding out big pile of logic resembling what the compiler would produce! :D
        //Remember, if the checked spot is a wall we do NOT draw a parapet

        if (IsWall(north))
        {
            //0xxx
            if (IsWall(south))
            {
                //00xx
                if (IsWall(east) && IsWall(west)) { return Wall_0000; }
                if (IsWall(east) && (!IsWall(west))) { return Wall_0001; }
                if ((!IsWall(east)) && IsWall(west)) { return Wall_0010; }
                if ((!IsWall(east)) && (!IsWall(west))) { return Wall_0011; }
            }
            else
            {
                //01xx
                if (IsWall(east) && IsWall(west)) { return Wall_0100; }
                if (IsWall(east) && (!IsWall(west))) { return Wall_0101; }
                if ((!IsWall(east)) && IsWall(west)) { return Wall_0110; }
                if ((!IsWall(east)) && (!IsWall(west))) { return Wall_0111; }
            }
        }
        else
        {
            //1xxx
            if (IsWall(south))
            {
                //10xx
                if (IsWall(east) && IsWall(west)) { return Wall_1000; }
                if (IsWall(east) && (!IsWall(west))) { return Wall_1001; }
                if ((!IsWall(east)) && IsWall(west)) { return Wall_1010; }
                if ((!IsWall(east)) && (!IsWall(west))) { return Wall_1011; }
            }
            else
            {
                //11xx
                if (IsWall(east) && IsWall(west)) { return Wall_1100; }
                if (IsWall(east) && (!IsWall(west))) { return Wall_1101; }
                if ((!IsWall(east)) && IsWall(west)) { return Wall_1110; }
                if ((!IsWall(east)) && (!IsWall(west))) { return Wall_1111; }
            }
        }
        return null; //we should never reach this
    }

    /// <summary>
    /// IsWall() decides if the terrain object given is a wall.
    /// </summary>
    private static bool IsWall(GameObject terrain)
    {
        // this is a lousy way to test for wall-ness, but I'm not sure
        // what the right way is! We can replace this later.
        //return terrain != null && terrain.name == "Wall"; //looks better indoors
        return terrain != null && (terrain.name == "Wall" || terrain.name == "Door"); //looks better outdoors
    }
}
