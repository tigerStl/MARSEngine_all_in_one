// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

#pragma once

__declspec(dllexport)
LRESULT __stdcall MessageHookProc(int nCode, WPARAM wparam, LPARAM lparam);

using namespace System;

namespace ManagedInjector
{
	public ref class Injector : System::Object
	{
		public:
			static System::String^ currentFilePath;
			static void Launch(System::IntPtr windowHandle, 
				System::String^ assemblyName, 
				System::String^ className, 
				System::String^ methodName,
				System::String^ injectType
				);
			static void LaunchAndConnectToPort(
				System::IntPtr windowHandle,
				int iTcpPort,
				System::String^ assemblyName,
				System::String^ className,
				System::String^ methodName,
				System::String^ injectType);
				
			static int UnLoad(DWORD pid, System::String^ libFile);

			static void LogMessage(System::String^ message, bool append);
			static void SetLogMessagePath(System::String^ strTargetPath);
			static BOOL IsModalWindowAccordingToThisParticularSpec(System::IntPtr hwnd);

			static void LoadMessageCenter();

			static bool IsInjected(System::String^ strProcessName);
			static bool IsInjectedById(int iProcessId);
			static void WriteLogtoFile(System::String^ strMessage);
			static void WriteLogtoFile(const char* m);
			static void LoadLibInject(int iProcessId);
		 
	};
}