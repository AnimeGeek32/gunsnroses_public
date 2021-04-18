using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Rewired;

public class CharacterSelectionController : MonoBehaviour {
    public float fadeInTime = 1.0f;
    public float fadeOutTime = 1.0f;
    public bool player1IsReady = false;
    public bool player2IsReady = false;
    public Image player1Portrait;
    public Image player2Portrait;
    public RawImage fader;
    public Color faderSolidColor = Color.black;
    public Color faderTransparentColor = new Color(0f, 0f, 0f, 0f);
    public Color selectedPortraitColor = new Color(1f, 1f, 1f, 0.8f);
    public string battleScene = "Main";

    private Player _player1;
    private Player _player2;
    private bool _isFading = true;

	// Use this for initialization
	void Start () {
        _player1 = ReInput.players.GetPlayer(0);
        _player2 = ReInput.players.GetPlayer(1);

        if (fader != null) {
            fader.gameObject.SetActive(true);
        }
        StartCoroutine(FadeIn());
	}

    // Update is called once per frame
    void Update()
    {
        if (!_isFading) {
            if (player1IsReady && player2IsReady)
            {
                StartLoadingBattleScene();
            }

            if (!player1IsReady)
            {
                if (_player1.GetButtonDown("Accept"))
                {
                    player1Portrait.color = selectedPortraitColor;
                    player1IsReady = true;
                }
            }

            if (!player2IsReady)
            {
                if (_player2.GetButtonDown("Accept"))
                {
                    player2Portrait.color = selectedPortraitColor;
                    player2IsReady = true;
                }
            }
        }
    }

    IEnumerator FadeIn() {
        float currentTime = 0f;
        _isFading = true;
        fader.color = faderSolidColor;
        while (currentTime < fadeInTime) {
            float deltaTime = Time.deltaTime;
            yield return new WaitForSeconds(deltaTime);
            currentTime += deltaTime;
            fader.color = Color.Lerp(faderSolidColor, faderTransparentColor, currentTime);
        }
        fader.color = faderTransparentColor;
        fader.gameObject.SetActive(false);
        _isFading = false;
    }

    void StartLoadingBattleScene() {
        if (!_isFading) {
            _isFading = true;
            StartCoroutine(LoadBattleScene());
        }
    }

    IEnumerator LoadBattleScene() {
        float currentTime = 0f;
        fader.gameObject.SetActive(true);
        fader.color = faderTransparentColor;
        while (currentTime < fadeOutTime) {
            float deltaTime = Time.deltaTime;
            yield return new WaitForSeconds(deltaTime);
            currentTime += deltaTime;
            fader.color = Color.Lerp(faderTransparentColor, faderSolidColor, currentTime);
        }
        fader.color = faderSolidColor;

        SceneManager.LoadScene(battleScene);
    }
}
