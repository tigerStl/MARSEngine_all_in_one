// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

//#import "C:\\automationTest\\Automation Workbooks\\dlls\\MarsInterMQCenter.tlb" raw_interfaces_only

#include "stdafx.h"
#include "stdio.h"
#include "iostream"
#include "fstream"

#include <direct.h>
#include <TlHelp32.h>
#include "Injector.h"
#include <vcclr.h>
#include <stdexcept>
#include <windows.h>
#include <Lmcons.h>


#ifdef _NET4
#using "WindowsBase.dll"
#endif



using namespace ManagedInjector;
//using namespace MarsInterMQCenter;
using namespace System::Runtime::InteropServices;
using namespace System::Diagnostics;


static unsigned int WM_GOBABYGO = ::RegisterWindowMessage(L"Injector_GOBABYGO!");
static unsigned int WM_TCPSVC   = ::RegisterWindowMessage(L"Injector_TCPSVC!");

static unsigned int WM_BACKHOME = ::RegisterWindowMessage(L"BAKHOME");
#define tiger_debug true
#ifdef tiger_debug
static unsigned int WM_MARSGETTYPE = ::RegisterWindowMessage(L"MARS_NAVIGATE_TYPE");
#endif
static HHOOK _messageHookHandle;


struct MARSInjectParameter {
	char* assemblyNameWithPath;
	char* className;
	char* methodToCall;
};


//static int MarsThreadToLoadInjector(void* para)
static int MarsThreadToLoadInjector()
{
	
	/*
	try
	{
		System::Diagnostics::EventLog::WriteEntry("MarsEvent", "Begins from MarsThreadToLoadInjector");
		wchar_t* acmRemote = (wchar_t*)para;
		System::String^ s = gcnew System::String(acmRemote);

		cli::array<System::String^>^ acmSplit = s->Split('$');
		System::Diagnostics::Debug::WriteLine(String::Format("About to load assembly {0}", acmSplit[0]));
		System::String^ strTargetPath = acmSplit[0];
		if (!System::IO::File::Exists(strTargetPath))
		{
			///ÎÄ¼þ²»´æÔÚ
			System::Diagnostics::EventLog::WriteEntry("MarsEvent", System::String::Format("target message path:[{0}] can't find", strTargetPath));
			return -1;
		}

		//"MarsInterMQCenter.dll"), "Mars.message.Inter.MQCenter.interProcess", "StartMonitorThread"
		System::Reflection::Assembly^ assembly = System::Reflection::Assembly::LoadFrom(strTargetPath);
		if (assembly != nullptr)
		{
			System::Type^ type = assembly->GetType(acmSplit[1]);
			if (type != nullptr)
			{
				System::Diagnostics::EventLog::WriteEntry("MarsEvent", String::Format("Just loaded the type {0}", acmSplit[1]));

				System::Reflection::MethodInfo^ mthdResolved = type->GetMethod("Mars_AssemblyResolveInstall", System::Reflection::BindingFlags::Static | System::Reflection::BindingFlags::Public);
				if (mthdResolved != nullptr)
				{
					System::Diagnostics::EventLog::WriteEntry("MarsEvent", "try to install resolve");
					mthdResolved->Invoke(nullptr, nullptr);
				}
				System::Reflection::MethodInfo^ mthdExit = type->GetMethod("Mars_AppExitEventHandleInstall", System::Reflection::BindingFlags::Static | System::Reflection::BindingFlags::Public);
				if (mthdExit != nullptr)
				{
					System::Diagnostics::EventLog::WriteEntry("MarsEvent", "try to install exit event to kill thread");
					mthdExit->Invoke(nullptr, nullptr);
				}

				System::Reflection::MethodInfo^ methodInfo = type->GetMethod(acmSplit[2], System::Reflection::BindingFlags::Static | System::Reflection::BindingFlags::Public);
				if (methodInfo != nullptr)
				{
					try
					{
						System::Diagnostics::Debug::WriteLine(System::String::Format("About to invoke {0} on type {1}", methodInfo->Name, acmSplit[1]));
						methodInfo->Invoke(nullptr, nullptr);
					}
					catch (System::Exception^ e)
					{
						System::Diagnostics::EventLog::WriteEntry("MarsEvent", System::String::Format("Exception:[{0}] stack:[{1}]", e->Message, e->StackTrace));
					}
				}
			}
		}
	}
	catch (const std::exception& err)
	{
		System::String^ s = gcnew System::String(err.what());
		System::Diagnostics::EventLog::WriteEntry("MarsEvent",System::String::Format("Exception:[{0}]", s));
	}
	*/
	return 0;
}
int filterException(int code, PEXCEPTION_POINTERS ex) {
	std::cout << "Filtering " << std::hex << code << std::endl;
	return EXCEPTION_EXECUTE_HANDLER;
}
BOOL WINAPI EjectLibW(DWORD dwProcessId,BOOL* bFound, PCWSTR pszLibFile) {

	BOOL bOk = FALSE; // Assume that the function fails
	HANDLE hthSnapshot = NULL;
	HANDLE hProcess = NULL, hThread = NULL;

	__try {
		// Grab a new snapshot of the process
		hthSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPMODULE, dwProcessId);
		if (hthSnapshot == INVALID_HANDLE_VALUE) __leave;

		// Get the HMODULE of the desired library
		MODULEENTRY32W me = { sizeof(me) };
		//BOOL bFound = FALSE;
		BOOL bMoreMods = Module32FirstW(hthSnapshot, &me);
		for (; bMoreMods; bMoreMods = Module32NextW(hthSnapshot, &me)) {
			OutputDebugString(me.szModule);
			*bFound = (_wcsicmp(me.szModule, pszLibFile) == 0) ||
				(_wcsicmp(me.szExePath, pszLibFile) == 0);
			if (*bFound) break;
		}
		if (!(*bFound)) __leave;

		// Get a handle for the target process.
		hProcess = OpenProcess(
			PROCESS_QUERY_INFORMATION |
			PROCESS_CREATE_THREAD |
			PROCESS_VM_OPERATION,  // For CreateRemoteThread
			FALSE, dwProcessId);
		if (hProcess == NULL) __leave;

		// Get the real address of FreeLibrary in Kernel32.dll
		PTHREAD_START_ROUTINE pfnThreadRtn = (PTHREAD_START_ROUTINE)
			GetProcAddress(GetModuleHandle(TEXT("Kernel32")), "FreeLibrary");
		if (pfnThreadRtn == NULL) __leave;

		// Create a remote thread that calls FreeLibrary()
		hThread = CreateRemoteThread(hProcess, NULL, 0,
			pfnThreadRtn, me.modBaseAddr, 0, NULL);
		if (hThread == NULL) __leave;

		// Wait for the remote thread to terminate
		WaitForSingleObject(hThread, INFINITE);

		bOk = TRUE; // Everything executed successfully
	}
	/*__except (filterException(GetExceptionCode(), GetExceptionInformation())) {
		std::cout << "caught:" << std::endl;
	}*/
	__finally { // Now we can clean everything up
		//filterException(GetExceptionCode(), GetExceptionInformation());
		if (hthSnapshot != NULL)
			CloseHandle(hthSnapshot);

		if (hThread != NULL)
			CloseHandle(hThread);

		if (hProcess != NULL)
			CloseHandle(hProcess);
	}

	return(bOk);
}
/// <summary>
/// return 0, 表示失败
///        1, 表示成功卸载
///        2, 表示不存在该模块
/// </summary>
/// <param name="pid"></param>
/// <param name="libFile"></param>
/// <returns></returns>
int Injector::UnLoad(DWORD pid, System::String^ libFile) {
	BOOL isOk = false , isFound = false;
	IntPtr pstr = Marshal::StringToHGlobalAnsi(libFile);

	isOk = EjectLibW(pid, &isFound, (PCWSTR)pstr.ToPointer());
	if (!isFound) return 2;
	if (isOk) return 1;
	return 0;
}


//public delegate int MarsRemoteThreadProc(void* param);
public delegate int MarsRemoteThreadProc();


void Injector::LaunchAndConnectToPort(
	System::IntPtr windowHandle,
	int iTcpPort,
	System::String^ assemblyName,
	System::String^ className,
	System::String^ methodName,
	System::String^ injectType) {

	System::String^ assemblyClassAndMethod = assemblyName + "$" + className + "$" + methodName+"$"+iTcpPort;
	LogMessage(String::Format("LaunchAndConnectToPort, %s", assemblyClassAndMethod),true);
	pin_ptr<const wchar_t> acmLocal = PtrToStringChars(assemblyClassAndMethod);
	HINSTANCE hinstDLL;
	if (::GetModuleHandleEx(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS, (LPCTSTR)&MessageHookProc, &hinstDLL))
	{
		LogMessage("\tGetModuleHandleEx successful", true);
		DWORD processID = 0;
		DWORD threadID = ::GetWindowThreadProcessId((HWND)windowHandle.ToPointer(), &processID);
		if (processID) {
			LogMessage("\tGot process id", true);
			HANDLE hProcess = ::OpenProcess(PROCESS_ALL_ACCESS, FALSE, processID);
			bool isDlg = false;
			if (hProcess)
			{
				LogMessage("\tGot process handle", true);
				int buffLen = (assemblyClassAndMethod->Length + 1) * sizeof(wchar_t);
				void* acmRemote = ::VirtualAllocEx(hProcess, NULL, buffLen, MEM_COMMIT, PAGE_READWRITE);
#if _demo_for_14
				SYSTEMTIME currentTime;
				GetSystemTime(&currentTime);
				SYSTEMTIME targetDate;
				targetDate.wYear = 2025;
				targetDate.wMonth = 9;
				targetDate.wDay = 15;
				targetDate.wHour = 0;
				targetDate.wMinute = 0;
				targetDate.wSecond = 0;
				targetDate.wMilliseconds = 0;
				BOOL ISOVER = FALSE;
				if (currentTime.wYear > targetDate.wYear)
				{
					ISOVER = true;
				}
				else if (currentTime.wYear == targetDate.wYear)
				{
					if (currentTime.wMonth > targetDate.wMonth)
					{
						ISOVER = true;
					}
					else if (currentTime.wMonth == targetDate.wMonth)
					{
						if (currentTime.wDay > targetDate.wDay)
						{
							ISOVER = true;
						}
					}
				}
				if (ISOVER) return;
#endif
				if (acmRemote)
				{
					LogMessage("\tVirtualAllocEx successful", true);
					::WriteProcessMemory(hProcess, acmRemote, acmLocal, buffLen, NULL);
					if (!injectType) {
						_messageHookHandle = ::SetWindowsHookEx(WH_CALLWNDPROC, &MessageHookProc, hinstDLL, threadID);
					}
					else if (String::Compare("Normal", injectType, true) == 0) {
						_messageHookHandle = ::SetWindowsHookEx(WH_CALLWNDPROC, &MessageHookProc, hinstDLL, threadID);
					}
					else if (String::Compare("Dialog", injectType, true) == 0) {
						LogMessage("Dialog", true);
						_messageHookHandle = ::SetWindowsHookEx(WH_MSGFILTER, &MessageHookProc, hinstDLL, threadID);
						if (!_messageHookHandle) {
							_messageHookHandle = ::SetWindowsHookEx(WH_SYSMSGFILTER, &MessageHookProc, hinstDLL, threadID);
						}
						isDlg = true;
					}
					else if (String::Compare("Wpf", injectType, true) == 0) {
						_messageHookHandle = ::SetWindowsHookEx(WH_CALLWNDPROC, &MessageHookProc, hinstDLL, threadID);
					}

					int ierror = ::GetLastError();
					LogMessage(String::Format("\tSetWindowsHookEx error code:{0}", ierror), true);

					if (_messageHookHandle)
					{
						LogMessage("\tSetWindowsHookEx successful", true);
						if (!isDlg) {
							LRESULT RSLT = ::SendMessage((HWND)windowHandle.ToPointer(), WM_TCPSVC, (WPARAM)acmRemote, 0);
							DWORD dError = ::GetLastError();
							LogMessage(String::Format("\tresult:[{0}-lastError:{1}]", RSLT, dError), true);
						}
						else {
							LogMessage("before call dialog message:", true);
							LRESULT RSLT = ::PostMessage((HWND)windowHandle.ToPointer(), WM_TCPSVC, (WPARAM)acmRemote, 0);
							DWORD dError = ::GetLastError();
							LogMessage(String::Format("result:[{0}-lastError:{1}]", RSLT, dError), true);
						}
						::UnhookWindowsHookEx(_messageHookHandle);
					}

					::VirtualFreeEx(hProcess, acmRemote, 0, MEM_RELEASE);
					}

				::CloseHandle(hProcess);
				}
		}
		::FreeLibrary(hinstDLL);
	}
}

//-----------------------------------------------------------------------------
//Spying Process functions follow
//-----------------------------------------------------------------------------
void Injector::Launch(System::IntPtr windowHandle, System::String^ assembly, System::String^ className, System::String^ methodName, System::String^ injectType)
{
	System::String^ assemblyClassAndMethod = assembly + "$" + className + "$" + methodName;
	if (injectType && String::Compare(injectType, "Wpf", true) == 0) {
		assemblyClassAndMethod += "$" + injectType;
	}

//#undef GetTempPath
//	System::String^ tmpPath = System::IO::Path::GetTempPath();
	LogMessage("======launch=========", true);
	LogMessage(assemblyClassAndMethod,true);
	LogMessage(String::Format("current handle of wnd", windowHandle), true);
	pin_ptr<const wchar_t> acmLocal = PtrToStringChars(assemblyClassAndMethod);
	
	HINSTANCE hinstDLL;	

	if (::GetModuleHandleEx(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS, (LPCTSTR)&MessageHookProc, &hinstDLL))
	{
		LogMessage("GetModuleHandleEx successful", true);
		DWORD processID = 0;
		DWORD threadID = ::GetWindowThreadProcessId((HWND)windowHandle.ToPointer(), &processID);
		// 如果对方进程被一个模态窗口锁着，无法通过window procedure处理
		if (processID)
		{
			LogMessage("Got process id", true);
			HANDLE hProcess = ::OpenProcess(PROCESS_ALL_ACCESS, FALSE, processID);
			bool isDlg = false;
			if (hProcess)
			{
				LogMessage("Got process handle", true);				

				int buffLen = (assemblyClassAndMethod->Length + 1) * sizeof(wchar_t);
				void* acmRemote = ::VirtualAllocEx(hProcess, NULL, buffLen, MEM_COMMIT, PAGE_READWRITE);

				if (acmRemote)
				{
					LogMessage("VirtualAllocEx successful", true);
					::WriteProcessMemory(hProcess, acmRemote, acmLocal, buffLen, NULL);

					if (!injectType) {
						_messageHookHandle = ::SetWindowsHookEx(WH_CALLWNDPROC, &MessageHookProc, hinstDLL, threadID);
					}else if ((String::Compare("Normal", injectType, true) == 0)
						   || (String::Compare("Wpf"   , injectType, true) == 0)) {
						_messageHookHandle = ::SetWindowsHookEx(WH_CALLWNDPROC, &MessageHookProc, hinstDLL, threadID);
					}
					else if (String::Compare("Dialog", injectType, true) == 0){
						LogMessage("Dialog", true);
						_messageHookHandle = ::SetWindowsHookEx(WH_MSGFILTER, &MessageHookProc, hinstDLL, threadID);
						if (!_messageHookHandle) {
							_messageHookHandle = ::SetWindowsHookEx(WH_SYSMSGFILTER, &MessageHookProc, hinstDLL, threadID);
						}
						isDlg = true;
					}

					int ierror = ::GetLastError();
					LogMessage(String::Format("SetWindowsHookEx error code:{0}", ierror), true);

					//SetWindowsHookEx(WH_CBT, &MessageHookProc, hinstDLL, threadID);
					if (_messageHookHandle)
					{
						LogMessage("SetWindowsHookEx successful", true);
#ifdef tiger_debug
						LogMessage(String::Format("handle:[{0}-pointer:{1}-DDD:[{2}]]", windowHandle.ToInt64(), (int)windowHandle.ToPointer(), (WPARAM)windowHandle.ToInt64()), true);
						//::SendMessage((HWND)windowHandle.ToPointer(), WM_MARSGETTYPE, (WPARAM)windowHandle.ToInt64(), 0);
						//::SendMessage((HWND)windowHandle.ToPointer(), WM_MARSGETTYPE, (WPARAM)windowHandle.ToInt64(), 0);

#endif
						if (!isDlg) {
							
							LRESULT RSLT = ::SendMessage((HWND)windowHandle.ToPointer(), WM_GOBABYGO, (WPARAM)acmRemote, 0);
							DWORD dError = ::GetLastError();
							LogMessage(String::Format("result:[{0}-lastError:{1}]", RSLT, dError), true);
						}
						else {
							LogMessage("before call dialog message:", true);
							//LRESULT RSLT = ::SendDlgItemMessage((HWND)windowHandle.ToPointer(),0, WM_GOBABYGO, (WPARAM)acmRemote, 0);
							LRESULT RSLT = ::PostMessage((HWND)windowHandle.ToPointer(), WM_GOBABYGO, (WPARAM)acmRemote, 0);
							DWORD dError = ::GetLastError();
							LogMessage(String::Format("result:[{0}-lastError:{1}]", RSLT, dError), true);
						}
						::UnhookWindowsHookEx(_messageHookHandle);
					}

					::VirtualFreeEx(hProcess, acmRemote, 0, MEM_RELEASE);
				}

				::CloseHandle(hProcess);
			}
			else {
				LogMessage("No process id is get", true);
			}
		}
		::FreeLibrary(hinstDLL);
	}
}

BOOL Injector::IsModalWindowAccordingToThisParticularSpec(System::IntPtr hwnd)
{
	LogMessage("IsModalWindowAccordingToThisParticularSpec begins", true);
	// child windows cannot have owners
	if (::GetWindowLong((HWND)hwnd.ToPointer(), GWL_STYLE) & WS_CHILD) return FALSE;

	HWND hwndOwner = GetWindow((HWND)hwnd.ToPointer(), GW_OWNER);
	if (hwndOwner == NULL) return FALSE; // not an owned window

	if (IsWindowEnabled(hwndOwner)) return FALSE; // owner is enabled

	LogMessage("\t[true]", true);
	return TRUE; // an owned window whose owner is disabled
}


void Injector::SetLogMessagePath(System::String^ strTargetPath)
{
	currentFilePath = strTargetPath;
}

void Injector::LoadMessageCenter()
{
	//
	// Initialize COM.
	//HRESULT hr = CoInitialize(NULL);
	//IMSMQHostMainPtr pMsmq(__uuidof(MSMQHostMain));
}

void Injector::LogMessage(System::String^ message, bool append)
{	            
	//System::String ^ applicationDataPath = Environment::GetFolderPath(Environment::SpecialFolder::ApplicationData);
#undef GetTempPath
	System::String^ applicationDataPath = System::IO::Path::GetTempPath();// (currentFilePath) ? currentFilePath : "c:\\temp";
	//System::String ^ applicationDataPath = "c:\\temp";
	applicationDataPath += "\\mars";

	if (!System::IO::Directory::Exists(applicationDataPath))
	{
		System::IO::Directory::CreateDirectory(applicationDataPath);
	}

	System::String ^ pathname = applicationDataPath + "\\marsInjectorLog.txt";

	if (!append)    
	{    
		System::IO::File::Delete(pathname);        
	}

	System::IO::FileInfo ^ fi = gcnew System::IO::FileInfo(pathname);	
	            
	System::IO::StreamWriter ^ sw = fi->AppendText();   
	sw->WriteLine(System::DateTime::Now.ToString("MM/dd/yyyy HH:mm:ss") + " : " + message);
	sw->Close();

	//if (!System::Diagnostics::EventLog::SourceExists("MarsEvent")) System::Diagnostics::EventLog::CreateEventSource("MarsEvent", "Mars");

	//System::Diagnostics::EventLog::WriteEntry("MarsEvent", message);
	
}

typedef void (*MarsInjectDll)(const char *pstrPath);
typedef void (*MarsInjectFuncNoPara)();

__declspec(dllexport) 
LRESULT __stdcall MessageHookProc(int nCode, WPARAM wparam, LPARAM lparam)
{
#if !_ForClickOnce
	System::Diagnostics::EventLog::WriteEntry("MarsEvent", System::String::Format("begin Message code {0}, w [{1}] l:[{2}]", nCode, wparam, lparam));
#endif

	struct stat info;
	if (stat("c:\\temp\\mars", &info) != 0) {
		//create 
		_mkdir("c:\\temp\\mars");
	}
	std::ofstream marsLog;
	marsLog.open("c:\\temp\\mars\\marsagent.log", std::ios::out | std::ios::app);

	// 获取当前时间
	time_t now = time(nullptr);
	struct tm t;
	localtime_s(&t, &now);
	char dateStr[20];
	// 格式化为 yyyyMMddHH:mm:ss
	strftime(dateStr, sizeof(dateStr), "%Y%m%d%H:%M:%S", &t);
	marsLog << "=========Current Time: "<< dateStr<<"==========="<< std::endl;

	if (nCode == HC_ACTION)
	{
		try
		{
			CWPSTRUCT* msg = (CWPSTRUCT*)lparam;			
			if (msg != NULL && (msg->message == WM_GOBABYGO)||(msg->message == WM_TCPSVC))
			{			
				wchar_t* acmRemote = (wchar_t*)msg->wParam;

				String^ acmLocal = gcnew System::String(acmRemote);
#if !_ForClickOnce
				//System::Diagnostics::EventLog::WriteEntry("MarsEvent", System::String::Format("acmLocal = {0}", acmLocal));
#else
				marsLog << "acmLocal = " << acmRemote << std::endl;
#endif

				/** 格式：
				* 有两种模式，1，普通模式
				* System::String^ assemblyClassAndMethod = assembly + "$" + className + "$" + methodName;
				* 2，tcp client模式，最后一个是port
				* System::String^ assemblyClassAndMethod = assembly + "$" + className + "$" + methodName+"$"+iPort;
				*
				* */

				/// 
				/// For QT and other pure dll model, format is
				/// QT:DllNameAndItsPath$functionName$parameter for Initialization function as string
				/// The Function should be like bool InitEnv(const char* parameter)
				cli::array<System::String^>^ acmSplit = acmLocal->Split('$');
				marsLog << "About to load assembly " << (char*)(void*)Marshal::StringToHGlobalAnsi(acmSplit[0]) << "|"<< (char*)(void*)Marshal::StringToHGlobalAnsi(acmSplit[1]) << std::endl;
				//::re	
				System::String^ strTargetPath = acmSplit[0];
				marsLog << "after get Taget" << std::endl;
				if (!strTargetPath->StartsWith("QT:")) {
#pragma region standard .net mode 
					if (!System::IO::File::Exists(strTargetPath))
					{
						marsLog << "strTargetPath is not" << std::endl;
						return CallNextHookEx(_messageHookHandle, nCode, wparam, lparam);
					}
					try
					{
						AppDomain^ defaultDomain = AppDomain::CurrentDomain;
						marsLog << "domain----" << std::endl;

						//System::Windows::Forms::FormCollection^ forms = System::Windows::Forms::Application::OpenForms;
						marsLog << "forms----" << std::endl;
						System::Reflection::Assembly^ assembly = System::Reflection::Assembly::LoadFrom(strTargetPath);
						marsLog << "after load..." << ((!assembly)?"NULL":"NOT NULL") << std::endl;
						
						if (assembly != nullptr)
						{
							AppDomain::CurrentDomain->AppendPrivatePath(System::IO::Path::GetDirectoryName(strTargetPath));
							marsLog << "after get target path:" << (char*)(void*)Marshal::StringToHGlobalAnsi(System::IO::Path::GetDirectoryName(strTargetPath))
								<< "|"
								<< (char*)(void*)Marshal::StringToHGlobalAnsi(acmSplit[1])
								<< std::endl;
							System::Type^ type = assembly->GetType(acmSplit[1]);
							marsLog << (type == nullptr ? "type is null" : "type is not null") << std::endl;
							marsLog << "get type:" << (char*)(void*)Marshal::StringToHGlobalAnsi(type->ToString()) << std::endl;
							if (type != nullptr)
							{
								System::Reflection::MethodInfo^ mthdResolved = type->GetMethod("Mars_AssemblyResolveInstall", System::Reflection::BindingFlags::Static | System::Reflection::BindingFlags::Public);
								marsLog << "after get method Mars_AssemblyResolveInstall" << std::endl;

								if (mthdResolved != nullptr)
								{
									marsLog << "before call method" << std::endl;
									mthdResolved->Invoke(nullptr, nullptr);
									marsLog << "after call method" << std::endl;
								}
								System::Reflection::MethodInfo^ mthdExit = type->GetMethod("Mars_AppExitEventHandleInstall", System::Reflection::BindingFlags::Static | System::Reflection::BindingFlags::Public);
								if (mthdExit != nullptr)
								{
									marsLog << "before call method mthdExit " << std::endl;
									mthdExit->Invoke(nullptr, nullptr);
									marsLog << "after call method mthdExit" << std::endl;
								}
								System::Reflection::MethodInfo^ methodInfo = type->GetMethod(acmSplit[2], System::Reflection::BindingFlags::Static | System::Reflection::BindingFlags::Public);
								//System::String^ strMethodTypeInfo = (!methodInfo) ? "NULL" : methodInfo->Name;

								System::String^ strMethodTypeInfo = (!methodInfo) ? "NULL" : methodInfo->Name;
								IntPtr pStr = System::Runtime::InteropServices::Marshal::StringToHGlobalAnsi(strMethodTypeInfo);
								marsLog << "methodInfo is: " << static_cast<const char*>(pStr.ToPointer()) << std::endl;
								System::Runtime::InteropServices::Marshal::FreeHGlobal(pStr);

								//marsLog << "methodInfo is:" << (LPCWSTR)(&strMethodTypeInfo) << std::endl;
								if (methodInfo != nullptr)
								{
									try
									{
										if (msg->message == WM_TCPSVC) {
											/*tcp 模式，最后一个参数是端口*/
											int iPort=-1;
											if (acmSplit->Length != 4) {
												marsLog << "Error, tcp service mode, but the no port passed" << std::endl;
												return CallNextHookEx(_messageHookHandle, nCode, wparam, lparam);
											}
											if (!int::TryParse(acmSplit[3], iPort)) {
												marsLog << "Error, the para mater is not a port number" << std::endl;
												return CallNextHookEx(_messageHookHandle, nCode, wparam, lparam);
											}
											methodInfo->Invoke(nullptr, gcnew array<Object^> {iPort});
										}
										else {
											// 构造参数（可选，传递 nullptr 使用默认参数 "Normal"）
											cli::array<Object^>^ params = gcnew cli::array<Object^>(1);
											if (acmSplit->Length == 4) // 有参数, 如wpf
												params[0] = acmSplit[3];											
											else
												params[0] = "Normal"; // 或者 params[0] = "Normal"; 或 params[0] = "Wpf";
											methodInfo->Invoke(nullptr, params);
											//methodInfo->Invoke(nullptr, nullptr);
										}
									}
									catch (System::Exception^ e)
									{

										marsLog << "exception methodInfo->Invoke(nullptr, nullptr) " << (char*)(void*)Marshal::StringToHGlobalAnsi(e->Message) <<std::endl;
										marsLog << "exception " << (char*)(void*)Marshal::StringToHGlobalAnsi(e->StackTrace) << std::endl;
										if (e->InnerException) {
											marsLog << "exception " << (char*)(void*)Marshal::StringToHGlobalAnsi(e->InnerException->Message) << std::endl;
											marsLog << "exception " << (char*)(void*)Marshal::StringToHGlobalAnsi(e->InnerException->StackTrace) << std::endl;
										}

									}
								}
							}
						}
					}
					catch (System::Exception^ e)
					{
#if !_ForClickOnce
						System::Diagnostics::EventLog::WriteEntry("MarsEvent", System::String::Format("Exception:[{0}] stack:[{1}]", e->Message, e->StackTrace));
#else
						System::Diagnostics::EventLog::WriteEntry("MarsEvent", System::String::Format("Exception:[{0}] stack:[{1}]", e->Message, e->StackTrace));
						marsLog<<"Exception:"<< (char*)(void*)Marshal::StringToHGlobalAnsi(e->Message) << std::endl;
						marsLog << "stack:" << (char*)(void*)Marshal::StringToHGlobalAnsi(e->StackTrace) << std::endl;
						if (e->InnerException)
							marsLog << "Exception:" << (char*)(void*)Marshal::StringToHGlobalAnsi(e->InnerException->Message) << std::endl;

#endif
					}
#pragma endregion //standard .net mode 
				}
				else
				{
#pragma region QT and other standard dll 
					System::String^ pstrDllName = strTargetPath->Substring(3);
					/*::SetErrorMode(0);
					char buff[FILENAME_MAX];
					_getcwd(buff, FILENAME_MAX);
					_chdir()*/

					marsLog << "before loadlib" << std::endl;
					marsLog << (LPCWSTR)(&pstrDllName) << std::endl;
					HMODULE hdlOfDll = ::LoadLibraryW((LPCWSTR)(&pstrDllName));
					DWORD lstError = ::GetLastError();

					marsLog << "Last Error:" << lstError << std::endl;
					if (hdlOfDll == NULL)
					{
						marsLog << "after Load:" << (char *)(void *)Marshal::StringToHGlobalAnsi(pstrDllName) << std::endl;
					}
					else
					{
						marsLog << "after Load" << std::endl;
					}
					//::sprintf("handle %d", hdlOfDll);
					//System::Diagnostics::EventLog::WriteEntry("MarsEvent",System::String::Format("after Load:[{0}] handle returnd:[{1}]", pstrDllName, hdlOfDll));
					if (!hdlOfDll)
					{
						DWORD lstError = ::GetLastError();
						marsLog << "hdlOfDll Error " << lstError << std::endl;
						return 0;
					}
					System::String^ pFunctionName = acmSplit[1];
					if (hdlOfDll == NULL)
						return 0;
					//MarsInjectDll *pFuncInit=(MarsInjectDll *)::GetProcAddress(hdlOfDll, (LPCSTR)(&pFunctionName));
					MarsInjectFuncNoPara pFunc = (MarsInjectFuncNoPara )::GetProcAddress(hdlOfDll, (LPCSTR)(&pFunctionName));
					char* s=new char[256];
					//::sprintf(s, "Get function :%d", pFuncInit);
					::sprintf(s, "Get function :%d", pFunc);
#if !_ForClickOnce
					//System::Diagnostics::EventLog::WriteEntry("MarsEvent", gcnew System::String(s));
#else
					marsLog << s << std::endl;
#endif
					pFunc();

#pragma endregion //QT and other standard dll ½áÊø
				}
			}
#ifdef tiger_debug
			else {
				if (msg != NULL && msg->message == WM_MARSGETTYPE)
				{
					Injector::LogMessage(String::Format("wparam:{0}", msg->wParam), true);
					System::String ^ applicationDataPath = Environment::GetFolderPath(Environment::SpecialFolder::ApplicationData);
					Injector::LogMessage(String::Format("applicationDataPath:[{0}]", applicationDataPath), true);
					System::IntPtr p = System::IntPtr((long)msg->wParam);
					Injector::LogMessage(String::Format("begin WM_MARSGETTYPE:{0}-wparam:{1}", p, wparam), true);
					//System::Windows::Forms::Control^ pcontrol=System::Windows::Forms::Control::FromHandle(p);
					//System::Windows::Forms::Control^ pcontrol = System::Windows::Forms::Control::Fro
					AppDomain^ pCurrntDomain = System::Threading::Thread::GetDomain();

					Injector::LoadMessageCenter();
					//if (pcontrol) 
					/*{
						Injector::LogMessage(String::Format(L"pcontrol's type:[{0}]", pcontrol->GetType()->FullName), true);
					}
					else {
						Injector::LogMessage("control from handle is null", true);
					}*/
					if (pCurrntDomain)
					{
						/*Injector::LogMessage(String::Format(L"current domain's type:[{0}]", pCurrntDomain->FriendlyName), true);
						System::Windows::Interop::HwndSource^ pwfpObj = System::Windows::Interop::HwndSource::FromHwnd(p);
						if (pwfpObj)
						{
							Injector::LogMessage(String::Format("HwndSource's type:[{0}-{1}]", pwfpObj->GetType()->FullName, p), true);
							Injector::LogMessage(String::Format(L"HwndSource's RootVisual type:[{0}]", pwfpObj->RootVisual->GetType()->FullName), true);
							((System::Windows::Window^)(pwfpObj->RootVisual))->Title = L"Tiger Changed";
						}
						else
						{
							Injector::LogMessage("HwndSource from handle is null", true);
						}*/
					}
					else {
						Injector::LogMessage("current domain is null", true);
					}
				}
			}
		}
		catch (System::Exception^ ex)
		{
#if !_ForClickOnce
			//System::Diagnostics::EventLog::WriteEntry("MarsEvent", System::String::Format("Exception:[{0}] stack:[{1}]", ex->Message, ex->StackTrace));
#else
			marsLog << (char*)(void*)Marshal::StringToHGlobalAnsi(ex->Message) << std::endl;
			marsLog << (char*)(void*)Marshal::StringToHGlobalAnsi(ex->StackTrace) << std::endl;
#endif
		}
#endif
	}
else
{
	CWPSTRUCT* msg = (CWPSTRUCT*)lparam;
	if (msg != NULL && msg->message == WM_GOBABYGO) {
#if !_ForClickOnce
		System::Diagnostics::EventLog::WriteEntry("MarsEvent", "WM_GOBABYGO GETTED,BY NOT ACTION");
#endif
	}
 }
	return CallNextHookEx(_messageHookHandle, nCode, wparam, lparam);
}

BOOL AdjustPrivileges(HANDLE hProcess, LPCTSTR lpPrivilegeName)
{
	//******************************************************
	// 调整进程权限
	//******************************************************
	HANDLE hToken;
	TOKEN_PRIVILEGES tkp;
	// 打开进程的权限标记
	if (!::OpenProcessToken(hProcess,
		TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, &hToken))
		return FALSE;
	// 传入 lpPrivilegeName 的 Luid 值
	if (!::LookupPrivilegeValue(NULL,
		lpPrivilegeName,
		&tkp.Privileges[0].Luid))
		return FALSE;

	tkp.PrivilegeCount = 1;
	tkp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
	if (!::AdjustTokenPrivileges(hToken, FALSE, &tkp, 0,
		(PTOKEN_PRIVILEGES)NULL, 0))
		return FALSE;
	return TRUE;
}
void Injector::LoadLibInject(int pid)
{
#if !_ForClickOnce
	System::Diagnostics::EventLog::WriteEntry("MarsEvent", "LoadLibInject begins");
#endif
	HANDLE hRemoteProcess = NULL;
	if ((hRemoteProcess = ::OpenProcess(PROCESS_ALL_ACCESS,
		FALSE,
		pid)) == NULL)
	{
#if !_ForClickOnce
		System::Diagnostics::EventLog::WriteEntry("MarsEvent",  "OpenProcess faile!!");
#endif
		return ;

	}
	BOOL Adjust = AdjustPrivileges(hRemoteProcess, SE_DEBUG_NAME);
	if (Adjust == FALSE)
	{
		printf("Adjust process Privileges faile!!\n");
		return ;
	}

	std::string strPath = "C:\\automationTest\\Automation Workbooks\\dlls - t_qt\\TestObjEngineHostDll.dll";

}
								  
#define MARS_INJECTOR_MODULENAME "ManagedInjector"
#define MARS_MQCENTER "MarsInterMQCenter"
#define MARS_ENV_HOME "MARS_HOME"

bool Injector::IsInjected(System::String^ strProcessName)
{
	//return true;
	cli::array<System::Diagnostics::Process^>^ arrP = System::Diagnostics::Process::GetProcessesByName(strProcessName);
	if ((&arrP == NULL)||(arrP->Length==0))
	{
		return false;
	}
	
	System::Diagnostics::Process^ curP = System::Diagnostics::Process::GetCurrentProcess(); 
	
	int idx = -1;
	for (int i = 0; i < arrP->Length; i++) {
		if (!arrP[i]) continue;
		if (arrP[i]->SessionId == curP->SessionId) {
			idx = i;
			break;
		}
	}
	if (idx < 0) return false;

	//arrP[0]->Refresh();
	arrP[idx]->Refresh();
#pragma region original code
	//for (int i = 0; i < arrP[0]->Modules->Count; i++)
	//{
	//	try
	//	{

	//		if (System::String::IsNullOrEmpty(arrP[0]->Modules[i]->ModuleName)) continue;
	//		//WriteLogtoFile(arrP[0]->Modules[i]->ModuleName);
	//		if (arrP[0]->Modules[i]->FileName->Contains(MARS_INJECTOR_MODULENAME)) return true;
	//		if (arrP[0]->Modules[i]->FileName->Contains(MARS_MQCENTER)) return true;
	//	}
	//	catch (const std::exception&)
	//	{
	//		return false;
	//	}
	//}
#pragma endregion
	for (int i = 0; i < arrP[idx]->Modules->Count; i++)
	{
		try
		{
			WriteLogtoFile(String::Format("IsInjected?: {0} ", arrP[idx]->Modules[i]->ModuleName));
			if (System::String::IsNullOrEmpty(arrP[idx]->Modules[i]->ModuleName)) continue;
			//WriteLogtoFile(arrP[0]->Modules[i]->ModuleName);
			if (arrP[idx]->Modules[i]->FileName->Contains(MARS_INJECTOR_MODULENAME)) return true;
			WriteLogtoFile(String::Format("\tcheck:{0} ", MARS_INJECTOR_MODULENAME));

			if (arrP[idx]->Modules[i]->FileName->Contains(MARS_MQCENTER)) return true;
			WriteLogtoFile(String::Format("\tcheck:{0} ", MARS_MQCENTER));
		}
		catch (const std::exception& e)
		{
			WriteLogtoFile(e.what());
			return false;
		}
	}
	return false;
}

bool Injector::IsInjectedById(int iProcessId)
{
	System::Diagnostics::Process^ pPro = System::Diagnostics::Process::GetProcessById(iProcessId);
	
	for (int i = 0; i < pPro->Modules->Count; i++)
	{
		if (System::String::IsNullOrEmpty(pPro->Modules[i]->ModuleName)) continue;
		//WriteLogtoFile(arrP[0]->Modules[i]->ModuleName);
		if (pPro->Modules[i]->ModuleName->Contains(MARS_INJECTOR_MODULENAME)) return true;
		if (pPro->Modules[i]->ModuleName->Contains(MARS_MQCENTER)) return true;

	}
	return false;
}
void Injector::WriteLogtoFile(const char* m) {
	String^ tmpFile = "c:\\temp\\injector_%s.txt";

	char username[UNLEN + 1];
	DWORD username_len = UNLEN + 1;
	GetUserNameA(username, &username_len);

	String^ strUsername = gcnew String(username);
	tmpFile = String::Format("c:\\temp\\injector_{0}.txt", strUsername);

	System::IO::StreamWriter^ sw;
	if (!System::IO::File::Exists(tmpFile))
	{
		try
		{
			sw = System::IO::File::CreateText(tmpFile);
			sw->WriteLine("Mars Test Log....");
			sw->WriteLine(m);
			sw->Close();
		}
		catch (const std::exception&)
		{

		}
	}
	try
	{
		System::IO::StreamWriter^ sw = System::IO::File::AppendText(tmpFile);
		sw->WriteLine(m);
		sw->Close();
	}
	catch (const std::exception&)
	{

	}
}

void Injector::WriteLogtoFile(System::String^ strMessage)
{
	String^ tmpFile = "c:\\temp\\injector_%s.txt";
	
	char username[UNLEN + 1];
	DWORD username_len = UNLEN + 1;
	GetUserNameA(username, &username_len);
	
	String^ strUsername = gcnew String(username);
	tmpFile = String::Format("c:\\temp\\injector_{0}.txt",strUsername);

	System::IO::StreamWriter^ sw ;
	if (!System::IO::File::Exists(tmpFile))
	{
		try
		{
			sw=System::IO::File::CreateText(tmpFile);
			sw->WriteLine("Mars Test Log....");
			sw->WriteLine(strMessage);
			sw->Close();
		}
		catch (const std::exception&)
		{
				
		}		
	}
	try
	{
		System::IO::StreamWriter^ sw = System::IO::File::AppendText(tmpFile);
		sw->WriteLine(strMessage);
		sw->Close();
	}
	catch (const std::exception&)
	{
			
	}
	
}