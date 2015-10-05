using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;


public enum Janken{
	ROCK = 0,
	PAPER,
	SCISSORS,
}

public enum J_RESULT{
	WIN,
	LOSE,
	DRAW,
}
public class Game : MonoBehaviour {


	//REFERENCES UI
	[SerializeField] private bool useComputer;
	[SerializeField] private Text resultLabel;
	[SerializeField] private Text opp_resultLabel;
	[SerializeField] private Text scoreLabel;
	[SerializeField] private Text timerLabel;

	[SerializeField] private Button Start_BTN;

	[SerializeField] private Image playerResultImage;
	[SerializeField] private Image opponentResultImage;


	public Sprite[] resultSprites;

	//RESULTS
	private Janken player_j;
	private Janken opponent_j;
	private J_RESULT result;
	private J_RESULT opp_result;
	private int score = 0;



	//GAME LOGIC
	private bool initJanken = false;
	private bool initHand = false;
	private int countDown = 0;
	private float resultDuration = 0;




	// Use this for initialization
	void Start () {
		player_j = Janken.ROCK;
		opponent_j = Janken.ROCK;
		score = 0;


	}

	public void StartGame(){
		if (initJanken)
			return;
		ToggleStart ();
		initJanken = true;
		countDown = 3;
		timerLabel.text = countDown.ToString();
		initHand = false;
		StartCoroutine (StartJanken ());
	}

	IEnumerator StartJanken(){

		while (countDown >= 0) {
			timerLabel.text = countDown.ToString();
			yield return new WaitForSeconds(1);
			countDown--;
		}

		EnableHands ();


	}

	private void EnableHands(){
		initHand = true;
	}
	private void DisableHands(){
		initHand = false;
	}

	private void ToggleStart(){
		Start_BTN.gameObject.SetActive (!Start_BTN.gameObject.activeSelf);
	}


	public void OnClickHand(int value){
		if (initHand == false)
			return;
		player_j = (Janken)value;
		DisableHands ();
		RollJanken();
	}



	private void RollJanken(){
			
		//** RANDOM OPPONENT VALUE
		if (useComputer)
			opponent_j = GetRandomOpponent ();

		result = GetResult (player_j, opponent_j);

		switch (result) {
			case J_RESULT.WIN:
				score++;
				opp_result = J_RESULT.LOSE;
				break;
			case J_RESULT.LOSE:
				opp_result = J_RESULT.WIN;
				break;
			case J_RESULT.DRAW:
				opp_result = J_RESULT.DRAW;
				break;
		}

		ShowResult ();
	}

	private Janken GetRandomOpponent(){
		return  (Janken)Random.Range (0, 3);
	}

	private J_RESULT GetResult(Janken player, Janken opponent){

		if (player == opponent)
			return J_RESULT.DRAW;
		else if ((player == Janken.ROCK && opponent == Janken.SCISSORS) ||
			(player == Janken.PAPER && opponent == Janken.ROCK) ||
			(player == Janken.SCISSORS && opponent == Janken.PAPER))
			return J_RESULT.WIN;
		else if ((player == Janken.ROCK && opponent == Janken.PAPER) ||
			(player == Janken.PAPER && opponent == Janken.SCISSORS) ||
			(player == Janken.SCISSORS && opponent == Janken.ROCK))
			return J_RESULT.LOSE;
			
		return J_RESULT.WIN;
	}


	private void ShowResult(){
		resultDuration = 2;
		StartCoroutine (AnimateResult ());
	}

	IEnumerator AnimateResult(){
		
		playerResultImage.sprite = resultSprites [(int)player_j];
		while (resultDuration >= 0) {
			RandomizeResults();
			yield return new WaitForSeconds(0.1f);
			resultDuration -= 0.1f;
		}


		
		//resultLabel.text = result.ToString ();
		//opp_resultLabel.text = opp_result.ToString ();
		
		//	playerResultImage.sprite = resultSprites [(int)player_j];
		opponentResultImage.sprite = resultSprites [(int)opponent_j];

		scoreLabel.text = score.ToString ();

		//RESET GAME
		initJanken = false;
		ToggleStart ();
	}

	private void RandomizeResults(){
		//playerResultImage.sprite = resultSprites [Random.Range (0, 3)];
		opponentResultImage.sprite = resultSprites [Random.Range (0, 3)];
		//resultLabel.text = ((J_RESULT)Random.Range (0, 3)).ToString();
	//	opp_resultLabel.text = ((J_RESULT)Random.Range (0, 3)).ToString();
	}

}
