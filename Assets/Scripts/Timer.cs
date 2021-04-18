using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour {

	private int timeLeft;
	private int timerDuration;
	public Text timerText;
	public int timerDelayed;
	public bool timerDone = false;
	private float incrementTime;


	// Use this for initialization
	void Start () {
		timerDuration = 60;
		timerDelayed = 4;
		timeLeft = timerDuration;
		incrementTime = 0;
		timerText.text = timeLeft.ToString();
	}
	
	// Update is called once per frame
	void Update () {
		
		if (timerDelayed > 0) { //wait for the delay to be done
			//Debug.Log("delay timer");
			incrementTime += Time.deltaTime;
			if (incrementTime >= 1) {
				incrementTime = 0;
				timerDelayed -= 1;
			}
			return;
		}
		
		if (!timerDone) {
			incrementTime += Time.deltaTime;
			if (timeLeft > 0) {
				if (incrementTime >= 1) {
					incrementTime = 0;
					timeLeft -= 1;
					timerText.text = timeLeft.ToString ();
				}
			} else {
				GameOver ();
			}
		}
	}

	public void GameOver()
	{	
		timerDone = true;
		GunsNRoses.GameManager.instance.TimerEnded ();		
	}

	public void Reset() {
		timeLeft = timerDuration;
		incrementTime = 0;
		timerDone = false;
		timerText.text = timeLeft.ToString();
	}
}
