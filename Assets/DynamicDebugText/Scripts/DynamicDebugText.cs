#region Using Statements

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;

#endregion

public class DynamicDebugText : MonoBehaviour {
    [Header("DebugInfo")]
    [SerializeField, GetSet("Debugging")]
    private bool debugging;

    public bool Debugging {
        set {
            debugging = value;
            SetDebugItemsVisibility(debugging);
        }
        get { return debugging; }
    }

    [Tooltip("Whether or not the text will rotate to face the Main Camera.")]public bool BillBoardText = true;
    [Tooltip("Parent object that DebugItemPrefab will be parented to.")]public Transform DebugItemHolder;
    [Tooltip("Prefab for individual text elements. If this is null, the component will attempt to load it from resources.")]public GameObject DebugItemPrefab;
    [Tooltip("Offset of Debug Items relative to the parent object.")]public Vector3 DebugItemsOffset;
    [Tooltip("Spacing between Debug Items.")]public float DebugItemSpacing = 0.5f;
    [Tooltip("Color of Debug Item text.")]public Color DebugItemColor = Color.white;
    private List<TextMeshPro> DebugItems = new List<TextMeshPro>();

    private List<Func<string>> DebugActions = new List<Func<string>>();
    public Camera MainCamera;

    protected virtual void Start() {
        if (MainCamera == null) {
            MainCamera = Camera.main;
        }
        //Check if offset isn't zero, so it doesn't move it on prefabs where it was manually moved.
        if (DebugItemsOffset != Vector3.zero) {
            DebugItemHolder.transform.localPosition = DebugItemsOffset;
        }
    }

    protected virtual void Update() {
        if (Debugging) {
            PerformDebugActions();
        }
    }

    protected virtual void PerformDebugActions() {
        for (int i = 0; i < DebugActions.Count; i++) {
            DebugItems[i].text = DebugActions[i]();
        }
        if (BillBoardText && MainCamera != null) {
            DebugItemHolder.transform.rotation = MainCamera.transform.rotation;
        }
    }


    /// <summary>Adds a debug item.</summary>
    /// <param name="inputFunc">The input function.
    /// <strong>Example</strong>: () =&gt; { return "Pos: "+Items[Index].gameObject.transform.position.ToString(); };</param>
    /// <param name="itemName">Name of the item.</param>
    public virtual void AddDebugItem(Func<string> inputFunc, string itemName = null) {
        //If no input Func is specified, bail out. The extensions pass a null value when adding this component automatically.
        if (inputFunc == null) {
            return;
        }
        
        //If the DebugItemPrefab is null, try and load it from resources.
        if (DebugItemPrefab == null) {
            GameObject tempDebugItemPrefab = Resources.Load(DynamicDebugTextExtensions.PrefabName) as GameObject;
            //If loading from resources fails, bail out. 
            if (tempDebugItemPrefab == null) {
                Debug.LogErrorFormat("Failed to load DebugItemPrefab! Make sure you didn't rename or move it from DynamicDebugText/Resources. It should be named {0}. If you need to rename it, change 'PrefabName' in DynamicDebugExtensions.cs to the new name.", DynamicDebugTextExtensions.PrefabName);
                return;
            }
        }
        
        //Create the new Debug Item.
        GameObject newDebugItem = Instantiate(DebugItemPrefab, DebugItemHolder);
        
        //If itemName isn't null, name the DebugItem after it.
        if (!string.IsNullOrEmpty(itemName)) {
            newDebugItem.name = itemName;
        }
        
        //Only set it to active if debugging.
        if (Debugging) {
            newDebugItem.SetActive(true);
        }

        //Add to the beginning of the list, so the order of calls to AddDebugItem matches the order displayed
        DebugItems.Insert(0, newDebugItem.GetComponent<TextMeshPro>());
        DebugActions.Insert(0, inputFunc);
        UpdateSpacing();
    }

   
    public virtual bool RemoveDebugItem(string itemName) {
        //Loop backwards through items, since we're removing. Not necessary, but always good to iterate backwards when removing while iterating.
        for (int i = DebugItems.Count - 1; i >= 0; i--) {
            if (DebugItems[i].name == itemName) {
                Destroy(DebugItems[i].gameObject);
                DebugItems.RemoveAt(i);
                DebugActions.RemoveAt(i);
                //Update spacing to remove gaps.
                UpdateSpacing();
                //Return true if we found it.
                return true;
            } 
        }
        //Log an error and return false if we got here, because we didn't find the DebugItem.
        Debug.LogErrorFormat("Failed to Remove DebugItem '{0}' as it is not an existing item!", itemName);
        return false;
    }

    /// <summary>Removes a debug item.</summary>
    /// <param name="itemIndex">Index of the item.</param>
    /// <returns>
    ///   <c>true</c> if item was found and removed, <c>false</c> otherwise.</returns>
    public virtual bool RemoveDebugItem(int itemIndex) {
        //Make sure the itemIndex is in bounds of the list. If not, log an error and bail out.
        if (itemIndex < 0 || itemIndex >= DebugItems.Count) {
            Debug.LogErrorFormat("Failed to Remove DebugItem by index '{0}' as it is out of bounds!", itemIndex);
            return false;
        }
        
        DebugItems.RemoveAt(itemIndex);
        DebugActions.RemoveAt(itemIndex);
        //Update spacing to remove gaps.
        UpdateSpacing();
        return true;
    }

    /// <summary>Updates the spacing between Debug Items.</summary>
    public virtual void UpdateSpacing() {
        //Shift all items up, because we're adding at the beginning.
        for (int i = 0; i < DebugItems.Count; i++) {
            DebugItems[i].transform.localPosition = DebugItemPrefab.transform.localPosition + new Vector3(0,   (i) * DebugItemSpacing, 0);
        }
    }

    /// <summary>Sets the size of all Debug Items.</summary>
    /// <param name="inputSize">Size of the text.</param>
    public virtual void SetDebugTextSize(float inputSize) {
        for (int i = 0; i < DebugItems.Count; i++) {
            TextMeshPro textComponent;
            if (DebugItems[i].TryGetComponent(out textComponent)) {
                textComponent.fontSize = inputSize;
            }
            
        }
    }

    /// <summary>Sets the color of all Debug Items.</summary>
    /// <param name="inputColor">Color of the text.</param>
    public virtual void SetDebugItemColor(Color inputColor) {
        DebugItemColor = inputColor;
        for (int i = 0; i < DebugItems.Count; i++) {
            DebugItems[i].color = DebugItemColor;
        }
    }

    /// <summary>Sets Debug Items visibility.</summary>
    /// <param name="inputValue">  If set to true, shows all Debug Items. Hides them otherwise.</param>
    protected virtual void SetDebugItemsVisibility(bool inputValue) {
        for (int i = 0; i < DebugItems.Count; i++) {
            DebugItems[i].gameObject.SetActive(inputValue);
        }
    }

    /// <summary>  Destroys all Debug Items.</summary>
    protected virtual void Reset() {
        for (int i = 0; i < DebugItems.Count; i++) {
            Destroy(DebugItems[i].gameObject);
        }
        DebugItems.Clear();
        DebugActions.Clear();
    }
}