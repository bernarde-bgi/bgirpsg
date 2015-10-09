#import "CashplayUnityAdapter.h"
#import "CashplayConfig.h"
#import <CoreLocation/CoreLocation.h>

extern UIViewController *UnityGetGLViewController();

@implementation CashplayUnityAdapter
{
    Cashplay* 			_cashplay;
    NSString* 			_callbackObjectName;
    CashplayGameResult 	_gameResult;
    NSString* 			_gameEntryFee;
    NSString* 			_gameWinningAmount;
    int 				_gamePlayersCount;
    NSArray* 			_gamePlayersScore;
    int 				_gameLocalPlayerIndex;
}

-(CashplayGameResult)gameResult
{
    return _gameResult;
}

-(NSString *)gameEntryFee
{
    return _gameEntryFee;
}

-(NSString *)gameWinningAmount
{
    return _gameWinningAmount;
}

-(int)gamePlayersCount
{
    return _gamePlayersCount;
}

-(NSArray *)gamePlayersScore
{
    return _gamePlayersScore;
}

-(int)gameLocalPlayerIndex
{
    return _gameLocalPlayerIndex;
}


-(id)initWithCallbackObjectName:(NSString*)callbackObjectName verbose:(BOOL)verbose
{
    if (self = [super init])
    {
#if !CASHPLAY_LOG_ENABLED
        verbose = NO;
#endif

		_callbackObjectName = [[NSString alloc] initWithFormat:@"%@", callbackObjectName];
        UIViewController * viewController = UnityGetGLViewController();
        
        NSMutableArray * extensions = [[NSMutableArray alloc] init];
        _cashplay = [[Cashplay alloc]initWithDelegate:self
                                                 host:viewController
                                               gameId:[NSString stringWithFormat:@"%s", CASHPLAY_GAME_ID]
                                               secret:[NSString stringWithFormat:@"%s", CASHPLAY_SECRET]
                                             testMode:CASHPLAY_TEST_MODE
                                              verbose:verbose
                                           extensions:extensions];
    }
    return self;
}

-(void)setUserInfoWithFirstName:(NSString*)firstName
			lastName:(NSString*)lastName
			username:(NSString*)username
			email:(NSString*)email
			dateOfBirth:(NSString*)dateOfBirth
			phoneNumber:(NSString*)phoneNumber
{
    [_cashplay setHost:UnityGetGLViewController()];
    [_cashplay setUserInfoWithFirstName: [NSString stringWithFormat:@"%@", firstName] 
				lastName:[NSString stringWithFormat:@"%@", lastName] 
				username:[NSString stringWithFormat:@"%@", username] 
				email:[NSString stringWithFormat:@"%@", email] 
				dateOfBirth:[NSString stringWithFormat:@"%@", dateOfBirth] 
				phoneNumber:[NSString stringWithFormat:@"%@", phoneNumber] ];
}

-(void)findGame
{
    [_cashplay setHost:UnityGetGLViewController()];
    [_cashplay findGame];
}

-(void)findGameWithCustomParams:(NSString*)params
{
	[_cashplay setHost:UnityGetGLViewController()];
	[_cashplay findGameWithCustomParams:[NSString stringWithFormat:@"%@", params] ];
}

-(void)forfeitGame
{
    [_cashplay setHost:UnityGetGLViewController()];
    [_cashplay forfeitGame];
}

-(void)abandonGame
{
    [_cashplay abandonGame];
}

-(void)forceAbandonGame
{
    [_cashplay forceAbandonGame];
}

-(void)reportGameFinish:(int)score silent:(BOOL)silent
{
    [_cashplay setHost:UnityGetGLViewController()];
    [_cashplay reportGameFinish:score silent:silent];
}

-(void)viewLeaderboard
{
    [_cashplay setHost:UnityGetGLViewController()];
    [_cashplay viewLeaderboard];
}

-(void)deposit
{
    [_cashplay setHost:UnityGetGLViewController()];
    [_cashplay deposit];
}

-(void)tournamentJoined:(NSString*)instanceId
{
	[_cashplay setHost:UnityGetGLViewController()];
	[_cashplay tournamentJoined:instanceId];
}

-(void)sendLog
{
    [_cashplay setHost:UnityGetGLViewController()];
    [_cashplay sendLog];
}

-(void)onGameStarted:(NSString*)matchId
{
    UnitySendMessage([_callbackObjectName UTF8String], "onGameStarted", [matchId UTF8String]);
}

-(void)onGameFinished:(NSString*)matchId
{
    UnitySendMessage([_callbackObjectName UTF8String], "onGameFinished", [matchId UTF8String]);
}

-(void)onGameCreated:(NSString*)matchId
          playerName:(NSString*)playerName
     maxPlayersCount:(int)maxPlayersCount
{
    UnitySendMessage([_callbackObjectName UTF8String], "onGameCreated", [[NSString stringWithFormat:@"%@ %i %@", matchId, maxPlayersCount, playerName] UTF8String]);
}

-(void)onCanceled
{
    UnitySendMessage([_callbackObjectName UTF8String], "onCanceled", "0");
}

-(void)onError:(CashplayResult)errorCode
{
    NSString * errorString = [NSString stringWithFormat:@"%i", (int)errorCode];
    UnitySendMessage([_callbackObjectName UTF8String], "onError", [errorString UTF8String]);
}

-(void)onLogIn:(NSString*)userName
{
    UnitySendMessage([_callbackObjectName UTF8String], "onLogIn", [[NSString stringWithFormat:@"%@", userName] UTF8String]);
}

-(void)onLogOut
{
    UnitySendMessage([_callbackObjectName UTF8String], "onLogOut", "");
}

-(void)onCustomEvent:(NSString*)eventName 
	   andParams:(NSDictionary*)params
{
	NSError *error; 
	NSData *jsonData = [NSJSONSerialization dataWithJSONObject:params 
                                                   	   options:0
                                                         error:&error];

    NSString *jsonString = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
	
    UnitySendMessage([_callbackObjectName UTF8String], "onCustomEvent", [[NSString stringWithFormat:@"%@ %@", eventName, jsonString] UTF8String]);
}

@end

static CashplayUnityAdapter * _instance = nil;

extern "C"
{
    const char * CpOutString(NSString * string)
    {
        if (!string)
            return NULL;
        
        char * res = (char*)malloc(strlen(string.UTF8String) + 1);
        strcpy(res, string.UTF8String);
        return res;
    }

    void CashplayInit(char * callbackObjectName, int verbose)
    {
        NSString * callback = [NSString stringWithFormat:@"%s", callbackObjectName];
        _instance = [[CashplayUnityAdapter alloc] initWithCallbackObjectName:callback verbose:verbose];
    }

    void CashplaySetUserInfo(char * firstName, char * lastName, char * username, char * email, char * dateOfBirth, char * phoneNumber)
    {
	if (_instance == nil)
            return;

        [_instance setUserInfoWithFirstName:[NSString stringWithFormat:@"%s", firstName]
				lastName:[NSString stringWithFormat:@"%s", lastName]
				username:[NSString stringWithFormat:@"%s", username]
				email:[NSString stringWithFormat:@"%s", email]
				dateOfBirth:[NSString stringWithFormat:@"%s", dateOfBirth]
				phoneNumber:[NSString stringWithFormat:@"%s", phoneNumber]
		];
    }
    
    void CashplayFindGame()
    {
        if (_instance == nil)
            return;
        
        [_instance findGame];
    }
	
	void CashplayFindGameWithCustomParams(char * params)
	{
		if (_instance == nil)
			return;
		
		[_instance findGameWithCustomParams:[NSString stringWithFormat:@"%s", params]];
	}
    
    void CashplayForfeitGame()
    {
        if (_instance == nil)
            return;
        
        [_instance forfeitGame];
    }
	
	void CashplayAbandonGame()
	{
		if (_instance == nil)
			return;
			
		[_instance abandonGame];
	}
	
	void CashplayForceAbandonGame()
	{
		if (_instance == nil)
			return;
			
		[_instance forceAbandonGame];
	}
    
    void CashplayReportGameFinish(int score)
    {
        if (_instance == nil)
            return;
        
        [_instance reportGameFinish:score silent:NO];
    }
    
    void CashplayViewLeaderboard()
    {
        if (_instance == nil)
            return;
        
        [_instance viewLeaderboard];
    }
    
    void CashplayDeposit()
    {
        if (_instance == nil)
            return;
        
        [_instance deposit];
    }
	
	void CashplayTournamentJoined(char * instanceId)
	{
		if (_instance == nil)
			return;
			
		[_instance tournamentJoined:[NSString stringWithFormat:@"%s", instanceId]];
	}
    
    void CashplaySendLog()
    {
        if (_instance == nil)
            return;
        
        [_instance sendLog];
    }
    
    int CashplayGetGameResult()
    {
        if (_instance == nil)
            return (int)CashplayGameResult_WaitingForOpponents;
        
        return _instance.gameResult;
    }

    const char * CashplayGetGameEntryFee()
    {
        if (_instance == nil)
            return "";
        
        return CpOutString([_instance gameEntryFee]);
    }
    
    const char * CashplayGetGameWinningAmount()
    {
        if (_instance == nil)
            return "";
        
        return CpOutString([_instance gameWinningAmount]);
    }
    
    int CashplayGetGameTournamentPlayersCount()
    {
        if (_instance == nil)
            return 0;
        
        return _instance.gamePlayersCount;
    }
    
    int CashplayGetGamePlayersCount()
    {
        if (_instance == nil)
            return 0;
        
        return _instance.gamePlayersScore.count;
    }
    
    const char * CashplayGetGamePlayerName(int index)
    {
        if (_instance == nil)
            return "";
        
        return CpOutString([_instance gamePlayersScore][index][0]);
    }
    
    const char * CashplayGetGamePlayerScore(int index)
    {
        if (_instance == nil)
            return "";
        
        return CpOutString([_instance gamePlayersScore][index][1]);
    }
    
    const char * CashplayGetGamePlayerCountry(int index)
    {
        if (_instance == nil)
            return "";
        
        return CpOutString([_instance gamePlayersScore][index][2]);
    }

    BOOL CashplayGetGamePlayerMe(int index)
    {
        if (_instance == nil)
            return NO;
        
        return (_instance.gameLocalPlayerIndex == index);
    }
}
