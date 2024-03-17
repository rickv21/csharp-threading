using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FileManager.Models
{
    /// <summary>
    /// Model responsible for the Config JSON file.
    /// </summary>
    public class ConfigModel
    {
        //Test values.
        public string Option1 { get; set; }
        public int ClickCount { get; set; }

        //Path of the config file, is hardcoded to the Documents folder.
        private static readonly string configFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "filemanager_config.json");

        /// <summary>
        /// Loads the values from the config file and creates the config file it it does not exist.
        /// </summary>
        /// <returns>The ConfigModel with values set.</returns>
        public static ConfigModel LoadOrCreateDefault()
        {
            if (File.Exists(configFilePath))
            {
                var json = File.ReadAllText(configFilePath);
                return JsonConvert.DeserializeObject<ConfigModel>(json);
            }
            else
            {
                //Create new config.
                var config = new ConfigModel
                {
                    Option1 = "This is a test value from filemanager_config.json in your Documents folder 🤯",
                    ClickCount = 0
                };
                config.Save();
                return config;
            }
        }

        /// <summary>
        /// Saves the config values to the config file.
        /// </summary>
        public void Save()
        {
            var json = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(configFilePath, json);
        }
    }
}
