using UnityEngine;


 public class LocationJSONLoader: MonoBehaviour
 {
    public PanelPopulator infoPanel; //reference to the script that will populate the info panel, this will be assigned in the inspector
    private LocationCollection loadedData; //this is storing the parsed data for lookup
     void Start()
     {
        TextAsset jsonFile = Resources.Load<TextAsset>("test"); //this is linking ot the JSON file that will be looked at
        if (jsonFile == null) // Null check for safety
        {
            Debug.LogError("Could not find test.json");
            return;
        }

        loadedData = JsonUtility.FromJson<LocationCollection>(jsonFile.text); // convers the JSON text into the LocationCollection object, allowing us to use the .commands in unity, and stores that info in the variable lodadedData
        Debug.Log($"Loaded{loadedData?.locations?.Length ?? 0} locations");

        //LocationCollection data = JsonUtility.FromJson<LocationCollection>(jsonFile.text); //converts the JSON text into a LocationCollection object

        //Optional debug to see if the dynamic panel was working by simply displaying first location in JSON DB
        // if (infoPanel != null && data.locations != null && data.locations.Length >0)
        // {
        //     infoPanel.Populate(data.locations[0]); //this is populating the info panel with the first location's data, can be expanded later based on the order selected? 
        // }   
        #region Debug logs to verify the data is being loaded correctly, can be removed later
        // Debug.Log(data.locations.Length); //this will print the number of items in the array
        // //this will print the number of items in the maintenanceHistory array for the first location    
        // Debug.Log(data.locations[0].name);
        // Debug.Log(data.locations[0].type);
        // Debug.Log(data.locations[0].condition);
        // Debug.Log(data.locations[0].yearBuilt);
        // Debug.Log(data.locations[0].description);
        // Debug.Log(data.locations[0].infrastructureDetails.builtBy);
        // Debug.Log(data.locations[0].infrastructureDetails.budget);
        // Debug.Log(data.locations[0].infrastructureDetails.lastInspection);
        // Debug.Log(data.locations[0].infrastructureDetails.nextInspection);
        // Debug.Log(data.locations[0].maintenanceHistory.Length);
        // Debug.Log(data.locations[0].maintenanceHistory[0].date);
        // Debug.Log(data.locations[0].maintenanceHistory[0].description);
        // Debug.Log(data.locations[0].maintenanceHistory[0].contractor);
        // Debug.Log(data.locations[0].maintenanceHistory[0].budget);

        // Debug.Log(data.locations[1].name);
        // Debug.Log(data.locations[1].type);
        // Debug.Log(data.locations[1].condition);
        // Debug.Log(data.locations[1].yearBuilt);
        // Debug.Log(data.locations[1].description);
        // Debug.Log(data.locations[1].infrastructureDetails.builtBy);
        // Debug.Log(data.locations[1].infrastructureDetails.budget);
        // Debug.Log(data.locations[1].infrastructureDetails.lastInspection);
        // Debug.Log(data.locations[1].infrastructureDetails.nextInspection);
        // Debug.Log(data.locations[1].maintenanceHistory.Length);
        // Debug.Log(data.locations[1].maintenanceHistory[0].date);
        // Debug.Log(data.locations[1].maintenanceHistory[0].description);
        // Debug.Log(data.locations[1].maintenanceHistory[0].contractor);
        // Debug.Log(data.locations[1].maintenanceHistory[0].budget);
        #endregion
     }

     public LocationData GetLocationDataByName(string name)
    {
        if(string.IsNullOrEmpty(name) || loadedData == null || loadedData.locations == null) return null; //our friend the null check

        //Exact match (case-insensitive) 
        foreach (var loc in loadedData.locations)
        {
            if (loc == null || string.IsNullOrEmpty(loc.name)) continue; 
            if (string.Equals(loc.name,name,System.StringComparison.OrdinalIgnoreCase)) return loc;
        }

        //Fallback: Forgiving substring match
        foreach (var loc in loadedData.locations)
        {
            if (loc == null || string.IsNullOrEmpty(loc.name)) continue;
            if (loc.name.IndexOf(name,System.StringComparison.OrdinalIgnoreCase)>=0 ||
            name.IndexOf(loc.name, System.StringComparison.OrdinalIgnoreCase)>=0)
            return loc;
        }

        return null;
    }
 }