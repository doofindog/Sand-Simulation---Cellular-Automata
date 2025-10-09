
#region Using Statements
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#endregion
public class ExtensionTest : MonoBehaviour {
	//Target to add the component to
	//Notice how this is NOT the GameObject
	//The extension will add a DynamicDebugText
	public Component Target;

	private void Start() {
		Target.AddDebugItem(() => "Added via extension",null, new Vector3(0,2,0));
		Target.Debugging(true);
	}
}