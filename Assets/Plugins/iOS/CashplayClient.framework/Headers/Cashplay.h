#import <CoreLocation/CoreLocation.h>
#import <MessageUI/MessageUI.h>
#import <AdSupport/AdSupport.h>
#import "PublicHeaders.h"

@protocol CashplayDelegate <NSObject>

-(void)onGameStarted:(NSString*)matchId;
-(void)onGameFinished:(NSString*)matchId;

@optional
-(void)onCanceled;
-(void)onError:(CashplayResult) errorCode;
-(void)onLogIn:(NSString*)userName;
-(void)onLogOut;
-(void)onCustomEvent:(NSString*)eventName andParams:(NSDictionary*)params;
@end

@protocol CashplayAnalyticsDelegate <NSObject>

- (void)logEvent:(NSString*)event;
- (void)logEvent:(NSString*)event withParameters:(NSDictionary*)parameters;

@end

@interface Cashplay : NSObject

+(NSString*)version;

+(void)application:(UIApplication *)application didFinishLaunchingWithOptions:(NSDictionary*)launchOptions;

+(void)application:(UIApplication*)application didRegisterForRemoteNotificationsWithDeviceToken:(NSData *)deviceToken;

+(void)application:(UIApplication*)application didReceiveRemoteNotification:(NSDictionary *)userInfo;

+(void)application:(UIApplication*)application didReceiveLocalNotification:(UILocalNotification *)notification;

+(BOOL)application:(UIApplication *)application
           openURL:(NSURL *)url
 sourceApplication:(NSString *)sourceApplication
        annotation:(id)annotation;

-(void)setHost:(UIViewController *)host;

-(id)initWithDelegate:(id<CashplayDelegate>)delegate
                 host:(UIViewController *)host
               gameId:(NSString *)gameId
               secret:(NSString*)secret
             testMode:(BOOL)testMode
              verbose:(BOOL)verbose
           extensions:(NSArray *)extensions;

-(void)findGame;

-(void)findGameWithCustomParams:(NSString*)params;

-(void)reportGameFinish:(NSInteger) score silent:(BOOL)silent;

-(void)reportGameFinish:(NSInteger) score withUserData:(NSString*)userData silent:(BOOL)silent;

-(void)forfeitGame;

-(void)abandonGame;

-(void)forceAbandonGame;

-(void)timeout;

-(void)viewLeaderboard;

-(void)deposit;

-(void)setUserInfoWithFirstName:(NSString*)firstName
                       lastName:(NSString*)lastName
                       username:(NSString*)username
                          email:(NSString*)email
                    dateOfBirth:(NSString*)dateOfBirth
                    phoneNumber:(NSString*)phoneNumber;

-(void)sendLog;

-(void)setAnimationEnabled:(BOOL)enabled;

-(void)tournamentJoined:(NSString*)instanceID;

@end
