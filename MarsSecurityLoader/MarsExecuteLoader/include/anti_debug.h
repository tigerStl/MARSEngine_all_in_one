#ifndef ANTI_DEBUG_H
#define ANTI_DEBUG_H

#include <windows.h>
#include <iostream>

// 1. 检测调试器是否存在（IsDebuggerPresent）
bool CheckDebugger() {
    return IsDebuggerPresent();
}

// 2. 检测父进程是否是调试器（CheckRemoteDebuggerPresent）
bool CheckRemoteDebugger() {
    BOOL isDebuggerPresent = FALSE;
    CheckRemoteDebuggerPresent(GetCurrentProcess(), &isDebuggerPresent);
    return isDebuggerPresent;
}

// 3. 通过 NtQueryInformationProcess 检测调试器
typedef NTSTATUS(NTAPI* pNtQueryInformationProcess)(
    HANDLE, UINT, PVOID, ULONG, PULONG);

bool CheckNtQueryInfo() {
    HMODULE hNtDll = LoadLibraryA("ntdll.dll");
    if (!hNtDll) return false;

    pNtQueryInformationProcess NtQueryInfo =
        (pNtQueryInformationProcess)GetProcAddress(hNtDll, "NtQueryInformationProcess");

    if (!NtQueryInfo) return false;

    DWORD debugPort = 0;
    NTSTATUS status = NtQueryInfo(GetCurrentProcess(), 7, &debugPort, sizeof(DWORD), NULL);
    return (status == 0 && debugPort != 0);
}

// 4. 使用 CloseHandle 反调试（非法关闭一个无效句柄）
bool CheckCloseHandle() {
    __try {
        CloseHandle((HANDLE)0xDEADC0DE);  // 试图关闭无效句柄
        return false;
    }
    __except (EXCEPTION_EXECUTE_HANDLER) {
        return true; // 如果异常被捕获，说明没有调试器
    }
}

// 5. 反调试主函数
void AntiDebugCheck() {
    if (CheckDebugger() || CheckRemoteDebugger() || CheckNtQueryInfo() || !CheckCloseHandle()) {
        std::cerr << "检测到调试器，程序终止！" << std::endl;
        ExitProcess(1);
    }
}

#endif // ANTI_DEBUG_H
