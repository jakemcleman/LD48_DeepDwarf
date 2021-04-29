using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Tilemaps;
using UnityEngine;

[RequireComponent(typeof(WorldBuilder))]
public class WorldSaver : MonoBehaviour
{
    private WorldBuilder world;

    private void Awake()
    {
        world = GetComponent<WorldBuilder>();
    }

    private void Start()
    {
        SaveStateManager saveMan = FindObjectOfType<SaveStateManager>();
        saveMan.onSaveTriggered.AddListener(SaveData);
        saveMan.onLoadTriggered.AddListener(LoadData);
    }

    private void SaveData(string savefile, SaveStateManager.SaveType type)
    {
        if(type != SaveStateManager.SaveType.Player)
        {
            Debug.Log("World Save Triggered");
            SaveDataAsync(savefile);
        }
    }

    private void LoadData(string savefile)
    {
        string path = Application.persistentDataPath + "/" + savefile;
        path += ".world";

        if(!File.Exists(path)) 
        {
            Debug.Log("No world save file found, treating as new game");
            return;
        }

        string seedStr = "";
        string boundsStr = "";
        string encodedWorldStr = "";

        try
        {
            
            using (StreamReader reader = new StreamReader(path))
            {
                seedStr = reader.ReadLine();
                boundsStr = reader.ReadLine();
                encodedWorldStr = reader.ReadLine();
            };
        } catch(System.Exception e)
        {
            Debug.LogErrorFormat("Save File {0} could not be read!\n{1}", savefile, e.Message);
            world.BuildWorld();
            return;
        }

        world.seed = int.Parse(seedStr);
        int minBound = int.Parse(boundsStr.Substring(0, boundsStr.IndexOf(",")));
        int maxBound = int.Parse(boundsStr.Substring(boundsStr.IndexOf(",") + 1));
        int worldWidth = maxBound - minBound;

        string[] runLengths = encodedWorldStr.Split(',');
        TileBase[] tiles = new TileBase[world.depthLimit * worldWidth];
        int tileIndex = 0;

        //Debug.LogFormat("Loading tiles length {0}", tiles.Length);

        foreach(string str in runLengths)
        {
            Debug.Log(str);
            if(str.Length < 3 || str[0] == '-' || str[str.Length-1] == '-') 
            {
                Debug.LogFormat("WEIRD BOI AT {0} - {1}", tileIndex, str);
                continue;
            }


            int type = int.Parse(str.Substring(0, str.IndexOf("-")));
            //Debug.LogFormat("Parsed type: {0}", type);
            int count = int.Parse(str.Substring(str.IndexOf("-") + 1));
            //Debug.LogFormat("Parsed count: {0}", count);


            TileBase tile = destringify(type);
            for(int i = 0; i < count; ++i)
            {
                tiles[tileIndex++] = tile;
            }
        }

        if(tileIndex < tiles.Length) Debug.LogWarning("Didn't read enough tile data!");

        world.SetTilesRaw(tiles, minBound, maxBound);
        world.BuildWorld();
    }

    private string stringify(TileBase tile)
    {
        if(tile == null) return "0";
        else if(tile == world.sandTile) return "1";
        else if(tile == world.rockTile) return "2";
        else if(tile == world.goldTile) return "3";
        else if(tile == world.ironTile) return "4";
        else if(tile == world.copperTile) return "5";
        else return "0";
    }

    private TileBase destringify(int tile)
    {
        if(tile == 1) return world.sandTile;
        else if(tile == 2) return world.rockTile;
        else if(tile == 3) return world.goldTile;
        else if(tile == 4) return world.ironTile;
        else if(tile ==  5) return world.copperTile;
        else return null;
    }

    private async void SaveDataAsync(string savefile)
    {
        string path = Application.persistentDataPath + "/" + savefile;
        path += ".world";

        string serialized = "";
        serialized = world.seed.ToString();
        serialized += "\n" + world.MinBound + "," + world.MaxBound;
        TileBase[] tiles = world.GetTilesRaw();

        //Debug.LogFormat("Written tiles length {0}", tiles.Length);

        System.Action serializeTilesAction = () =>
        {
            serialized += "\n";

            int runLengthCount = 1;
            int totalRLE = 0;
            string compressed = "";
            string uncompressed = "";
            for(int i = 0; i < tiles.Length; ++i)
            {
                uncompressed += stringify(tiles[i]);
                if(i < tiles.Length - 1 && tiles[i] == tiles[i+1])
                {
                    runLengthCount++;
                }
                else 
                {
                    compressed += stringify(tiles[i]);
                    compressed += "-" + runLengthCount + ",";
                    totalRLE += runLengthCount;
                    runLengthCount = 1;
                }
            }

            //Debug.LogFormat("Total RLE: {0}", totalRLE);
            serialized += compressed;
            serialized += "\nDEBUG UNCOMPRESSED: " + uncompressed;
        };


        using (StreamWriter writer = File.CreateText(path))
        {
            Task serializeTask = new Task(serializeTilesAction);
            serializeTask.Start();
            await serializeTask;
            await writer.WriteAsync(serialized);
            Debug.Log("Saved to " + path);
        }
    }
}
