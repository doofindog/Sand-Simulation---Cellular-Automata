
#region Using Statements
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#endregion    
public class BasicTest : MonoBehaviour {

    //List of all items in the scene.
    private DynamicDebugText[] Items;
    public bool Grounded;
    private void Start() {
        //Usually this isn't a good idea, as FindObjectsOfType *can* be slow. Not always though. In big scenes, it searches every GameObject
        //There's only a couple objects in this scene, so it's fast; but in bigger scenes, avoid this.
        Items = GameObject.FindObjectsOfType<DynamicDebugText>();
        
        //Loop through all items in scene, and add 
        for (int i = 0; i < Items.Length; i++) {
            //Need to pass index by reference, else you get an error due to "closure" because index is passed by reference.
            var Index = i; 
            
            //Add a debug item using Lambda expressions
            Items[i].AddDebugItem(() => { return Items[Index].gameObject.name; });

            //You can make a Func<string> and pass it as well
            Func<string> newFunc = () => { return "Pos: "+Items[Index].gameObject.transform.position.ToString(); };
            Items[i].AddDebugItem(newFunc);
            
            
            //You can add colors as well
            //IMPORTANT: Due to what's called "scoping" any variables that will change at runtime have to be changed in the Lambda, like below.
            //Most of the time, there won't be any issues because what you want to display would generally be something that IS changing
            if (i >0 ) {
                Items[i].AddDebugItem(() => "<color=#FF0000>You </color><color=#00FF00>can </color><color=#0000FF>add </color><color=#888888>color </color><color=#FFFF00>too.</color>");
            } else {
                string colorString;
                Items[i].AddDebugItem(() => {
                    if (Grounded) {
                        colorString = "#00FF00";
                    } else {
                        colorString = "#FF0000";
                    }
                    return string.Format("Toggle: <color={0}>{1}</color>", colorString, Grounded);
                });
                
                gameObject.AddDebugItem(() => { bool grounded = true; return "Grounded: " + Grounded;});
            }
        }
    }

    public void Toggle() {
        Grounded = !Grounded;
    }
}