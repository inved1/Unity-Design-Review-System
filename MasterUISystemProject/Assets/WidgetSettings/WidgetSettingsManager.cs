﻿using UnityEngine;
using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Xml;
using System.Text;
using System.Reflection;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class WidgetSettingsManager : MonoBehaviour{

	public string settingsFileFolderPath;

	public RectTransform widgetList;
	public RectTransform fieldsList;

	private Type activeSettingsFileType;

	private List<string> loadedFiles = new List<string>();

	void Start()
	{
		LoadSettingsFiles ();
		//GenerateSettingsFileButtons();
		//XmlIO.Save(new MiniMapSettings(), settingsFileFolderPath + "\\MiniMapSettings.sets");
	}

	public void LoadSettingsFiles()
	{
		if (string.IsNullOrEmpty (settingsFileFolderPath)) 
			settingsFileFolderPath = Application.dataPath;	

		List<string> settingsFiles = new List<string>(Directory.GetFiles(settingsFileFolderPath, "*.sets"));
		foreach(string file in settingsFiles)
		{
			string tmpFile = file.Substring(file.LastIndexOf("\\") + 1);
			Type fileType = System.Type.GetType(tmpFile.Substring(0,tmpFile.Length - 5));
			WidgetSettings loadedFile = XmlIO.Load(file, fileType) as WidgetSettings;
			loadedFile.ApplySettings();
			loadedFiles.Add(tmpFile);

		}

	}

	public void GenerateSettingsFileButtons()
	{

		// we need to clear out the children in the list before we generate new ones
		for (int i = 0; i < widgetList.transform.childCount; i ++)
		{
			widgetList.transform.GetChild(i).gameObject.SetActive(false);
			Destroy(widgetList.transform.GetChild(i).gameObject);

		}

		foreach(string name in loadedFiles)
		{
			GameObject newBut = Instantiate(Resources.Load("WidgetSettings/WidgetButton")) as GameObject;
			newBut.transform.SetParent(widgetList.transform);
			newBut.GetComponentInChildren<Text>().text = name;

			// code to add a listener to the button OnClicked() event
			EventTrigger eTrigger = newBut.GetComponent<EventTrigger>();
			EventTrigger.TriggerEvent trigger = new EventTrigger.TriggerEvent();
			
			// The following line adds the DisplaySettingsFile function as a listener to the EventTrigger on the button we instantiated.
			trigger.AddListener((eventData)=>DisplaySettingsFile(newBut));

			// The next line adds the entry we created to the Event Trigger of the instantiated button.
			// The entry consists of two parts, the listener we set up earlier, and the EventTriggerType.
			// The EventTriggerType tells the EventTrigger when to send out the message that the event has occured.
			// We use PointerClick so we know when the used has clicked on a button.
			EventTrigger.Entry entry = new EventTrigger.Entry(){callback = trigger, eventID = EventTriggerType.PointerClick};
			eTrigger.triggers.Add(entry);

		}
	}

	public void DisplaySettingsFile(GameObject clickedButton)
	{
		string file = settingsFileFolderPath + "/" + clickedButton.GetComponentInChildren<Text> ().text;

		string tmpFile = clickedButton.GetComponentInChildren<Text> ().text.Substring(0, clickedButton.GetComponentInChildren<Text> ().text.Length - 5);
		Type fileType = System.Type.GetType(tmpFile);
		activeSettingsFileType = fileType;

		FieldInfo[] fieldsArray = fileType.GetFields ();

		for (int i = 0; i < fieldsArray.Length; i++) 
		{
			GameObject fieldUI = Instantiate (Resources.Load ("WidgetSettings/" + fieldsArray [i].FieldType.Name + "_UI")) as GameObject;
			fieldUI.transform.SetParent (fieldsList.transform);
			fieldUI.transform.FindChild("Title").GetComponent<Text>().text = fieldsArray[i].Name;


		}

	}

	public void SaveSettingsFile()
	{
		WidgetSettings objToSave = (WidgetSettings)System.Activator.CreateInstance(activeSettingsFileType);

		object[] valuesToSave = new object[fieldsList.childCount];

		bool anyNull = false;

		for (int i = 0; i < fieldsList.childCount; i ++)
		{
			valuesToSave[i] = fieldsList.GetChild(i).GetComponent<FieldUIs>().GetFieldValue();
			if(valuesToSave[i] == null)
			{
				Debug.Log("need to decide how to implement the error message that should be displayed here");
				anyNull = true;
				break;
			}
		}
		if (anyNull)
			return;

		objToSave.SetValues(valuesToSave);

		string file = settingsFileFolderPath + "/" + activeSettingsFileType.Name + ".sets";

		Debug.Log(file);
		XmlIO.Save (objToSave, file);
	}
}
