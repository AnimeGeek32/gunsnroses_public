using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSound : MonoBehaviour {

	[FMODUnity.EventRef]
	public string jumpEvent;
	[FMODUnity.EventRef]
	public string landEvent;
	[FMODUnity.EventRef]
	public string footstepEvent;
	//[FMODUnity.EventRef]
	//public string dashEvent;
	[FMODUnity.EventRef]
	public string attackEvent;
	[FMODUnity.EventRef]
	public string slideEvent;
	FMOD.Studio.EventInstance slideSound;

	//FMOD.Studio.EventInstance dashSound;

	public void start ()
	{
		//dashSound = FMODUnity.RuntimeManager.CreateInstance (dashEvent);
		slideSound = FMODUnity.RuntimeManager.CreateInstance(slideEvent);
	}
	public void OnJumpSound()
	{
		FMODUnity.RuntimeManager.PlayOneShot (jumpEvent, transform.position);
	}
	public void OnLandSound()
	{
		FMODUnity.RuntimeManager.PlayOneShot (landEvent, transform.position);
	}
	public void OnFootstepSound()
	{
		FMODUnity.RuntimeManager.PlayOneShot (footstepEvent, transform.position);
	}
	public void OnDashStartSound()
	{
		//dashSound.start ();
	}
	public void OnDashEndSound()
	{
		//dashSound.stop (FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
	}
	public void OnAttackSound()
	{
		FMODUnity.RuntimeManager.PlayOneShot (attackEvent, transform.position);
	}
	public void OnDieSound()
	{
		//FMODUnity.RuntimeManager.PlayOneShot (dieEvent, transform.position);
	}
	public void OnSlideSoundStart()
	{
		//slideSound.start ();
		FMODUnity.RuntimeManager.PlayOneShot (slideEvent, transform.position);
	}
	public void OnSlideSoundStop()
	{
		//FMOD.Studio.PLAYBACK_STATE playbackState;
		//slideSound.getPlaybackState (out playbackState);
		//if (playbackState == FMOD.Studio.PLAYBACK_STATE.PLAYING)
		//{
		//	slideSound.stop (FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
		//}

	}


}
