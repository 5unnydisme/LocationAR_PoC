using UnityEngine;
using System; //allowing you to use the [Serializable] function which lets you switch between C# and JSON

[Serializable]
public class CVAnalysisResult //placeholder for now, will change based on SB
{
    public string defect_type; //"pothole"
    public string urgency; //"low", "medium", "high"
    public float confidence; //94%
    public string message; 
    public bool detected;
    public string model_name;
}
[Serializable]
public class InfrastructureDetails
{
    public string builtBy;
    public string budget; 
    public string lastInspection;
    public string nextInspection;
}

[Serializable]
public class MaintenanceRecord
{
    public string date;
    public string description;
    public string contractor; 
    public string budget;
}

[Serializable]
public class LocationData //this is the single array entry houding the entire info for each location in the JSON list
{
    public string name;
    public string type;
    public float latitude;
    public float longitude;
    public string condition;
    public int yearBuilt;
    public string description;
    public InfrastructureDetails infrastructureDetails; //this is a nested class that represents the infrastructureDetails object within each location in the JSON file
    public MaintenanceRecord[] maintenanceHistory; //this is a nested class that represents the maintenanceHistory array within each location in the JSON file, we gotta add these in so that Unity can create those data fields inside the object
}

[Serializable]
public class LocationCollection //this is the class that represents the entire JSON file, it has an
{
    public LocationData[] locations; //this is an array of LocationData objects, which represents the "locations" array in the JSON file
}