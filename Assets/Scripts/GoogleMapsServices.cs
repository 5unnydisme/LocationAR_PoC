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
    private const int MAX_PLACE_ID_CANDIDATES = 3;

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
        public string formatted_address;
        public AddressComponent[] address_components;
    }

    [System.Serializable]
    private class AddressComponent
    {
        public string long_name;
        public string[] types;
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

            int candidateCount = Mathf.Min(MAX_PLACE_ID_CANDIDATES, response.snappedPoints.Length);
            Debug.Log($"[GoogleMapsService] Step 1 complete - trying {candidateCount} placeId candidate(s)");
            debugOverlayController?.ShowAPIDebug("roads", $"candidates: {candidateCount}");

            for (int i = 0; i < candidateCount; i++)
            {
                string placeId = response.snappedPoints[i] != null ? response.snappedPoints[i].placeId : null;
                if (string.IsNullOrEmpty(placeId))
                {
                    continue;
                }

                Debug.Log($"[GoogleMapsService] Trying candidate {i + 1}/{candidateCount} placeId: {placeId}");
                debugOverlayController?.ShowAPIDebug("roads", $"candidate {i + 1}/{candidateCount}: {placeId}");

                string resolvedRoadName = null;
                bool hardFailure = false;

                yield return StartCoroutine(TryGetRoadNameFromPlaceIdCoroutine(placeId, (roadName, hasHardFailure) =>
                {
                    resolvedRoadName = roadName;
                    hardFailure = hasHardFailure;
                }));

                if (hardFailure)
                {
                    ActiveLocationName = "ERROR_NETWORK";
                    onComplete?.Invoke(ActiveLocationName);
                    yield break;
                }

                if (!string.IsNullOrEmpty(resolvedRoadName))
                {
                    ActiveLocationName = resolvedRoadName;
                    onComplete?.Invoke(ActiveLocationName);
                    yield break;
                }
            }

            Debug.LogWarning("[GoogleMapsService] No usable road name found from candidate placeIds");
            debugOverlayController?.ShowAPIDebug("places", "NO USABLE ROAD NAME after candidate attempts");
            ActiveLocationName = "ERROR_NO_DATA";
            onComplete?.Invoke(ActiveLocationName);
            yield break;
        }
    }

    // Step 2: Places API to get the road name from a placeId candidate.
    // onComplete: roadNameOrNull, hardFailure
    private IEnumerator TryGetRoadNameFromPlaceIdCoroutine(string placeId, System.Action<string, bool> onComplete)
    {
        //Build the Place Details request URL
        // Include route-capable data before fallback to plain place name.
        string url = $"{PLACE_DETAILS_URL}?place_id={placeId}&fields=name,formatted_address,address_component&key={apiKey}";
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
                onComplete?.Invoke(null, true);
                yield break;
            }

            string json = request.downloadHandler.text;
            Debug.Log($"[GoogleMapsService] Place Details raw response:{json}"); //our sanity check to see what the raw json data from the API looks like in the console

            PlaceDetailsResponse response = JsonUtility.FromJson<PlaceDetailsResponse>(json); //parsing the json data into the containers we set-up earlier

            //Check the API status field- OK means success.
            if (response == null || response.status != "OK" || response.result == null)
            {
                Debug.LogWarning($"[GoogleMapsService] Place Details returned no name. Status: {response?.status}");
                debugOverlayController?.ShowAPIDebug("places", $"NO NAME - API status: {response?.status}");
                onComplete?.Invoke(null, false);
                yield break;
            }

            string source;
            string resolvedRoadName = ExtractBestRoadLabel(response, out source);
            if (string.IsNullOrEmpty(resolvedRoadName))
            {
                Debug.LogWarning($"[GoogleMapsService] Place Details had no usable road label. Status: {response.status}");
                debugOverlayController?.ShowAPIDebug("places", $"NO USABLE LABEL - status: {response.status}");
                onComplete?.Invoke(null, false);
                yield break;
            }

            Debug.Log($"[GoogleMapsService] Step 2 complete - Road Name: {resolvedRoadName} (source: {source})");
            debugOverlayController?.ShowAPIDebug("places", $"Road: {resolvedRoadName} ({source})");

            onComplete?.Invoke(resolvedRoadName, false);
        }
    }

    private string ExtractBestRoadLabel(PlaceDetailsResponse response, out string source)
    {
        source = "none";
        if (response == null || response.result == null)
        {
            return null;
        }

        string routeName = ExtractRouteFromAddressComponents(response.result.address_components);
        if (!string.IsNullOrEmpty(routeName))
        {
            source = "route";
            return routeName;
        }

        if (!string.IsNullOrEmpty(response.result.name))
        {
            source = "name";
            return response.result.name;
        }

        if (!string.IsNullOrEmpty(response.result.formatted_address))
        {
            source = "formatted_address";
            int commaIndex = response.result.formatted_address.IndexOf(',');
            if (commaIndex > 0)
            {
                return response.result.formatted_address.Substring(0, commaIndex).Trim();
            }

            return response.result.formatted_address.Trim();
        }

        return null;
    }

    private string ExtractRouteFromAddressComponents(AddressComponent[] components)
    {
        if (components == null)
        {
            return null;
        }

        for (int i = 0; i < components.Length; i++)
        {
            AddressComponent component = components[i];
            if (component == null || string.IsNullOrEmpty(component.long_name) || component.types == null)
            {
                continue;
            }

            for (int j = 0; j < component.types.Length; j++)
            {
                if (component.types[j] == "route")
                {
                    return component.long_name;
                }
            }
        }

        return null;
    }
}