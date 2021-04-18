using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LifeBar : MonoBehaviour {

	public GameObject lifeBarSprite;
	public Color playerColor = Color.white;
	public float playerLife;
	public bool isRightPlayer;

	private float maxWidth;
	private float startingWidth;

	// Use this for initialization
	void Start () {
		playerLife = 100;
		maxWidth = transform.localPosition.x;
		startingWidth = 510;
	}
	
	// Update is called once per frame
	void Update () {
		float barLength = playerLife;
		if (barLength < 0) {
			barLength = 0.0f;
		}
		transform.localScale = new Vector3 (barLength, 100, 1);
		float xPosition = 0;
		if (isRightPlayer) {
			xPosition = maxWidth - ((startingWidth - (startingWidth * (barLength/100)))/2);
			} else {
			xPosition = ((startingWidth - (startingWidth * (barLength/100)))/2) + maxWidth;
			}
		transform.localPosition = new Vector3 (xPosition, 0, 0);
		
	}
}
