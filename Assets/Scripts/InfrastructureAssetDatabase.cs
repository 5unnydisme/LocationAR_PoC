using System.Collections.Generic;
using UnityEngine;

public class InfrastructureAssetDatabase : MonoBehaviour
{
    //List to store all our infrastructure assets
    //We use List instead of array- 
    //1. Listst are easier to add/remove
    //2. We can make use of methods like Find(),Count etc
    //3. We might want to filter out options later on 
    //We can use the list in the following ways-
    // - Finding nearby assets based on GPS location
    // - Filtering assets by type (show only roads, only buildings, etc.)
    // - Displaying asset information in AR when user points camera at location

    public List<InfrastructureAsset> infrastructureAssets;

    void Start()
    {
        LoadInfrastructureAssets();
    }

    void LoadInfrastructureAssets()
    {
        //Step 1: Load the JSON file from Resources folder, we're opening the shipping bx
        TextAsset jsonFile = Resources.Load<TextAsset>("InfrastructureAssets");

        if (jsonFile == null)
        {
            Debug.LogError("Could find InfrastructureAssets.json in Resources folder");
            return;
        }

        //Step 2: Convert JSON text into our wrapper class, we're unpacking our box 
        InfrastructureAssetCollection assetCollection = JsonUtility.FromJson <InfrastructureAssetCollection>(jsonFile.text);

        //Step 3: Convert array to List for easier managemment, we're taking the book out of the box and arranging on a shelf
        infrastructureAssets = new List<InfrastructureAsset>(assetCollection.assets);

        //Step 4: Confirm how many assets we loaded
         Debug.Log($"Successfully loaded {infrastructureAssets.Count} infrastructure assets!");

        //Optional to print out the names for each asset, remove when more than 5
        foreach (InfrastructureAsset asset in infrastructureAssets)
        {
            Debug.Log($"Loaded asset: {asset.assetName} at ({asset.latitude}, {asset.longitude})");
        }
    }
}
