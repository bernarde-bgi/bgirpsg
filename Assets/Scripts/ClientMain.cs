using UnityEngine;
using System.Collections;

public class ClientMain : MonoBehaviour {

	
	[RPC]
	public void ShowRPCHand(int value){
		Debug.Log ("[RPC] "+ value.ToString());
		Game.instance.OpponentDrawn (value);
	}

}
