#pragma once
#ifndef _MARSCONST_
#define _MARSCONST_

#include "windows.h"

//#define  

///< 枚举窗口参数
typedef struct
{
	HWND    hwndWindow;     // 窗口句柄
	DWORD   dwProcessID;    // 进程ID
}EnumWindowsArg;


#endif