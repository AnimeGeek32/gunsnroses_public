using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayMusicOnStart : MonoBehaviour {
    [FMODUnity.EventRef]
    public string musicEvent = "";

	// Use this for initialization
	void Start () {
        MusicManager.Instance.PlayMusic(musicEvent);
	}
}
