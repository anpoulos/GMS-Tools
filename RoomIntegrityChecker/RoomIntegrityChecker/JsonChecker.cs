using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RoomIntegrityChecker
{
    public class JsonChecker
    {
        public JsonChecker()
        {
            LoadJson();
        }

        private void LoadJson()
        {
            Console.WriteLine("Please enter your root directory for rooms: ");
            var rootDirectoryPath = Console.ReadLine();

            var folders = Directory.GetDirectories(rootDirectoryPath, "*", System.IO.SearchOption.AllDirectories);
            var foundDuplicates = false;
            foreach (var folderName in folders)
            {
                var fullFolderPath = Path.Combine(rootDirectoryPath, folderName);
                if (ScanFolder(fullFolderPath))
                {
                    foundDuplicates = true;
                }
            }
            if (!foundDuplicates)
            {
                Console.WriteLine("\nNo duplicate keys detected");
            }
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadLine();
        }
        private bool ScanFolder(string folderPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(folderPath) + ".yy";
            var fullFilePath = Path.Combine(folderPath, fileName);
            //using (StreamReader r = new StreamReader(@"W:\WIP\SystemPurge\System-Purge\Solution\rooms\rm_rc_18\rm_rc_18.yy"))
            using(StreamReader r = new StreamReader(fullFilePath))
            {
                var foundDuplicates = false;
                string json = r.ReadToEnd();
                var tokens = JsonConvert.DeserializeObject<JToken>(json);
                var dictionaryKeyMap = new Dictionary<string, int>();
                RecursiveCheckForInstanceCreationOrder(tokens, dictionaryKeyMap);
                var duplicateKeys = dictionaryKeyMap.Where(k => k.Value > 1);
                if (duplicateKeys.Any())
                {
                    foundDuplicates = true;
                    Console.WriteLine($"\n {duplicateKeys.Count()} Duplicate keys detected in {fileName}");
                    foreach (var keyValuePair in duplicateKeys)
                    {
                        Console.WriteLine($"\n\tDuplicate key detected: {keyValuePair.Key} is used {keyValuePair.Value} times");
                    }
                }

                return foundDuplicates;
            }
        }
        private void RecursiveCheckForInstanceCreationOrder(JToken jToken, Dictionary<string, int> dictionaryKeyMap)
        {
            foreach(var childToken in jToken)
            {
                if (childToken.IsTokenNamed("instanceCreationOrder"))
                {
                    RecursiveCheckForNameKey(childToken, dictionaryKeyMap);
                }
                else
                {
                    RecursiveCheckForInstanceCreationOrder(childToken, dictionaryKeyMap);
                }
            }
        }
        private void OutputListOfDuplicatesInInstanceCreationOrder(JToken jToken, Dictionary<string, int> dictionaryKeyMap)
        {
            foreach(var childToken in jToken)
            {
                RecursiveCheckForNameKey(childToken, dictionaryKeyMap);
            }
        }
        private void RecursiveCheckForNameKey(JToken jToken, Dictionary<string, int> dictionaryKeyMap)
        {
            foreach (var childToken in jToken)
            {
                if (childToken.IsTokenNamed("name"))
                {
                    var instanceKey = childToken.First.Value<string>();
                    if (!dictionaryKeyMap.ContainsKey(instanceKey))
                    {
                        dictionaryKeyMap.Add(instanceKey, 0);
                    }
                    dictionaryKeyMap[instanceKey] += 1;
                }
                else
                {
                    RecursiveCheckForNameKey(childToken, dictionaryKeyMap);
                }
            }            
        }
    }
    public static class JTokenExtensionMethods
    {
        public static bool IsTokenNamed(this JToken jToken, string name)
        {
            return jToken is JProperty jProperty && !string.IsNullOrWhiteSpace(jProperty.Name) && jProperty.Name.ToLower() == name.ToLower();
        }
    }
}
