using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;

public class GetGpsLocation : MonoBehaviour
{
    [SerializeField]
    private Button gpsButton;
    
    private bool _isUpdating = false;

    private void Update()
    {
        
        if (!_isUpdating)
        {
            StartCoroutine(GetLocation());
            _isUpdating = true;
        }
    }
    IEnumerator GetLocation()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
            Permission.RequestUserPermission(Permission.CoarseLocation);
        }
        // First, check if user has location service enabled
        if (!Input.location.isEnabledByUser)
            yield return new WaitForSeconds(10);

        // Start service before querying location
        Input.location.Start(1f,1f);
        
        // Wait until service initializes
        var maxWait = 10;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // Service didn't initialize in 20 seconds
        if (maxWait < 1)
        {
            print("Timed out");
            yield break;
        }

        // Connection has failed
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            print("Unable to determine device location");
            yield break;
        }
        else
        {
            print("Success");
            //ChangeColor();
        }
        // Stop service if there is no need to query location updates continuously
        _isUpdating = false;
        Input.location.Stop();
    }
    public void Wrapper()
    {
        StartCoroutine(GetLocation());
        _isUpdating = true;
    }
    private void ChangeColor()
    {
        ColorBlock colors = gpsButton.colors;
        colors.normalColor = new Color(0, 75 ,0, 255); // Dark Green 
        gpsButton.colors = colors;
    }
    
}    