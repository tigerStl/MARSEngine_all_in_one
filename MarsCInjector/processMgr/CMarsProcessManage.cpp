#include "stdafx.h"
#include "windows.h"
#include <cstring>
#include <stdlib.h>
#include <iostream>
#include "Shlwapi.h"
#include <psapi.h>

#include "..\\marsConst\\MarsConst.h"

#include "CMarsProcessManage.h"
//using namespace std;

CMarsProcessManage::CMarsProcessManage(char* pstrProcessName)
{
	m_strCurrentProcessName = std::string(pstrProcessName);
	m_strInjectDll = "";
	GetModuleFileNameA(NULL, m_arrCurrentDirectory, sizeof(m_arrCurrentDirectory));
}


CMarsProcessManage::~CMarsProcessManage()
{
}

bool CMarsProcessManage::m_enumProcess()
{	
	
	DWORD aProcesses[2048], cbNeeded, cProcesses;
	unsigned int i;
	m_dwTargetProcessId = 0;
	
	if (!::EnumProcesses(aProcesses, sizeof(aProcesses), &cbNeeded))
	{
		return false;
	}
	
	cProcesses = cbNeeded / sizeof(DWORD);
	for (i = 0; i < cProcesses; i++)
	{
		if (aProcesses[i] != 0)
		{
			std::string strProcessName = m_getProcessName(aProcesses[i]);			
			if (!_stricmp(strProcessName.c_str(), m_strCurrentProcessName.c_str()))
			{
				//找到指定进程
				m_dwTargetProcessId = aProcesses[i];
				return true;
			}
		}
	}
	
	return false;
}

std::string  CMarsProcessManage::m_getCurrentProcessName()
{
	return this->m_strCurrentProcessName;
}

std::string  CMarsProcessManage::m_getProcessName(DWORD pid)
{
	TCHAR szProcessName[MAX_PATH] = TEXT("<unknown>");

	// Get a handle to the process.

	HANDLE hProcess = OpenProcess(PROCESS_QUERY_INFORMATION |
		PROCESS_VM_READ,
		FALSE, pid);

	// Get the process name.
	if (NULL != hProcess)
	{
		HMODULE hMod;
		DWORD cbNeeded;
		if (::EnumProcessModules(hProcess, &hMod, sizeof(hMod),
			&cbNeeded))
		{
			::GetModuleBaseName(hProcess, hMod, szProcessName,
				sizeof(szProcessName) / sizeof(TCHAR));
			::CloseHandle(hProcess);
			std::cout << szProcessName << " PID: " << pid << std::endl;
			return std::string(szProcessName);
		}
	}

	// Print the process name and identifier.
	
	//std::cout << szProcessName << " PID: " << pid << std::endl;
	//_tprintf(TEXT("%s  (PID: %u)\n"), szProcessName, processID);

	// Release the handle to the process.
	::CloseHandle(hProcess);
	return "";
}

static HHOOK _messageHookHandle;
static unsigned int WM_GOBABYGO = ::RegisterWindowMessage("Injector_GOBABYGO!");
typedef void (* FUNC_MARS_STARTTHREAD)();
__declspec(dllexport)

LRESULT _stdcall MessageHookProc(int nCode, WPARAM wparam, LPARAM lparam)
{
	//::MessageBox(0, "ddd", "dddd", 0);
	OFSTRUCT logFileInfo;
	char logFileName[] = "c:\\temp\\marsinjector.txt";
	char username[128];
	long unsigned int bufsize = 128;
	::GetUserNameA(username, &bufsize);
	char* logfm = new char[strlen(logFileName) + bufsize + 1];
	strcpy_s(logfm,sizeof logfm, logFileName);
	strcat_s(logfm, sizeof logfm, username);

	//GetFileAttributes(logFileName); // from winbase.h
	//if (INVALID_FILE_ATTRIBUTES == GetFileAttributes(logFileName) && GetLastError() == ERROR_FILE_NOT_FOUND)
	//{
	//	//File not found
	//	
	//}
	/*HANDLE f = ::CreateFile(logFileName, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ, NULL,
		CREATE_NEW | OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
		*/
	HANDLE f = ::CreateFile(logfm, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ|FILE_SHARE_WRITE, NULL,
		CREATE_NEW | OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
	::SetFilePointer(f, NULL, NULL, FILE_END);		
	char logInfo[1024] = "Begin Message Proc";
	CWPSTRUCT* msg = (CWPSTRUCT*)lparam;
	if (nCode != HC_ACTION)
	{
		sprintf_s(logFileName, "action is:%d, MessageIs:", HC_ACTION, msg->message);
		::CloseHandle(f);
		return CallNextHookEx(_messageHookHandle, nCode, wparam, lparam);
	}
	
	if (msg == NULL) return CallNextHookEx(_messageHookHandle, nCode, wparam, lparam);
	if (msg->message != WM_GOBABYGO) return CallNextHookEx(_messageHookHandle, nCode, wparam, lparam);


	::WriteFile(f, logInfo, strlen(logInfo), NULL, NULL);
	::strcpy_s(logInfo, "before load dll");
	DWORD iLastError;
	try
	{
		///mars的消息处理
		char* pFiles = (char*)msg->wParam; //需要加载的加载的动态链接库
		if (!PathFileExists(pFiles))
		{
			sprintf_s(logInfo, "can't find file:%s", pFiles);
			::memset(logInfo, 0, sizeof(logInfo));
			::WriteFile(f, logInfo,strlen(logInfo), NULL, NULL);
			return  CallNextHookEx(_messageHookHandle, nCode, wparam, lparam);
		}

		//HMODULE injectorDllHdl = ::LoadLibrary("C:\\automationTest\\Automation Workbooks\\dlls\\QTInjectorDll.dll");
		HMODULE injectorDllHdl = ::LoadLibrary(pFiles);
		if (!injectorDllHdl)
		{
			iLastError = ::GetLastError();
			::memset(logInfo, 0, sizeof(logInfo));
			sprintf_s(logInfo, "LoadLibrary last Error is:%d", iLastError);
			//::strcpy_s(logInfo, "LoadLibrary last Error is:");
			::WriteFile(f, logInfo, strlen(logInfo), NULL, NULL);
			//char arrData[20];
			//::strcpy_s(logInfo, _itoa(iLastError,arrData, 10));
			//::WriteFile(f, logInfo, strlen(logInfo), NULL, NULL);
			return CallNextHookEx(_messageHookHandle, nCode, wparam, lparam);
		}
		FUNC_MARS_STARTTHREAD strtThread = (FUNC_MARS_STARTTHREAD)::GetProcAddress(injectorDllHdl, "InitQT");
		if (!strtThread)
		{
			iLastError = ::GetLastError();
			::memset(logInfo, 0, sizeof(logInfo));
			sprintf_s(logInfo, "GetProcAddress last Error is:%d", iLastError);
			::WriteFile(f, logInfo, ::strlen(logInfo), NULL, NULL);
			//::strcpy_s(logInfo, "GetProcAddress last Error is:");
			//char arrData[20];
			//::strcpy_s(logInfo, _itoa(iLastError, arrData, 10));
			return CallNextHookEx(_messageHookHandle, nCode, wparam, lparam);
		}
		::memset(logInfo, 0, sizeof(logInfo));
		::strcpy_s(logInfo, "before call InitQT");
		::WriteFile(f, logInfo, ::strlen(logInfo), NULL, NULL);
		strtThread();
		::strcpy_s(logInfo, "InitQT end");
		::WriteFile(f, logInfo, ::strlen(logInfo), NULL, NULL);

		return CallNextHookEx(_messageHookHandle, nCode, wparam, lparam);
	}
	catch (const std::exception& e)
	{
		::strcpy_s(logInfo, "Error:");
		::strcat_s(logInfo, e.what());
		::WriteFile(f, logInfo, ::strlen(logInfo), NULL, NULL);

		return CallNextHookEx(_messageHookHandle, nCode, wparam, lparam);
	}

}

BOOL CALLBACK EnumWindowsProc(HWND hwnd, LPARAM lParam)
{
	EnumWindowsArg *pArg = (EnumWindowsArg *)lParam;
	DWORD  dwProcessID = 0;
	// 通过窗口句柄取得进程ID
	::GetWindowThreadProcessId(hwnd, &dwProcessID);
	if (dwProcessID == pArg->dwProcessID)
	{
		pArg->hwndWindow = hwnd;
		// 找到了返回TRUE
		return FALSE;
	}
	// 没找到，继续找，返回TRUE
	return TRUE;
}

bool CMarsProcessManage::m_injectDllToPid()
{
	//cout << "Inject into " << m_strCurrentProcessName << " begin..." << endl;
	std::string strError = "OK";
	bool isOk = false;
	try
	{
		EnumWindowsArg strctArg;
		strctArg.dwProcessID = m_dwTargetProcessId;
		strctArg.hwndWindow = 0;
		//找到主窗口
		if (!::EnumWindows((WNDENUMPROC)EnumWindowsProc, (LPARAM)(&strctArg)))
		{   ///找到主窗口的handle
			if (!strctArg.hwndWindow)
			{
				//cout << "Can't find such process :" << m_dwTargetProcessId << " Main window handle!" << endl;
				return false;
			}
		}
		else
		{
			//cout << "Can't find such process :" << m_dwTargetProcessId << " Main window handle!" << endl;
			return false;
		}
		DWORD iErrorCode = -1;
		HINSTANCE hinstDLL;
		if (!GetModuleHandleEx(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS, (LPCTSTR)&MessageHookProc, &hinstDLL))
		{
			iErrorCode = GetLastError();
			//cout << "Can't GetModuleHandleEx, with ErrorId:" << iErrorCode << endl;
			return false;
		};
				
		DWORD processID = 0;
		DWORD threadID = ::GetWindowThreadProcessId(strctArg.hwndWindow, &processID);
		if (!processID)
		{
			iErrorCode = GetLastError();
			//cout << "Can't GetWindowThreadProcessId, with ErrorId:" << iErrorCode << endl;
			return false;
		}

		HANDLE hProcess = ::OpenProcess(PROCESS_ALL_ACCESS, FALSE, processID);
		char arrData[20];
		if (!hProcess)
		{
			iErrorCode = ::GetLastError();
			
			m_strCurrrentError = "Can't Open Process by id with error code:"+ std::string(_itoa(iErrorCode, arrData,10));
			//cout << m_strCurrrentError <<iErrorCode << endl;
			return false;
		}
		
		std::string strData2Proc = m_strInjectDll;

		//char* arrDllName=(char*)m_strInjectDll.c_str()  ;//  "C:\\automationTest\\Automation Workbooks\\dlls\\QTInjectorDll.dll";
		char* arrDllName = (char*)strData2Proc.c_str();
		int buffLen = strlen(arrDllName) + 1;
		void *pMemRemote = VirtualAllocEx(hProcess, NULL, buffLen , MEM_COMMIT, PAGE_READWRITE);
		if (!pMemRemote)
		{
			iErrorCode = ::GetLastError();
			m_strCurrrentError = "Can't Open Process by id with error code:" + std::string(_itoa(iErrorCode, arrData, 10));
			std::cout << m_strCurrrentError << iErrorCode << std::endl;
			return false;
		}
		::WriteProcessMemory(hProcess, pMemRemote, arrDllName, buffLen, NULL);

		_messageHookHandle=::SetWindowsHookEx(WH_CALLWNDPROC, &MessageHookProc, hinstDLL, threadID);
		if (!_messageHookHandle)
		{
			iErrorCode = ::GetLastError();
			m_strCurrrentError = "Can't SetWindowsHookEx with error code:" + std::string(_itoa(iErrorCode, arrData, 10));
			std::cout << m_strCurrrentError << iErrorCode << std::endl;
			return false;
		}
		//cout << "Set Hook Successful" << endl;
		LRESULT rlst = ::SendMessage(strctArg.hwndWindow, WM_GOBABYGO, (WPARAM)pMemRemote, 0);
		//cout << "Send message Done" << endl;
		//::UnhookWindowsHookEx(_messageHookHandle);
	}
	catch (const std::exception& e)
	{
		//cout << "Exception :" << e.what() << endl;
	}
	//cout << "Inected "<< strError << endl;
	return isOk;
}

