﻿using UnityEngine;
using UnityEngine.InputSystem;

public class GameSceneManager : MonoBehaviour {
	[SerializeField] protected Camera  _camera;
	[SerializeField] protected Level[] _levels;
	[SerializeField] protected int     _levelIndex;
	[SerializeField] protected Robot   _robot;
	[SerializeField] protected Flower  _flower;
	[SerializeField] protected float   _loadLevelSpeed = 1;
	[SerializeField] protected GameUi  _ui;

	private Level currentLevel => _levels[_levelIndex];

	private float     levelLerp       { get; set; }
	private Transform cameraTransform { get; set; }

	private void Awake() {
		cameraTransform = _camera.transform;
		_flower.onDied.AddListener(HandleFlowerDead);
		foreach (var level in _levels) level.SetLevelEnabled(false);
		if (_levelIndex == _levels.Length - 1) PlayOutro();
		else RestartCurrentLevel();
		SetLoadLevelLerp(1);
		RefreshUiLevel();
		Inputs.controls.Global.RestartLevel.AddPerformListenerOnce(RestartCurrentLevel);
	}

	private void GoToNextLevel() {
		DisableLevel();
		_levelIndex++;
		levelLerp = 0;
		RefreshUiLevel();
	}

	private void RefreshUiLevel() => _ui.SetLevelName($"Level {_levelIndex + 1}");

	private void Update() {
		if (levelLerp >= 1) return;
		SetLoadLevelLerp(levelLerp + _loadLevelSpeed * Time.deltaTime);
	}

	private void SetLoadLevelLerp(float lerp) {
		levelLerp = lerp;
		cameraTransform.position = Vector3.Lerp(cameraTransform.position, currentLevel.cameraPosition, levelLerp * levelLerp);
		_camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, currentLevel.cameraOrthographicSize, levelLerp * levelLerp);
		if (levelLerp < 1) return;
		if (_levelIndex == _levels.Length - 1) {
			PlayOutro();
		}
		else {
			currentLevel.RestartMechanisms();
			EnableLevel();
		}
	}

	private void PlayOutro() {
		_robot.gameObject.SetActive(false);
		currentLevel.SetLevelEnabled(true);
		currentLevel.GetComponentInChildren<OutroLevel>().Play();
	}

	private void EnableLevel() {
		currentLevel.SetLevelEnabled(true);
		currentLevel.onExitReached.AddListenerOnce(HandleLevelExitReached);
		_robot.SetEnabled(true);
		Inputs.controls.Global.RestartLevel.Enable();
	}

	private void DisableLevel() {
		currentLevel.SetLevelEnabled(false);
		currentLevel.onExitReached.RemoveListener(HandleLevelExitReached);
		_robot.SetEnabled(false);
		Inputs.controls.Global.RestartLevel.Disable();
	}

	private void HandleFlowerDead() {
		_robot.SetEnabled(false);
		_ui.SetGameOverUiVisible(true);
	}

	private void RestartCurrentLevel(InputAction.CallbackContext obj) => RestartCurrentLevel();

	private void RestartCurrentLevel() {
		_flower.Revive();
		_robot.Attach(_flower);
		currentLevel.Respawn(_robot);
		_robot.SetEnabled(true);
		currentLevel.RestartMechanisms();
		_ui.SetGameOverUiVisible(false);
	}

	private void HandleLevelExitReached() {
		if (!_robot.holdsFlower) {
			Debug.Log("Aren't you missing something? (Hint: THE FLOWER!)");
			return;
		}
		GoToNextLevel();
	}
}