using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Utils;
using TMPro;
using Unity.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(ARAnchorManager))]
[RequireComponent(typeof(ARRaycastManager))]
public partial class AppManager : MonoBehaviour
{
    [SerializeField] [Tooltip("The ARCameraManager which will produce frame events.")]
    private ARCameraManager cameraManager;
    // http://138.246.234.228:40020
    // http://172.28.123.100:40020
    [SerializeField] private string webserviceURL;
    [SerializeField] private GameObject progressBar;
    [SerializeField] private CircularProgress circularProgress;
    [SerializeField] private List<GameObject> places;
    [SerializeField] private GameObject placePrefab;
    [SerializeField] private GameObject timeSelection;
    [SerializeField] private GameObject hint;
    [SerializeField] private GameObject colmapCamera;
    [SerializeField] private TMP_Dropdown timeDropdown;
    [SerializeField] private GameObject info;
    [SerializeField] private GameObject gpsButton;
    
    //Debug Buttons
    [SerializeField] private GameObject screenshot;
    [SerializeField] private GameObject scaleUpButton;
    [SerializeField] private GameObject scaleDownButton;
    
    //Todo add list of prefabs, implement selection and inheritance 

    // Scale factor of the model.
    // Scale sendlinger Tor ~ 5
    // Scale bookcase ~ 0.33f
    [SerializeField] [Tooltip("Scale of the model, so it fits the real world")]
    private float scaleFactor = 5f;

    private ARSessionOrigin _arSessionOrigin;
    private GameObject _spawnedObject;
    private ARRaycastManager _raycastManager;
    private Texture2D _cameraTexture;
    private ScreenInDegree _screenOrientation;
    private Pose _placementPose;
    private XRCameraIntrinsics _cameraIntrinsics;

    private bool _located;
    private bool _placementPoseIsValid;
    private bool _initCameraResolution;
    
    
    private byte[] JPGImage { get; set; }

    private ARCameraManager CameraManager
    {
        get => cameraManager;
        set => cameraManager = value;
    }
    
    private void Awake()
    {
        _arSessionOrigin = FindObjectOfType<ARSessionOrigin>();
        _raycastManager = GetComponent<ARRaycastManager>();
        timeSelection.SetActive(false);
        progressBar.SetActive(false);
        info.SetActive(false);
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        init_TMP_Dropdown();
    }
    
    private void Start()
    {
        timeDropdown.onValueChanged.RemoveListener(OnDropDownChanged);
        timeDropdown.onValueChanged.AddListener(OnDropDownChanged);
        _screenOrientation = gameObject.AddComponent<ScreenInDegree>();
    }
    
    void OnEnable() {
        cameraManager.frameReceived += OnCameraFrameReceived;
    }
 
    void OnDisable() {
        cameraManager.frameReceived -= OnCameraFrameReceived;
    }

    void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        // Change to higher screen resolution then standard 640*480. 
        if (!_initCameraResolution)
        {
            cameraManager.subsystem.currentConfiguration =
                cameraManager.GetConfigurations(Allocator.Temp)[2];
            _initCameraResolution = true;
        }
    }
    
    //https://github.com/CAOR-MINES-ParisTech/colibri-vr-unity-package/blob/ab11dab814461154185a1e868cc5496cc35d6086/Runtime/ExternalConnectors/COLMAPConnector.cs#L662
    private static void ConvertCoordinatesCOLMAPToUnity(ref Vector3 position, ref Quaternion rotation)
    {
        position = Quaternion.Inverse(rotation) * - position;
        position = Vector3.Scale(position, new Vector3(1, -1, 1));
        rotation = Quaternion.Inverse(rotation);
        rotation = new Quaternion(-rotation.x, rotation.y, -rotation.z, rotation.w);
    }
    
    private void Update()
    {
        // After double tapping enter debug mode. This can be turned of for production.
        if (Input.touchCount <= 0)
        {
            return;
        }

        foreach (var touch in Input.touches)
        {
            if (touch.tapCount == 2)
            {
                var listToDeactivate = new List<GameObject>(){screenshot, scaleUpButton, scaleDownButton};
                ChangeButtonsStatus(listToDeactivate);
            }    
        }
    }
    
    private static void ChangeButtonsStatus(IEnumerable<GameObject> uiElements, bool status)
    {
        // Change all delivered buttons, to the defined status (active, inactive).
        foreach (var uiElement in uiElements)
        {
            uiElement.SetActive(status);
        }
    }
    private static void ChangeButtonsStatus(IEnumerable<GameObject> uiElements)
    {
        // Change all delivered buttons, to their opposite status (active, inactive).
        foreach (var uiElement in uiElements)
        {
            uiElement.SetActive(!uiElement.activeSelf);
        }
    }

    public async void PlaceARContent()
    {
        // Start a loading animation, initialize sending  the current image to the server and
        // call CreatePrefab with the servers answer.  
        hint.SetActive(false);
        var currentCameraTransformation = CameraManager.transform;
        circularProgress.StartAnimation();
        UpdateCameraImage();
        var result = await SendToServer();
        circularProgress.Hide(() => CreatePrefab(result, currentCameraTransformation));
        timeSelection.SetActive(true);
        info.SetActive(true);
    }


    private void CreatePrefab(ColMapResult result, Component currentCameraTransformation)
    {
        // Spawn the AR content relative to the users pose. 
        Destroy(_spawnedObject);
        /*
         * Colmap returns the world to the camera coordinate system of an image using a quaternion (QW, QX, QY, QZ) and a translation vector (TX, TY, TZ).
         * In Unity the quaternion is defined as (QX, QY, QZ, QW), so the ordering needs to be changed. This is done in the constructor. 
         * https://colmap.github.io/format.html#images-txt
         */

        
        var colmapPosition = result.Position;
        var colmapRotation = result.Rotation;
        

        // Colmap uses a right hand coordinate system and Unity a left hand coordinate system. 
        // Its helpful to visualize the conversion with some objects.
        ConvertCoordinatesCOLMAPToUnity(ref colmapPosition, ref colmapRotation);

        colmapCamera.transform.position = colmapPosition * scaleFactor;
        colmapCamera.transform.rotation = colmapRotation;
        
        // The camera is defined by the distance to the model center point. 
        // Since we can not move the camera we need to find the point 0,0,0 relative to our camera.
        var colmapPose = new Pose(Vector3.zero, Quaternion.identity);
        var inversePose = colmapCamera.transform.InverseTransformPose(colmapPose);
        
        // Now we add the point position and rotation to our ARCamera by setting the object as a child of camera in
        // the local coordinate system of the camera
        colmapCamera.transform.parent = currentCameraTransformation.transform;
        colmapCamera.transform.localRotation = inversePose.rotation;
        colmapCamera.transform.localPosition = inversePose.position; //*scaleFactor
        
        // Set AR Session as parent, with the now correct position and rotation. 
        colmapCamera.transform.parent = _arSessionOrigin.transform;
        
        // Spawn the game object
        _spawnedObject = Instantiate(placePrefab, colmapCamera.transform);
        _spawnedObject.transform.localScale = new Vector3(scaleFactor,-scaleFactor,scaleFactor);
        
        // Initliaze the DropDown Menu
        OnDropDownChanged(0);
    }

    public void scaleUp()
    {
        scaleFactor += 0.2f;
        updateButtons();
    }
    public void scaleDown()
    {
        scaleFactor -= 0.1f;
        updateButtons();
    }

    private void updateButtons()
    {
        // For Debugging purposes to changing the buttons text to manipulate the models scale. 
        var testScaleUp = "+ " + scaleFactor;
        var testScaleDown = "- " + scaleFactor;
        var scaleUpBtn =  GameObject.Find("Scale up").GetComponent<Button>();
        scaleUpBtn.GetComponentInChildren<Text>().text = testScaleUp;
        var scaleDownBtn =  GameObject.Find("Scale down").GetComponent<Button>();
        scaleDownBtn.GetComponentInChildren<Text>().text = testScaleDown;
    }

    private unsafe void UpdateCameraImage()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            Permission.RequestUserPermission(Permission.Camera);

        // Attempt to get the latest camera image. If this method succeeds,
        // it acquires a native resource that must be disposed (see below).
        if (!CameraManager.TryAcquireLatestCpuImage(out var image)) return;

        // Display some information about the camera image
        //m_ImageInfo.text = string.Format(
        //        "Image info:\n\twidth: {0}\n\theight: {1}\n\tplaneCount: {2}\n\ttimestamp: {3}\n\tformat: {4}",
        //        image.width, image.height, image.planeCount, image.timestamp, image.format);

        // Once we have a valid XRCpuImage, we can access the individual image "planes"
        // (the separate channels in the image). XRCpuImage.GetPlane provides
        // low-overhead access to this data. This could then be passed to a
        // computer vision algorithm. Here, we will convert the camera image
        // to an RGBA texture and draw it on the screen.

        // Choose an RGBA format.
        // See XRCpuImage.FormatSupported for a complete list of supported formats.
        var format = TextureFormat.RGBA32;

        if (_cameraTexture == null || _cameraTexture.width != image.width || _cameraTexture.height != image.height)
            _cameraTexture = new Texture2D(image.width, image.height, format, false);

        // Convert the image to format, flipping the image across the Y axis.
        // We can also get a sub rectangle, but we'll get the full image here.
        var conversionParams = new XRCpuImage.ConversionParams(image, format, XRCpuImage.Transformation.MirrorY);

        // Texture2D allows us write directly to the raw texture data
        // This allows us to do the conversion in-place without making any copies.
        var rawTextureData = _cameraTexture.GetRawTextureData<byte>();
        try
        {
            image.Convert(conversionParams, new IntPtr(rawTextureData.GetUnsafePtr()), rawTextureData.Length);
        }
        finally
        {
            // We must dispose of the XRCpuImage after we're finished
            // with it to avoid leaking native resources.
            image.Dispose();
        }

        // Apply the updated texture data to our texture
        _cameraTexture.Apply();

        // Set the RawImage's texture so we can visualize it.
        // m_RawCameraImage.texture = m_CameraTexture;
        JPGImage = _cameraTexture.EncodeToJPG(75);

        var path = Application.persistentDataPath;
        File.WriteAllBytes(path + "/test.jpg", JPGImage);
    }

    private async Task<ColMapResult> SendToServer()
    {
        // Sends the the current screen image and the phones GPS location to the backendserver and expects the computed 
        //  Pose in a Colmap format as respone. 
        var location = Input.location.lastData;
        if (cameraManager.TryGetIntrinsics(out var intrinsics))
        {
            var rotationZ = _screenOrientation.Rotation;
            var s_intrinsics = intrinsics.ToString();
            var s_location = "longitude" + location.longitude + "," + "latitude" +
                             location.latitude;
            var message = s_intrinsics + "," + s_location + "," + "rotationZ" + rotationZ + ",";
            var result = await UploadToWebService<ColMapResult>(JPGImage, webserviceURL, message);
            return result;
        }

        return null;
    }

    private void init_TMP_Dropdown()
    {
        // To initliaze the entries from the menu, according to the prefab. 
        var dropdownEntries = (from Transform child in placePrefab.transform select child.name).ToList();
        timeDropdown.AddOptions(dropdownEntries);
    }
    
    private void OnDropDownChanged(int value)
    {
        // Sets one a menu the item activate,  at position of value.
        foreach (Transform child in _spawnedObject.transform)
        {
            child.gameObject.SetActive(false);
        }
        _spawnedObject.transform.Find(timeDropdown.options[value].text).gameObject.SetActive(true);
    }

    public void CaptureScreen()
    {
        // For taking screenshots without any UI Elements but the AR content. Can be used for "selfies"
        var listToDeactivate = new List<GameObject>(){info, timeSelection, gpsButton};
        ChangeButtonsStatus(listToDeactivate, false);
        ScreenCapture.CaptureScreenshot("testImage.jpg");
        ChangeButtonsStatus(listToDeactivate, true);

    }


    #region Helper Functions

    private Utils.Promise<T> UploadToWebService<T>(byte[] texture, string url, string message) where T : class
    {
        return new Utils.Promise<T>((resolve, reject) =>
            StartCoroutine(
                Extensions.UploadToWebService(resolve, reject, texture, url, message)
            )
        );
    }

    #endregion
}