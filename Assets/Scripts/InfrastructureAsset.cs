using System;
//This script is helping Unity 'parse' JSON into objects we can use in code,
//it basically opens up the JSON structure so we can access the data inside it, this is done by turning it into a list
[System.Serializable] //this is telling Unity that the class can be converted to/from JSON
public class InfrastructureAsset
{
    //Unique identifier for the asset
    public string assetID;

    //Readable name for the asset
    public string assetName;

    //Category of the asset (e.g. Building, Road, Bride) this could help sort in the futur
    public string assetType; 

    //Geographic coordinates latitude
    public double latitude;

    //Geographic coordinate longitide
    public double longitude;

    //Organisation or contractor that constructed the asset
    public string builtBy; 

    //Year when the asset was last maintained or repaired
    public string lastRepaired; 

    //Source of money used to build/maintain the asset 
    public string fundingSource;

}

[System.Serializable] //container to match your JSON structure
public class InfrastructureAssetCollection
{
    //Array to hold multiple assets
    public InfrastructureAsset[] assets;
}