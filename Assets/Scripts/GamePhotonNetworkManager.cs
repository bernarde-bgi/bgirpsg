using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class GamePhotonNetworkManager : Photon.PunBehaviour {

	[SerializeField] private GameObject createButton;
	[SerializeField] private GameObject joinButton;

	
	[SerializeField] private GameObject findMatchButton;
	[SerializeField] private Text findMatchLog;

	[SerializeField] private PhotonView myPhotonView;

	/*[SerializeField] private Transform  prefab;
	[SerializeField] private networkController networkcontrol;
	[SerializeField] private stateController stateControl;
	*/
	int port = 25000;
	public int maxClients = 1;
	string remoteIP = "127.0.0.1";
	
	private bool lobbyCreated = false;
	private bool onRoomCreated = false;
	private int playerCount = 0;
	
	
	private GameObject _server;
	private GameObject _peer;
	
	void Start(){
		players = new Hashtable ();
		if (Game.instance.useComputer) {
			DisableButtons ();
			Game.instance.InitializeGame();
		}
		else {
			EnableButtons();
		}

		
		FindMatchLog (false);
	}

	private void InitializePhoton(){

		PhotonNetwork.ConnectUsingSettings("v4.2");
	}

	/// <summary>
	/// Find Quick Match
	/// </summary>
	public void QuickMatch(){
		if (!lobbyCreated && !onRoomCreated) {
			InitializePhoton();
			DisableButtons();
			FindMatchLog (true);
		}
	}

	public void DisconnectMatch(){
		PhotonNetwork.Disconnect ();
	}

	/// <summary>
	/// Finds the opponent.OnCLICK
	/// </summary>
	public void FindOpponent(){
			//OLD UNITY NET
		//	Network.InitializeServer(maxClients,port,!Network.HavePublicAddress()); 
		//	networkcontrol.ListenForClients("janken");
	}

	
	/// <summary>
	/// Joins the game. OnCLICK
	/// </summary>
	public void JoinGame(){
		/*if (!serverInitalized) {
			networkcontrol.ListenServer();
			networkcontrol.FindServer();
			if(stateControl.LocalServer != null && stateControl.LocalServer.Count > 0)
				Network.Connect (stateControl.LocalServer[0].address, port);
		*/	
	}

	#region PHOTON CALLBACKS
	public override void OnJoinedLobby()
	{
		Debug.Log("JoinRandom");
		lobbyCreated = true;
		PhotonNetwork.JoinRandomRoom ();
	}

	private void CreateOrJoinMatch(){
		RoomOptions roomOptions = new RoomOptions (){isVisible = true, maxPlayers = 2};
		PhotonNetwork.JoinOrCreateRoom ("RPSRoom" + Random.Range (0, 1000),roomOptions,TypedLobby.Default);
	}

	public override void OnCreatedRoom(){
		onRoomCreated = true;
		Debug.Log("OnCreatedRoom");
		playerCount = PhotonNetwork.playerList.Length;
		Debug.Log (playerCount);
	}
	public override void OnJoinedRoom(){
		Debug.Log("OnJoinedRoom");
		playerCount = PhotonNetwork.playerList.Length;
		this.myPhotonView.RPC ("HasConnectedInRoom", PhotonTargets.All,PhotonNetwork.player.ID);
	}

	private void FindMatchLog(bool show){
		findMatchLog.gameObject.SetActive (show);
		findMatchLog.text = "Finding Match...";
	}


	
	public void OnPhotonRandomJoinFailed()
	{
		Debug.Log("OnPhotonRandomJoinFailed");
		CreateOrJoinMatch ();
	}
	

	#endregion

	private void EnableButtons(){
		findMatchButton.gameObject.SetActive (true);
//		createButton.gameObject.SetActive(true);
//		joinButton.gameObject.SetActive(true);
	}
	
	private void DisableButtons(){
		findMatchButton.gameObject.SetActive (false);
	//	createButton.gameObject.SetActive(false);
		//joinButton.gameObject.SetActive(false);
	}


	public void EndGame(bool showWinner = false){
		if (lobbyCreated) {
			//Network.Disconnect ();
			PhotonNetwork.Disconnect();	
		}
		Game.instance.ResetGame();
		if (!showWinner)
			StopAllCoroutines ();
		else {
			StopAllCoroutines ();
			Game.instance.AnimateResultAtEnd();
		}
	}
	
	void Update(){
		if (Game.instance.hasAWinner && Game.instance.hasGameStarted) {
			EndGame(true);
		}
	}
	
	
	
	void OnGUI(){
		if (Network.peerType == NetworkPeerType.Disconnected) {
			lobbyCreated = false;
		} else {
			if(!lobbyCreated){
				lobbyCreated = true;
				
			}
			if (Network.peerType == NetworkPeerType.Connecting)
				GUILayout.Label ("Network server is starting up...");
			else { 
				GUILayout.Label ("Network server is running.");          
				//showServerInformation ();    
				//showClientInformation ();
			}
			if (GUILayout.Button ("QUIT")) {               
				EndGame ();   
			}
		}
	}
	
	void showClientInformation(){
		GUILayout.Label("Clients: " + Network.connections.Length + "/" + maxClients);
		foreach(NetworkPlayer p in Network.connections) {
			GUILayout.Label(" Player from ip/port: " + p.ipAddress  + "/" + p.port); 
		}	
	}
	
	void showServerInformation(){
		GUILayout.Label("IP: " + Network.player.ipAddress + " Port: " + Network.player.port); 
	}
	
	


	
	
	#region RPC
	
	private Hashtable players;
	public void RemoteHand(int value){
		/*
		if (Network.isServer)	
			GetComponent<NetworkView>().RPC ("ShowRPCHand", RPCMode.OthersBuffered, value);
		else if (Network.isClient)
			GetComponent<NetworkView>().RPC ("ShowRPCHand", RPCMode.OthersBuffered, value);
			*/

		this.myPhotonView.RPC ("ShowRPCHand", PhotonTargets.All, value, PhotonNetwork.player.ID);
	} 
	
	[PunRPC]
	public void ShowRPCHand(int value, int ID){
		if (ID != PhotonNetwork.player.ID) {
			Debug.Log ("[PunRPC] " + value.ToString ());
			Game.instance.OpponentDrawn (value);
		}
	}

	[PunRPC]
	public void HasConnectedInRoom(int ID){
		Debug.Log ("[HasConnectedInRoom] " + ID);
		playerCount = PhotonNetwork.playerList.Length;
		if (playerCount >= 2) {
			FindMatchLog (false); 
			Game.instance.InitializeGame();
		}
	}

	#endregion
	
	
}
