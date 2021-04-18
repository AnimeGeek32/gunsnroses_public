using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kino;

[RequireComponent(typeof(Camera))]
public class MultiplayerCameraController : MonoBehaviour {
    [Header("Camera Controls")]
    [Range(0, 2)]
    public float DampTime = 0.2f;
    public float FadeInTime = 1.0f;

    // List of players
    public List<Transform> Players;

    public float startingTime = 5.1f;

    [Header("Orthographic Parameters")]
    // Minimal zoom size
    public float OrthoMinSize = 6.5f;
    public float OrthoScreenEdgeBuffer = 4f;

    [Header("Perspective Parameters")]
    // Coordinates when the round starts
    public Vector3 StartPosition;
    // Minimum coordinates to which the camera will be constrained
    public Vector3 MinPosition;
    // Maximum coordinates to which the camera will be constrained
    public Vector3 MaxPosition;

    [Header("Shake Properties")]
    public float shakeDuration = 1.0f;
    public float shakeAmount = 0.7f;

	protected Camera _camera;
	protected float _zoomSpeed;
	protected Vector3 _moveVelocity;
	protected Vector3 _newPosition;
	protected Vector3 _averagePosition;
	protected float _initialZ;
	protected float _xMin;
	protected float _xMax;
	protected float _yMin;
	protected float _yMax;
	protected float _aspectRatio;
	protected float _tanFov;
    protected bool _isOnStartingMode = true;
    protected AnalogGlitch _analogGlitch;
    protected bool _cameraIsShaking = false;
    protected float _currentShakeDuration = 0.0f;

	// Use this for initialization
	void Start () {
		_camera = GetComponentInChildren<Camera>();
		_initialZ = transform.position.z;
		_aspectRatio = Screen.width / Screen.height;
		_tanFov = Mathf.Tan(Mathf.Deg2Rad * Camera.main.fieldOfView / 2.0f);
        _analogGlitch = GetComponent<AnalogGlitch>();
        transform.position = StartPosition;
        StartCoroutine(SetOnStartingPosition());
        StartCoroutine(GlitchFadeIn());
	}
	
	// Update is called once per frame
	void FixedUpdate () {
        if (!_isOnStartingMode) {
            CameraMovement();
        }
	}

    void CameraMovement() {
        FindAveragePosition();
        ComputeZoom();
        ClampNewPosition();
        MoveCamera();
        if (_cameraIsShaking) {
            CameraShake();
        }
    }

	void MoveCamera()
	{
		// Smoothly transition to that position.
		transform.position = Vector3.SmoothDamp(transform.position, _newPosition, ref _moveVelocity, DampTime);
	}

	void FindAveragePosition()
	{
		_averagePosition = Vector3.zero;
		int numTargets = 0;

		// Go through all the targets and add their positions together.
		for (int i = 0; i < Players.Count; i++)
		{
			// Add to the average and increment the number of targets in the average.
			_averagePosition += Players[i].position;
			numTargets++;
		}

		// If there are targets divide the sum of the positions by the number of them to find the average.
		if (numTargets > 0)
		{
			_averagePosition /= numTargets;
		}

		// we fix the z value
		_averagePosition.z = _initialZ;

		// The desired position is the average position;
		_newPosition = _averagePosition;
	}

	void ComputeZoom()
	{
		if (_camera.orthographic)
		{
			float requiredSize;
			// Find the required size based on the desired position and smoothly transition to that size.
			requiredSize = FindRequiredOrthographicSize();
			_camera.orthographicSize = Mathf.SmoothDamp(_camera.orthographicSize, requiredSize, ref _zoomSpeed, DampTime);
			//GetLevelBounds();
		}
		else
		{
			float requiredDistance;
			requiredDistance = FindRequiredDistance();
			_newPosition.z = -requiredDistance;
		}
	}

	float FindRequiredOrthographicSize()
	{
		Vector3 desiredLocalPos = transform.InverseTransformPoint(_newPosition);

		float size = 0f;

		for (int i = 0; i < Players.Count; i++)
		{
			Vector3 targetLocalPos = transform.InverseTransformPoint(Players[i].position);
			Vector3 desiredPosToTarget = targetLocalPos - desiredLocalPos;

			size = Mathf.Max(size, Mathf.Abs(desiredPosToTarget.y));
			size = Mathf.Max(size, Mathf.Abs(desiredPosToTarget.x) / _camera.aspect);
		}

		size += OrthoScreenEdgeBuffer;

		size = Mathf.Max(size, OrthoMinSize);

		return size;
	}

	float FindRequiredDistance()
	{
		float maxDistance = 0;
		float newDistance = 0;
		for (int i = 0; i < Players.Count; i++)
		{
			newDistance = Vector3.Distance(Players[i].transform.position, _averagePosition);
			if (newDistance > maxDistance)
			{
				maxDistance = newDistance;
			}
		}

		float distanceBetweenPlayers = newDistance * 1.15f;
		float cameraDistance = (distanceBetweenPlayers / 2.0f / _aspectRatio) / _tanFov;
		return cameraDistance;
	}

	void ClampNewPosition()
	{
		if (_camera.orthographic)
		{
            /*
			if (_levelBounds.size != Vector3.zero)
			{
				_newPosition.x = Mathf.Clamp(_newPosition.x, _xMin, _xMax);
				_newPosition.y = Mathf.Clamp(_newPosition.y, _yMin, _yMax);
			}
			*/
		}
		else
		{
			_newPosition.x = Mathf.Clamp(_newPosition.x, MinPosition.x, MaxPosition.x);
			_newPosition.y = Mathf.Clamp(_newPosition.y, MinPosition.y, MaxPosition.y);
			_newPosition.z = Mathf.Clamp(_newPosition.z, MinPosition.z, MaxPosition.z);
		}
	}

    public void ResetCameraToStartPosition() {
        StartCoroutine(SetOnStartingPosition());
    }

    IEnumerator SetOnStartingPosition() {
        _isOnStartingMode = true;
        transform.position = StartPosition;
        yield return new WaitForSeconds(startingTime);
        _isOnStartingMode = false;
    }

    IEnumerator GlitchFadeIn() {
        float currentGlitchTime = FadeInTime;
        float currentGlitchAmount = currentGlitchTime / FadeInTime;
        _analogGlitch.scanLineJitter = 1.0f;

        while (currentGlitchTime >= 0.0f)
        {
            float deltaTime = Time.deltaTime;
            _analogGlitch.scanLineJitter = currentGlitchAmount;
            yield return new WaitForSeconds(deltaTime);
            currentGlitchTime -= deltaTime;
            currentGlitchAmount = currentGlitchTime / FadeInTime;
        }

        _analogGlitch.scanLineJitter = 0.0f;
        _analogGlitch.enabled = false;
    }

    public void StartCameraShake() {
        if (!_cameraIsShaking) {
            _currentShakeDuration = 0.0f;
            _cameraIsShaking = true;
        }
    }

    void CameraShake() {
        if (_currentShakeDuration < shakeDuration) {
            transform.position += Random.insideUnitSphere * shakeAmount;
            _currentShakeDuration += Time.fixedDeltaTime;
        } else {
            _cameraIsShaking = false;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw level bounds for the camera
        Color levelBoundColor = Color.yellow;
        Vector3 frontP1 = new Vector3(MinPosition.x, MinPosition.y, MinPosition.z);
        Vector3 frontP2 = new Vector3(MaxPosition.x, MinPosition.y, MinPosition.z);
        Vector3 frontP3 = new Vector3(MaxPosition.x, MaxPosition.y, MinPosition.z);
        Vector3 frontP4 = new Vector3(MinPosition.x, MaxPosition.y, MinPosition.z);
        Vector3 backP1 = new Vector3(MinPosition.x, MinPosition.y, MaxPosition.z);
        Vector3 backP2 = new Vector3(MaxPosition.x, MinPosition.y, MaxPosition.z);
        Vector3 backP3 = new Vector3(MaxPosition.x, MaxPosition.y, MaxPosition.z);
        Vector3 backP4 = new Vector3(MinPosition.x, MaxPosition.y, MaxPosition.z);

        // Front quad
        Debug.DrawLine(frontP1, frontP2, levelBoundColor);
        Debug.DrawLine(frontP2, frontP3, levelBoundColor);
        Debug.DrawLine(frontP3, frontP4, levelBoundColor);
        Debug.DrawLine(frontP4, frontP1, levelBoundColor);

        // Back quad
		Debug.DrawLine(backP1, backP2, levelBoundColor);
		Debug.DrawLine(backP2, backP3, levelBoundColor);
		Debug.DrawLine(backP3, backP4, levelBoundColor);
		Debug.DrawLine(backP4, backP1, levelBoundColor);

        // Connecting quads
        Debug.DrawLine(frontP1, backP1, levelBoundColor);
        Debug.DrawLine(frontP2, backP2, levelBoundColor);
        Debug.DrawLine(frontP3, backP3, levelBoundColor);
        Debug.DrawLine(frontP4, backP4, levelBoundColor);
    }
}
