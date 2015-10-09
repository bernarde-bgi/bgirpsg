//
//  CashplayCpp.h
//  Client
//
//  Created by Vladislav Vesely on 18/03/15.
//
//

#ifndef __Client__CashplayCpp__
#define __Client__CashplayCpp__

#include "PublicHeaders.h"

class ICashplayCppDelegate
{
public:
    // Lifecycle
    ICashplayCppDelegate()                                                                  {}
    virtual ~ICashplayCppDelegate()                                                         {}
    
    // Required
    virtual void onGameStarted(std::string sMatchID)                                        = 0;
    virtual void onGameFinished(std::string sMatchID)                                       = 0;
    
    // Optional
    virtual void onCanceled()                                                               {}
    virtual void onError(CashplayResult eResult)                                            {}
    virtual void onLogIn(std::string sUserName)                                             {}
    virtual void onLogOut()                                                                 {}
    virtual void onCustomEvent(std::string sEvent,
                               std::vector<std::pair <std::string,std::string>> rArrParams) {};
};

class CashplayCpp
{
public:
    
    // In case that RootViewController is null, [UIApplication sharedApplication].keyWindow.rootViewController will be used.
    static void initWithDelegate(ICashplayCppDelegate* pDelegate, const std::string& sGameId, const std::string& sSecret, bool bTestMode, bool bVerbose, void* pRootViewController = nullptr);
    static void findGame();
    static void reportGameFinish(int iScore, bool bSilent);
    static void reportGameFinish(int iScore, const std::string& sUserData, bool bSilent);
    static void forfeitGame();
    static void abandonGame();
    static void forceAbandonGame();
    static void timeout();
    static void viewLeaderboard();
    static void deposit();
    static void setUserInfo(const std::string& sFirstName,
                            const std::string& sLastName,
                            const std::string& sUserName,
                            const std::string& sEmail,
                            const std::string& sDateOfBirth,
                            const std::string& sPhoneNumber);
    
    static void sendLog();
    static void setAnimationEnabled(bool bEnabled);
    
};

#endif /* defined(__Client__CashplayCpp__) */
