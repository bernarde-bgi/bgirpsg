//
//  PublicHeaders.h
//  Client
//
//  Created by Vladislav Vesely on 22/04/15.
//  Copyright (c) 2015 BuddyBet. All rights reserved.
//

#ifndef Client_PublicHeaders_h
#define Client_PublicHeaders_h

typedef enum
{
    CashplayResult_Success                      = 0,
    CashplayResult_UnknownError                 = 1,
    CashplayResult_NetworkError                 = 2,
    CashplayResult_LocationUnavailable          = 3,
    CashplayResult_LocationDenied               = 4,
    CashplayResult_ConfigError                  = 5,
    CashplayResult_NotInGame                    = 6,
    CashplayResult_AlreadyInGame                = 7,
    CashplayResult_FunGameNotConfigured         = 9,
    CashplayResult_NotEnoughFunds               = 10,
    CashplayResult_DroppedForInactivity         = 11,
} CashplayResult;

typedef enum
{
    CashplayGameResult_Lose                     = 0,
    CashplayGameResult_Win                      = 1,
    CashplayGameResult_Draw                     = 2,
    CashplayGameResult_WaitingForOpponents      = 3,
    CashplayGameResult_BestGlobalScore          = 4,
    CashplayGameResult_BestPrivateScore         = 5,
    CashplayGameResult_NotBestPrivateScore      = 6,
    CashplayGameResult_RedirectedToCashplayUi   = 7,
} CashplayGameResult;


// Warm up notifications
static NSString * const C_CASHPLAY_WARM_UP_STARTED  = @"CPWarmUpStarted";
static NSString * const C_CASHPLAY_WARM_UP_SUCCESS  = @"CPWarmUpFinishedSuccess";
static NSString * const C_CASHPLAY_WARM_UP_FAILED   = @"CPWarmUpFinishedFailed";

#endif
