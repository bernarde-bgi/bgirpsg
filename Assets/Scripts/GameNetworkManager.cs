using UnityEngine;
using System.Collections;

public class GameNetworkManager : MonoBehaviour {

	[SerializeField] private GameObject createButton;
	[SerializeField] private GameObject joinButton;
	[SerializeField] private Transform  prefab;


	int port = 25000;
	public int maxClients = 1;
	string remoteIP = "127.0.0.1";

	private bool serverInitalized = false;

	
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
	}

	public void FindOpponent(){
		if (!serverInitalized) {
			//Network.InitializeServer (maxClients, port, false);
			Network.InitializeServer(maxClients,port,!Network.HavePublicAddress()); 
			//MasterServer.dedicatedServer = true;
			//MasterServer.RegisterHost("BGI", "Janken", "Peer to Peer");
		}
	}
	public void JoinGame(){
		if (!serverInitalized)
			MasterServer.PollHostList ();

		Game.instance.ResetGame ();

	}

	public void EndGame(){
		if (serverInitalized)
			Network.Disconnect ();

		StopAllCoroutines ();
	}

	void OnGUI(){
		if (Network.peerType == NetworkPeerType.Disconnected) {
			serverInitalized = false;
		} else {
			if(!serverInitalized){
				serverInitalized = true;

			}
			if (Network.peerType == NetworkPeerType.Connecting)
				GUILayout.Label ("Network server is starting up...");
			else { 
				GUILayout.Label ("Network server is running.");          
				showServerInformation ();    
				showClientInformation ();
			}
			if (GUILayout.Button ("Stop Server")) {               
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


	#region UNITY NETWORK
	void OnServerInitialized(){
		Debug.Log("OnServerInitialized");
		DisableButtons();
	}

	void OnDisconnectedFromServer(NetworkDisconnection info){
		Debug.Log("OnDisconnectedFromServer");
		EnableButtons ();
	}
	void OnConnectedToServer(){
		Debug.Log("OnConnectedToServer");

		DisableButtons ();
		Game.instance.InitializeGame ();
	}

	void OnPlayerConnected(NetworkPlayer player){
		Debug.Log("OnPlayerConnected");
		//spawnPlayer (player);
		Game.instance.InitializeGame ();
	}

	#endregion

	private void EnableButtons(){
		createButton.gameObject.SetActive(true);
		joinButton.gameObject.SetActive(true);
	}

	private void DisableButtons(){
		createButton.gameObject.SetActive(false);
		joinButton.gameObject.SetActive(false);
	}


	#region RPC

	private Hashtable players;
	public void RemoteHand(int value){
	 
		if (Network.isServer)	
			 GetComponent<NetworkView>().RPC ("ShowRPCHand", RPCMode.OthersBuffered, value);
		else if (Network.isClient)
			 GetComponent<NetworkView>().RPC ("ShowRPCHand", RPCMode.OthersBuffered, value);
	} 

	[RPC]
	public void ShowRPCHand(int value){
			Debug.Log ("[RPC] " + value.ToString ());
			Game.instance.OpponentDrawn (value);

	}


	void spawnPlayer(NetworkPlayer player) {
		Debug.Log("Spawning player game object for player " + player);
		GameObject go = Network.Instantiate (prefab.gameObject, Vector3.zero, Quaternion.identity, 0) as GameObject;
		players[player] = go;
		_peer = go;
	}

	#endregion
}
