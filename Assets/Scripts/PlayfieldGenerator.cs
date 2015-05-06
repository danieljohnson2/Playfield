using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PlayfieldGenerator
{
	private readonly GameObject[,] terrainTemplates;

	public PlayfieldGenerator (int width, int height)
	{
		this.terrainTemplates = new GameObject[width, height];
	}
	
	public int width {
		get { return terrainTemplates.GetLength (0); }
	}
	
	public int height {
		get { return terrainTemplates.GetLength (1); }
	}

	public GameObject this [int x, int y] {
		get {
			if (x >= 0 && x < width && y >= 0 && y < height) {
				return terrainTemplates [x, y];
			} else { 
				return null;
			}
		}

		set { terrainTemplates [x, y] = value; }
	}

	public GameObject[,] Generate (GameObject parent)
	{
		GameObject[,] terrainObjects = new GameObject[width, height];

		for (int y =0; y < height; ++y) {
			for (int x=0; x < width; ++x) {
				GameObject template = terrainTemplates [x, y];

				if (template != null) {
					GameObject obj = GameObject.Instantiate (template);
					obj.transform.parent = parent.transform;
					obj.transform.position = new Vector3 (x, -y, 0);
					terrainObjects [x, y] = obj;
				}
			}
		}

		return terrainObjects;
	}
}
