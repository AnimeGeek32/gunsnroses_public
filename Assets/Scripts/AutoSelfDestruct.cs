using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoSelfDestruct : MonoBehaviour {
    public float selfDestroyTimeInSecs = 0.5f;

	// Use this for initialization
	void Start () {
        Destroy(gameObject, selfDestroyTimeInSecs);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
