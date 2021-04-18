using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileController : MonoBehaviour {
    public float speed = 10f;
    public float selfDestructTime = 4f;

    [FMODUnity.EventRef]
    public string projectileDamageEvent;
    
    [Header("Cancel")]
    public GameObject cancelParticlesPrefab;

    private Rigidbody2D _rigidbody;
    private AttackController _attackController;

	// Use this for initialization
	void Start () {
        _rigidbody = GetComponent<Rigidbody2D>();
        _attackController = GetComponent<AttackController>();
        float direction = IsFacingRight ? 1f : -1f;
        _rigidbody.velocity = new Vector2(speed * direction, 0f);
        Destroy(gameObject, selfDestructTime);
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public bool IsFacingRight
	{
		get
		{
			return (transform.localScale.x >= 0);
		}
	}

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Platforms")) {
            Destroy(gameObject);
        } else if (collision.gameObject.layer == LayerMask.NameToLayer("Triggers")) {
            //Debug.Log("Another trigger detected.");
            if (collision.gameObject.tag == "Attack") {
                Debug.Log("Other attack detected.");
                AttackController otherAttack = collision.gameObject.GetComponent<AttackController>();
                if (otherAttack.owner != _attackController.owner) {
                    Destroy(gameObject);

                    Instantiate(cancelParticlesPrefab, collision.transform.position, Quaternion.identity);
					FMODUnity.RuntimeManager.PlayOneShot(projectileDamageEvent, transform.position);
                }
            }
        }
    }
}
