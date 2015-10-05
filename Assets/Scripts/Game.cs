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
	[SerializeField] public bool useComputer;
	[SerializeField] private Text resultLabel;
	[SerializeField] private Text opp_resultLabel;
	[SerializeField] private Text scoreLabel;
	[SerializeField] private Text opp_scoreLabel;
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
	private int opp_score = 0;



	//GAME LOGIC
	private bool initJanken = false;
	private bool initHand = false;
	private bool hasPlayerDrawn = false;
	private bool hasOppDrawn = false;
	private int countDown = 0;
	private float resultDuration = 0;




	
	private static Game _instance = default(Game);
	
	public static Game instance {
		get {
			if (!_instance) {
				GameObject gamecore = GameObject.Find("GamePanel");
				_instance = gamecore.GetComponent<Game>();
			}
			return _instance;
		}
	}
	
	void Awake ()
	{
		if(_instance != null && _instance != this) {
			GameObject.Destroy(this.gameObject);
			return;
		}	
		_instance = this;
		DontDestroyOnLoad(this);
	}


	// Use this for initialization
	void Start () {
		ResetGame ();
	//	StartCoroutine(StartGame (0.5f));

	}

	public void ResetGame(){
		player_j = Janken.ROCK;
		opponent_j = Janken.ROCK;
		score = 0;
		countDown = 3;
		initJanken = false;
		initHand = false;
		hasPlayerDrawn = false;
		hasOppDrawn = false;
	}

	public void InitializeGame(){
		StartCoroutine(StartGame (0.5f));
	}


	public IEnumerator StartGame(float delay){
		//if (initJanken)
		//	yield return;
	
		yield return new WaitForSeconds (delay);
		//ToggleStart ();
		initJanken = true;
		countDown = 3;
		timerLabel.text = countDown.ToString();
		initHand = false;
		hasPlayerDrawn = false;
		hasOppDrawn = false;
		StartCoroutine (StartJanken ());
		playerResultImage.sprite = resultSprites [0];
		opponentResultImage.sprite = resultSprites [0];
	//	StartCoroutine (StartAnimateHands ());
	//	StartCoroutine (StartAnimateOppHands ());
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
		hasPlayerDrawn = true;
		DisableHands ();
		if (useComputer)
			RollJanken ();
		else {
			if(hasPlayerDrawn && hasOppDrawn)
				RollJankenInfo();
		}
		
		playerResultImage.sprite = resultSprites [(int)player_j];

	}



	private void RollJanken(){
			
		//** RANDOM OPPONENT VALUE
		if (useComputer) {
		
			opponent_j = GetRandomOpponent ();
			RollJankenInfo();
		}
	}

	private void RollJankenInfo(){
		result = GetResult (player_j, opponent_j);
		
		switch (result) {
		case J_RESULT.WIN:
			score++;
			opp_result = J_RESULT.LOSE;
			break;
		case J_RESULT.LOSE:
			opp_score++;
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

		/*while (resultDuration >= 0) {
			RandomizeOppResults();
			yield return new WaitForSeconds(0.1f);
			resultDuration -= 0.1f;
		}*/

		yield return new WaitForSeconds(2f);

		
		//resultLabel.text = result.ToString ();
		//opp_resultLabel.text = opp_result.ToString ();
		
		//	playerResultImage.sprite = resultSprites [(int)player_j];
		opponentResultImage.sprite = resultSprites [(int)opponent_j];

		scoreLabel.text = score.ToString ();
		opp_scoreLabel.text = opp_score.ToString ();

		//RESET GAME
		initJanken = false;
		StartCoroutine(StartGame (2));
		//ToggleStart ();
	}

	private void RandomizeOppResults(){
		//playerResultImage.sprite = resultSprites [Random.Range (0, 3)];
		opponentResultImage.sprite = resultSprites [Random.Range (0, 3)];
		//resultLabel.text = ((J_RESULT)Random.Range (0, 3)).ToString();
	//	opp_resultLabel.text = ((J_RESULT)Random.Range (0, 3)).ToString();
	}
	private void RandomizePlayerResult(){
		playerResultImage.sprite = resultSprites [Random.Range (0, 3)];
	}

	private IEnumerator StartAnimateHands(){
		while (!hasPlayerDrawn) {
			RandomizePlayerResult();
			yield return new WaitForSeconds(0.15f);
		}
	}

	private IEnumerator StartAnimateOppHands(){
		while (!hasOppDrawn) {
			RandomizeOppResults();
			yield return new WaitForSeconds(0.15f);
		}
	}

	public void OpponentDrawn(int value){
		opponent_j = (Janken)value;
		hasOppDrawn = true;
		if(hasPlayerDrawn && hasOppDrawn)
			RollJankenInfo();

	}




}
