#region Using Statements

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

#endregion

public static class DynamicDebugTextExtensions {
    //Default prefab name. Located in DynmicDebugText/Resources.
    //Change this if for some reason you need to change the prefab name.
    public static string PrefabName = "Debug Text (0)";

    //Adds a Dynamic
    /// <summary>Adds a Debug Item.</summary>
    /// <param name="target">The target.</param>
    /// <param name="inputFunc">The input function.</param>
    /// <param name="itemName">Name of the item.</param>
    /// <param name="offset">The offset relative to the target.</param>
    /// <returns>DynamicDebugText.</returns>
    public static DynamicDebugText AddDebugItem(this GameObject target, Func<string> inputFunc, string itemName = null, Vector3? offset = null) {
        Transform targetTransform;
        if (target.TryGetComponent(out targetTransform)) {
            return targetTransform.AddDebugItem(inputFunc,itemName);
        }
        else {
            Debug.LogErrorFormat("Failed to add DynamicDebugText to object. If this was a GameObject, make sure it has a Transform component on it.");
            return null;
        }
    }


    /// <summary>Adds a Debug Item.</summary>
    /// <param name="target">The target.</param>
    /// <param name="inputFunc">The input function.</param>
    /// <param name="itemName">Name of the item.</param>
    /// <param name="offset">The offset relative to the target.</param>
    /// <returns>DynamicDebugText.</returns>
    public static DynamicDebugText AddDebugItem(this Component target, Func<string> inputFunc, string itemName = null, Vector3? offset = null) {
        DynamicDebugText debugComponent;
        if (!target.TryGetComponent<DynamicDebugText>(out debugComponent)) {
            debugComponent = target.gameObject.AddComponent<DynamicDebugText>();
            if (offset != null) {
                debugComponent.DebugItemsOffset = offset.Value;
            }
        }
        else {
            debugComponent.AddDebugItem(inputFunc, itemName);
            return debugComponent;
        }

        GameObject tempDebugItemPrefab = Resources.Load(PrefabName) as GameObject;
        if (tempDebugItemPrefab != null) {
            debugComponent.DebugItemHolder = new GameObject().transform;
            debugComponent.DebugItemHolder.SetParent(target.transform);
            debugComponent.DebugItemHolder.gameObject.name = "Debug Items";
            debugComponent.DebugItemPrefab = tempDebugItemPrefab;
            debugComponent.DebugItemPrefab = GameObject.Instantiate(tempDebugItemPrefab, debugComponent.DebugItemHolder.transform);
            debugComponent.DebugItemPrefab.gameObject.name = tempDebugItemPrefab.gameObject.name;
            debugComponent.DebugItemPrefab.transform.position = tempDebugItemPrefab.transform.position;
            debugComponent.DebugItemPrefab.transform.localScale = target.transform.localScale;

            debugComponent.AddDebugItem(inputFunc);
        }
        else {
            Debug.LogErrorFormat("Failed to load DebugItemPrefab! Make sure you didn't rename or move it from DynamicDebugText/Resources. It should be named {0}. If you need to rename it, change 'PrefabName' in DynamicDebugExtensions.cs to the new name.", PrefabName);
        }

        return debugComponent;
    }

    /// <summary>  Remove a Debug Item by name.</summary>
    /// <param name="target">The target.</param>
    /// <param name="itemName">Name of the item.</param>
    /// <returns>
    ///   <c>true</c> if it found and removed the item, <c>false</c> otherwise.</returns>
    public static bool RemoveDebugItem(this GameObject target, string itemName) {
        Transform targetTransform;
        if (target.TryGetComponent(out targetTransform)) {
            return targetTransform.RemoveDebugItem(itemName);
        }
        else {
            Debug.LogErrorFormat("Failed to Remove DebugItem '{0}' from {1} as it does not have a DynamicDebugText component!", itemName,target.name);
            return false;
        }
    }

    /// <summary>Removes a debug item by name.</summary>
    /// <param name="target">The target.</param>
    /// <param name="itemName">Name of the item.</param>
    /// <returns>
    ///   <c>true</c> if it found and removed the item, <c>false</c> otherwise.</returns>
    public static bool RemoveDebugItem(this Component target,  string itemName) {
        DynamicDebugText debugComponent;
        if (!target.TryGetComponent<DynamicDebugText>(out debugComponent)) {
            //If it doesn't have a DynamicDebugText component, there's nothing to remove. Throw an error and bail out.
            Debug.LogErrorFormat("Failed to Remove DebugItem '{0}' from {1} as it does not have a DynamicDebugText component!", itemName,target.name);
            return false;
        } else {
            return debugComponent.RemoveDebugItem(itemName);
        }
    }

    /// <summary>Removes a debug item by index.</summary>
    /// <param name="target">The target.</param>
    /// <param name="itemIndex">Index of the item.
    /// Items indices are equal to the order they were added in.</param>
    /// <returns>
    ///   <c>true</c> if it found and removed the item, <c>false</c> otherwise.</returns>
    public static bool RemoveDebugItem(this GameObject target, int itemIndex) {
        Transform targetTransform;
        if (target.TryGetComponent(out targetTransform)) {
            return targetTransform.RemoveDebugItem(itemIndex);
        }
        else {
            Debug.LogErrorFormat("Failed to Remove DebugItem at index '{0}' from {1} as it does not have a DynamicDebugText component!", itemIndex,target.name);
            return false;
        }
    }

    /// <summary>Removes a debug item by index.</summary>
    /// <param name="target">The target.</param>
    /// <param name="itemIndex">Index of the item.</param>
    /// <returns>
    ///   <c>true</c> if it found and removed the item, <c>false</c> otherwise.</returns>
    public static bool RemoveDebugItem(this Component target,  int itemIndex) {
        DynamicDebugText debugComponent;
        if (!target.TryGetComponent<DynamicDebugText>(out debugComponent)) {
            //If it doesn't have a DynamicDebugText component, there's nothing to remove. Throw an error and bail out.
            Debug.LogErrorFormat("Failed to Remove DebugItem at index '{0}' from {1} as it does not have a DynamicDebugText component!", itemIndex,target.name);
            return false;
        } else {
            return debugComponent.RemoveDebugItem(itemIndex);
        }
    }

    /// <summary>  Toggles the visibility of all Debug Items on the target.</summary>
    /// <param name="target">The target.</param>
    /// <param name="inputValue">if set to <c>true, shows all debug items on target. Hides them otherwise.</c></param>
    public static void Debugging(this Component target, bool inputValue) {
        DynamicDebugText debugComponent;
        if (!target.TryGetComponent<DynamicDebugText>(out debugComponent)) {
            target.AddDebugItem(null).Debugging = inputValue;
        }
        else {
            debugComponent.Debugging = inputValue;
        }
    }

    /// <summary>  Toggles the visibility of all Debug Items on the target.</summary>
    /// <param name="target">The target.</param>
    /// <param name="inputValue">if set to <c>true, shows all debug items on target. Hides otherwise.</c></param>
    public static void Debugging(this GameObject target, bool inputValue) {
        DynamicDebugText debugComponent;
        if (!target.TryGetComponent(out debugComponent)) {
            Debugging(debugComponent, inputValue);
            //target.AddDebugItem(null).Debugging = inputValue;
        }
        else {
            debugComponent.Debugging = inputValue;
        }
    }

    /// <summary>Sets the debug item offset, relative to the target</summary>
    /// <param name="target">The target.</param>
    /// <param name="inputOffset">The input offset.</param>
    public static void SetDebugOffset(this Component target, Vector3? inputOffset = null) {
        if (inputOffset != null) {
            DynamicDebugText targetDebugText;
            if (target.TryGetComponent(out targetDebugText)) {
                targetDebugText.DebugItemsOffset = inputOffset.Value;
            }
            else {
                Debug.LogWarning("{0} needs a DynamicDebugText component first. ");
            }
        }
        else {
            Debug.LogWarning("Can't set the offset to Null!");
        }
    }

    /// <summary>Sets the debug item offset, relative to the target</summary>
    /// <param name="target">The target.</param>
    /// <param name="inputOffset">The input offset.</param>
    public static void SetDebugOffset(this GameObject target, Vector3? inputOffset = null) {
        if (inputOffset != null) {
            DynamicDebugText targetDebugText;
            if (target.TryGetComponent(out targetDebugText)) {
                targetDebugText.SetDebugOffset(inputOffset.Value);
                //targetDebugText.DebugItemsOffset = inputOffset.Value;
            }
            else {
                Debug.LogWarning("{0} needs a DynamicDebugText component first. ");
            }
        }
        else {
            Debug.LogWarning("Can't set the offset to Null!");
        }
    }

    /// <summary>Sets the spacing between Debug Items.</summary>
    /// <param name="target">The target.</param>
    /// <param name="inputSpacing">The input spacing.</param>
    public static void SetDebugSpacing(this GameObject target, float inputSpacing) {
        if (target.TryGetComponent(out DynamicDebugText targetDebugText)) {
            targetDebugText.DebugItemSpacing = inputSpacing;
            targetDebugText.UpdateSpacing();
        }
        else {
            Debug.LogWarning("{0} needs a DynamicDebugText component first. ");
        }
    }

    /// <summary>Sets the spacing between Debug Items.</summary>
    /// <param name="target">The target.</param>
    /// <param name="inputSpacing">The input spacing.</param>
    public static void SetDebugSpacing(this Component target, float inputSpacing) {
        if (target.TryGetComponent(out DynamicDebugText targetDebugText)) {
            targetDebugText.DebugItemSpacing = inputSpacing;
            targetDebugText.UpdateSpacing();
        }
        else {
            Debug.LogWarning("{0} needs a DynamicDebugText component first. ");
        }
    }

    /// <summary>Sets the size of all Debug Item text.</summary>
    /// <param name="target">The target.</param>
    /// <param name="inputSize">Size of the input.</param>
    public static void SetDebugTextSize(this Component target, float inputSize) {
        if (target.TryGetComponent(out DynamicDebugText targetDebugText)) {
            targetDebugText.SetDebugTextSize(inputSize);
        }
        else {
            Debug.LogWarning("{0} needs a DynamicDebugText component first. ");
        }
    }

    /// <summary>Sets the size of all Debug Item text.</summary>
    /// <param name="target">The target.</param>
    /// <param name="inputSize">Size of the input.</param>
    public static void SetDebugTextSize(this GameObject target, float inputSize) {
        if (target.TryGetComponent(out DynamicDebugText targetDebugText)) {
            targetDebugText.SetDebugTextSize(inputSize);
        }
        else {
            Debug.LogWarning("{0} needs a DynamicDebugText component first. ");
        }
    }


    /// <summary>Sets the color of all Debug Item text.</summary>
    /// <param name="target">The target.</param>
    /// <param name="inputColor">Input color.</param>
    public static void SetDebugTextColor(this GameObject target, Color inputColor) {
        if (target.TryGetComponent(out DynamicDebugText targetDebugText)) {
            targetDebugText.SetDebugItemColor(inputColor);
        }
        else {
            Debug.LogWarning("{0} needs a DynamicDebugText component first. ");
        }
    }

    /// <summary>Sets the color of all Debug Item text.</summary>
    /// <param name="target">The target.</param>
    /// <param name="inputColor">Input color.</param>
    public static void SetDebugTextColor(this Component target, Color inputColor) {
        if (target.TryGetComponent(out DynamicDebugText targetDebugText)) {
            targetDebugText.SetDebugItemColor(inputColor);
        }
        else {
            Debug.LogWarning("{0} needs a DynamicDebugText component first. ");
        }
    }
}