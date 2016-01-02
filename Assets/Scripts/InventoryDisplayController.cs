using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// InventoryDisplayController places sprites for inventory items
/// into a game object; these sprites are not the same as the items
/// themselves, but indicate what the player is carrying.
/// 
/// An extra object exists inside this one which carries the
/// players HP count. Layout is handed by a vertical layout controller
/// on the same object.
/// </summary>
public class InventoryDisplayController : MonoBehaviour
{
	private GameObject[] inventorySprites = new GameObject[0];

	/// <summary>
	/// This updates the sprites to reflect the inventory of the player
	/// which youy pass in.
	/// </summary>
	public void UpdateInventoryFrom (GameObject player)
	{
		UpdateInventory (
			from i in Enumerable.Range (0, player.transform.childCount)
			select player.transform.GetChild (i).GetComponent<ItemController> () into item
			where item != null
            orderby item.heldDisplayPriority descending, item.name
			select item);
	}

	public void UpdateInventory (IEnumerable<ItemController> items)
	{
		GameObject[] newInventory =
			(from item in items
			 select CreateSprite (item) into s
			 where s != null
			 select s).ToArray ();

		foreach (GameObject i in inventorySprites) {
			Destroy (i);
		}

		inventorySprites = newInventory;
	}

	/// <summary>
	/// This method constructs a sprite from the item given;
	/// if the item itself has no sprite renderer, this method
	/// just returns null (and we skip it).
	/// </summary>
	private GameObject CreateSprite (ItemController item)
	{
		SpriteRenderer sr = item.GetComponent<SpriteRenderer> ();

		if (sr != null && sr.sprite != null) {
			var inventorySprite = new GameObject (item.name, typeof(RectTransform), typeof(LayoutElement), typeof(Image));
			var rt = inventorySprite.GetComponent<RectTransform> ();
			var le = inventorySprite.GetComponent<LayoutElement> ();
			var img = inventorySprite.GetComponent<Image> ();

			img.sprite = sr.sprite;
			le.preferredWidth = 32f;
			le.preferredHeight = 32f;
			rt.SetParent (transform);

			return inventorySprite;
		} else {
			return null;
		}
	}
}
