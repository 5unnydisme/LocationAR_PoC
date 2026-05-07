// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;

// //Panel manager is controlling all the UI panels by referencing and pulling info from LocationServiceManager and the LocationResolver script
// //This is done to keep the UI logic separate from the core location logic

// public class PanelManager_BackUp : MonoBehaviour
// {
//     #region Inspector Configuration
//     [Header("UI Panels")]
//     [Tooltip("The intro screen shown when the app starts")]
//     public GameObject introScreen;

//     [Tooltip("The Logo shown when the app starts")]
//     public GameObject logo;

//     [Tooltip("Panel shown when no location data available")]
//     public GameObject noLocationPanel; 

//     [Tooltip ("Panel shown when user is near Target 1")]
//     public GameObject target1Panel;

//     [Tooltip ("Pabel shown when user is near Target 2")]
//     public GameObject target2Panel; 

//     [Tooltip ("Panel shown when user is near Target 3")]
//     public GameObject target3Panel; 

//     [Header("UI Controls")]
//     [Tooltip("Button that triggers location resolver to see nearby targets")]
//     public Button getInfoButton;

//     [Tooltip("Button to get GPS location from user")]
//     public Button getLocationButton;

//     [Header("Script References")]
//     [Tooltip("Reference to the DebugOverlayController for GPS status")]
//     public DebugOverlayController debugOverlayController;

//     [Header("Script References")]
//     [Tooltip("Reference to the LocationServiceManager script")]
//     public LocationServiceManager locationServiceManager; 

//     [Tooltip("Reference to the LocationResolver script")]
//     public LocationResolver locationResolver;

//     //Reference to the JSON loader that holds parsed LocationData
//     public LocationJSONLoader locationJsonLoader; 

//     //Reusable info panel GameObject (the panel that has the PanelPopulator script attached)
//     public GameObject infoPanelGameObject;

//     //PanelPopulator component on the reusable info panel
//     public PanelPopulator infoPanelPopulator;

//     //GoogleMapsServices script reference for the GetInfo button logic
//     public GoogleMapsServices googleMapsServices;
//     #endregion 

//     #region Unity Lifycycle Methods //called when the script starts, initial UI States 

//     void Start()
//     {
//         SetupInitialUI();   
//     }
//     #endregion 

//     #region Initial Set-up Methods 
//     private void SetupInitialUI()
//     {
//         //Hide all panels to avoid overlapping 
//         HideAllPanels();

//         //Show only the intro screen 
//         introScreen.SetActive(true); 
        
//         //Get info button is disbaled, but the GetGPS button is active 
//         getLocationButton.interactable = true; 
//         getInfoButton.interactable = false;
//     }
//     #endregion 

//     #region Button Event Methods (called by UI buttons) 
//     //called when the Get Location button is pressed
//     public void OnGetLocationButtonPressed()
//     {
//         getLocationButton.interactable = false; //disabled button to prevent multiple presses 
//         //Requests location using existing method
//         if (locationServiceManager != null)
//         {
//             locationServiceManager.RequestLocation(); 
//             //Check GPS status from DebugOverlayController after a short delay
//             Invoke("CheckGPSStatusFromDebugOverlay", 2f); //delay to allow GPS to initialize
//         }
//         else
//         {
//             getLocationButton.interactable = true; //Re-enable button on error
//         }
//     }

//     public void OnGetInfoButtonPressed() //called when the GetInfoButton is pressed
//     {
//         if (locationServiceManager == null || googleMapsServices ==null)
//         return;
//         LocationServiceManager.LocationSnapshot snap = locationServiceManager.LastLocationSnapshot;
//         googleMapsServices.GetActiveLocationName(snap.latitude, snap.longitude, OnRoadNameReceived);
//     }

//     private void OnRoadNameReceived(string roadName)
//     {
//         HideAllPanels(); //Hide all panels before showing the info panel, prevents overlaps
//             LocationData locData = locationJsonLoader.GetLocationDataByName(roadName);

//     if (debugOverlayController != null)
//         debugOverlayController.ShowLookupDebug(roadName, locData?.name);
//     else
//         Debug.Log($"API road name='{roadName}' -> matched='{(locData == null ? "null" : locData.name)}'");

//     if (locData != null)
//     {
//         infoPanelPopulator.Populate(locData);
//         infoPanelGameObject.SetActive(true);
//     }
//     else
//     {
//         noLocationPanel.SetActive(true);
//     }
//     }
//       #region Old LocationResolver Logic
//     //   if (locationResolver == null)
//     //     {
//     //         return;
//     //     }  
//     //     //Call LocationResolver to take GPS snapshot and check location
//     //     locationResolver.ResolveLocation(); 
//     //     //Give LocationResolver a moment to update its state, then show appropriate panel
//     //     Invoke("ShowPanelBasedOnLocationResult", 0.1f);
//         #endregion 
//     #endregion

//     #region GPS Status Check (via DebugOverlayController)
//     //Checks the GPS status by reading from the DebugOverlayController 
//     //Avoiding confusion by changing the Status text in two different scripts 

//     private void CheckGPSStatusFromDebugOverlay()
//     {
//         if(debugOverlayController == null || locationServiceManager == null)
//         {
//             getLocationButton.interactable = true; 
//             return;
//         }

//         //Read the GPS status from the DebugOverlayController's monitoring
//         bool isGPSReady = locationServiceManager.State == LocationServiceManager.LocationServiceState.Running && locationServiceManager.HasValidLocation; 

//         if (isGPSReady)
//         {
//             //GPS is ready, then we want to enable the Get Info button
//             getInfoButton.interactable = true; 
//         }

//         else
//         {
//             //in the event GPS isn't ready, we want to check-back in and see what the dealio is
//             LocationServiceManager.LocationServiceState currentState = locationServiceManager.State; 
//             switch (currentState)
//             {
//                 case LocationServiceManager.LocationServiceState.Running: 
//                 //Still waiting on location fix, we want to check again
//                 Invoke("CheckGPSStatusFromDebugOverlay", 2000f); //check again after 2 second break
//                 break; 

//                 case LocationServiceManager.LocationServiceState.Initializing:
//                 //Still, initilizing the GPS, we wanna check again
//                 Invoke("CheckGPSStatusFromDebugOverlay", 2000f); //check again after 2 second 
//                 break;

//                 default: 
//                 //GPS has failed or in some other state, we want to re-enable the Get Location button to let the user try again getLocationButton.interactable = true; break; 
//                 getLocationButton.interactable = true; 
//                 break;
//             }
//         }
        
//     }
//     #endregion 

//     #region Panel Management Methods 

//     public void HideAllPanels() //Hides all the panels to prevent overlap
//     {
//      introScreen.SetActive(false); 
//      noLocationPanel.SetActive(false); 
//      target1Panel.SetActive(false); 
//      target2Panel.SetActive(false); 
//      target3Panel.SetActive(false);
//      infoPanelGameObject.SetActive(false);    
//     }

//     private void ShowPanelBasedOnLocationResult()
//     {
//         HideAllPanels(); 

//          LocationResolver.LocationDataAvailabilityState state = locationResolver.AvailabilityState;
        
//         switch (state)
//         {
//             case LocationResolver.LocationDataAvailabilityState.LocationDataAvailable:
//                 ShowTargetPanelBasedOnLocationName();
//                 break;
                
//             case LocationResolver.LocationDataAvailabilityState.NoLocationDataAvailable:
//                 noLocationPanel.SetActive(true);
//                 break;
                
//             case LocationResolver.LocationDataAvailabilityState.GPSNotReady:
//                 introScreen.SetActive(true);
//                 getLocationButton.interactable = true;
//                 getInfoButton.interactable = false;
//                 break;
                
//             case LocationResolver.LocationDataAvailabilityState.GPSAccuracyTooLow:
//                 introScreen.SetActive(true);
//                 getLocationButton.interactable = true;
//                 break;
//         }
//     }

//     private void ShowTargetPanelBasedOnLocationName()
//     {
//         string activeLocationName = locationResolver.ActiveLocationName;

//         //try JSON lookup first
//         if (locationJsonLoader != null && infoPanelPopulator != null && infoPanelGameObject != null)
//         {
//             LocationData locData = locationJsonLoader.GetLocationDataByName(activeLocationName);
//              // show lookup result on debug overlay (safe even if debugOverlay not assigned)
//              if (debugOverlayController != null)
//                 debugOverlayController.ShowLookupDebug(activeLocationName, locData?.name);
//             else
//                  Debug.Log($"Lookup activeName='{activeLocationName}' -> matched='{(locData==null? "null": locData.name)}'");

//              if (locData != null)
//                   {
//                     HideAllPanels();
//                     infoPanelPopulator.Populate(locData);
//                     infoPanelGameObject.SetActive(true);
//                     return;
//                  }
//         }

//         #region Old Code for showing static panels based on location name
//         // string activeLocationName = LocationResolver.ActiveLocationName;

//         // if (activeLocationName.Contains("Academic Area Pavement") || activeLocationName.Contains("target1"))
//         // {
//         //     target1Panel.SetActive(true);
//         // }
//         // else if (activeLocationName.Contains("Service Road ESB-2 Building") || activeLocationName.Contains("target2"))
//         // {
//         //     target2Panel.SetActive(true);
//         // }
//         // else if (activeLocationName.Contains("Fourth Avenue Road") || activeLocationName.Contains("target3"))
//         // {
//         //     target3Panel.SetActive(true);
//         // }
//         // else
//         // {
//         //     noLocationPanel.SetActive(true);
//         // }
//         #endregion

//     }
//    #endregion



//     public void ShowIntroScreen()
//     {
//             #region Helper Methods for UI Buttons
//     //Go back to intro screen and can be hooked to back buttons in the future
//         HideAllPanels();
//         introScreen.SetActive(true); 

//         //Reset button states based on the GPS status from DebugOverlay
//         if (locationServiceManager != null && locationServiceManager.State == LocationServiceManager.LocationServiceState.Running && locationServiceManager.HasValidLocation)
//         {
//           getInfoButton.interactable = false; 
//           getLocationButton.interactable = true;   
//         }
//         else
//         {
//             getLocationButton.interactable = true; 
//             getInfoButton.interactable = false;
//         }
//             #endregion
//     }
// }
