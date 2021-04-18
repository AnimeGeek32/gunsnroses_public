using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuInput : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

		if (Input.GetKey(KeyCode.Escape)) {
			Debug.Log("Pressing button to leeeave??");
            Application.Quit();
		}

		if (Input.anyKey) {
			SceneManager.LoadScene (1);
		}

	}
}
