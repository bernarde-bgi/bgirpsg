using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using System.IO;
using MiniJSON;

namespace Cashplay
{	
	public enum ErrorCode
	{
		None 					= 0,
		UnknownError 			= 1,
		NetworkError 			= 2,
		LocationUnavailable 	= 3,
		LocationDenied 			= 4,
		ConfigError 			= 5,
		NotInGame 				= 6,
		AlreadyInGame 			= 7,
		CanNotProlongGame 		= 8,
		FunGameNotConfigured 	= 9,
		NotEnoughFunds 			= 10,
		DroppedForInactivity 	= 11,
	}
	
	public enum GameResult
	{
		Lose 					= 0,
    	Win 					= 1,
    	Draw 					= 2,
	    WaitingForOpponents 	= 3,
    	BestGlobalScore 		= 4,
    	BestPrivateScore 		= 5,
    	NotBestPrivateScore 	= 6,
		RedirectedToCashplayUi 	= 7,
	}
	
	public class GameInfo
	{
		public 					GameResult Result 	{ get; set; }
		public string 			EntryFee 			{ get; set; }
		public string 			WinningAmount 		{ get; set; }
		public int 				PlayersCount 		{ get; set; }
	}
	
	public class PlayerScore
	{
		public string 			Name 				{ get; set; }
		public string 			Score 				{ get; set; }
		public string 			Country 			{ get; set; }
		public bool 			Me 					{ get; set; }
	}
	
	public static class Client
	{
		public static string Version = "1.4.6";

		#region Callbacks
		public static event Action<string, string, int> 	OnGameCreated;
		public static event Action<string> 					OnGameStarted;
		public static event Action<string> 					OnGameFinished;
		public static event Action 							OnCanceled;
		public static event Action<ErrorCode> 				OnError;
		public static event Action<string> 					OnLogIn;
		public static event Action 							OnLogOut;
		public static event Action<string, string> 			OnCustomEvent;

		private static void InvokeOnGameCreated(string matchId, string playerName, int maxPlayersCount)
		{
			if (OnGameCreated != null)
				OnGameCreated(matchId, playerName, maxPlayersCount);
		}
		
		private static void InvokeOnGameStarted(string matchId)
		{
			if (OnGameStarted != null)
				OnGameStarted(matchId);
		}
		
		private static void InvokeOnGameFinished(string matchId)
		{
			if (OnGameFinished != null)
				OnGameFinished(matchId);
		}
		
		private static void InvokeOnCanceled()
		{
			if (OnCanceled != null)
				OnCanceled();
		}

#if UNITY_WP8
		private static void InvokeOnError(CashplayWindowsPhoneClient.E_CASHPLAY_RESULT errorCode)
		{
			if (OnError != null)
				OnError ((ErrorCode)errorCode);
		}
#else
		private static void InvokeOnError(ErrorCode errorCode)
		{
			if (OnError != null)
				OnError(errorCode);
		}
#endif
		
		private static void InvokeOnLogIn(string userName)
		{
			if (OnLogIn != null)
				OnLogIn(userName);
		}

		private static void InvokeOnLogOut()
		{
			if (OnLogOut != null)
				OnLogOut();
		}

		private static void InvokeOnCustomEvent(string eventName, string jsonParams)
		{
			if (OnCustomEvent != null)
				OnCustomEvent(eventName, jsonParams);
		}
		
		private static string Obfuscate(string id)
		{
			if (id.Length < 8)
				return "****";
			return id.Substring(0, 4) + "****";
		}
		
		private static void onGameCreated(string status)
		{
			var parts = status.Split(' ');
			
			var matchId = (parts.Length > 0) ? parts[0] : "";
			var maxPlayersCount = 0;
			if (parts.Length > 1)
			{
				if (!int.TryParse(parts[1], out maxPlayersCount))
					maxPlayersCount = 0;
			}
			var playerName = (parts.Length > 1) ? status.Substring(parts[0].Length + parts[1].Length + 2) : "";
			DebugLog("onGameCreated: " + Obfuscate(matchId));
			InvokeOnGameCreated(matchId, playerName, maxPlayersCount);
		}
		
		private static void onGameStarted(string matchId)
		{
			DebugLog("onGameStarted: " + Obfuscate(matchId));
			InvokeOnGameStarted(matchId);
		}
		
		private static void onGameFinished(string matchId)
		{
			DebugLog("onGameFinished: " + Obfuscate(matchId));
			InvokeOnGameFinished(matchId);
		}
		
		private static void onCanceled(string statusString)
		{
			DebugLog("onCanceled: " + statusString);
			InvokeOnCanceled();
		}


		private static void onError(string statusString)
		{
			DebugLog("onError: " + statusString);

			#if !UNITY_WP8
				var errorCode = GetErrorCode(statusString);
				if (errorCode != ErrorCode.None)
				{
					InvokeOnError(errorCode);
					return;
				}
			#else
				var errorCode = (CashplayWindowsPhoneClient.E_CASHPLAY_RESULT)GetErrorCode(statusString);
				if (errorCode != CashplayWindowsPhoneClient.E_CASHPLAY_RESULT.ecrSuccess)
				{
					InvokeOnError(errorCode);
					return;
				}
			#endif
		}

		private static ErrorCode GetErrorCode(string status)
		{
			var result = ErrorCode.UnknownError;
			
			int statusCode = 0;
			var statusParts = status.Split(' ');
			if (statusParts.Length > 0 && int.TryParse(status, out statusCode))
			{
				try
				{
					result = (ErrorCode)statusCode;
				}
				catch(Exception ex)
				{
					DebugLog("Unknown result " + statusCode + ", error:\n" + ex);
					result = ErrorCode.UnknownError;
				}
			}
			
			return result;
		}
		
		private static void onLogIn(string userName)
		{
			DebugLog("onLogIn: " + userName);
			InvokeOnLogIn(userName);
		}

		private static void onLogOut(string statusString)
		{
			DebugLog("onLogOut: " + statusString);
			InvokeOnLogOut();
		}

		private static void onCustomEvent(string status)
		{
			var parts = status.Split(' ');			
			var eventName = (parts.Length > 0) ? parts[0] : "";
			var jsonParams = (parts.Length > 1) ? parts[1] : "";

			DebugLog("onCustomEvent: " + eventName + " " + jsonParams);
			
			InvokeOnCustomEvent(eventName, jsonParams);
		}
		#endregion
		
		#region Logging
		public static bool Verbose = true;
		
		private static void DebugLog(string message)
		{
			if (!Verbose)
				return;
			
			Debug.Log("[Cashplay.Client] " + message);
		}
		#endregion
		
#if UNITY_ANDROID
		#pragma warning disable 0649
		private static AndroidJavaObject _androidAdapterJavaObject;
		#pragma warning restore 0649
		private static GameObject _androidAdapterGameObject;
		private static CashplayAdapter _androidAdapterComponent;
		
		private static void InitAndroidAdapterIfNeeded()
		{
			if (_androidAdapterJavaObject != null ||
				_androidAdapterGameObject != null ||
				_androidAdapterComponent != null)
			{
				return;
			}
			
			_androidAdapterGameObject 					= new GameObject("CashplayAdapter");
			 GameObject.DontDestroyOnLoad(_androidAdapterGameObject);
			_androidAdapterComponent 					= _androidAdapterGameObject.AddComponent<CashplayAdapter>();
			
			_androidAdapterComponent.OnGameCreated 		+= onGameCreated;
			_androidAdapterComponent.OnGameStarted 		+= onGameStarted;
			_androidAdapterComponent.OnGameFinished 	+= onGameFinished;
			_androidAdapterComponent.OnCanceled 		+= onCanceled;
			_androidAdapterComponent.OnError 			+= onError;
			_androidAdapterComponent.OnLogIn 			+= onLogIn;
			_androidAdapterComponent.OnLogOut 			+= onLogOut;

#if !UNITY_EDITOR
			_androidAdapterJavaObject = new AndroidJavaObject("co.cashplay.android.unityadapter.UnityAdapter", _androidAdapterGameObject.name, true);
#endif
		}
		
		private static AndroidJavaObject AndroidAdapter
		{
			get
			{
				InitAndroidAdapterIfNeeded();
				return _androidAdapterJavaObject;
			}
		}
		
		private static CashplayAdapter Callbacks
		{
			get 
			{
				InitAndroidAdapterIfNeeded();
				return _androidAdapterComponent;
			}
		}

		#region Public Interface
		public static void Init()
		{
			InitAndroidAdapterIfNeeded();
		}
		
		public static void SetUserInfo(string firstName, string lastName, string username, string email, string dateOfBirth, string phoneNumber)
		{
			if (AndroidAdapter != null)
				AndroidAdapter.Call("setUserInfo", firstName, lastName, username, email, dateOfBirth, phoneNumber);
		}
		
		public static void FindGame()
		{
			DebugLog("FindGame");
			if (AndroidAdapter != null)
				AndroidAdapter.Call("findGame");
		}
		
		public static void FindGameWithCustomParams(IDictionary<object, object> parameters)
		{
			DebugLog("FindGameWithCustomParams");
		}
				
		public static void ForfeitGame()
		{
			DebugLog("ForfeitGame");
			if (AndroidAdapter != null)
				AndroidAdapter.Call("forfeitGame");
		}
		
		public static void AbandonGame()
		{
			DebugLog("AbandonGame");
			if (AndroidAdapter != null)
				AndroidAdapter.Call("abandonGame");
		}
		
		public static void ForceAbandonGame()
		{
			DebugLog("AbandonGame");
			if (AndroidAdapter != null)
				AndroidAdapter.Call("forceAbandonGame");
		}
		
		public static void ViewLeaderboard()
		{
			DebugLog("ViewLeaderboard");
			if (AndroidAdapter != null)
				AndroidAdapter.Call("viewLeaderboard");
		}
		
		public static void Deposit()
		{
			DebugLog("Deposit");
			if (AndroidAdapter != null)
				AndroidAdapter.Call("deposit");
		}
		
		public static void ReportGameFinish(int score)
		{
			DebugLog("ReportGameFinish: " + score);
			if (AndroidAdapter != null)
				AndroidAdapter.Call("reportGameFinish", score);
		}
		
		public static void ReportGameFinish(int score, bool silent)
		{
			DebugLog("ReportGameFinish: " + score + " " + silent);
			if (AndroidAdapter != null)
				AndroidAdapter.Call("reportGameFinish", score);
		}
		
		public static void TournamentJoined(string instanceId)
		{
			DebugLog("TournamentJoined");
		}
		
		public static void SendLog()
		{
			DebugLog("Sending log");
			if (AndroidAdapter != null)
				AndroidAdapter.Call("sendLog");
		}
		#endregion
		
#elif UNITY_IPHONE
#if UNITY_EDITOR
		private static void 	CashplayInit(string adapterName, bool verbose)		{ }
		private static void 	CashplaySetUserInfo(string firstName, string lastName, string username, string email, string dateOfBirth, string phoneNumber) { }
		private static void 	CashplayFindGame() 									{ }
		private static void		CashplayFindGameWithCustomParams(string parameters)	{ }
		private static void 	CashplayForfeitGame() 								{ }
		private static void 	CashplayAbandonGame() 								{ }
		private static void 	CashplayForceAbandonGame() 							{ }
		private static void 	CashplayViewLeaderboard() 							{ }
		private static void 	CashplayDeposit() 									{ }
		private static void 	CashplayReportGameFinish(int score) 				{ }
		private static void 	CashplayReportGameFinish(int score, bool silent) 	{ }
		private static void		CashplayTournamentJoined(string instanceId)			{ }
		private static void 	CashplaySendLog() 									{ }
		
		private static string 	CashplayGetGameEntryFee() 							{ return ""; }
		private static string 	CashplayGetGameWinningAmount() 						{ return ""; }
		private static int 		CashplayGetGameTournamentPlayersCount() 			{ return 0; }
		private static int 		CashplayGetGameResult() 							{ return (int)GameResult.WaitingForOpponents; }
		private static int 		CashplayGetGamePlayersCount() 						{ return 0; }
		private static string 	CashplayGetGamePlayerName(int index)			 	{ return ""; }
		private static string 	CashplayGetGamePlayerScore(int index) 				{ return ""; }
		private static string 	CashplayGetGamePlayerCountry(int index) 			{ return ""; }
		private static bool 	CashplayGetGamePlayerMe(int index) 					{ return false; }
#else
		[DllImport ("__Internal")]
		private static extern void CashplayInit(string adapterName, bool verbose);
		
		[DllImport ("__Internal")]
		private static extern void CashplaySetUserInfo(string firstName, string lastName, string username, string email, string dateOfBirth, string phoneNumber);
		
		[DllImport ("__Internal")]
		private static extern void CashplayFindGame();
		
		[DllImport ("__Internal")]
		private static extern void CashplayFindGameWithCustomParams(string parameters);

		[DllImport ("__Internal")]
		private static extern void CashplayForfeitGame();
		
		[DllImport ("__Internal")]
		private static extern void CashplayAbandonGame();
		
		[DllImport ("__Internal")]
		private static extern void CashplayForceAbandonGame();
		
		[DllImport ("__Internal")]
		private static extern void CashplayViewLeaderboard();
		
		[DllImport ("__Internal")]
		private static extern void CashplayDeposit();
		
		[DllImport ("__Internal")]
		private static extern void CashplayReportGameFinish(int score, bool silent);
		
		[DllImport ("__Internal")]
		private static extern void CashplayTournamentJoined(string instanceId);

		[DllImport ("__Internal")]
		private static extern void CashplaySendLog();

		[DllImport ("__Internal")]
		private static extern string CashplayGetGameEntryFee();

		[DllImport ("__Internal")]
		private static extern string CashplayGetGameWinningAmount();
		
		[DllImport ("__Internal")]
		private static extern int CashplayGetGameTournamentPlayersCount();
		
		[DllImport ("__Internal")]
		private static extern int CashplayGetGameResult();

		[DllImport ("__Internal")]
		private static extern int CashplayGetGamePlayersCount();

		[DllImport ("__Internal")]
		private static extern string CashplayGetGamePlayerName(int index);

		[DllImport ("__Internal")]
		private static extern string CashplayGetGamePlayerScore(int index);

		[DllImport ("__Internal")]
		private static extern string CashplayGetGamePlayerCountry(int index);

		[DllImport ("__Internal")]
		private static extern bool CashplayGetGamePlayerMe(int index);
#endif

		private static GameObject _iosAdapterGameObject;
		private static CashplayAdapter _iosAdapterComponent;

		private static void InitIosAdapterIfNeeded()
		{
			if (_iosAdapterGameObject != null ||
			    _iosAdapterComponent != null)
			{
				return;
			}
			
			_iosAdapterGameObject 				= new GameObject("CashplayAdapter");
			GameObject.DontDestroyOnLoad(_iosAdapterGameObject);
			_iosAdapterComponent 				= _iosAdapterGameObject.AddComponent<CashplayAdapter>();
			
			_iosAdapterComponent.OnGameCreated 	+= onGameCreated;
			_iosAdapterComponent.OnGameStarted 	+= onGameStarted;
			_iosAdapterComponent.OnGameFinished += onGameFinished;
			_iosAdapterComponent.OnCanceled 	+= onCanceled;
			_iosAdapterComponent.OnError 		+= onError;
			_iosAdapterComponent.OnLogIn 		+= onLogIn;
			_iosAdapterComponent.OnLogOut 		+= onLogOut;
			_iosAdapterComponent.OnCustomEvent 	+= onCustomEvent;

			CashplayInit(_iosAdapterGameObject.name, true);
		}

		#region Public Interface
		public static void Init()
		{
			InitIosAdapterIfNeeded();
		}
		
		public static void SetUserInfo(string firstName, string lastName, string username, string email, string dateOfBirth, string phoneNumber)
		{
			InitIosAdapterIfNeeded();
			DebugLog("SetUserInfo [Unity.iOS]");
			CashplaySetUserInfo(firstName, lastName, username, email, dateOfBirth, phoneNumber);
		}
		
		public static void FindGame()
		{
			InitIosAdapterIfNeeded();
			DebugLog("FindGame [Unity.iOS]");
			CashplayFindGame();
		}
		
		public static void FindGameWithCustomParams(IDictionary<object, object> parameters)
		{
			string outString = MiniJSON.Json.Serialize (parameters);
			DebugLog("FindGameWithCustomParams [Unity.iOS]:\n" + outString);
			InitIosAdapterIfNeeded();
			
			CashplayFindGameWithCustomParams(outString);
		}
		
		public static void ForfeitGame()
		{
			InitIosAdapterIfNeeded();
			DebugLog("ForfeitGame [Unity.iOS]");
			CashplayForfeitGame();
		}
		
		public static void AbandonGame()
		{
			InitIosAdapterIfNeeded();
			DebugLog("AbandonGame [Unity.iOS]");
			CashplayAbandonGame();
		}
		
		public static void ForceAbandonGame()
		{
			InitIosAdapterIfNeeded();
			DebugLog("ForceAbandonGame [Unity.iOS]");
			CashplayForceAbandonGame();
		}
		
		public static void ViewLeaderboard()
		{
			InitIosAdapterIfNeeded();
			DebugLog("ViewLeaderboard [Unity.iOS]");
			CashplayViewLeaderboard();
		}
		
		public static void Deposit()
		{
			InitIosAdapterIfNeeded();
			DebugLog("Deposit [Unity.iOS]");
			CashplayDeposit();
		}
		
		public static void ReportGameFinish(int score)
		{
			InitIosAdapterIfNeeded();
			DebugLog("ReportGameFinish [Unity.iOS]: " + score);
			CashplayReportGameFinish(score, false);
		}
		
		public static void ReportGameFinish(int score, bool silent)
		{
			InitIosAdapterIfNeeded();
			DebugLog("ReportGameFinish [Unity.iOS]: " + score + " " + silent);
			CashplayReportGameFinish(score, silent);
		}
		
		public static void TournamentJoined(string instanceId)
		{
			InitIosAdapterIfNeeded();
			DebugLog("TournamentJoined [Unity.iOS]: " + instanceId);
			CashplayTournamentJoined(instanceId);
		}
		
		public static void SendLog()
		{
			InitIosAdapterIfNeeded();
			DebugLog("SendLog [Unity.iOS]");
			CashplaySendLog();
		}
		#endregion
#elif UNITY_WP8
		#pragma warning disable 0649
		private static CashplayWindowsPhoneClient.Cashplay _wpObject;
		private static string gameId_WP = "bb75e9c04e110133138b723c91847147";
		private static string gameSecret_WP = "";
		private static bool testMode_WP = true;
		private static bool logEnabled_WP = true;
		#pragma warning restore 0649

		private static void InitWPAdapterIfNeeded()
		{
			if (_wpObject != null)
				return;

			_wpObject 							 = new CashplayWindowsPhoneClient.Cashplay();
			_wpObject.CashplayOnCancel 			+= InvokeOnCanceled;
			_wpObject.CashplayOnError 			+= InvokeOnError;
			_wpObject.CashplayOnGameFinished 	+= InvokeOnGameFinished;
			_wpObject.CashplayOnGameStarted 	+= InvokeOnGameStarted;
			_wpObject.CashplayOnLogIn 			+= InvokeOnLogIn;
			_wpObject.CashplayOnLogOut 			+= InvokeOnLogOut;

			_wpObject.InitSDK(gameId_WP, gameSecret_WP, testMode_WP, logEnabled_WP);
		}

		private static CashplayWindowsPhoneClient.Cashplay WPAdapter
		{
			get
			{
				InitWPAdapterIfNeeded();
				return _wpObject;
			}
		}		

		#region Public Interface
		public static void Init()
		{
			InitWPAdapterIfNeeded();
		}
		
		public static void SetUserInfo(string firstName, string lastName, string username, string email, string dateOfBirth, string phoneNumber)
		{
			DebugLog("SetUserInfo [Unity.WP]");
			if (WPAdapter != null)
				WPAdapter.SetUserInfoWith (firstName, lastName, username, email, dateOfBirth, phoneNumber);
		}
		
		public static void FindGame()
		{
			DebugLog("FindGame [Unity.WP]");
			if (WPAdapter != null)
				WPAdapter.FindGame ();
		}
		
		public static void FindGameWithCustomParams(IDictionary<object, object> parameters)
		{
			DebugLog("FindGameWithCustomParams [Unity.WP]");
		}
		
		public static void ForfeitGame()
		{
			DebugLog("ForfeitGame [Unity.WP]");
			if (WPAdapter != null)
				WPAdapter.ForfeitGame ();
		}
		
		public static void AbandonGame()
		{
			DebugLog("AbandonGame [Unity.WP]");
			if (WPAdapter != null)
				WPAdapter.AbandonGame ();
		}
		
		public static void ViewLeaderboard()
		{
			DebugLog("ViewLeaderboard [Unity.WP]");
			if (WPAdapter != null)
				WPAdapter.ViewLeaderboard ();
		}
		
		public static void Deposit()
		{
			DebugLog("Deposit [Unity.WP]");
			if (WPAdapter != null)
				WPAdapter.Deposit ();
		}
		
		public static void ReportGameFinish(int score)
		{
			DebugLog("ReportGameFinish [Unity.WP]: " + score);
			if (WPAdapter != null)
				WPAdapter.ReportGameFinish (score);
		}
		
		public static void ReportGameFinish(int score, bool silent)
		{
			DebugLog("ReportGameFinish [Unity.WP]: " + score + " " + silent);
			if (WPAdapter != null)
				WPAdapter.ReportGameFinish (score);
		}
		
		public static void TournamentJoined(string instanceId)
		{
			DebugLog("TournamentJoined [Unity.WP]");
		}
		
		public static void SendLog()
		{
			DebugLog("SendLog [Unity.WP]");
			if (WPAdapter != null)
				WPAdapter.SendLog ();
		}
		#endregion
#else
		#region Public Interface
		public static void Init()
		{
			DebugLog("Cashplay is not supported on this platform");
		}
		
		public static void SetUserInfo(string firstName, string lastName, string username, string email, string dateOfBirth, string phoneNumber)
		{
			DebugLog("Cashplay is not supported on this platform");
		}
		
		public static void FindGame()
		{
			DebugLog("Cashplay is not supported on this platform");
		}
				
		public static void ForfeitGame()
		{
			DebugLog("Cashplay is not supported on this platform");
		}
		
		public static void AbandonGame()
		{
			DebugLog("Cashplay is not supported on this platform");
		}
		
		public static void ForceAbandonGame()
		{
			DebugLog("Cashplay is not supported on this platform");
		}
		
		public static void ViewLeaderboard()
		{
			DebugLog("Cashplay is not supported on this platform");
		}
		
		public static void Deposit()
		{
			DebugLog("Cashplay is not supported on this platform");
		}
		
		public static void ReportGameFinish(int score)
		{
			DebugLog("Cashplay is not supported on this platform");
		}
		
		public static void ReportGameFinish(int score, bool silent)
		{
			DebugLog("Cashplay is not supported on this platform");
		}
		
		public static void SendLog()
		{
			DebugLog("Cashplay is not supported on this platform");
		}
		#endregion
#endif
	}
}
