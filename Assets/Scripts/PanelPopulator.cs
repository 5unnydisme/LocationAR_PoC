using UnityEngine;
using TMPro;

public class PanelPopulator : MonoBehaviour
{
    #region UI References to be assigned in the Inspector
    //TMP UI references that will be assigned
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI typeText;
    public TextMeshProUGUI conditionText;
    public TextMeshProUGUI yearBuiltText;
    public TextMeshProUGUI descriptionText;

    public TextMeshProUGUI builtByText; 
    public TextMeshProUGUI budgetText;
    public TextMeshProUGUI lastInspectionText; 
    public TextMeshProUGUI nextInspectionText; 

    public TextMeshProUGUI maintenanceDateText;
    public TextMeshProUGUI maintenanceDescriptionText;
    public TextMeshProUGUI maintenanceContractorText;
    public TextMeshProUGUI maintenanceBudgetText;    
    #endregion

public void Populate(LocationData data)
    {
        if (data == null) return; //safety check to avoid null reference errors
        
      //Setting up the headers and the pill container info in the panel here  
        nameText.text = data.name ?? ""; //?? puts the string value inside the quotations
        typeText.text = data.type ?? "";
        conditionText.text = data.condition ?? "";
        yearBuiltText.text = "Built " + data.yearBuilt;
        descriptionText.text = data.description ?? "";

        if (data.infrastructureDetails != null)
        {
            builtByText.text = "Built By:" + (data.infrastructureDetails.builtBy ?? "");
            budgetText.text = "Budget:" + (data.infrastructureDetails.budget ?? "");
            lastInspectionText.text = "Last Inspection:" + (data.infrastructureDetails.lastInspection ?? "");
            nextInspectionText.text = "Next Inspection:" + (data.infrastructureDetails.nextInspection ?? "");
        }

        if (data.maintenanceHistory !=null && data.maintenanceHistory.Length >0)
        {
            var m = data.maintenanceHistory[0]; //just taking the first entry for now, can be expanded later based on the order selected? 
            maintenanceDateText.text = "Last Maintenance:" + (m.date ?? "");
            maintenanceDescriptionText.text = "Maintenance Description:" + (m.description ?? "");
            maintenanceContractorText.text = "Maintenance Contractor:" + (m.contractor ?? "");
            maintenanceBudgetText.text = "Maintenance Budget:" + (m.budget ?? "");
        }

    }

}