using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BlueprintSplitter
{
    class Program
    {
        public static void Main(string[] args)
        {
            if (args.Count() != 3)
            {
                Console.WriteLine($"Usage: {Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().Location)} <blueprint_size_limit> <input_filename> <output_filename>");
            }

            // Input params
            int blueprintSizeLimit = int.Parse(args[0]);
            string blueprint = File.ReadAllText(args[1]);
            string outputFilename = args[2];
            
            // Parse input
            string decodedBlueprint = Blueprint.Decode(blueprint);
            var inputBlueprint = JObject.Parse(decodedBlueprint);

            // Setup Blueprint book
            var blueprintBook = new JObject();
            blueprintBook["blueprint_book"] = new JObject();
            blueprintBook["blueprint_book"]["blueprints"] = new JArray();
            blueprintBook["blueprint_book"]["item"] = inputBlueprint["blueprint"]["item"].DeepClone();
            blueprintBook["blueprint_book"]["label"] = inputBlueprint["blueprint"]["label"].DeepClone();
            blueprintBook["blueprint_book"]["active_index"] = 0;
            blueprintBook["blueprint_book"]["version"] = inputBlueprint["blueprint"]["version"].DeepClone();

            // Add original blueprint to book
            (blueprintBook["blueprint_book"]["blueprints"] as JArray).Add(inputBlueprint.DeepClone());

            // Setup template for split blueprints
            var blueprintTemplate = new JObject();
            blueprintTemplate["blueprint"] = new JObject();
            blueprintTemplate["blueprint"]["description"] = inputBlueprint["blueprint"]["description"].DeepClone();
            blueprintTemplate["blueprint"]["icons"] = inputBlueprint["blueprint"]["icons"].DeepClone();
            blueprintTemplate["blueprint"]["item"] = inputBlueprint["blueprint"]["item"].DeepClone();
            blueprintTemplate["blueprint"]["version"] = inputBlueprint["blueprint"]["version"].DeepClone();
            blueprintTemplate["blueprint"]["entities"] = new JArray();

            // Keep connected entities in a single blueprint.
            var connectionList = new JArray();
            for (int i = 0; i < inputBlueprint["blueprint"]["entities"].Count(); i++)
            {
                if (inputBlueprint["blueprint"]["entities"][i]["connections"] != null || inputBlueprint["blueprint"]["entities"][i]["neighbours"] != null)
                {
                    connectionList.Add(inputBlueprint["blueprint"]["entities"][i].DeepClone());
                }
            }

            if (connectionList.Count > blueprintSizeLimit)
            {
                throw new ArgumentException($"There are {connectionList.Count} connections, the blueprint size limit should be greater than this number.");
            }

            var connectionBlueprint = blueprintTemplate.DeepClone();
            connectionBlueprint["blueprint"]["label"] = "Connections";
            connectionBlueprint["blueprint"]["entities"] = connectionList;
            (blueprintBook["blueprint_book"]["blueprints"] as JArray).Add(connectionBlueprint);

            // Initialise loop
            (blueprintBook["blueprint_book"]["blueprints"] as JArray).Add(blueprintTemplate.DeepClone());
            (blueprintBook["blueprint_book"]["blueprints"] as JArray).Last()["blueprint"]["label"] = "0";

            // Loop over input
            for (int i = 0; i < inputBlueprint["blueprint"]["entities"].Count(); i++)
            {
                if (inputBlueprint["blueprint"]["entities"][i]["connections"] == null && inputBlueprint["blueprint"]["entities"][i]["neighbours"] == null) // Ignore connected entities
                {
                    if ((blueprintBook["blueprint_book"]["blueprints"] as JArray).Last()["blueprint"]["entities"].Count() >= blueprintSizeLimit)
                    {
                        (blueprintBook["blueprint_book"]["blueprints"] as JArray).Add(blueprintTemplate.DeepClone());
                        (blueprintBook["blueprint_book"]["blueprints"] as JArray).Last()["blueprint"]["label"] = i.ToString();
                    }

                    ((blueprintBook["blueprint_book"]["blueprints"] as JArray).Last()["blueprint"]["entities"] as JArray).Add(inputBlueprint["blueprint"]["entities"][i].DeepClone());
                }
            }

            File.WriteAllText(outputFilename, Blueprint.Encode(blueprintBook.ToString(Newtonsoft.Json.Formatting.None)));

            //for (int i = 0; i < (blueprintBook["blueprint_book"]["blueprints"] as JArray).Count(); i++)
            //{
            //    File.WriteAllText($"{i}.txt", (blueprintBook["blueprint_book"]["blueprints"] as JArray)[i].ToString());
            //}

            //File.WriteAllText($"blueprint.txt", blueprintBook.ToString());
            //Console.WriteLine(Blueprint.Encode(blueprintBook.ToString(Newtonsoft.Json.Formatting.None)));
            //Console.ReadLine();
        }
    }
}
