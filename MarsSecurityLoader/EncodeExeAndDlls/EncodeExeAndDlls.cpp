#include <windows.h>
#include <vector>
#include <iostream>
#include <fstream>
#include <filesystem>
#include <openssl/evp.h>
#include "json.hpp"

using json = nlohmann::json;
namespace fs = std::filesystem;

std::vector<unsigned char> readFile(const std::string& filename) {
    std::ifstream file(filename, std::ios::binary);
    return { std::istreambuf_iterator<char>(file), std::istreambuf_iterator<char>() };
}

bool aesEncrypt(const std::vector<unsigned char>& plaintext, std::vector<unsigned char>& ciphertext, const unsigned char* key, const unsigned char* iv) {
    EVP_CIPHER_CTX* ctx = EVP_CIPHER_CTX_new();
    if (!ctx) return false;

    int len;
    int ciphertext_len;
    ciphertext.resize(plaintext.size() + EVP_MAX_BLOCK_LENGTH);

    EVP_EncryptInit_ex(ctx, EVP_aes_256_cbc(), NULL, key, iv);
    EVP_EncryptUpdate(ctx, ciphertext.data(), &len, plaintext.data(), plaintext.size());
    ciphertext_len = len;
    EVP_EncryptFinal_ex(ctx, ciphertext.data() + len, &len);
    ciphertext_len += len;
    ciphertext.resize(ciphertext_len);

    EVP_CIPHER_CTX_free(ctx);
    return true;
}

void encryptFiles(const std::string& inputDir, const std::string& outputFile, const unsigned char* key, const unsigned char* iv) {
    std::vector<unsigned char> exeData;
    std::vector<std::pair<std::string, std::vector<unsigned char>>> dllDataList;

    for (const auto& entry : fs::directory_iterator(inputDir)) {
        if (entry.is_regular_file()) {
            std::vector<unsigned char> fileData = readFile(entry.path().string());
            if (entry.path().extension() == ".exe") {
                exeData = fileData;
            }
            else if (entry.path().extension() == ".dll") {
                dllDataList.emplace_back(entry.path().string(), fileData);
            }
        }
    }

    if (exeData.empty()) {
        std::cerr << "No EXE file found in input directory!" << std::endl;
        return;
    }

    std::vector<unsigned char> mergedData;
    mergedData.insert(mergedData.end(), exeData.begin(), exeData.end());
    for (const auto& [filename, dllData] : dllDataList) {
        mergedData.insert(mergedData.end(), dllData.begin(), dllData.end());
    }

    std::vector<unsigned char> encryptedData;
    if (!aesEncrypt(mergedData, encryptedData, key, iv)) {
        std::cerr << "Encryption failed!" << std::endl;
        return;
    }

    std::ofstream outFile(outputFile, std::ios::binary);
    outFile.write(reinterpret_cast<const char*>(encryptedData.data()), encryptedData.size());
    outFile.close();
}

int main() {
    std::ifstream configFile("appsettings.json");
    json config;
    configFile >> config;

    std::string inputDir = config["EncryptionSettings"]["Directories"][0];
    std::string outputDir = config["EncryptionSettings"]["Directories"][1];
    std::string outputFile = outputDir + "\\" + config["EncryptionSettings"]["OutputFile"].get<std::string>();
    std::string keyIVFile = config["EncryptionSettings"]["keyIVFile"];

    std::ifstream keyFile(keyIVFile);
    std::string keyStr, ivStr;
    std::getline(keyFile, keyStr);
    std::getline(keyFile, ivStr);

    unsigned char key[32] = { 0 };
    unsigned char iv[16] = { 0 };
    memcpy(key, keyStr.data(), keyStr.size() > 32 ? 32 : keyStr.size());
    memcpy(iv, ivStr.data(), ivStr.size() > 16 ? 16 : ivStr.size());

    encryptFiles(inputDir, outputFile, key, iv);

    return 0;
}
