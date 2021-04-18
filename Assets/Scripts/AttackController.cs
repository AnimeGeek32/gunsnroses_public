using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum AttackType
{
    TYPE_NORMALMELEE,
    TYPE_KICK,
    TYPE_SPECIAL
};

public class AttackController : MonoBehaviour {
    public Transform owner;
    public int attackPower = 10;
    public AttackType attackType = AttackType.TYPE_NORMALMELEE;
    public GameObject collisionParticle;
    public bool shakeCameraOnHit = false;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
