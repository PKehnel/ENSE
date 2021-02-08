using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;

public class InfoText : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown timeDropdown;
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private GameObject infoImage;
    [SerializeField] private GameObject placePrefab;
    [SerializeField] private float infoTextScale = 0.7f;
    
    private ScreenOrientation _currentScreenOrientation;
    private ScreenInDegree _screenOrientation;
    private Dictionary<int, string> stories = new Dictionary<int, string>();

    private void Start()
    {
        _screenOrientation = gameObject.AddComponent<ScreenInDegree>();
        InitializeStories();
        timeDropdown.onValueChanged.RemoveListener(SwitchText);
        timeDropdown.onValueChanged.AddListener(SwitchText);
    }
    
    /// <summary>
    /// Load the stories from a model into a dictionary.  
    /// </summary>
    private void InitializeStories()
    {
        var storyTexts = (from Transform child in placePrefab.transform select child.GetComponent<Text>().text).ToList();
        var key = 0;
        foreach (var text in storyTexts)
        {
            stories[key] = text;
            key++;
        }
    }
    
    /// <summary>
    /// Activate and Deactivate the info text. Also resize it to the current screen rotation. 
    /// </summary>
    public void DisplayInfo()
    {
        infoImage.SetActive(!infoImage.activeSelf);
        if (infoImage.activeSelf)
        {
            var image = infoImage.GetComponent<Image>();
            image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, infoTextScale*_screenOrientation.Width);
            image.rectTransform.ForceUpdateRectTransforms();
            var storyNumber = timeDropdown.value;
            SwitchText(storyNumber);
            //todo resize when screen is rotated, inherit place prefab from app manager 
        }
        else
            infoText.text = "";
    }

    private void SwitchText(int storyNumber)
    {
        infoText.text = stories[storyNumber];
    }
}
