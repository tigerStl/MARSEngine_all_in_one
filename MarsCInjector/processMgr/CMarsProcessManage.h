#pragma once
#include <string>
#include <Windows.h>
#pragma comment(lib,"shlwapi.lib") 

class CMarsProcessManage
{
public:
	CMarsProcessManage(char* pstrProcessName);
	~CMarsProcessManage();

	bool m_enumProcess();
	bool m_injectDllToPid();

	std::string  m_strCurrrentError;
	std::string m_getCurrentProcessName();
	std::string m_strInjectDll;
private:
	std::string m_strCurrentProcessName;

	std::string  m_getProcessName(DWORD pid);
	DWORD m_dwTargetProcessId;
	
	char m_arrCurrentDirectory[MAX_PATH];
};

