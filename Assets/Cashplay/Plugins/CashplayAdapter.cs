using UnityEngine;
using System;

public class CashplayAdapter : MonoBehaviour
{
	public event Action<string> OnGameCreated;
	public void onGameCreated(string statusString)
	{
		if (OnGameCreated != null)
			OnGameCreated(statusString);
	}
	
	public event Action<string> OnGameStarted;
	public void onGameStarted(string statusString)
	{
		if (OnGameStarted != null)
			OnGameStarted(statusString);
	}
	
	public event Action<string> OnGameFinished;
	public void onGameFinished(string statusString)
	{
		if (OnGameFinished != null)
			OnGameFinished(statusString);
	}
	
	public event Action<string> OnCanceled;
	public void onCanceled(string statusString)
	{
		if (OnCanceled != null)
			OnCanceled(statusString);
	}
	
	public event Action<string> OnError;
	public void onError(string statusString)
	{
		if (OnError != null)
			OnError(statusString);
	}
	
	public event Action<string> OnLogIn;
	public void onLogIn(string statusString)
	{
		if (OnLogIn != null)
			OnLogIn(statusString);
	}

	public event Action<string> OnLogOut;
	public void onLogOut(string statusString)
	{
		if (OnLogOut != null)
			OnLogOut(statusString);
	}

	public event Action<string> OnCustomEvent;
	public void onCustomEvent(string statusString)
	{
		if (OnCustomEvent != null)
			OnCustomEvent(statusString);
	}
}
