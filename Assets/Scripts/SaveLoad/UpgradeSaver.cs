using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[RequireComponent(typeof(UpgradeManager))]
public class UpgradeSaver : MonoBehaviour
{
    private UpgradeManager upgradeManager;

    private void Awake()
    {
        upgradeManager = GetComponent<UpgradeManager>();
    }

    private void Start()
    {
        SaveStateManager saveMan = FindObjectOfType<SaveStateManager>();
        saveMan.onSaveTriggered.AddListener(SaveData);
        saveMan.onLoadTriggered.AddListener(LoadData);
    }

    public void SaveData(string savefile)
    {
        Debug.Log("Save Triggered");
        SaveDataAsync(savefile);
    }

    private async void SaveDataAsync(string savefile)
    {
        string path = Application.persistentDataPath + "/" + savefile;
        path += ".upgrades";
        using (StreamWriter writer = File.CreateText(path))
        {
            await writer.WriteAsync(upgradeManager.SerializeUpgrades());
            Debug.Log("Saved to " + path);
        }
    }

    private void LoadData(string savefile)
    {
        string path = Application.persistentDataPath + "/" + savefile;
        path += ".upgrades";

        try
        {
            using (StreamReader reader = new StreamReader(path))
            {
                string upgradesString = reader.ReadToEnd();
                upgradeManager.DeserializeUpgrades(upgradesString);
            };
        } catch(System.Exception e)
        {
            Debug.LogErrorFormat("Save File {0} could not be read!\n{1}", savefile, e.Message);
        }
        
    }
}