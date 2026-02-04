#pragma once

#include <string>
#include <vector>

#pragma once
class EncryptionSettings {
public:
    std::vector<std::string> Directories; // 存储目录列表
    std::string OutputFile;              // 存储输出文件名

    // 打印数据（用于测试）
    void Print() const {
        printf("Directories:\n");
        for (const auto& dir : Directories) {
            printf("  %s\n", dir.c_str());
        }
        printf("OutputFile: %s\n", OutputFile.c_str());
    }
};

