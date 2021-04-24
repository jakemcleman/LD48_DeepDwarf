using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WaterFlow : MonoBehaviour
{
    private Tilemap waterTiles;
    private Tilemap terrainTiles;

    public Tile waterTile;
    public Tile waterSurfaceTile;

    public Tile[] waterloggableTiles;
    private HashSet<Tile> waterloggableSet;

    private Queue<Vector3Int> toUpdateNextFrame;

    public int xBounds = 100;
    public int depthLimit = 100;

    public int waterLevel = 0;

    public float waterUpdateRate = 0.1f;
    private float lastWaterUpdate;

    private void Awake()
    {
        toUpdateNextFrame = new Queue<Vector3Int>();

        waterloggableSet = new HashSet<Tile>();
        foreach(Tile waterloggable in waterloggableTiles)
        {
            waterloggableSet.Add(waterloggable);
        }
    }

    private void Start()
    {
        waterTiles = transform.Find("Ocean").GetComponent<Tilemap>();
        terrainTiles = transform.Find("Terrain").GetComponent<Tilemap>();

        for(int i = -xBounds; i < xBounds; ++i)
        {
            Vector3Int tilePos = new Vector3Int(i, waterLevel, 0);
            if(isWaterLoggable(terrainTiles.GetTile(tilePos)))
            {
                waterTiles.SetTile(tilePos, waterSurfaceTile);
                toUpdateNextFrame.Enqueue(tilePos);
            }
        }

        lastWaterUpdate = Time.time;
    }

    private void Update()
    {
        if(Time.time - lastWaterUpdate > waterUpdateRate)
        {
            UpdateWater();
        }
    }

    private void UpdateWater()
    {
        Queue<Vector3Int> updating = new Queue<Vector3Int>(toUpdateNextFrame);
        toUpdateNextFrame = new Queue<Vector3Int>();

        while(updating.Count > 0)
        {
            UpdateWaterTile(updating.Dequeue());
        }

        lastWaterUpdate = Time.time;
    }

    private void RegisterWaterChange(Vector3Int tilePos)
    {
        toUpdateNextFrame.Enqueue(tilePos);
        toUpdateNextFrame.Enqueue(tilePos + new Vector3Int(0, -1, 0));
        toUpdateNextFrame.Enqueue(tilePos + new Vector3Int(1, 0, 0));
        toUpdateNextFrame.Enqueue(tilePos + new Vector3Int(-1, 0, 0));
    }

    public TileBase Dig(Vector3 worldPos)
    {
        Vector3Int tilePos = terrainTiles.WorldToCell(worldPos);
        TileBase present = terrainTiles.GetTile(tilePos);

        if(present != null)
        {
            terrainTiles.SetTile(tilePos, null);
            RegisterTerrainChange(tilePos);
        }

        return present;
    }

    public void RegisterTerrainChange(Vector3Int tilePos)
    {
        toUpdateNextFrame.Enqueue(tilePos);
        toUpdateNextFrame.Enqueue(tilePos + new Vector3Int(0, 1, 0));
        toUpdateNextFrame.Enqueue(tilePos + new Vector3Int(1, 0, 0));
        toUpdateNextFrame.Enqueue(tilePos + new Vector3Int(-1, 0, 0));
    }

    private bool TryPlaceWater(Vector3Int tilePos)
    {
        if(tilePos.x > xBounds || tilePos.x < -xBounds || tilePos.y < -depthLimit) return false;
        if(waterTiles.GetTile(tilePos) != null) return false;
        if(!isWaterLoggable(terrainTiles.GetTile(tilePos))) return false;

        waterTiles.SetTile(tilePos, waterTile);

        RegisterWaterChange(tilePos);

        return true;
    }

    public bool isWaterLoggable(TileBase tile)
    {
        if(tile == null || (Tile)tile == null) return true;

        return waterloggableSet.Contains((Tile)tile);
    }

    private void UpdateWaterTile(Vector3Int tilePos)
    {
        if(waterTiles.GetTile(tilePos) == null) return;

        if(!isWaterLoggable(terrainTiles.GetTile(tilePos)))
        {
            waterTiles.SetTile(tilePos, null);

            RegisterWaterChange(tilePos);
        }

        bool didSpread = true;

        if(!TryPlaceWater(tilePos + new Vector3Int(0, -1, 0)))
        {
            if(!TryPlaceWater(tilePos + new Vector3Int(1, 0, 0)))
            {
                didSpread = TryPlaceWater(tilePos + new Vector3Int(-1, 0, 0));
            }
        }

        if(didSpread)
        {
            toUpdateNextFrame.Enqueue(tilePos);
        }
    }
}
