#include <windows.h>
#include <vector>
#include <iostream>
#include <fstream>
#include <openssl/evp.h>
#include <nlohmann/json.hpp>
#include <metahost.h>
#pragma comment(lib, "mscoree.lib")

using json = nlohmann::json;

std::vector<unsigned char> readFile(const std::string& filename) {
    std::ifstream file(filename, std::ios::binary);
    return { std::istreambuf_iterator<char>(file), std::istreambuf_iterator<char>() };
}

bool aesDecrypt(const std::vector<unsigned char>& ciphertext, std::vector<unsigned char>& plaintext, const unsigned char* key, const unsigned char* iv) {
    EVP_CIPHER_CTX* ctx = EVP_CIPHER_CTX_new();
    if (!ctx) return false;

    int len;
    int plaintext_len;
    plaintext.resize(ciphertext.size());

    EVP_DecryptInit_ex(ctx, EVP_aes_256_cbc(), NULL, key, iv);
    EVP_DecryptUpdate(ctx, plaintext.data(), &len, ciphertext.data(), ciphertext.size());
    plaintext_len = len;
    EVP_DecryptFinal_ex(ctx, plaintext.data() + len, &len);
    plaintext_len += len;
    plaintext.resize(plaintext_len);

    EVP_CIPHER_CTX_free(ctx);
    return true;
}

void runPEFromMemory(const std::vector<unsigned char>& exeData) {
    PIMAGE_DOS_HEADER dosHeader = (PIMAGE_DOS_HEADER)exeData.data();
    PIMAGE_NT_HEADERS ntHeaders = (PIMAGE_NT_HEADERS)(exeData.data() + dosHeader->e_lfanew);

    LPVOID exeMem = VirtualAlloc(NULL, ntHeaders->OptionalHeader.SizeOfImage, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
    if (!exeMem) {
        std::cerr << "Memory allocation failed" << std::endl;
        return;
    }

    memcpy(exeMem, exeData.data(), ntHeaders->OptionalHeader.SizeOfHeaders);
    PIMAGE_SECTION_HEADER section = IMAGE_FIRST_SECTION(ntHeaders);
    for (int i = 0; i < ntHeaders->FileHeader.NumberOfSections; ++i, ++section) {
        memcpy((LPVOID)((uintptr_t)exeMem + section->VirtualAddress), exeData.data() + section->PointerToRawData, section->SizeOfRawData);
    }

    LPVOID entryPoint = (LPVOID)((uintptr_t)exeMem + ntHeaders->OptionalHeader.AddressOfEntryPoint);
    ((void(*)())entryPoint)();
}
/// <summary>
/// 
/// </summary>
bool executeInMemory(const std::vector<unsigned char>& exeData) {
    // 创建内存映射文件
    HANDLE hMapping = CreateFileMapping(INVALID_HANDLE_VALUE, nullptr, PAGE_READWRITE, 0, exeData.size(), nullptr);
    if (!hMapping) {
        std::cerr << "CreateFileMapping failed. Error code: " << GetLastError() << std::endl;
        return false;
    }

    // 将文件映射到内存
    void* mappedMemory = MapViewOfFile(hMapping, FILE_MAP_ALL_ACCESS, 0, 0, exeData.size());
    if (!mappedMemory) {
        std::cerr << "MapViewOfFile failed. Error code: " << GetLastError() << std::endl;
        CloseHandle(hMapping);
        return false;
    }

    // 将 exe 数据复制到映射的内存区域
    memcpy(mappedMemory, exeData.data(), exeData.size());

    // 创建进程结构体
    STARTUPINFOA startupInfo = { sizeof(startupInfo) };
    PROCESS_INFORMATION processInfo;

    // 使用 CreateProcess 来执行内存中的程序
    BOOL success = CreateProcessA(
        nullptr,                // 应用程序路径（NULL 表示使用内存中的映像）
        reinterpret_cast<LPSTR>(mappedMemory), // 命令行参数
        nullptr,                // 安全属性
        nullptr,                // 安全属性
        FALSE,                  // 是否继承句柄
        CREATE_NEW_CONSOLE,     // 创建新的控制台
        nullptr,                // 环境变量
        nullptr,                // 当前目录
        &startupInfo,           // 启动信息
        &processInfo            // 进程信息
    );

    if (!success) {
        std::cerr << "CreateProcess failed. Error code: " << GetLastError() << std::endl;
        UnmapViewOfFile(mappedMemory);
        CloseHandle(hMapping);
        return false;
    }

    // 等待进程完成
    WaitForSingleObject(processInfo.hProcess, INFINITE);

    // 清理
    CloseHandle(processInfo.hProcess);
    CloseHandle(processInfo.hThread);
    UnmapViewOfFile(mappedMemory);
    CloseHandle(hMapping);

    return true;
}



void saveVectorToFile(const std::vector<unsigned char>& data, const std::string& filePath) {
    // 创建输出文件流，打开指定路径的文件
    std::ofstream outFile(filePath, std::ios::binary);

    if (!outFile) {
        std::cerr << "Error opening file for writing: " << filePath << std::endl;
        return;
    }

    // 将 std::vector 的数据写入文件
    outFile.write(reinterpret_cast<const char*>(data.data()), data.size());

    if (!outFile) {
        std::cerr << "Error writing to file: " << filePath << std::endl;
    }

    outFile.close();
}

std::vector<unsigned char> readFileToMemory(const std::string& filePath) {
    std::ifstream file(filePath, std::ios::binary);

    if (!file.is_open()) {
        std::cerr << "Failed to open file: " << filePath << std::endl;
        return {};
    }

    // 获取文件大小
    file.seekg(0, std::ios::end);
    size_t fileSize = file.tellg();
    file.seekg(0, std::ios::beg);

    // 读取文件内容到内存
    std::vector<unsigned char> buffer(fileSize);
    file.read(reinterpret_cast<char*>(buffer.data()), fileSize);
    file.close();

    return buffer;
}

// 使用虚拟内存模式
// 将内存中的数据写入虚拟内存并执行
bool ExecuteInVirtualMemory(const std::vector<unsigned char>& exeData) {
    // 分配内存空间
    LPVOID pMemory = VirtualAlloc(NULL, exeData.size(), MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
    if (!pMemory) {
        std::cerr << "VirtualAlloc failed. Error: " << GetLastError() << std::endl;
        return false;
    }

    // 将数据从内存中复制到分配的内存
    memcpy(pMemory, exeData.data(), exeData.size());

    // 使用 CreateThread 创建新线程并执行程序
    HANDLE hThread = CreateThread(
        NULL,                  // 默认安全属性
        0,                     // 默认堆栈大小
        (LPTHREAD_START_ROUTINE)pMemory, // 入口点
        NULL,                  // 参数
        0,                     // 默认创建状态
        NULL                   // 不需要线程ID
    );

    if (hThread == NULL) {
        std::cerr << "CreateThread failed. Error: " << GetLastError() << std::endl;
        VirtualFree(pMemory, 0, MEM_RELEASE);
        return false;
    }

    // 等待线程执行完成
    WaitForSingleObject(hThread, INFINITE);

    // 清理
    CloseHandle(hThread);
    VirtualFree(pMemory, 0, MEM_RELEASE);

    return true;
}

HRESULT LoadCLR() {
    ICLRMetaHost* pMetaHost = nullptr;
    ICLRRuntimeInfo* pRuntimeInfo = nullptr;
    ICLRRuntimeHost* pRuntimeHost = nullptr;

    HRESULT hr = CLRCreateInstance(CLSID_CLRMetaHost, IID_PPV_ARGS(&pMetaHost));
    if (FAILED(hr)) return hr;

    hr = pMetaHost->GetRuntime(L"v4.0.30319", IID_PPV_ARGS(&pRuntimeInfo));
    if (FAILED(hr)) return hr;

    BOOL bLoadable;
    hr = pRuntimeInfo->IsLoadable(&bLoadable);
    if (FAILED(hr) || !bLoadable) return E_FAIL;

    hr = pRuntimeInfo->GetInterface(CLSID_CLRRuntimeHost, IID_PPV_ARGS(&pRuntimeHost));
    if (FAILED(hr)) return hr;

    hr = pRuntimeHost->Start();

    //pRuntimeInfo->ExecuteInDefaultAppDomain();
    return hr;
}

// 在内存中加载并运行 PE 文件 from deep seek
void executePEInMemory(const std::vector<unsigned char>& peData) {
    // 解析 PE 文件头
    PIMAGE_DOS_HEADER dosHeader = (PIMAGE_DOS_HEADER)peData.data();
    if (dosHeader->e_magic != IMAGE_DOS_SIGNATURE) {
        throw std::runtime_error("Invalid PE file: DOS header signature mismatch");
    }

    PIMAGE_NT_HEADERS ntHeaders = (PIMAGE_NT_HEADERS)(peData.data() + dosHeader->e_lfanew);
    if (ntHeaders->Signature != IMAGE_NT_SIGNATURE) {
        throw std::runtime_error("Invalid PE file: NT header signature mismatch");
    }

    // 分配内存
    void* baseAddress = VirtualAlloc(
        (LPVOID)ntHeaders->OptionalHeader.ImageBase,
        ntHeaders->OptionalHeader.SizeOfImage,
        MEM_RESERVE | MEM_COMMIT,
        PAGE_EXECUTE_READWRITE
    );
    if (!baseAddress) {
        baseAddress = VirtualAlloc(
            NULL,
            ntHeaders->OptionalHeader.SizeOfImage,
            MEM_RESERVE | MEM_COMMIT,
            PAGE_EXECUTE_READWRITE
        );
        if (!baseAddress) {
            throw std::runtime_error("Failed to allocate memory for PE file");
        }
    }

    // 复制 PE 头
    memcpy(baseAddress, peData.data(), ntHeaders->OptionalHeader.SizeOfHeaders);

    // 复制节区
    PIMAGE_SECTION_HEADER sectionHeader = IMAGE_FIRST_SECTION(ntHeaders);
    for (int i = 0; i < ntHeaders->FileHeader.NumberOfSections; i++) {
        void* sectionDestination = (void*)((uintptr_t)baseAddress + sectionHeader[i].VirtualAddress);
        const void* sectionSource = peData.data() + sectionHeader[i].PointerToRawData;
        memcpy(sectionDestination, sectionSource, sectionHeader[i].SizeOfRawData);
    }

    // 修复重定位
    if (ntHeaders->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_BASERELOC].Size > 0) {
        PIMAGE_BASE_RELOCATION relocation = (PIMAGE_BASE_RELOCATION)(
            (uintptr_t)baseAddress + ntHeaders->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_BASERELOC].VirtualAddress
            );
        while (relocation->VirtualAddress) {
            uintptr_t relocationBase = (uintptr_t)baseAddress + relocation->VirtualAddress;
            uint16_t* relocInfo = (uint16_t*)(relocation + 1);
            int numRelocs = (relocation->SizeOfBlock - sizeof(IMAGE_BASE_RELOCATION)) / sizeof(uint16_t);

            for (int i = 0; i < numRelocs; i++) {
                if ((relocInfo[i] >> 12) == IMAGE_REL_BASED_HIGHLOW) {
                    uintptr_t* patchAddress = (uintptr_t*)(relocationBase + (relocInfo[i] & 0xFFF));
                    *patchAddress += (uintptr_t)baseAddress - ntHeaders->OptionalHeader.ImageBase;
                }
            }

            relocation = (PIMAGE_BASE_RELOCATION)((uintptr_t)relocation + relocation->SizeOfBlock);
        }
    }

    // 设置内存权限
    for (int i = 0; i < ntHeaders->FileHeader.NumberOfSections; i++) {
        void* sectionAddress = (void*)((uintptr_t)baseAddress + sectionHeader[i].VirtualAddress);
        DWORD protect = 0;
        DWORD sectionCharacteristics = sectionHeader[i].Characteristics;

        if (sectionCharacteristics & IMAGE_SCN_MEM_EXECUTE) {
            protect = (sectionCharacteristics & IMAGE_SCN_MEM_WRITE) ? PAGE_EXECUTE_READWRITE : PAGE_EXECUTE_READ;
        }
        else if (sectionCharacteristics & IMAGE_SCN_MEM_READ) {
            protect = (sectionCharacteristics & IMAGE_SCN_MEM_WRITE) ? PAGE_READWRITE : PAGE_READONLY;
        }
        else {
            protect = PAGE_NOACCESS;
        }

        DWORD oldProtect;
        VirtualProtect(sectionAddress, sectionHeader[i].Misc.VirtualSize, protect, &oldProtect);
    }

    // 执行入口点
    void(*entryPoint)() = (void(*)())((uintptr_t)baseAddress + ntHeaders->OptionalHeader.AddressOfEntryPoint);
    entryPoint();

    // 释放内存（通常不会执行到这里）
    VirtualFree(baseAddress, 0, MEM_RELEASE);
}

void executeManagedEXEInMemory(const std::vector<unsigned char>& peData) {
    // 初始化 CLR
    ICLRMetaHost* metaHost = nullptr;
    ICLRRuntimeInfo* runtimeInfo = nullptr;
    ICLRRuntimeHost* runtimeHost = nullptr;

    if (CLRCreateInstance(CLSID_CLRMetaHost, IID_PPV_ARGS(&metaHost)) != S_OK) {
        throw std::runtime_error("Failed to create CLR meta host");
    }

    if (metaHost->GetRuntime(L"v4.0.30319", IID_PPV_ARGS(&runtimeInfo)) != S_OK) {
        metaHost->Release();
        throw std::runtime_error("Failed to get CLR runtime info");
    }

    BOOL isLoadable;
    if (runtimeInfo->IsLoadable(&isLoadable) != S_OK || !isLoadable) {
        runtimeInfo->Release();
        metaHost->Release();
        throw std::runtime_error("CLR runtime is not loadable");
    }

    if (runtimeInfo->GetInterface(CLSID_CLRRuntimeHost, IID_PPV_ARGS(&runtimeHost)) != S_OK) {
        runtimeInfo->Release();
        metaHost->Release();
        throw std::runtime_error("Failed to get CLR runtime host");
    }

    if (runtimeHost->Start() != S_OK) {
        runtimeHost->Release();
        runtimeInfo->Release();
        metaHost->Release();
        throw std::runtime_error("Failed to start CLR runtime");
    }

    // 加载程序集
    DWORD returnValue;
    HRESULT exeHR = runtimeHost->ExecuteInDefaultAppDomain(
        (LPCWSTR)peData.data(), // 程序集数据
        nullptr,                // 类型名称（null 表示使用入口程序集）
        L"Main",                // 方法名称
        L"",                    // 参数
        &returnValue            // 返回值
    );
    if (exeHR != S_OK) {
        runtimeHost->Release();
        runtimeInfo->Release();
        metaHost->Release();
        throw std::runtime_error("Failed to execute managed EXE in memory");
    }

    // 释放资源
    runtimeHost->Release();
    runtimeInfo->Release();
    metaHost->Release();
}


int main() {
    std::ifstream configFile("appsettings.json");
    json config;
    configFile >> config;

    std::string outputDir = config["EncryptionSettings"]["Directories"][1];
    std::string encryptedFile = outputDir + "\\" + config["EncryptionSettings"]["OutputFile"].get<std::string>();
    std::string keyIVFile = config["EncryptionSettings"]["keyIVFile"];

    std::ifstream keyFile(keyIVFile);
    std::string keyStr, ivStr;
    std::getline(keyFile, keyStr);
    std::getline(keyFile, ivStr);

    unsigned char key[32] = { 0 };
    unsigned char iv[16] = { 0 };
    memcpy(key, keyStr.data(), keyStr.size() > 32 ? 32 : keyStr.size());
    memcpy(iv, ivStr.data(), ivStr.size() > 16 ? 16 : ivStr.size());

    std::vector<unsigned char> encData = readFile(encryptedFile);
    std::vector<unsigned char> decData;

    if (!aesDecrypt(encData, decData, key, iv)) {
        std::cerr << "Decryption failed" << std::endl;
        return -1;
    }
    saveVectorToFile(decData, outputDir + "\\tmp.exe");

    // for test
    // 读取 .exe 文件到内存
    const std::string exeFilePath = "H:\\tiger\\automationTest\\Automation Workbooks\\MARSEncoded\\ToBeEncoded\\EncodedDemoExe.exe";
    std::vector<unsigned char> exeData = readFileToMemory(exeFilePath);
    //ExecuteInVirtualMemory(exeData);
    //executePEInMemory(exeData);
    // 
    executeManagedEXEInMemory(exeData);
    // test end
    executeInMemory(decData);
    runPEFromMemory(decData);
    return 0;
}
