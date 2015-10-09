//@protocol CashplayAnalyticsDelegate;
#import "Cashplay.h"

@interface CashplayExtension : NSObject

@property (nonatomic, weak) id<CashplayAnalyticsDelegate> analyticsDelegate;

@end
