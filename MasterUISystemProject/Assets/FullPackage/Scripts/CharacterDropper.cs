﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CharacterDropper : MonoBehaviour {

    public Toggle randomToggle;
    public Text modelLabel;
    public RectTransform modelList;
    public ToggleGroup modelToggleGroup;
    public RectTransform dropCharacterSelectPanel;
    public GameObject avatar;
    public Dropdown newCharWanderSelect;
    public Dropdown charEditWanderSelect;
    public RectTransform charOptionsPanel;
    public Dropdown charOptionsWanderSelect;
    public Image buttonImage;
    public Projector radiusProjector;
    public WidgetCanvasManager canvasManager;

    private List<GameObject> loadedCharacters = new List<GameObject>();
    private List<Toggle> toggleList = new List<Toggle>();
    private GameObject charToDrop;
    private GameObject charToEdit;
    private NavMeshWander navMeshWanderToEdit;
    private bool dropModeOn;
    private bool charEditModeOn;
    private bool radiusSelectMode;
    private bool modelOptionsGreyed;
    private bool setCharLocalRadius = false;
    private Vector3 dropLocation = Vector3.zero;
    private Camera mouseCam;
    private RaycastHit hit;
    private int randomCharIndex = -1;

    private NavMeshWander.WanderMode prevWanderMode = NavMeshWander.WanderMode.Idle;

    void OnDisable()
    {
        Destroy(charToDrop);
        ToggleMode(false);
        dropModeOn = false;
        charEditModeOn = false;
        radiusSelectMode = false;
        modelOptionsGreyed = false;
        radiusProjector.gameObject.SetActive(false);
        CloseCharacterOptions();
    }

    void OnEnable()
    {
        ToggleMode(false);
        dropModeOn = false;
        charEditModeOn = false;
        radiusSelectMode = false;
        modelOptionsGreyed = false;
        radiusProjector.gameObject.SetActive(false);
        CloseCharacterOptions();
    }

    public void ToggleMenu()
    {
        dropModeOn = !dropModeOn;
        ToggleMode(dropModeOn);
    }

    private void ToggleMode(bool mode)
    {
        CloseCharacterOptions();
        radiusProjector.gameObject.SetActive(false);

        if (mode)
        {
            dropCharacterSelectPanel.gameObject.SetActive(true);
            modelToggleGroup.SetAllTogglesOff();
            randomToggle.isOn = true;
            charToDrop = GetCharacter();
            buttonImage.color = Color.red;
        }
        else
        {
            dropCharacterSelectPanel.gameObject.SetActive(false);
            Destroy(charToDrop);
            buttonImage.color = Color.white;
        }
    }

    private GameObject GetCharacter()
    {
        GameObject returnChar = null;
        if (randomToggle.isOn)
            returnChar = CreateRandomChar();
        else
        {
            toggleList = modelToggleGroup.ActiveToggles().ToList<Toggle>();
            foreach (GameObject character in loadedCharacters)
            {
                if(character.name == toggleList[0].name)
                {
                    modelLabel.text = "Model: " + character.name;
                    returnChar = Instantiate(character, dropLocation, Quaternion.identity) as GameObject;
                }
            }
        }
        
        returnChar.GetComponent<CapsuleCollider>().enabled = false;
        returnChar.GetComponent<NavMeshAgent>().enabled = false;
        returnChar.GetComponent<NavMeshWander>().enabled = false;
        return returnChar;
    }

    private GameObject CreateRandomChar()
    {
        /*
        int randomIndex = (int)Random.Range(0, loadedCharacters.Count - 1);
        modelLabel.text = "Model: " + loadedCharacters[randomIndex].name;
        */
        randomCharIndex++;
        if (randomCharIndex >= loadedCharacters.Count)
            randomCharIndex = 0;
        modelLabel.text = "Model: " + loadedCharacters[randomCharIndex].name;   
        return Instantiate(loadedCharacters[randomCharIndex], dropLocation, Quaternion.identity) as GameObject;
    }

    void Start()
    {
        charOptionsPanel.gameObject.SetActive(false);
        Object[] tmpArray = Resources.LoadAll("Characters/");
        foreach (Object obj in tmpArray)
        { 
            loadedCharacters.Add(obj as GameObject);
            GameObject newToggle = Instantiate(Resources.Load("CustomToggle"), Vector3.zero, Quaternion.identity) as GameObject;
            newToggle.name = obj.name;
            newToggle.transform.SetParent(modelList.transform);
            newToggle.transform.SetAsLastSibling();
            newToggle.transform.FindChild("Label").GetComponent<Text>().text = obj.name;
            newToggle.GetComponent<Toggle>().group = modelToggleGroup;
            modelToggleGroup.RegisterToggle(newToggle.GetComponent<Toggle>());
        }
    }

    void Update()
    {
        /// here is where we will do the raycast and show a temporary character where it will be dropped
        /// we will have a reference to the temporary character and update its position to wherever the raycast from the mouse is pointing
        /// when "dropping" we will just stop updating the position
        /// need to make sure the temporary character is deleted/removed whenever the dropcharacter button is disabled (this script)   

        // this if statement is to make control the toggle group and random toggle
        #region toggles
        if (randomToggle.isOn)
        {
            if(modelToggleGroup.AnyTogglesOn())
            {
                modelToggleGroup.SetAllTogglesOff();
            }

            if (!modelOptionsGreyed)
            {
                foreach (Toggle tog in modelToggleGroup.GetComponentsInChildren<Toggle>())
                {
                    tog.GetComponentInChildren<Text>().color = Color.grey;
                }
                modelOptionsGreyed = true;
            }
        }
        else
        {
            if (modelOptionsGreyed)
            {
                foreach (Toggle tog in modelToggleGroup.GetComponentsInChildren<Toggle>())
                {
                    tog.GetComponentInChildren<Text>().color = Color.black;
                }
                modelOptionsGreyed = false;
            }
        }

        // this if is to make sure that a toggle in the toggle group is on if the random toggle is off
        if (!modelToggleGroup.AnyTogglesOn() && !randomToggle.isOn)
        {
            modelToggleGroup.GetComponentInChildren<Toggle>().isOn = true;
            modelToggleGroup.NotifyToggleOn(modelToggleGroup.GetComponentInChildren<Toggle>());
        }
        #endregion

        // this finds the camera whose viewport contains the mouse cursor
        mouseCam = FindMouseCamera();

        


        if (dropModeOn)
        {
            // GENERAL RAYCAST INTO THE VIRTUAL WORLD
            if(mouseCam != null)
                Physics.Raycast(mouseCam.ScreenPointToRay(Input.mousePosition), out hit, 1000, ~(1 << 9 | 1 << 8));

            if (!charEditModeOn)
            {

                #region makes sure the displayed char is correct
                if (charToDrop == null)
                    charToDrop = GetCharacter();

                if (modelToggleGroup.ActiveToggles().Count() > 0
                    && charToDrop.name != modelToggleGroup.ActiveToggles().ToList()[0].name + "(Clone)"
                    && !randomToggle.isOn)
                {
                    Destroy(charToDrop);
                    charToDrop = GetCharacter();
                }
                #endregion

                /// CODE FOR MANAGING AND POSITIONING A TEMPORARY AVATAR FOR DROPPING
                //sets the position of the temp avatar
                if (mouseCam != null && !radiusSelectMode)
                {
                    if (hit.point != null)
                        dropLocation = hit.point;
                    else
                        dropLocation = avatar.transform.position + avatar.transform.forward * 2f;
                }

                if (charToDrop != null && !radiusSelectMode)
                    charToDrop.transform.position = dropLocation;


                //if we are pointing at an existing avatar
                if (hit.transform != null && hit.transform.GetComponent<NavMeshWander>() != null)
                {
                    if (Input.GetMouseButtonDown(0))
                        OpenCharacterOptions();
                    if (!radiusSelectMode && hit.transform != charToDrop.transform)
                        charToDrop.SetActive(false);
                }
                else
                {
                    if (!charToDrop.activeSelf && hit.transform != null)
                        charToDrop.SetActive(true);
                }

                //if we are in radius select mode, set the size of the projector
                if (radiusSelectMode)
                {
                    if ( mouseCam != null && Physics.Raycast(mouseCam.ScreenPointToRay(Input.mousePosition), out hit, 1000, ~(3 << 8)))
                        radiusProjector.orthographicSize = (charToDrop.transform.position - hit.point).magnitude;
                }
                

                //if we right click while hover over something that isnt an existing avatar
                if (Input.GetMouseButtonDown(1) && hit.transform != null && hit.transform.GetComponent<NavMeshWander>() == null)
                {
                    if (!radiusSelectMode)
                    {
                        if ((NavMeshWander.WanderMode)newCharWanderSelect.value == NavMeshWander.WanderMode.Local)
                        {
                            Debug.Log("dropping character in local wander");
                            charToDrop.GetComponent<NavMeshWander>().localWanderCenter = hit.point;
                            charToDrop.GetComponent<NavMeshWander>().enabled = true;
                            radiusProjector.gameObject.SetActive(true);
                            radiusProjector.transform.position = charToDrop.transform.position + new Vector3(0, 2, 0);
                            radiusSelectMode = true;
                        }
                        else
                        {
                            charToDrop.GetComponent<CapsuleCollider>().enabled = true;
                            charToDrop.GetComponent<NavMeshAgent>().enabled = true;
                            charToDrop.GetComponent<NavMeshWander>().enabled = true;
                            charToDrop.GetComponent<NavMeshWander>().mode = (NavMeshWander.WanderMode)newCharWanderSelect.value;
                            charToDrop = GetCharacter();
                        }
                    }
                    else
                    {
                        
                        charToDrop.GetComponent<CapsuleCollider>().enabled = true;
                        charToDrop.GetComponent<NavMeshAgent>().enabled = true;
                        charToDrop.GetComponent<NavMeshWander>().enabled = true;
                        charToDrop.GetComponent<NavMeshWander>().localWanderRadius = radiusProjector.orthographicSize;
                        charToDrop.GetComponent<NavMeshWander>().mode = (NavMeshWander.WanderMode)newCharWanderSelect.value;
                        radiusProjector.gameObject.SetActive(false);
                        radiusSelectMode = false;
                        charToDrop = GetCharacter();
                    }
                }

                //drop the temp avatar
                if (Input.GetMouseButtonDown(2))
                {
                    Destroy(charToDrop);
                    charToDrop = GetCharacter();
                }
            }// chareditmode
        }
        else // not in drop mode
        {
            // GENERAL RAYCAST INTO THE VIRTUAL WORLD
            if (mouseCam != null)
                Physics.Raycast(mouseCam.ScreenPointToRay(Input.mousePosition), out hit, 1000, ~(1 << 9));

            //if we are in radius select mode, set the size of the projector
            if (radiusSelectMode)
            {
                if (mouseCam != null && Physics.Raycast(mouseCam.ScreenPointToRay(Input.mousePosition), out hit, 1000, ~(3 << 8)))
                    radiusProjector.orthographicSize = (charToEdit.transform.position - hit.point).magnitude;
            }

            //if we are pointing at an existing avatar
            if (hit.transform != null && hit.transform.GetComponent<NavMeshWander>() != null)
                if (Input.GetMouseButtonDown(0) && !charEditModeOn)
                    OpenCharacterOptions();

            if (charEditModeOn)
            {
                
                if ((NavMeshWander.WanderMode)charEditWanderSelect.value == NavMeshWander.WanderMode.Local && !radiusSelectMode && !setCharLocalRadius)
                {
                    Debug.Log("char edit wander is local");
                    radiusProjector.gameObject.SetActive(true);
                    radiusProjector.transform.position = charToEdit.transform.position + new Vector3(0, 2, 0);
                    radiusSelectMode = true;
                }
            }

            if (radiusSelectMode && Input.GetMouseButtonUp(1))
            {
                radiusSelectMode = false;
                setCharLocalRadius = true;
                radiusProjector.gameObject.SetActive(false);
            }
        }
    }

    public void OpenCharacterOptions()
    {
        Destroy(charToDrop);
        charEditModeOn = true;
        setCharLocalRadius = false;
        charToEdit = hit.transform.gameObject;
        navMeshWanderToEdit = charToEdit.GetComponent<NavMeshWander>();
        prevWanderMode = navMeshWanderToEdit.mode;
        navMeshWanderToEdit.mode = NavMeshWander.WanderMode.Idle;
        charOptionsPanel.gameObject.SetActive(true);
        charOptionsPanel.transform.position = Input.mousePosition;
        charOptionsWanderSelect.value = (int)navMeshWanderToEdit.mode;

    }

    public void ApplyOptions()
    {
        prevWanderMode = (NavMeshWander.WanderMode)charOptionsWanderSelect.value;

        if (prevWanderMode == NavMeshWander.WanderMode.Local)
        {
            navMeshWanderToEdit.localWanderCenter = charToEdit.transform.position;
            navMeshWanderToEdit.localWanderRadius = radiusProjector.orthographicSize;
        }
       

        CloseCharacterOptions();
    }

    public void CloseCharacterOptions()
    {
        charToEdit = null;
        charEditModeOn = false;
        radiusSelectMode = false;
        if (navMeshWanderToEdit != null)
            navMeshWanderToEdit.mode = prevWanderMode;
        charOptionsPanel.gameObject.SetActive(false);
    }

    public void DeleteCharacter()
    {
        Destroy(charToEdit);
        charToEdit = null;
        CloseCharacterOptions();
    }

    private Camera FindMouseCamera()
    {
        List<Camera> camList = (from cam in GameObject.FindObjectsOfType<Camera>() where cam.targetTexture == null select cam).ToList();
        foreach(Camera cam in camList)
        {
            if(Input.mousePosition.x > cam.pixelRect.xMin && Input.mousePosition.x < cam.pixelRect.xMax
                && Input.mousePosition.y > cam.pixelRect.yMin && Input.mousePosition.y < cam.pixelRect.yMax)
            {
                return cam;
            }
        }
        return null;
    }
}
