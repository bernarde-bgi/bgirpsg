using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MiniJSON;

public class SimpleGame : MonoBehaviour
{
	public string versionString;
	public string errorString;
	
	private bool playingTheGame;
	private int score;
	
	void Start ()
	{
		Cashplay.Client.OnGameStarted += (matchId) =>
		{
			Debug.Log ("[Cashplay.SimpleGame] Game started: " + matchId);
			errorString = "";
			score = 0;
			playingTheGame = true;
		};
		
		Cashplay.Client.OnGameFinished += (matchId) =>
		{
			Debug.Log ("[Cashplay.SimpleGame] Game finished: " + matchId);
			errorString = "";
			playingTheGame = false;
		};
		
		Cashplay.Client.OnCanceled += () =>
		{
			Debug.Log ("[Cashplay.SimpleGame] Returned");
			errorString = "";
		};
		
		Cashplay.Client.OnError += (error) =>
		{
			Debug.Log ("[Cashplay.SimpleGame] Error: " + error);
			errorString = "ErrorCode: " + error;
		};

		Cashplay.Client.Init();
	}
	
	void OnGUI()
	{
		GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 20));
		GUI.skin.button.fixedHeight = (Screen.height - 20) / 5;
		GUI.skin.button.fixedWidth = Screen.width - 20;
		GUI.skin.textField.wordWrap = true;
		GUI.skin.textField.fixedWidth = (Screen.width - 20);
		GUI.skin.textField.fixedHeight = (Screen.height - 20) * 4 / 10;

		GUILayout.Space(20);
		GUILayout.Label(versionString);
		GUILayout.Label(errorString);
		
		if (playingTheGame)
		{
			GUILayout.Label("Score: " + score);
			if (GUILayout.Button("Add Score"))
			{
				score += 1;
			}
			if (GUILayout.Button("Finish"))
			{
				Cashplay.Client.ReportGameFinish(score);
			}
			if (GUILayout.Button("Forfeit"))
			{
				Cashplay.Client.ForfeitGame();
			}
		}
		else
		{
			if (GUILayout.Button("Find Game"))
			{
				Cashplay.Client.FindGame();
			}
		}
		
		if (GUILayout.Button("Exit"))
		{
			Application.Quit();
		}
		GUILayout.EndArea();
	}
}
