Unity Plugin for Cashplay
========
The Cashplay Unity Plugin allows Unity game code (written in Javascript, C# or Boo) to call functions from Cashplay iOS and Android SDKs. Provides a simple interface to initialize, find game, forfeit game and report game results.


Compatibility
--------

- Supports Android, iOS and Windows Phone platforms.
- Requires Unity 4.5.5 or higher.
- Requires XCode 6 or higher for iOS builds.
- Requires Visual Studio 2012 or higher for Windows Phone builds.
- Doesn't require Unity Pro license. 
- Can be used from C#, JavaScript or Boo scripts.


Adding the plugin to your Project
--------

1. Import Cashplay Unity package to your Unity project. It comes with a game example in Assets/Cashplay/Examples/SimpleGame.scene
2. From Unity editor main menu use 'Cashplay -> Setup' to open Cashplay setup tool.
3. Type your Game ID and Secret received from Cashplay developer console to the corresponding fields of the setup tool. Then press Apply button.
4. That's it. If Cashplay setup window doesn't contain any errors, Android and iOS builds will have Cashplay APIs available. For all other platforms Cashplay calls will just do nothing.

**Additional steps in Unity 5.X:**

1. In the Project window, go to "Assets/Plugins" and click on CashplayWindowsPhoneClient. Some options will appear in the Inspector window. 
2. In "Select platforms for plugin" select Editor and then click on Apply. 
3. Now go to "Assets/Plugins/WP8" and click also on CashplayWindowsPhoneClient. 
4. In the Inspector window select WP8Player inside "Select platforms for plugin" and inside "Platform settings" set the Placeholder to "Assets/Plugins/CashplayWindowsPhoneClient"


Deploying to Windows Phone
--------

If you are deploying to Windows Phone, you have to do the following additional steps:

1. Make sure you have switched to Windows Phone 8 as platform in Unity's Build Settings window.
2. Click on Build to build the Visual Studio project.
3. Open the generated project in Visual Studio.
4. In WMAppManifest.xml make sure that the following Capabilities are checked:
  - ID_CAP_IDENTITY_DEVICE
  - ID_CAP_ISV_CAMERA
  - ID_CAP_LOCATION
  - ID_CAP_NETWORKING
  - ID_CAP_PUSH_NOTIFICATION
  - ID_CAP_WEBBROWSERCOMPONENT
5. Copy the contents of your "/Assets/Plugins/WP8/WP_CASHPLAY_ASSETS" Unity's project folder to the "Assets" folder inside the Visual Studio project.
6. Now you are ready to build the project in Visual Studio and deploy it to the device.


Compatibility with other Unity3D Android plugins
--------

In case you are deploying to Android and your project integrates several Unity plugins for Android that override the default AndroidManifest.xml you have to proceed as follows:

1. Import all third-party Unity plugins for Android that you will use
2. Set up all these plugins and, in case they need to modify the AndroidManifest.xml file with their settings, merge all of them into a single AndroidManifest.xml
3. Import Cashplay plugin
4. Go to 'Cashplay -> Setup' and fill in all the fields
5. Apply

Cashplay plugin will set up everything on AndroidManifest.xml preserving the existing configuration.



Cashplay Public Interfaces
--------

```C#
namespace Cashplay
{
  enum ErrorCode
  {
    None = 0,
    UnknownError = 1,
    NetworkError = 2,
    LocationUnavailable = 3,
    LocationDenied = 4,
    ConfigError = 5,
    NotInGame = 6,
    AlreadyInGame = 7,
    CanNotProlongGame = 8,
    FunGameNotConfigured = 9,
    NotEnoughFunds = 10,
    DroppedForInactivity = 11,
  }
  
  public enum GameResult
  {
    Lose = 0,
    Win = 1,
    Draw = 2,
    Unknown = 3,
    BestGlobalScore = 4,
    BestPrivateScore = 5,
    NotBestPrivateScore = 6,
    RedirectedToCashplayUi = 7,
  }
	
  public class GameInfo
  {
    public GameResult Result { get; set; } // GameResult.Win
    public string EntryFee { get; set; } // “$0.55”
    public string WinningAmount { get; set; } // “$1”
    public int PlayersCount { get; set; } // 2
  }
	
  public class PlayerScore
  {
    public string Name { get; set; } // “John”
    public string Score { get; set; } // “Doe”
    public string Country { get; set; } // “US”
    public bool Me { get; set; } // true
  }

  static class Client
  {
    // Basic functionality
    public static event Action<string> OnGameStarted;
    public static event Action<string> OnGameFinished;
    public static event Action OnCanceled;
    public static event Action<ErrorCode> OnError;
		
    public static void Init();
    public static void FindGame();
    public static void ReportGameFinish(int score);


    // Advanced optional features
    public static void ForfeitGame();
    public static void AbandonGame();
    
    public static void PlayAgain(bool silent);
    
    public static void SetUserInfo(string firstName, string lastName, string username, string email, string dateOfBirth, string phoneNumber);

    public static void ViewLeaderboards();
    public static void Deposit();

    public static bool Verbose;
    public static void SendLog();		
  }
}
```

Methods:

- Init: Intializates Cashplay, should be invoked on the game launch.
- FindGame: Opens Cashplay and gives control to it. Cashplay will handle login/registration, tournament selection, and other Cashplay related activity, and just trigger OnGameStarted event.
- ReportGameFinish: Notifies Cashplay that game was finished and reports the player's score.
- ForfeitGame: Notifies Cashplay that player forfeits, and reports 0 score.
- AbandonGame: Notifies Cashplay that player abandons the waiting room before another player is matched.
- SetUserInfo: Sets initial data about player to pre-fill the corresponding fields in the Cashplay registration form. Optional, invoke if you have some information about user
- SendLog: Send log over email (requires additional android.permission.READ_LOGS and android.permission.WRITE_EXTERNAL_STORAGE permission for Android)

Events:

- OnGameStarted: Triggered when tournament is selected
- OnGameFinished: Triggered when ReportGameFinish, ForfeitGame or AbandonGame were successfully completed
- OnCanceled: Triggered when player selects Return back to game
- OnError: Triggered when error occurs

All public methods can be called from anywhere in your code. However it is up to you to make sure that callbacks are set before invoking any functionality. Any actions that may cause inconsistency (like trying to report game finish without finding a game first, or finding a second game without finishing first one, etc) will cause appropriate error reported via OnError event.

Verbose field controls whether plugin should print diagnostic info to the console, this information might be useful during development, but don't forget to turn it off before you go public.


Sample Async Game Sequence
--------

1. Signup to Cashplay.Client.OnGameStarted and Cashplay.Client.OnGameFinished events
2. Call Cashplay.Client.FindGame when user wants to start multiplayer game
3. Switch to the game screen when OnGameStarted event is received
4. Call Cashplay.Client.ReportGameFinish when game is finished
5. Switch away from game screen when OnGameFinished event is received

Here's a breakdown of the example game located Assets/Cashplay/Examples/SimpleGame.cs. It is a regular MonoBehaviour component with native Unity GUI, that is spinning in OnGUI function and consists of menu screen and game screen. Signup to Cashplay events is performed in its Start function. After event handlers assigned, Cashplay.Client.Init call is performed to finish Cashplay initialization and check if game was started from push notification click (in which case results screen will be opened automatically).

```C#
void Start ()
{
	Cashplay.Client.OnGameStarted += (matchId) =>
	{
		Debug.Log ("[Cashplay.SimpleGame] Game started");
		_score = 0;
		_playingTheGame = true;
	};
	
	Cashplay.Client.OnGameFinished += (matchId) =>
	{
		Debug.Log ("[Cashplay.SimpleGame] Game finished");
		_playingTheGame = false;
	};

	Cashplay.Client.OnCanceled += () =>
	{
		Debug.Log ("[Cashplay.SimpleGame] Returned");
	};
	
	Cashplay.Client.OnError += (error) =>
	{
		Debug.Log ("[Cashplay.SimpleGame] Error: " + error);
	};
	
	Cashplay.Client.OnLogIn += (userName) =>
	{
		Debug.Log ("[Cashplay.SimpleGame] User log in: " + userName);
	};

	Cashplay.Client.OnLogOut += () =>
	{
		Debug.Log ("[Cashplay.SimpleGame] User log out");
	};

	Cashplay.Client.Init();
}
```

In the menu screen user sees two buttons - 'Find Game' and 'Exit'. Here's the 'Find Game' button code:

```C#
if (GUILayout.Button("Find Game"))
{
	Cashplay.Client.FindGame();
}
```

As you can see, no game id is provided to the FindGame function, it is specified in the manifest file. If user successfully logs in and starts the game we receive Cashplay.Client.OnGameStarted event, reset the score and switch to game screen. Callback code from Start function:

```C#
Cashplay.Client.OnGameStarted += (matchId) =>
{
	Debug.Log ("[Cashplay.SimpleGame] Game started");
	_score = 0;
	_playingTheGame = true;
};
```

If user cancels the game search or some error is occured, we receive either Cashplay.Client.OnCanceled or Cashplay.Client.OnError, example just traces this events to the log:

```C#
Cashplay.Client.OnCanceled += () =>
{
	Debug.Log ("[Cashplay.SimpleGame] Returned");
};

Cashplay.Client.OnError += (error) =>
{
	Debug.Log ("[Cashplay.SimpleGame] Error: " + error);
};
```

In the game screen you can increase your score by clicking 'Add Score' button. To test forfeit and game finish actions there are corresponding buttons. Here's the 'Forfeit' button code:

```C#
if (GUILayout.Button("Forfeit"))
{
	Cashplay.Client.ForfeitGame();
}
```

and here's the 'Finish' button code, notice the argument to the function, it is the integer value with score gained by the user in the game:

```C#
if (GUILayout.Button("Finish"))
{
	Cashplay.Client.ReportGameFinish(_score);
}
```

Both of this functions will end up with invoking Cashplay.Client.OnGameFinished event. In this event handler you should hide the game screen and move to menu:

```C#
Cashplay.Client.OnGameFinished += (matchId) =>
{
	Debug.Log ("[Cashplay.SimpleGame] Game finished");
	_playingTheGame = false;
};
```


Handling notifications
--------

There's the situation when player wants to play a game but there's no opponents, in this case player is still allowed to play the game, but will only be notified about its result after a while. This notification comes in form of a remote notification and will be displayed in device notifications area, when this message is activated user will be navigated to game results screen. Normally push notifications handling code will be added automatically, so all you need to do is to prepare a push notification certificate and upload it in cashplay developer console.

Push notification certificate should be provided in a pem format. 
Here’s the guide on its creation: http://www.raywenderlich.com/32960/apple-push-notification-services-in-ios-6-tutorial-part-1

Important points:
* Your application should have explicit bundle identifier. Apps with wildcards (like com.company. *) are not ok.
* You have to create production certificate. Development certificate won’t work.
* You have to create an iOS Distribution (AdHoc) provisioning profile. Development profile won't work.
* After you added push notifications certificate and generated new provisioning profile, you should create a new game build with this updated provisioning profile.


Handling deep links
--------

In some cases you might want to have cashplay opened automatically on game start for promotion purposes. This can be achieved through deep linking by opening an URL like cashplay-[gameid]://home

In order to add deep linking support to your game you should check the “Deep Linking Enabled” checkbox in Cashplay Setup window in Unity Editor. With this option enabled, your application will start and go directly to cashplay upon invocation of an URL, similar to this: cashplay-b56d01b0dc100130fad840407ab63ff2://home, but with your actual game id.


GooglePlay support
-------- 

In order to publish your game to Google PlayStore you need two builds (two apk files), here are the requirements to this builds:

Build 1 (the one you will publish to Google PlayStore):
- Has "Platform" option set to "Google Play Market Version" in Cashplay > Setup window

Build 2 (cash related operations extension that will be uploaded to Cashplay console):
- Has "Platform" option set to "Google Play Cash Extension" in Cashplay > Setup window
- Has the ".cashextension" suffix in its bundle id. For example if your build1 is com.mycompany.mygame, then build2 have to be com.mycompany.mygame.cashextension)

Other than that, this builds should be identical.