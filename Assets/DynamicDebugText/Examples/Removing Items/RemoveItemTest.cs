
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoveItemTest : MonoBehaviour {
    public GameObject LeftCapsule;
    public GameObject RightCapsule;

    private GameObject currentTarget;
    private DropdownItem currentItem;

    private void Start() { currentTarget = LeftCapsule; }

    public void SelectLeftButtonClick() {
        Debug.LogFormat("SelectLeftButtonClick()");
        currentTarget = LeftCapsule;
    }

    public void SelectRightButtonClick() {
        Debug.LogFormat("SelectRightButtonClick()");
        currentTarget = RightCapsule;
    }

    public void AddButtonClick() {
        Debug.LogFormat("AddButtonClick()");
        switch (currentItem) {
            case DropdownItem.Position:
                currentTarget.AddDebugItem(() => {
                        return "Pos: " + currentTarget.gameObject.transform.position;
                    },
                    DropdownItem.Position.ToString());
                break;
            case DropdownItem.Rotation:
                currentTarget.AddDebugItem(() => {
                        return "Rot: " + currentTarget.gameObject.transform.rotation;
                    },
                    DropdownItem.Rotation.ToString());
                break;
            case DropdownItem.Name:
                currentTarget.AddDebugItem(() => {
                        return "Name: " + currentTarget.gameObject.name;
                    },
                    DropdownItem.Name.ToString());
                break;
            default: throw new ArgumentOutOfRangeException();
        }
    }

    public void RemoveButtonClick() {
        Debug.LogFormat("RemoveButtonClick()");
        currentTarget.RemoveDebugItem(currentItem.ToString());
    }

    public void OnDropdownChanged(int dropdownIndex) {
        Debug.LogFormat("OnDropdownChanged({0})", (DropdownItem) dropdownIndex);
        currentItem = (DropdownItem) dropdownIndex;
    }

    public void OnInputFieldEndEdit(string inputFieldText) {
        if (Enum.IsDefined(typeof(DropdownItem), inputFieldText)) {
            if (Enum.TryParse(inputFieldText, out DropdownItem selectedItem)) {
                currentItem = selectedItem;
            }
        } else {
            ParseInputField(inputFieldText);
        }
    }

    private void ParseInputField(string inputFieldText) {
        if (int.TryParse(inputFieldText, out int inputFieldIndex)) {
            if (Enum.IsDefined(typeof(DropdownItem), inputFieldIndex)) {
                currentItem = (DropdownItem) inputFieldIndex;
                return;
            }
        }

        Debug.LogErrorFormat("Invalid input for index. Please enter an integer, or valid name from the dropdown.");
    }

    private enum DropdownItem {
        Position = 0,
        Rotation = 1,
        Name = 2
    }
}