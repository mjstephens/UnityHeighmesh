using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// From: https://gist.github.com/mstevenson/5103365
public class FPS : MonoBehaviour {

    string label = "";
    float count;
	
    IEnumerator Start ()
    {
        GUI.depth = 2;
        while (true) 
        {
                yield return new WaitForSeconds (0.1f);
                count = (1 / Time.deltaTime);
                label = "FPS :" + (Mathf.Round (count));
            yield return new WaitForSeconds (0.5f);
        }
    }
	
    void OnGUI ()
    {
        GUI.Label (new Rect (5, 40, 100, 25), label);
    }
}
