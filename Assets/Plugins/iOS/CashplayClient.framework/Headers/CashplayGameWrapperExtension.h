#import "CashplayExtension.h"

@interface CashplayGameWrapperExtension : CashplayExtension

-(void)beforeGameStarted;
-(void)afterGameStarted;
-(void)beforeGameFinishedWithScore:(NSInteger)score;
-(void)afterGameFinishedWithScore:(NSInteger)score;
-(BOOL)isReadyForResultsReporting;
-(NSDictionary*)getResultExtras;

@end
