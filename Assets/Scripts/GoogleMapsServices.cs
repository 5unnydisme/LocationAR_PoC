using System.Collections;
using UnityEngine;
using UnityEngine.Networking; 

//This script is handling two API calls in sequence: 
//1. Nearest Roads API: Is taking lat/lon glocation and returning a placeId
//2. Places API: Is taking the placeId and returning road name string which will be used by PanelManager

public class GoogleMapsServices : MonoBehaviour
{
    //API Configuration
    [Tooltip("Your Google Maps API key goes here")]
    [SerializeField] private string apiKey = "YOUR_API_KEY_HERE";

    [Tooltip("Optional: assign DebugOverlayController to see API steps on device")]
    [SerializeField] private DebugOverlayController debugOverlayController;

    //Endpoint Base URLs, this is done so our lines in the API request code are cleaner and easier to read, we just append the parameters to these base URLs
    private const string NEAREST_ROADS_URL = "https://roads.googleapis.com/v1/nearestRoads";
    private const string PLACE_DETAILS_URL  = "https://maps.googleapis.com/maps/api/place/details/json";

    //Stores the most recently resolved road name- readable by other scripts
    public string ActiveLocationName {get;private set;}="";

    //API Response Data Classes, this is so that the JSON info that comes in from the API 
    // can be mapped onto specific C# classes, making them into objects that can be easily used in code 

    //Maps to: {"snappedPoints": [{"placeId":"...}]}
    [System.Serializable] 
    private class SnappedPoint
    {
        public string placeId;
    }

    [System.Serializable]
    private class NearestRoadsResponse
    {
        public SnappedPoint[] snappedPoints;
    }

    //Maps to: {"result": {"name":"..."},"status":OK"}
    [System.Serializable]
    private class PlaceResult
    {
        public string name;
    }

    [System.Serializable]
    private class PlaceDetailsResponse
    {
        public string status;
        public PlaceResult result;
    }

    //Public Entry Point - starting point for the API calls
    // This Public Method here is what will be referenced by the other script to use the "resolved" activeLocationName to trigger other things like pulling the info from the JSON DB
    
    public void GetActiveLocationName(double latitude, double longitude, System.Action<string> onComplete) //Callback means that when this is done, call this function and give the result. This is public so that later other scripts can use this value
    {
        Debug.Log($"[GoogleMapsService] Starting lookup for Lat={latitude:F6}, Lon={longitude:F6}");
        StartCoroutine(GetNearestRoadCoroutine(latitude, longitude, onComplete));
    }

    //Step 1: Nearest Roads API to get the placeId

    private IEnumerator GetNearestRoadCoroutine(double latitude, double longitude, System.Action<string>onComplete)
    {
        //Build the Nearest Roads request URL
        //The points parameter is: latitude, longitude provided by the GPS snapshot- LocationServiceManager
        string url = $"{NEAREST_ROADS_URL}?points={latitude},{longitude}&key={apiKey}";
        Debug.Log($"[GoogleMapsService] Step 1 - Calling Nearest Roads API"); //For our sanity to know that the API call has begun

        //UnityWebRequest handles the HTTP GET request
        //The 'using; block automatically cleans up memory when done 
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 10; //Fail fast if no response within 10 seconds
            Debug.Log($"[GoogleMapsService] Roads Request URL: {url}");

            //Wait here till the request finishes, this pause is why we use a Coroutine
            yield return request.SendWebRequest();

            //Log full response for on-device debugging
            Debug.Log($"[GoogleMapsService] Roads ResponseCode: {request.responseCode}");
            Debug.Log($"[GoogleMapsService] Roads Body: {request.downloadHandler?.text}");
            debugOverlayController?.ShowAPIDebug("roads-http", $"RC:{request.responseCode} URL:{url}");

            //Check if the request failed (no internet, wrong key etc.)
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[GoogleMapsService] Nearest Roads API failed:{request.error}");
                debugOverlayController?.ShowAPIDebug("roads", $"FAILED: {request.error}");
                ActiveLocationName = "ERROR_NETWORK"; // HTTP failure — no internet, quota, timeout
                onComplete?.Invoke(ActiveLocationName);
                yield break;
            }

            //Log the raw response so it can be seen in the debug overlay
            string json = request.downloadHandler.text;
            Debug.Log($"[GoogleMapsService] Nearest Roads raw response: {json}");

            //Parse the JSON response into our established C# classes
            NearestRoadsResponse response = JsonUtility.FromJson<NearestRoadsResponse>(json);

            //Check if we actually have any road data
            if (response == null || response.snappedPoints == null || response.snappedPoints.Length == 0)
            {
                Debug.LogWarning("[GoogleMapsService] No snapped points in response - no road found at these coordinates");
                debugOverlayController?.ShowAPIDebug("roads", "NO SNAPPED POINTS - no road at these coords");
                ActiveLocationName = "ERROR_NO_DATA"; // API worked but no road at these coords
                onComplete?.Invoke(ActiveLocationName);
                yield break;
            }

            //Take the first closes snapped road
            string placeId = response.snappedPoints[0].placeId;
            Debug.Log($"[GoogleMapsService] Step 1 complete - placeId: {placeId}");
            debugOverlayController?.ShowAPIDebug("roads", $"placeId: {placeId}");

            //Move to Step 2 with the placeId we just got
            yield return StartCoroutine(GetRoadNameFromPlaceIdCoroutine(placeId, onComplete));
        }
    }

    // Step 2: Places API to get the road name from the placeId
    private IEnumerator GetRoadNameFromPlaceIdCoroutine(string placeId, System.Action<string> onComplete)
    {
        //Build the Place Details request URL
        //"fields=name" limits the response to just the name field (saves data, and quota?)
        string url = $"{PLACE_DETAILS_URL}?place_id={placeId}&fields=name&key={apiKey}";
        Debug.Log($"[GoogleMapsService] Step 2 - Calling Place Details API");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 10; //Fail fast if no response within 10 seconds
            Debug.Log($"[GoogleMapsService] Places Request URL: {url}");

            yield return request.SendWebRequest();

            //Log full response for on-device debugging
            Debug.Log($"[GoogleMapsService] Places ResponseCode: {request.responseCode}");
            Debug.Log($"[GoogleMapsService] Places Body: {request.downloadHandler?.text}");
            debugOverlayController?.ShowAPIDebug("places-http", $"RC:{request.responseCode} URL:{url}");

            //Error handling check #1 if the request fails 
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[GoogleMapsService] Place Details API failed:{request.error}");
                debugOverlayController?.ShowAPIDebug("places", $"FAILED: {request.error}");
                ActiveLocationName = "ERROR_NETWORK"; // HTTP failure — no internet, quota, timeout
                onComplete?.Invoke(ActiveLocationName);
                yield break;
            }

            string json = request.downloadHandler.text;
            Debug.Log($"[GoogleMapsService] Place Details raw response:{json}"); //our sanity check to see what the raw json data from the API looks like in the console

            PlaceDetailsResponse response = JsonUtility.FromJson<PlaceDetailsResponse>(json); //parsing the json data into the containers we set-up earlier

            //Check the API status field- OK means success. this is different from our check with the Nearest Road
            if (response == null || response.status != "OK" || response.result == null || string.IsNullOrEmpty(response.result.name))
            {
                Debug.LogWarning($"[GoogleMapsService] Place Details returned no name. Status: {response?.status}");
                debugOverlayController?.ShowAPIDebug("places", $"NO NAME - API status: {response?.status}");
                ActiveLocationName = "ERROR_NO_DATA"; // API worked but Places returned no name for this placeId
                onComplete?.Invoke(ActiveLocationName);
                yield break;
            }

            //If we reach these lines of code, it means we have a road name from the API 
            ActiveLocationName = response.result.name;
            Debug.Log($"[GoogleMapsService] Step 2 complete - Road Name:{ActiveLocationName}");
            debugOverlayController?.ShowAPIDebug("places", $"Road: {ActiveLocationName}");

            //Notify the caller the we're done and pass them the road name
            onComplete?.Invoke(ActiveLocationName); 

        }
    }
}