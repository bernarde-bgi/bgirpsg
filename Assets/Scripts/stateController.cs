using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public enum State
{
	explore,
	mainmenu,
	singlemenu,
	multimenu,
	ingamemenu,
	quitgame,
	waitingforplayer,
	waitingforserver,
	connectfailed,
	loadinggame
}

public class stateController: MonoBehaviour {

	public class Server
	{
		public string name { get; set; }
		public string address { get; set; }
		
		public Server(string n, string a)
		{
			name = n;
			address = a;
		}
	}

	public GUISkin skin;
	
	private List<int> stateStack = new List<int>();
	private List<Server> localServers = new List<Server>();
	private string[] displayLocalServers;
	private string[] savedGameNames;
	
	private Rect rectDialogBox;
	private Rect rectTopBanner;
	private Rect rectMenuWindow;
	private Rect rectBottom;
	private float screenW;
	private float screenH;
	private Vector2 scrollPositionLANServerSelect;
	private int selectedLANServer;
	private string gameName;	
	private string newSingleGameName = "";
	private string newMultiGameName = "";
	public int currentState;
	
	private networkController networkcontrol;
	private GameObject PlayerObject;
	
	void Start()
	{
		PushState((int)State.mainmenu);
		UpdateRectangles();
		networkcontrol = gameObject.GetComponent<networkController>();
		
		// pre-create so empty lists
		// don't cause null ref errors
		// no one likes null ref errors
		
		displayLocalServers = localServers.Select(x => x.name).ToArray();
		
	}

	public List<Server> LocalServer{
		get{
			return localServers;
		}
	}
	
	
	void Update()
	{
		
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			if(currentState==(int)State.explore) PushState((int)State.ingamemenu);
			else PopState();
				
		}

	}
	
	public void ExitToMainMenu()
	{
		Debug.Log("Clearing State Stack, setting State.mainmenu");
		ClearState();
		PushState((int)State.mainmenu);
		PlayerObject = null;
	
	}

	
	public void SetLocalPlayerObject(GameObject p)
	{
		PlayerObject = p;
	}
	
	public void AddLocalServer(string n, string a)
	{
		localServers.Add(new Server(n,a));
		displayLocalServers = localServers.Select(x => x.name).ToArray();
	}
	
	
	void OnGUI() 
	{
		/*
		GUI.skin = skin;
				
		switch (currentState) 
		{
			
		case (int)State.mainmenu:
			rectMenuWindow = GUI.Window(0,rectMenuWindow,DrawMainMenuWindow,"");
			break;
		case (int)State.singlemenu:
			rectMenuWindow = GUI.Window(0,rectMenuWindow,DrawSingleMenuWindow,"");
			break;
		case (int)State.multimenu:
			rectMenuWindow = GUI.Window(0,rectMenuWindow,DrawMultiMenuWindow,"");
			break;
		case (int)State.ingamemenu:
			rectMenuWindow = GUI.Window(0,rectMenuWindow,DrawInGameMenuWindow,"");
			break;
			
		}
		
		DrawHUD();
		*/
    }
	
	private void UpdateRectangles()	
	{
		
		screenW = (float)Screen.width;
		screenH = (float)Screen.height;
		rectDialogBox = new Rect(screenW * 0.35F, screenH * 0.25F, screenW * 0.3F, screenH * 0.25F);
		rectTopBanner = new Rect(screenW * 0.25F, 8.0F, screenW * 0.5F, screenH * 0.1F);
		rectMenuWindow = new Rect(screenW * 0.25F, screenH * 0.15F, screenW * 0.50F, screenH * 0.70F);
		rectBottom = new Rect(5,(screenH * 0.85F)-5, (screenW-10), (screenH * 0.15F)-10);
	}
	
	private void DrawHUD()
	{
		GUILayout.BeginArea(rectBottom);
		
		GUILayout.Label("You must open your game to the LAN");
		GUILayout.Label("before other people will see your game.");
		GUILayout.Label("To do this, start a single player game,");
		GUILayout.Label("hit ESC, and select Open to LAN.");
		
		GUILayout.EndArea();
	}
	
	private void DrawMainMenuWindow(int windowID)
	{
		GUI.DragWindow(new Rect(0,0,1000,20));
		
		if(GUILayout.Button("Single Player"))
		{
			// update the games data
			// and populate savedGameNames list
			
			PushState((int)State.singlemenu);
		}
		
		if(GUILayout.Button("MultiPlayer"))
		{
			

			// ask the network controller to
			// update its local LAN Game list
			
			localServers.Clear();
			
			// we must open a broadcast listener
			// before trying to find a server
			
			networkcontrol.ListenServer();
			
			// now send out a message that any number
			// of servers can respond on
			
			networkcontrol.FindServer();
			
			PushState((int)State.multimenu);
		}
		

				
		if(GUILayout.Button("Quit Game"))
		{
			Application.Quit();
		}


	}
	
	private void DrawInGameMenuWindow(int windowID)
	{
		GUI.DragWindow(new Rect(0,0,1000,20));
		if(GUILayout.Button("Return to Game"))
		{
			PopState();
		}
		
		if(GUILayout.Button("Open to LAN"))
		{
			
			networkcontrol.StartServer();
			
			// open the broadcast listener
			
			networkcontrol.ListenForClients(gameName);
			PopState();
		}
		
		if(GUILayout.Button("Return to Main Menu"))
		{
			if(Network.connections.Length > 0){
				
				// OnDisconnectedFromServer will take
				// care of the ExitToMainMenu for us
				
				Network.Disconnect();
				
			} else {
				
				gameObject.BroadcastMessage("ExitToMainMenu",SendMessageOptions.DontRequireReceiver);
				
			}

		}
		
		if(GUILayout.Button("Quit Game"))
		{
			Application.Quit();
		}
	}
	
	
	private void DrawSingleMenuWindow(int windowID)
	{
		GUI.DragWindow(new Rect(0,0,1000,20));
		
		GUILayout.BeginHorizontal();
		
		if(GUILayout.Button("Create New Game: "))
		{
			
			if(newSingleGameName != "")
			{
				gameName = newSingleGameName;
				
				// reset the game state to explore
				
				ClearState();
				networkcontrol.FakeServerJoin(new Vector3(0,2F,0));
				PushState((int)State.explore);

			}
			
		}
		
		newSingleGameName = GUILayout.TextField(newSingleGameName, 20);
		
		GUILayout.EndHorizontal();
		
		if(GUILayout.Button("Cancel"))
		{
			PopState();
		}
		
	}
	
	private void DrawMultiMenuWindow(int windowID)
	{
		
		GUILayout.BeginHorizontal();
		
		GUILayout.BeginVertical();
		
		
		scrollPositionLANServerSelect = GUILayout.BeginScrollView(scrollPositionLANServerSelect,false,true);
		
			selectedLANServer = GUILayout.SelectionGrid(selectedLANServer, displayLocalServers, 1);
		
		GUILayout.EndScrollView();
		
		if(displayLocalServers.Length > 0 )
		{
			if(GUILayout.Button("Join LAN Game"))
			{
				networkcontrol.ConnectToServer(localServers[selectedLANServer].address);
				
				// reset the game state to explore
				
				ClearState();
				PushState((int)State.explore);

				
			}
		} else {
			GUILayout.Label("Searching for LAN Games");
		}
		
		GUILayout.BeginHorizontal();
		if(GUILayout.Button("Host LAN Game:"))
		{
			if(newMultiGameName != "")
			{
				// start the Unity network server if
				// it's not already running
				
				networkcontrol.StartServer();
				
				// open the broadcast listener
				
				networkcontrol.ListenForClients(newMultiGameName);
				
				// create new game
				
				gameName = newMultiGameName;
				
				// reset the game state to explore
				
				ClearState();
				PushState((int)State.explore);

			}
		}
		newMultiGameName=GUILayout.TextField(newMultiGameName);
		GUILayout.EndHorizontal();
		if(GUILayout.Button("Refresh List"))
		{
			localServers.Clear();
			networkcontrol.StopBroadcastListener();
			networkcontrol.ListenServer();
			networkcontrol.FindServer();

		}
		
		GUILayout.EndVertical();
		

		GUILayout.EndHorizontal();
		if(GUILayout.Button("Cancel"))
		{
			// close the broadcast listener
			// so that it can be re-opened later
			
			networkcontrol.StopBroadcastListener();
			PopState();
		}

		
	}
		
	private void ClearState()
	{
		stateStack.Clear();
	}
	
	
	public void PushState(int i)
	{
		// push the requested state
		// to the top of the stack
		
		stateStack.Add(i);
		currentState = i;
		
	}
	public void PopState()
	{
		// pop the top element off
		// the stack and revert the
		// game state to the previous
		// state
		
		int count = stateStack.Count;
		if(count>1)
		{
			stateStack.RemoveAt(count-1);
			count--;
			currentState = stateStack[count-1];
		} else {
			Debug.LogError("Tried to remove a state from an empty state stack");
		}
		
	}
	public void ReplaceState(int i)
	{
		// replace the top state on the 
		// state stack with the provided
		// state - used for one-way state
		// transitions like going from
		// playing the game to winning the
		// game
		
		int count = stateStack.Count;
		if(count > 0)
		{
			stateStack.RemoveAt(count-1);
		} 
		
		PushState(i);
	}
	
	

}
