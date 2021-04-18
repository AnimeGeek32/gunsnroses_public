using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour {
    [FMODUnity.EventRef]
    public string musicEvent = "";
    public bool allowFadeoutOnStop = false;
    private static MusicManager _instance = null;
    private static FMOD.Studio.EventInstance _musicInstance;

    void Awake()
    {
        if (_instance != null) {
            Destroy(gameObject);
        }
    }

    // Use this for initialization
    void Start () {
        if (_instance == null) {
            _instance = this;
        }

        DontDestroyOnLoad(gameObject);

        if (musicEvent != "") {
            _musicInstance = FMODUnity.RuntimeManager.CreateInstance(musicEvent);
            _musicInstance.start();
        }
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public static MusicManager Instance {
        get {
            return _instance;
        }
    }

    public void PlayMusic(string newEvent) {
        StopMusic();
        musicEvent = newEvent;

        if (!_musicInstance.isValid()) {
            _musicInstance = FMODUnity.RuntimeManager.CreateInstance(musicEvent);
            _musicInstance.start();
        }
    }

    public void StopMusic() {
        if (_musicInstance.isValid()) {
            _musicInstance.stop(allowFadeoutOnStop ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
            _musicInstance.release();
            _musicInstance.clearHandle();
        }
    }
}
