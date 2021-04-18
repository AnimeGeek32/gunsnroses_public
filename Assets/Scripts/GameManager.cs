using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;
#if !UNITY_PS4
using UnityEngine.Analytics;
#endif
using Rewired;

namespace GunsNRoses
{
    using System.Collections.Generic;
    using UnityEngine.UI;      //Allows us to use UI.

    public class GameManager : MonoBehaviour
    {


        private enum AnimationState
        {
            start, showRoud, showTitles, ready, fight, fadeOut, showWinner, gameOver, end, stop
        }

        private enum GameState
        {
            initialize, inProgress, player1Wins, player2Wins, noWinner, matchOver
        }

        private enum Round
        {
            first, second, third
        }

        // Properties

        public GameObject player1UI;
        public GameObject player2UI;
        private float shakeDuration = 0.1f;
        private float shakeAmount = 0.1f;
        private bool enableShakeP1 = false;
        private bool shakeInProgressP1 = false;
        private bool enableShakeP2 = false;
        private bool shakeInProgressP2 = false;

        private AnimationState animationState = AnimationState.start;
        private GameState gameState = GameState.inProgress;
        private Round currentRound = Round.first;

        public GameObject player1;
        public GameObject player2;

        public LifeBar player1LifeBar;
        public LifeBar player2LifeBar;
        public Timer timer;

        private Vector3 player1InitialPosition;
        private Vector3 player2InitialPosition;
        private Vector3 player1InitialScale;
        private Vector3 player2InitialScale;

        public Image readyImage;
        public Image fightImage;

        public Image round1Image;
        public Image round2Image;
        public Image round3Image;
        public Image player1Win1;
        public Image player2Win1;
        public Image player1Win2;
        public Image player2Win2;

        private int player1WinCount = 0;
        private int player2WinCount = 0;

        public Text statusText;

        public Image player1WinsImage;
        public Image player2WinsImage;
        public Image noWinnerImage;

        [Header("Debug UI")]
        public bool enableDebugUIOnStart = false;
        public Text player1DebugUI;
        public Text player2DebugUI;

        [Header("Pause UI")]
        public RawImage pauseOverlay;
        public Text pauseText;

        [FMODUnity.EventRef]
        public string gunzDeathSound;
        [FMODUnity.EventRef]
        public string rosezDeathSound;
		[FMODUnity.EventRef]
		public string roundSoundEvent;
		FMOD.Studio.EventInstance roundSound;
		[FMODUnity.EventRef]
		public string fightSoundEvent;
        FMOD.Studio.EventInstance fightSound;
		[FMODUnity.EventRef]
        public string pauseSnapShotEvent;
        FMOD.Studio.EventInstance pauseSnapShot;
		

        private float animationTime = 0.0f;
        private float animationDuration = 0.85f;
        private AnimationCurve fadeInCurve;
        private AnimationCurve fadeOutCurve;
        private bool _gameIsPaused = false;
        private Player _player1Input;
        private Player _player2Input;
        public float gameRestartInSecs = 7.0f;

        public static GameManager instance = null;              //Static instance of GameManager which allows it to be accessed by any other script.

        //Awake is always called before any Start functions
        void Awake()
        {
            Debug.Log("AWAKE GAME!");
            if (instance == null)           //Check if instance already exists
                instance = this;
            else if (instance != this)      //Replace current instance with new one
            {
                Destroy(instance.gameObject);
                instance = null;
                instance = this;
            }

            //Sets this to not be destroyed when reloading scene
            //DontDestroyOnLoad(gameObject);
        }

        // Use this for initialization
        void Start()
        {

            fadeInCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, animationDuration, 1.0f);
            fadeOutCurve = AnimationCurve.EaseInOut(0.0f, 1.0f, animationDuration, 0.0f);

            //HIDE UI VIEWS
            readyImage.color = new Color(readyImage.color.r, readyImage.color.g, readyImage.color.b, 0.0f);
            fightImage.color = new Color(fightImage.color.r, fightImage.color.g, fightImage.color.b, 0.0f);

            player1WinsImage.color = new Color(player1WinsImage.color.r, player1WinsImage.color.g, player1WinsImage.color.b, 0.0f);
            player2WinsImage.color = new Color(player2WinsImage.color.r, player2WinsImage.color.g, player2WinsImage.color.b, 0.0f);
            noWinnerImage.color = new Color(noWinnerImage.color.r, noWinnerImage.color.g, noWinnerImage.color.b, 0.0f);

            round1Image.color = new Color(round1Image.color.r, round1Image.color.g, round1Image.color.b, 0.0f);
            round2Image.color = new Color(round2Image.color.r, round2Image.color.g, round2Image.color.b, 0.0f);
            round3Image.color = new Color(round3Image.color.r, round3Image.color.g, round3Image.color.b, 0.0f);

            // Save character inital state
            player1InitialPosition = player1.transform.position;
            player2InitialPosition = player2.transform.position;
            player1InitialScale = player1.transform.localScale;
            player2InitialScale = player2.transform.localScale;

            // Make sure the game is not paused
            pauseText.enabled = false;
            pauseOverlay.enabled = false;
            _gameIsPaused = false;

			roundSound = FMODUnity.RuntimeManager.CreateInstance (roundSoundEvent);
            fightSound = FMODUnity.RuntimeManager.CreateInstance(fightSoundEvent);
			pauseSnapShot = FMODUnity.RuntimeManager.CreateInstance(pauseSnapShotEvent);
            FMOD.Studio.PLAYBACK_STATE playbackState;
            pauseSnapShot.getPlaybackState(out playbackState);
            if (playbackState == FMOD.Studio.PLAYBACK_STATE.PLAYING)
            {
                pauseSnapShot.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            }

            _player1Input = ReInput.players.GetPlayer(0);
            _player2Input = ReInput.players.GetPlayer(1);
            if (!enableDebugUIOnStart)
            {
                Destroy(player1DebugUI.gameObject);
                player1DebugUI = null;
                Destroy(player2DebugUI.gameObject);
                player2DebugUI = null;
            }
        }

        // Update is called once per frame
        void Update()
        {

            if (enableShakeP1 && !shakeInProgressP1)
            {
                StartCoroutine(ShakeUI(1));
            }

            if (enableShakeP2 && !shakeInProgressP2)
            {
                StartCoroutine(ShakeUI(2));
            }

            if (gameState == GameState.inProgress)
            {
                if (player1LifeBar.playerLife <= 0 || player2LifeBar.playerLife <= 0)
                {
                    if (player1LifeBar.playerLife <= 0 && player2LifeBar.playerLife <= 0)
                    {
                        gameState = GameState.noWinner;
                    }
                    else if (player1LifeBar.playerLife <= 0)
                    {
                        FMODUnity.RuntimeManager.PlayOneShot(gunzDeathSound, transform.position);
                        gameState = GameState.player2Wins;
                        player2.GetComponent<PlayerCharacterController>().StartWinPose();
                    }
                    else if (player2LifeBar.playerLife <= 0)
                    {
                        FMODUnity.RuntimeManager.PlayOneShot(rosezDeathSound, transform.position);
                        gameState = GameState.player1Wins;
                        player1.GetComponent<PlayerCharacterController>().StartWinPose();
                    }

                    animationState = AnimationState.showWinner;

                }
            }

            animationTime += Time.deltaTime;
            if (animationTime >= animationDuration)
            {
                animationTime = 0.0f;

                //NOTE: Should we move to a switch statement?
                if (animationState == AnimationState.start)
                {
                    animationState = AnimationState.showRoud;
                }
                else if (animationState == AnimationState.showRoud)
                {
                    animationState = AnimationState.showTitles;
                }
                else if (animationState == AnimationState.showTitles)
                {
                    animationState = AnimationState.ready;
                }
                else if (animationState == AnimationState.ready)
                {
                    animationState = AnimationState.fight;
                }
                else if (animationState == AnimationState.fight)
                {
                    animationState = AnimationState.fadeOut;
                }
                else if (animationState == AnimationState.fadeOut)
                {
                    animationState = AnimationState.stop;
                }
                else if (animationState == AnimationState.showWinner)
                {
                    scoreRound();
                    if (player1WinCount == 2 || player2WinCount == 2)
                    {
                        animationState = AnimationState.end;
                    }
                    else
                    {
                        animationState = AnimationState.gameOver;
                    }
                }
                else if (animationState == AnimationState.gameOver)
                {
                    animationState = AnimationState.start;
                    nextRound();
                }
            }

            //NOTE: Should we move to a switch statement?
            if (animationState == AnimationState.start)
            {
                
                if (currentRound == Round.first)
                {
                    FMOD.Studio.PLAYBACK_STATE playbackState;
                    roundSound.getPlaybackState(out playbackState);
                    if (playbackState == FMOD.Studio.PLAYBACK_STATE.STOPPED)
                    {
                        roundSound.setParameterValue("round_number", 0);
                        roundSound.start();
                    }
                    
                    round1Image.color = new Color(round1Image.color.r, round1Image.color.g, round1Image.color.b, fadeInCurve.Evaluate(animationTime));
                }
                else if (currentRound == Round.second)
                {
                    FMOD.Studio.PLAYBACK_STATE playbackState;
                    roundSound.getPlaybackState(out playbackState);
                    if (playbackState == FMOD.Studio.PLAYBACK_STATE.STOPPED)
                    {
                        roundSound.setParameterValue("round_number", 1);
                        roundSound.start();
                    }
                    round2Image.color = new Color(round2Image.color.r, round2Image.color.g, round2Image.color.b, fadeInCurve.Evaluate(animationTime));
                }
                else if (currentRound == Round.third)
                {
                    FMOD.Studio.PLAYBACK_STATE playbackState;
                    roundSound.getPlaybackState(out playbackState);
                    if (playbackState == FMOD.Studio.PLAYBACK_STATE.STOPPED)
                    {
                        roundSound.setParameterValue("round_number", 2);
                        roundSound.start();
                    }
                    round3Image.color = new Color(round3Image.color.r, round3Image.color.g, round3Image.color.b, fadeInCurve.Evaluate(animationTime));
                }
            }
            else if (animationState == AnimationState.showRoud)
            {
                if (currentRound == Round.first)
                {
                    round1Image.color = new Color(round1Image.color.r, round1Image.color.g, round1Image.color.b, fadeOutCurve.Evaluate(animationTime));
					}
                else if (currentRound == Round.second)
                {
                    round2Image.color = new Color(round2Image.color.r, round2Image.color.g, round2Image.color.b, fadeOutCurve.Evaluate(animationTime));
                }
                else if (currentRound == Round.third)
                {
                    round3Image.color = new Color(round3Image.color.r, round3Image.color.g, round3Image.color.b, fadeOutCurve.Evaluate(animationTime));
                }
            }
            else if (animationState == AnimationState.showTitles)
            {
                readyImage.color = new Color(readyImage.color.r, readyImage.color.g, readyImage.color.b, fadeInCurve.Evaluate(animationTime));
            }
            else if (animationState == AnimationState.ready)
            {
                readyImage.color = new Color(readyImage.color.r, readyImage.color.g, readyImage.color.b, fadeOutCurve.Evaluate(animationTime));
            }
            else if (animationState == AnimationState.fight)
            {
                FMOD.Studio.PLAYBACK_STATE playbackState;
                fightSound.getPlaybackState(out playbackState);
                if (playbackState == FMOD.Studio.PLAYBACK_STATE.STOPPED)
                {
                    fightSound.start();
                }
                fightImage.color = new Color(fightImage.color.r, fightImage.color.g, fightImage.color.b, fadeInCurve.Evaluate(animationTime));

            }
            else if (animationState == AnimationState.fadeOut)
            {
                fightImage.color = new Color(fightImage.color.r, fightImage.color.g, fightImage.color.b, fadeOutCurve.Evaluate(animationTime));
            }
            else if (animationState == AnimationState.showWinner)
            {
                if (gameState == GameState.noWinner)
                {
                    noWinnerImage.color = new Color(noWinnerImage.color.r, noWinnerImage.color.g, noWinnerImage.color.b, fadeInCurve.Evaluate(animationTime));
                }
                else if (gameState == GameState.player1Wins)
                {
                    player1WinsImage.color = new Color(player1WinsImage.color.r, player1WinsImage.color.g, player1WinsImage.color.b, fadeInCurve.Evaluate(animationTime));
                }
                else if (gameState == GameState.player2Wins)
                {
                    player2WinsImage.color = new Color(player2WinsImage.color.r, player2WinsImage.color.g, player2WinsImage.color.b, fadeInCurve.Evaluate(animationTime));
                }
            }
            else if (animationState == AnimationState.gameOver)
            {
                if (gameState == GameState.noWinner)
                {
                    noWinnerImage.color = new Color(noWinnerImage.color.r, noWinnerImage.color.g, noWinnerImage.color.b, fadeOutCurve.Evaluate(animationTime));
                }
                else if (gameState == GameState.player1Wins)
                {
                    player1WinsImage.color = new Color(player1WinsImage.color.r, player1WinsImage.color.g, player1WinsImage.color.b, fadeOutCurve.Evaluate(animationTime));
                }
                else if (gameState == GameState.player2Wins)
                {
                    player2WinsImage.color = new Color(player2WinsImage.color.r, player2WinsImage.color.g, player2WinsImage.color.b, fadeOutCurve.Evaluate(animationTime));
                }
            }

            if (enableDebugUIOnStart) {
                player1DebugUI.text = "P1 Last Active Input name: ";
                if (_player1Input.controllers.GetLastActiveController() == null) {
                    player1DebugUI.text += "None";
                } else {
                    player1DebugUI.text += _player1Input.controllers.GetLastActiveController().name;
                }
                player2DebugUI.text = "P2 Last Active Input name: ";
                if (_player2Input.controllers.GetLastActiveController() == null) {
                    player2DebugUI.text += "None";
                } else {
                    player2DebugUI.text += _player2Input.controllers.GetLastActiveController().name;
                }
            }
        }

        public void RemoveLife(string characterName, int amout)
        {
            if (characterName == "Gunz_character")
            {
                player1LifeBar.playerLife -= amout;
                enableShakeP1 = true;
            }
            else if (characterName == "Rosez_character")
            {
                player2LifeBar.playerLife -= amout;
                enableShakeP2 = true;
            }
        }

        public void TimerEnded()
        {
#if !UNITY_PS4
            //TODO: refactor analytics
            Analytics.CustomEvent("roundOver", new Dictionary<string, object>
                 {
                    { "winner", player1LifeBar.playerLife > player2LifeBar.playerLife ? "player1" : "player2"},
                    { "round", player1WinCount + player2WinCount },
                    { "character", player1LifeBar.playerLife > player2LifeBar.playerLife ? "Gunz" : "Rosez"}
                });
#endif

            if (gameState == GameState.inProgress)
            {
                if (player1LifeBar.playerLife == player2LifeBar.playerLife)
                {
                    gameState = GameState.noWinner;
                }
                else if (player1LifeBar.playerLife > player2LifeBar.playerLife)
                {
                    gameState = GameState.player1Wins;
                    player1.GetComponent<PlayerCharacterController>().StartWinPose();
                }
                else
                {
                    gameState = GameState.player2Wins;
                    player2.GetComponent<PlayerCharacterController>().StartWinPose();
                }
                animationState = AnimationState.showWinner;
            }
        }

        void scoreRound()
        {
            if (gameState == GameState.player1Wins)
            {

                player1WinCount += 1;

#if !UNITY_PS4
                //TODO: refactor analytics
                Analytics.CustomEvent("round over", new Dictionary<string, object>
                 {
                    { "winner", "player1" },
                    { "round", player1WinCount + player2WinCount },
                    { "character", "Gunz" }
                });
#endif

                if (player1WinCount == 1)
                {
                    player1Win1.color = Color.green;
                }
                else if (player1WinCount == 2)
                {
                    player1Win2.color = Color.green;
                    gameState = GameState.matchOver;
                    animationState = AnimationState.end;
                    timer.timerDone = true;
                    StartCoroutine(ExitToMenu());
                }
            }
            else if (gameState == GameState.player2Wins)
            {
                player2WinCount += 1;

#if !UNITY_PS4
                //TODO: refactor analytics
                Analytics.CustomEvent("round over", new Dictionary<string, object>
				 {
					{ "winner", "player2" },
					{ "round", player1WinCount + player2WinCount },
					{ "character", "Rosez" }
				});
#endif

                if (player2WinCount == 1) {
					player2Win1.color = Color.green;
				} else if (player2WinCount == 2) {
					player2Win2.color = Color.green;
					gameState = GameState.matchOver;
					animationState = AnimationState.end;
					timer.timerDone = true;
					StartCoroutine (ExitToMenu());
				}
			}
		}

		void nextRound() {

			if (currentRound != Round.third && gameState != GameState.noWinner) {
				if (currentRound == Round.first) {
					currentRound = Round.second;
					statusText.text = "ROUND 2";
				} else if (currentRound == Round.second) {
					currentRound = Round.third;
					statusText.text = "ROUND 3";
				}
			}

			if (gameState != GameState.matchOver) {
				player1.transform.position = player1InitialPosition;
				player2.transform.position = player2InitialPosition;
				player1.transform.localScale = player1InitialScale;
				player2.transform.localScale = player2InitialScale;

				player1LifeBar.playerLife = 100;
				player2LifeBar.playerLife = 100;

				gameState = GameState.inProgress;
				timer.Reset ();
                player1.GetComponent<PlayerCharacterController>().InitiateStartPose();
                player2.GetComponent<PlayerCharacterController>().InitiateStartPose();
                Camera.main.GetComponent<MultiplayerCameraController>().ResetCameraToStartPosition();
			}
		}

        public void LeaveGame() {            
			SceneManager.LoadScene(0);
        }

        public bool GetPauseStatus {
            get {
                return _gameIsPaused;
            }
        }

        public void TogglePause() {
            if (_gameIsPaused) {
                pauseOverlay.enabled = false;
                pauseText.enabled = false;
               	FMOD.Studio.PLAYBACK_STATE playbackState;
				pauseSnapShot.getPlaybackState (out playbackState);
				if (playbackState == FMOD.Studio.PLAYBACK_STATE.PLAYING)
				{
					pauseSnapShot.stop (FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
				}
                Time.timeScale = 1.0f;
                _gameIsPaused = false;
            } else {
                pauseOverlay.enabled = true;
                pauseText.enabled = true;
                //FMODUnity.RuntimeManager.PauseAllEvents(true);
				FMOD.Studio.PLAYBACK_STATE playbackState;
				pauseSnapShot.getPlaybackState (out playbackState);
				if (playbackState == FMOD.Studio.PLAYBACK_STATE.STOPPED)
				{
					pauseSnapShot.start();
				}
                Time.timeScale = 0.0f;
                _gameIsPaused = true;
            }
        }

		private void callAnalytics(Dictionary<string, object> dict) {

		}

		IEnumerator ShakeUI(int player) {

			GameObject playerUI = player1UI;

			if (player == 1) {
				shakeInProgressP1 = true;
				playerUI = player1UI;
			} else if (player == 2) {
				shakeInProgressP2 = true;
				playerUI = player2UI;
			}

			float elapsed = 0.0f;


			Vector3 originalPos = playerUI.transform.position;

			while (elapsed < shakeDuration) {

				elapsed += Time.deltaTime;          

				float percentComplete = elapsed / shakeDuration;         
				float damper = 1.0f - Mathf.Clamp(4.0f * percentComplete - 3.0f, 0.0f, 1.0f);

				// map value to [-1, 1]
				float x = Random.value * 2.0f - 1.0f;
				float y = Random.value * 2.0f - 1.0f;
				x *= shakeAmount * damper;
				y *= shakeAmount * damper;

				Vector3 moveVector = new Vector3 (x, y, originalPos.z);
				playerUI.transform.position = originalPos + moveVector;

				yield return null;
			}

			playerUI.transform.position = originalPos;

			if (player == 1) {
				enableShakeP1 = false;
				shakeInProgressP1 = false;
			} else if (player == 2) {
				enableShakeP2 = false;
				shakeInProgressP2 = false;
			}
		}

		IEnumerator ExitToMenu()
		{
			yield return new WaitForSeconds(5);
			SceneManager.LoadScene(0);
		}

		IEnumerator RestartGameInSecs(float sec)
		{
			yield return new WaitForSeconds(sec);
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		}
	}
}
