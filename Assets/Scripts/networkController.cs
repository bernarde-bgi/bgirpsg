using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using System.Threading;

public class Player
{
	public NetworkPlayer np  { get; set; }
	public GameObject go  { get; set; }
	public NetworkViewID vi { get; set; }
	
	public Player (NetworkPlayer n, GameObject g)
	{
		np = n;
		go = g;
	}
	public Player (NetworkPlayer n, GameObject g, NetworkViewID v)
	{
		np = n;
		go = g;
		vi = v;
	}
}

public class UdpState
{
	public IPEndPoint e ;
	public UdpClient u ;
}


public class networkController : MonoBehaviour 
{
	
	public List<Player> players = new List<Player>();
	
	public static bool waitingResponse = false;
	public string receiveData = "";
	public string sentData = "";
	public int clientPort;
	public int netstate;
	public string LocalAddress = "127.0.0.1";
	
	private stateController statecontrol;
	private UdpClient broadcastClient;
	private IPEndPoint broadcastEndPoint;
	
	private GameObject PlayerObject;
	private int playerCount;
	public bool LANOnly = true;
	private string multiGameName;
	public GameObject RemotePlayerPrefab;
	public GameObject LocalPlayerPrefab;
	
	private float nextNetworkUpdateTime;
	private float networkUpdateIntervalMax = 0.1F;
	private Vector3 lastPlayerPosition;
	
	private string ServerAddress;
	
	
	public void SetLocalPlayerObject(GameObject p)
	{
		PlayerObject = p;
	}
	
	public void Start()
	{
		LocalAddress = GetLocalIPAddress();
		Debug.Log("Local IP Address is " + LocalAddress);
		statecontrol = gameObject.GetComponent<stateController>();
	}

	void Update () {
		
		if(waitingResponse)
		{
			Thread.Sleep(10);
		}
	
		if(Time.realtimeSinceStartup > nextNetworkUpdateTime)
		{
			nextNetworkUpdateTime = Time.realtimeSinceStartup + networkUpdateIntervalMax;
			if(PlayerObject!=null)
			{
				if(lastPlayerPosition != PlayerObject.transform.position)
				{
					lastPlayerPosition = PlayerObject.transform.position;
					GetComponent<NetworkView>().RPC("ClientUpdatePlayer",RPCMode.Server,lastPlayerPosition);
			
				}
			}
		}
	}
	
	
	public int FindPlayer(NetworkPlayer n)
	{
		int index = players.FindIndex(item => item.np==n);
		if(index < 0)
		{
			Debug.LogWarning("Unable to find player " + n.ToString());
		}
		return index;
	}
	
	public void ExitToMainMenu()
	{
		
		// un-parent the main camera
		
		Camera.main.transform.parent = null;
		
		// destroy all players and clear the players stack
		
		Debug.Log("Destroying all player objects");
		GameObject[] rps = GameObject.FindGameObjectsWithTag("Player");
    
        foreach (GameObject r in rps) {
			if(r){
				Debug.Log("Destroying " + r.name);
				Destroy(r);
			}

        }
		PlayerObject = null;
		playerCount = 0;
		
		// Important!
		// disconnect only if connected
		// this test must be done or it
		// will cause an infinite loop
		// with OnDisconnectedFromServer()		
   
	}
	
	public void StopListening()
	{
		waitingResponse = false;
		
	}
	
	public string GetLocalIPAddress()
	{
	   IPHostEntry host;
	   string LocalAddress = "";
	   host = Dns.GetHostEntry(Dns.GetHostName());
	   foreach (IPAddress ip in host.AddressList)
	   {
	     if (ip.AddressFamily == AddressFamily.InterNetwork)
	     {
	       LocalAddress = ip.ToString();
	     }
	   }
	   return LocalAddress;
	}
	
	/********************/
	/****** CLIENT ******/
	/********************/
	
	public void ListenServer()
	{
		// open a listening port on a random port
		// to receive a response back from server
		// using 0 doesn't seem to work reliably
		// so we'll just do it ourselves
		
		int myPort = UnityEngine.Random.Range(15001,16000);
		
		IPEndPoint ep1 = new IPEndPoint(IPAddress.Any, myPort);
		UdpClient uc1 = new UdpClient(ep1);
		UdpState us1 = new UdpState(); 
		us1.e = ep1; 
		us1.u = uc1;
		uc1.BeginReceive(new AsyncCallback(ListenServerCallback), us1);
		broadcastClient = uc1;
		broadcastEndPoint = ep1;
		
		Debug.Log("Broadcast listener opened on port " + broadcastEndPoint.Port.ToString());
	}
	
	public void FindServer()
	{
		
		// open a broadcast on known port 15000 and 
		// send own broadcast listener port to the LAN
		
		UdpClient uc2 = new UdpClient();
		byte [] sendBytes = BitConverter.GetBytes(broadcastEndPoint.Port);
		
		// Important!
		// this is disabled by default
		// so we have to enable it
		
		uc2.EnableBroadcast = true;
		
		IPEndPoint ep2 = new IPEndPoint(IPAddress.Broadcast, 15000);
		uc2.BeginSend(sendBytes, sendBytes.Length, ep2, new AsyncCallback(FindServerCallback), uc2);
				
		waitingResponse = true;
		
		Debug.Log("Find server message sent on broadcast listener");
	
	}
	
	public void FindServerCallback(IAsyncResult ar)
	{
		
		// broadcast has finished, endSend
		
		UdpClient uc1 = (UdpClient)ar.AsyncState;
		int bytesSent = uc1.EndSend(ar);
		
		// close the broadcast client
		
		uc1.Close();
				
	}
	
		
	public void ListenServerCallback(IAsyncResult ar)
	{
		
		// server has responded with its game name
		// send this to the stateController
				
		UdpClient uc1 = (UdpClient)((UdpState)(ar.AsyncState)).u;
		IPEndPoint ep1 = (IPEndPoint)((UdpState)(ar.AsyncState)).e;
		byte[] receiveBytes = uc1.EndReceive(ar, ref ep1);
		receiveData = Encoding.ASCII.GetString(receiveBytes);
		
		statecontrol.AddLocalServer(receiveData,ep1.Address.ToString());
		
		Debug.Log("Server responded to find server message over broadcast listener");
		
		// Important!
		// close the listening port
		// and re-open it just in
		// case another server responds
		
		uc1.Close();
		Debug.Log("Broadcast listener closed");
		
		ListenServer();
				
	}
	
	public void StopBroadcastListener()
	{
		// close the broadcast listener
		// this is needed to start a
		// new search
		
		broadcastClient.Close();
		
		Debug.Log("Broadcast listener closed");
	}

	/********************/
	/****** SERVER ******/
	/********************/
	

	public void ListenForClients(string g)
	{
		
		// open a listening port on known port 15000
		// to listen for any clients
		
		IPEndPoint ep1 = new IPEndPoint(IPAddress.Any, 15000);
		UdpClient uc1 = new UdpClient(ep1);
		UdpState us1 = new UdpState(); 
		us1.e = ep1; 
		us1.u = uc1;
		uc1.BeginReceive(new AsyncCallback(ListenForClientsCallback), us1);
		
		multiGameName = g;
		
		waitingResponse = true;
		
		Debug.Log("Server listening port opened");
		
	}
	
	public void ListenForClientsCallback(IAsyncResult ar)
	{
		// we received a broadcast from a client
		
		Debug.Log("Client message received on server listening port");
		
		UdpClient uc1 = (UdpClient)((UdpState)(ar.AsyncState)).u;
		IPEndPoint ep1 = (IPEndPoint)((UdpState)(ar.AsyncState)).e;
		byte[] receiveBytes = uc1.EndReceive(ar, ref ep1);
		clientPort = BitConverter.ToInt32(receiveBytes,0);
		
		Debug.Log("Client is listening for reply on broadcast port " + clientPort.ToString());
		
		// send a response back to the client on the port
		// they sent us
		
		sentData = multiGameName;
		byte [] sendBytes = Encoding.ASCII.GetBytes(sentData);
		
		UdpClient uc2 = new UdpClient();
		IPEndPoint ep2 = new IPEndPoint(ep1.Address, clientPort);
		uc2.BeginSend(sendBytes, sendBytes.Length, ep2, new AsyncCallback(RespondClientCallback), uc2);
		
		// Important!
		// close and re-open the broadcast listening port
		// so that another async operation can start 
		
		uc1.Close();
		Debug.Log("server listening port closed");
		ListenForClients(multiGameName);

		waitingResponse = true;

		
	}
	
	public void RespondClientCallback(IAsyncResult ar)
	{
		
		// reply to client has finished
		
		UdpClient uc1 = (UdpClient)ar.AsyncState;
		int bytesSent = uc1.EndSend(ar);
		
		// close the response port
		
		uc1.Close();
		
		waitingResponse = true;
				
	}
	
	/********************/
	/****** SHARED ******/
	/********************/
	
	public void StartServer()
	{
		if(!Network.isServer){
			
			bool useNat=false;
			if (LANOnly==true)
				useNat=false;
			else
				useNat=!Network.HavePublicAddress();
			
			Network.InitializeServer(16,25000,useNat);
		}
		
	}
	
	void OnServerInitialized() 
	{
        Debug.Log("Server initialized and ready");
    }
	
	void OnPlayerConnected(NetworkPlayer p) 
	{
		if(Network.isServer)
		{
			playerCount++;			
			NetworkViewID newViewID = Network.AllocateViewID();
			GetComponent<NetworkView>().RPC("JoinPlayer", RPCMode.All, newViewID, PlayerObject.transform.position, p);
			Debug.Log("Player " + newViewID.ToString() + " connected from " + p.ipAddress + ":" + p.port);
			Debug.Log("There are now " + playerCount + " players.");
		}
    }
	public void FakeServerJoin(Vector3 pos)
	{
		// simulate a server join for single player mode
		
		StartServer();
		NetworkViewID newPlayerView = Network.AllocateViewID();
		NetworkPlayer netPlayer = new NetworkPlayer();
		JoinPlayer(newPlayerView, pos, netPlayer);

	}

	[RPC]
	public void JoinPlayer(NetworkViewID newPlayerView, Vector3 pos, NetworkPlayer netPlayer)
	{
		
		
		if(netPlayer.ipAddress==LocalAddress)
		{
			Debug.Log("Server responded to my connection request, instantiating player " + newPlayerView.ToString());
			
			GameObject newPlayer = Instantiate(LocalPlayerPrefab, pos, Quaternion.identity) as GameObject;
			newPlayer.GetComponent<NetworkView>().viewID = newPlayerView;
			newPlayer.tag = "Player";
			newPlayer.name = "Player " + newPlayerView.ToString();

			
			newPlayer.GetComponent<playerController>().isLocalPlayer = true;
			newPlayer.GetComponent<playerController>().target = pos;
			newPlayer.GetComponent<playerController>().netPlayer = netPlayer;
			
			// populate a data structure with player information
			
			players.Add(new Player(netPlayer, newPlayer, newPlayerView));		

			// set the global PlayerObject as a convenience variable
			// to easily find the local player GameObject to send position updates
			
			PlayerObject = newPlayer;
			
			// attach the main camera
			
			Camera.main.transform.position = PlayerObject.transform.position + new Vector3(0,1F,0);
			Camera.main.transform.parent = PlayerObject.transform;
			
			// provide the playerObject to all scripts on the world object
			
			gameObject.BroadcastMessage("SetLocalPlayerObject",PlayerObject,SendMessageOptions.DontRequireReceiver);
							
			
		} else {
			
			Debug.Log("Another player connected: " + newPlayerView.ToString());
			
			GameObject newPlayer = Instantiate(RemotePlayerPrefab, pos, Quaternion.identity) as GameObject;
			newPlayer.GetComponent<NetworkView>().viewID = newPlayerView;
			newPlayer.tag = "Player";
			newPlayer.name = "Player " + newPlayerView.ToString();

			
			// because this in not the local player, deactivate the character controller
			
			newPlayer.GetComponent<CharacterController>().enabled = false;
			newPlayer.GetComponent<playerController>().isLocalPlayer = false;
			newPlayer.GetComponent<playerController>().target = pos;
			newPlayer.GetComponent<playerController>().netPlayer = netPlayer;
			
			// populate a data structure with player information
			
			players.Add(new Player(netPlayer, newPlayer, newPlayerView));		

		}
		
	}
	
	
	public void ConnectToServer(string s)
	{
    	Network.Connect(s, 25000);
		
	}
	
	void OnConnectedToServer()
	{
		
		GetComponent<NetworkView>().RPC("SendAllPlayers", RPCMode.Server);
	}
	
	[RPC]
	void SendAllPlayers(NetworkMessageInfo info)
	{
		if(Network.isServer)
		{
			Debug.Log("Received SendAllPlayers request from player " + info.sender.ToString());
			
			GameObject[] goPlayers = GameObject.FindGameObjectsWithTag("Player");
			foreach(GameObject gop in goPlayers)
			{
				NetworkPlayer gonp = gop.GetComponent<playerController>().netPlayer;
				NetworkViewID gonvid = gop.GetComponent<NetworkView>().viewID;
				
				// do no tell the requestor about themselves
				
				// we make this comparison using the
				// server-assigned index number of the 
				// player instead of the ipAddress because
				// more than one player could be playing
				// under one ipAddress -- info.sender.ToString()
				// conveniently returns this player index number
				
						
				if(gonp.ToString() != info.sender.ToString())
				{
					Debug.Log("Sent information about " + gop.name);
					GetComponent<NetworkView>().RPC("JoinPlayer", info.sender, gonvid, gop.transform.position, gonp);
				} else {
					Debug.Log("Did not send information about " + gop.name);
				}
	    	}
		}

	}
	
    void OnFailedToConnect(NetworkConnectionError error) 
	{
        Debug.Log("Could not connect to server: " + error);
		
    }

		
	[RPC]
	void ClientUpdatePlayer(Vector3 pos, NetworkMessageInfo info)
	{
		// a client is sending us a position update
		// normally you would do a lot of bounds checking here
		// but for this simple example, we'll just
		// trust the player (normally wouldn't do this)
		
		NetworkPlayer netPlayer = info.sender;
		GetComponent<NetworkView>().RPC("ServerUpdatePlayer",RPCMode.Others, netPlayer, pos);
	
		// now update it for myself the server
		
		ServerUpdatePlayer(netPlayer, pos);
		
	}
	
	[RPC]
	void ServerUpdatePlayer(NetworkPlayer netPlayer, Vector3 pos)
	{
		// the server is telling us to update a player
		// again, normally you would do a lot of bounds
		// checking here, but this is just a simple example
		
		int index = players.FindIndex(item => item.np==netPlayer);
		if(index > -1)
		{
			players[index].go.GetComponent<playerController>().target = pos;
		}
			
		
	}
	
	
	void OnPlayerDisconnected(NetworkPlayer player) 
	{
		if(Network.isServer){
			
			playerCount--;
			
			Debug.Log("Player " + player.ToString() + " disconnected.");
			Debug.Log("There are now " + playerCount + " players.");			
			GetComponent<NetworkView>().RPC("DisconnectPlayer", RPCMode.All, player);
			
		}
    }
	
	[RPC]
	void DisconnectPlayer(NetworkPlayer netPlayer)
	{
		if(Network.isClient) 
		{
			Debug.Log("Player Disconnected: " + netPlayer.ToString());
		}
		
		// now we have to do the reverse lookup from
		// the NetworkPlayer --> GameObject
		// this is easy with the List
		
		int index = players.FindIndex(item => item.np==netPlayer);
		if(index > -1)
		{
			// we check to see if the gameobject exists
			// or not first just as a safety measure
			// trying to destory a gameObject that
			// doesn't exist causes a runtime error
			
			if(players[index].go != null)
			{
				Destroy(players[index].go);
			}
						
			// we also have to remove the List entry
			
			players.RemoveAt(index);
		}
	}
	
		
	void OnDisconnectedFromServer(NetworkDisconnection info) {
		
		if(statecontrol.currentState == (int)State.loadinggame)
		{
			Debug.Log("Server Disconnect Requested from Game Load");
		} else {
			Debug.Log("Lost connection to the server");
			gameObject.BroadcastMessage("ExitToMainMenu");
		}
		
		
    }
	
}