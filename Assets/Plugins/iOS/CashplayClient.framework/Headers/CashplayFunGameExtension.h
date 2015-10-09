#import "CashplayExtension.h"
#import "CashplayFunGameClient.h"

@interface CashplayFunGameExtension : CashplayExtension

-(CashplayFunGameClient*) makeFunGameClientWithDelegate:(id<CashplayFunGameDelegate>)delegate;

@end
