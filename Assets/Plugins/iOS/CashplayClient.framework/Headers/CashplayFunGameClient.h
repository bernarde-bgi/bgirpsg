#import "Cashplay.h"

@protocol CashplayFunGameDelegate <NSObject>

-(void)funGameStartedWithMatchId:(NSString*)matchId;
-(void)funGameFinishedWithMatchId:(NSString*)matchId;

@end

@interface CashplayFunGameClient : NSObject

@property (weak, nonatomic) id<CashplayAnalyticsDelegate> analyticsDelegate;

-(BOOL)enabled;

-(void)start;

-(void)finishGameWithScore:(NSInteger)score;

-(void)forfeitGame;

-(void)abandonGame;

-(void)forceAbandonGame;

-(void)timeout;

@end
