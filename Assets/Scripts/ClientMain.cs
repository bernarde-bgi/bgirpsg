using UnityEngine;
using System.Collections;

public class ClientMain : MonoBehaviour {

	
	[PunRPC]
	public void ShowRPCHand(int value){
		Debug.Log ("[PunRPC] "+ value.ToString());
		Game.instance.OpponentDrawn (value);
	}

}
