using System.IO;
using UnityEngine;

// Check Script Execution Order settings
public class ConfigManager : MonoBehaviour
{
    const string FILE_NAME = "/config.txt";

    // Set in the inspector.
    [SerializeField] ConfigObject defaultConfig;

    private void Awake()
    {
        Load();
    }

    private void Load()
    {
        // Load the config file if its not possible create one with the default values
        if (!File.Exists(Application.dataPath + FILE_NAME))
        {
            CreateConfigFile();
            // Set the default config variables to the settings.
            SetValuesByConfigObject(defaultConfig);
        }
        else
        {
            string loadedString = File.ReadAllText(Application.dataPath + FILE_NAME);

            ConfigObject loadedConfig;
            try
            {
                loadedConfig = JsonUtility.FromJson<ConfigObject>(loadedString);
                Debug.Log("Config File Loaded Successfully");
            }
            catch
            {
                loadedConfig = defaultConfig;
                CreateConfigFile();
            }

            // Set the loaded config variables to the settings.
            // If the Json file was corrupted or was unable to deserialize then 
            // we simply apply the default config values.
            SetValuesByConfigObject(loadedConfig);
        }
    }

    private void SetValuesByConfigObject(ConfigObject co)
    {
        // Set the ServerSettings values by the givin object
        ServerSettings.maxPlayerCount = co.maxPlayerCount;
        ServerSettings.tickRate = co.tickRate;
        ServerSettings.lagCompensation = co.lagCompensation;
        ServerSettings.backTrackingBufferTimeMS = co.backTrackingBufferTimeMS;
    }

    private void CreateConfigFile()
    {
        string json = JsonUtility.ToJson(defaultConfig, true);
        File.WriteAllText(Application.dataPath + FILE_NAME, json);
        Debug.Log("File Created successfully");
    }


    [System.Serializable]
    private class ConfigObject
    {
        [Header("Network Values")]
        public ushort maxPlayerCount;
        public ushort tickRate;
        [Header("Server Side Lag Compensation Values")]
        public bool lagCompensation;
        public ushort backTrackingBufferTimeMS; 

        public ConfigObject(ushort maxPlayerCount, ushort tickRate, bool lagCompensation, ushort backTrackingBufferTimeMS)
        {
            this.maxPlayerCount = (ushort) Mathf.Clamp(maxPlayerCount, 1, 30);
            this.tickRate = (ushort) Mathf.Clamp(tickRate, 1, 120);
            this.lagCompensation = lagCompensation;
            this.backTrackingBufferTimeMS = (ushort) Mathf.Clamp(backTrackingBufferTimeMS, 1, 1000);
        }
    }
}
