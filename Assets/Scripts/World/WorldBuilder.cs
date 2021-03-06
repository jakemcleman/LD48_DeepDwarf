using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class WorldBuilder : MonoBehaviour
{
    public enum TileType
    {
        Sand,
        Stone,
        Ore, 
        Air
    }

    private Tilemap terrainTiles;

    public int seed = 0;
    private int noiseOffsetX = 0;
    private int noiseOffsetY = 0;

    public int xBounds = 100;
    public int depthLimit = 100;

    public int waterLevel = 0;

    public int minDepth = 20;


    public float sandThickAvg = 4;
    public float sandThickVariance = 4;
    public Tile sandTile;
    public Tile rockTile;

    public Tile goldTile;
    public Tile ironTile;
    public Tile copperTile;
    public GameObject goldNuggetPrefab;
    public GameObject ironNuggetPrefab;
    public GameObject copperNuggetPrefab;

    public GameObject stoneBreakEffectPrefab;

    public GameObject[] randomDebris;

    private int minExtent;
    private int maxExtent;

    private WaterFlow flower;

    public float passiveNuggetSpawnInterval = 1f;
    private float lastNuggetSpawnTime;
    public int maxPassiveNuggets = 10;
    public int minSpawnDistance = 12;
    public int maxSpawnDistance = 25;
    private LinkedList<GameObject> passiveNuggets;

    private void Start()
    {
        terrainTiles = transform.Find("Terrain").GetComponent<Tilemap>();
        flower = GetComponent<WaterFlow>();

        if(seed == 0) seed = Random.Range(int.MinValue, int.MaxValue);
        Random.InitState(seed);

        noiseOffsetX = Random.Range(int.MinValue, int.MaxValue);
        noiseOffsetY = Random.Range(int.MinValue, int.MaxValue);

        
        minExtent = -30;
        maxExtent = 30;
        for(int x = minExtent; x < maxExtent; ++x)
        {
            GenerateSlice(x);
        }

        if(goldNuggetPrefab == null)
        {
            Debug.LogWarning("No prefab assigned for gold nugget!");
        }

        lastNuggetSpawnTime = Time.time;

        passiveNuggets = new LinkedList<GameObject>();
    }
    
    private Vector3Int PickNuggetSpawnSpot()
    {
        Vector3 camPos = Camera.main.transform.position;
        int camX = (int)camPos.x;
        int lowLow = System.Math.Max(minExtent, camX - maxSpawnDistance);
        int lowHigh = System.Math.Max(lowLow, camX - minSpawnDistance);
        int highHigh = System.Math.Min(maxExtent, camX + maxSpawnDistance);
        int highLow = System.Math.Min(highHigh, camX + minSpawnDistance);
        bool canUseLow = lowLow != lowHigh;
        bool canUseHigh = highLow != highHigh;

        bool useLow = canUseLow && (!canUseHigh || Random.Range(0, 2) == 1);

        int rangeLow = useLow ? lowLow : highLow;
        int rangeHigh = useLow ? lowHigh : highHigh;

        return new Vector3Int(Random.Range(rangeLow, rangeHigh), Random.Range(-minDepth, waterLevel), 0);
    }

    private void Update()
    {
        Vector3 camPos = Camera.main.transform.position;
        int camX = (int)camPos.x;

        if(camX + 30 > maxExtent && maxExtent < xBounds) 
        {
            for(int x = maxExtent; x < camX + 30 && x < xBounds; ++x)
            {
                GenerateSlice(x);
            }
        }

        if(camX - 30 < minExtent && minExtent > -xBounds) 
        {
            for(int x = minExtent - 1; x > camX - 30 && x > -xBounds; --x)
            {
                GenerateSlice(x);
            }
        }

        if(Time.time - lastNuggetSpawnTime > passiveNuggetSpawnInterval)
        {
            lastNuggetSpawnTime = Time.time;

            if(passiveNuggets.Count < maxPassiveNuggets)
            {
                Vector3Int spawnPos = PickNuggetSpawnSpot();
                GameObject toSpawn = randomDebris[Random.Range(0, randomDebris.Length)];
                GameObject drop = Instantiate(toSpawn, terrainTiles.CellToWorld(spawnPos), Quaternion.identity);
                drop.GetComponent<Pickup>().OnPickup.AddListener(OnPassiveNuggetPickedUp);
                //drop.GetComponent<WaterInteraction>().waterGravityScale = 0.05f;
                passiveNuggets.AddLast(drop);
            }
            else
            {
                foreach(GameObject nugget in passiveNuggets)
                {
                    if(Vector2.Distance(camPos, nugget.transform.position) > maxSpawnDistance)
                    {
                        passiveNuggets.Remove(nugget);
                        Destroy(nugget);
                        break;
                    }
                }
            }   
        }
    }

    private void OnPassiveNuggetPickedUp(GameObject nugget)
    {
        passiveNuggets.Remove(nugget);
    }

    private void GenerateSlice(int x)
    {
        float heightSamplePos =  (x - xBounds) / 40.0f;

        float hilliness = Mathf.PerlinNoise(heightSamplePos / 2, noiseOffsetY - 1.0f);
        float hill = Helpers.Remap(hilliness * Mathf.PerlinNoise(heightSamplePos, noiseOffsetY), 0, 1, -minDepth, -depthLimit / 3);
        float micro = Helpers.Remap(hilliness * Mathf.PerlinNoise(heightSamplePos * 20, noiseOffsetY), 0, 1, 0, -10);
        float height = hill + micro;
        float sandThickness = Helpers.Remap(Mathf.PerlinNoise(heightSamplePos, noiseOffsetY + 0.5f), 1, 0, sandThickAvg + sandThickVariance, sandThickAvg - sandThickVariance);

        for(int y = -depthLimit; y < hill; ++y)
        {
            Tile toPlace = y < (height - sandThickness) ? rockTile : sandTile;

            if(y != -depthLimit)
            {
                float holeSampleXpos = (x - xBounds) / 12.0f;
                float holeSampleYpos = (y) / 51.0f  + noiseOffsetY;

                float plasmaSampleXpos = (x) / 32.0f;
                float plasmaSampleYpos = (y) / 5.0f  + noiseOffsetY;
                
                float caveDensity = Helpers.Remap(y, -minDepth, -depthLimit, 0.6f, 0.85f);
                float holesCave = (Mathf.PerlinNoise(holeSampleXpos, holeSampleYpos) + Mathf.PerlinNoise(plasmaSampleXpos, plasmaSampleYpos) ) / 2.0f;
                float plasmaCave = Mathf.Sin(Mathf.PerlinNoise(plasmaSampleXpos, plasmaSampleYpos) * Mathf.PerlinNoise(plasmaSampleXpos, plasmaSampleYpos) * Mathf.PI);
                
                float caveDetail = Mathf.PerlinNoise(x / 3f, y / 3f);

                float plasmaWeight = 2;
                float holesWeight = 3;
                float noiseWeight = 1;
                
                float cave = ((holesCave * holesWeight) + (plasmaCave * plasmaWeight) + (caveDetail * noiseWeight)) / (plasmaWeight + holesWeight + noiseWeight);

                if(cave > caveDensity)
                {
                    toPlace = null;
                }

                if(toPlace == rockTile)
                {
                    float goldOreSampleXpos = (x - xBounds) / 70.0f;
                    float goldOreSampleYpos = (y) / 20.0f;
                    float goldOre = Mathf.PerlinNoise(goldOreSampleXpos, goldOreSampleYpos);
                    float goldDensity = Helpers.Remap(y, -minDepth, -depthLimit, 0.8f, 0.4f);

                    float ironOreSampleXpos = (x - xBounds) / 40.0f;
                    float ironOreSampleYpos = (y) / 15.0f;
                    float ironOre = Mathf.PerlinNoise(ironOreSampleXpos, ironOreSampleYpos);
                    float ironDensity = Helpers.Remap(y, -minDepth, -depthLimit, 0.75f, 0.6f);

                    float copperOreSampleXpos = (x - xBounds) / 55.0f;
                    float copperOreSampleYpos = (y) / 17.0f;
                    float copperOre = Mathf.PerlinNoise(copperOreSampleXpos, copperOreSampleYpos);
                    float copperDensity = Helpers.Remap(y, -minDepth, -depthLimit, 0.7f, 0.5f);

                    if(goldOre > goldDensity && Random.Range(goldDensity, 1f) < goldOre)
                    {
                        toPlace = goldTile;
                    }
                    else if(copperOre > copperDensity && Random.Range(copperDensity, 1f) < copperOre)
                    {
                        toPlace = copperTile;
                    }
                    else if(ironOre > ironDensity && Random.Range(ironDensity, 1f) < ironOre)
                    {
                        toPlace = ironTile;
                    }
                }
            }

            Vector3Int tilePos = new Vector3Int(x, y, 0);
            terrainTiles.SetTile(tilePos, toPlace);
        }

        flower.AddSlice(x);

        if(x > maxExtent) maxExtent = x;
        else if(x < minExtent) minExtent = x;
    }

    private GameObject SpawnDrops(TileBase brokenType, Vector3Int tilePos)
    {
        Tile brokenTile = (Tile)brokenType;

        GameObject toSpawn = null;
        GameObject fx = null;

        if(brokenTile == goldTile)
        {
            toSpawn = goldNuggetPrefab;
            fx = stoneBreakEffectPrefab;
        }
        else if(brokenTile == ironTile)
        {
            fx = stoneBreakEffectPrefab;
            toSpawn = ironNuggetPrefab;
        }
        else if(brokenTile == copperTile)
        {
            fx = stoneBreakEffectPrefab;
            toSpawn = copperNuggetPrefab;
        }
        else if(brokenTile == rockTile)
        {
            fx = stoneBreakEffectPrefab;
        }

        Vector3 position = terrainTiles.CellToWorld(tilePos) + new Vector3(0.5f, 0.5f, 0);

        if(fx != null)
        {
            position.z = 1;
            GameObject drop = Instantiate(fx, position, Quaternion.identity);
        }

        if(toSpawn != null)
        {
            GameObject drop = Instantiate(toSpawn, position, Quaternion.identity);
            return drop;
        }

        return null;
    }

    
    public TileType Dig(Vector3 worldPos)
    {
        Vector3Int tilePos = terrainTiles.WorldToCell(worldPos);
        TileBase present = terrainTiles.GetTile(tilePos);

        if(worldPos.y <= -depthLimit) return TileType.Air;

        if(present != null)
        {
            terrainTiles.SetTile(tilePos, null);
            flower.RegisterTerrainChange(tilePos);

            SpawnDrops(present, tilePos);
        }

        if(present == null) return TileType.Air;
        if(present == sandTile) return TileType.Sand;
        if(present == rockTile) return TileType.Stone;
        else return TileType.Ore;
    }

    public Vector3 SnapToTile(Vector3 worldPos)
    {
        return terrainTiles.CellToWorld(terrainTiles.WorldToCell(worldPos));
    }

    public Vector3Int SnapToGrid(Vector3 worldPos)
    {
        return terrainTiles.WorldToCell(worldPos);
    }

    public bool TilePresent(Vector3 worldPos)
    {
        return terrainTiles.GetTile(terrainTiles.WorldToCell(worldPos)) != null;
    }

    public bool IsBreathable(Vector3 worldPos)
    {
        Vector3Int tilePos = terrainTiles.WorldToCell(worldPos);
        return flower.isBreathable(tilePos);
    }
}
