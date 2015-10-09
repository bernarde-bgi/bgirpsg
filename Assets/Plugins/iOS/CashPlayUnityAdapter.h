#import "CashplayConfig.h"
#import <CashplayClient/Cashplay.h>

#import <CoreLocation/CoreLocation.h>
#import <AdSupport/AdSupport.h>

@interface CashplayUnityAdapter : NSObject<CashplayDelegate>

-(id)initWithCallbackObjectName:(NSString*)callbackObjectName verbose:(BOOL)verbose;
-(void)findGame;
-(void)findGameWithCustomParams:(NSString*)params;
-(void)forfeitGame;
-(void)abandonGame;
-(void)forceAbandonGame;
-(void)reportGameFinish:(int)score silent:(BOOL)silent;
-(void)sendLog;
-(void)viewLeaderboard;
-(void)deposit;
-(void)tournamentJoined:(NSString*)instanceId;

-(CashplayGameResult)gameResult;
-(NSString *)gameEntryFee;
-(NSString *)gameWinningAmount;
-(NSArray *)gamePlayersScore;
-(int)gamePlayersCount;
-(int)gameLocalPlayerIndex;

@end
