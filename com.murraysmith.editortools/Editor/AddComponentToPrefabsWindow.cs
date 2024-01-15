using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.SceneManagement;

public class AddComponentToPrefabsWindow : EditorWindow
{
    GameObject temp;
    TextField folderInput;
    InspectorElement inspector;

    Button runScriptButton;
    Toggle recursiveToggle;
    bool recursiveToggleValue;
    InspectorInput inspectorObj;
    

    [MenuItem("MurraysTools/BatchComponentAdd")]
    public static void OpenBatchComponentAddWindow() {
        EditorWindow wnd = GetWindow<AddComponentToPrefabsWindow>();
        wnd.titleContent = new GUIContent("Batch Add Component to Prefabs");
    }

    public void CreateGUI()
    {
        temp = new GameObject();
        
        inspectorObj = temp.AddComponent<InspectorInput>();
        
        folderInput = new TextField("");
        
        inspector = new InspectorElement(inspectorObj);

        runScriptButton = new Button();
        runScriptButton.RegisterCallback<ClickEvent>(OnRunScriptClicked);
        runScriptButton.text = "Add To All Prefabs";

        recursiveToggle = new Toggle();
        recursiveToggle.RegisterCallback<ChangeEvent<bool>>((evt) => 
        {
            recursiveToggleValue = evt.newValue;
        });

        rootVisualElement.Add(new Label("Prefab Folder:"));
        rootVisualElement.Add(folderInput);
        rootVisualElement.Add(new Label("Recursive:"));
        rootVisualElement.Add(recursiveToggle);        
        rootVisualElement.Add(new Label("Component to copy:"));
        rootVisualElement.Add(inspector);
        rootVisualElement.Add(runScriptButton);

        inspector.style.flexGrow = new StyleFloat(0.1f);
        runScriptButton.style.flexGrow = new StyleFloat(0.1f);
    }

    public void OnDestroy() {
        if (temp != null) {
            DestroyImmediate(temp, true);
        }
    }

    private void OnRunScriptClicked(ClickEvent evt) {
        string folder = folderInput.value;
        Component component = inspectorObj.component;

        AddComponentToAllPrefabsInFolder(folder, component, recursiveToggleValue);
    }


    private void AddComponentToAllPrefabsInFolder(string folderPath, Component component, bool recursive, bool isRoot = true) {
        List<GameObject> allMapItems;

        string[] fileEntries = Directory.GetFiles(Application.dataPath+"/"+folderPath);
        allMapItems = new List<GameObject>();
        foreach (string entry in fileEntries) {
            if (entry.EndsWith(".prefab")) {
                
                if (folderPath.Contains("Resources")) {
                    string filePath = string.Join("/", folderPath.Split("/")[1..^0]) + "/" + entry.Split("/").Last().Split("\\").Last().Split(".")[0];
                    allMapItems.Add((GameObject)Resources.Load(filePath, typeof(GameObject)));
                }
                else {
                    string filePath = folderPath + "/" + entry.Split("/").Last().Split("\\").Last();
                    if (!filePath.StartsWith("Assets")) {
                        filePath = "Assets/" + filePath;
                    }
                    allMapItems.Add((GameObject)AssetDatabase.LoadAssetAtPath<GameObject>(filePath));

                }
            }
        }

        Type componentType = component.GetType();

        foreach (GameObject mapItem in allMapItems) {

            GameObject instance = PrefabUtility.InstantiatePrefab(mapItem) as GameObject;

            var newComponent = ObjectFactory.AddComponent(instance, componentType);
            string assetPath = AssetDatabase.GetAssetPath(mapItem);
            
            PropertyInfo[] properties = componentType.GetProperties();
            foreach (PropertyInfo property in properties)
            {

                if (property.IsDefined(typeof(ObsoleteAttribute), true) || property.GetValue(newComponent) == property.GetValue(component)) continue;
                
                if (property.GetSetMethod() != null) {
                    property.SetValue(newComponent, property.GetValue(component));
                }

            }

            PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);

            DestroyImmediate(instance, true);   
        }

        if (recursive) {
            string[] subDirectories = Directory.GetDirectories(Application.dataPath+"/"+folderPath);
            foreach (string directory in subDirectories) {
                AddComponentToAllPrefabsInFolder(folderPath + "/" + directory.Split("/").Last().Split("\\").Last(), component, recursive, false);
            }
        }
    }
}
