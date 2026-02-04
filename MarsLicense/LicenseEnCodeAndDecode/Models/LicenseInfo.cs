using System;
using System.Collections.Generic;

namespace LicenseEnCodeAndDecode.Models
{
    /// <summary>
    /// License信息模型
    /// </summary>
    public class LicenseInfo
    {
        /// <summary>
        /// 许可证数量
        /// </summary>
        public int LicenseCount { get; set; }

        /// <summary>
        /// 有效期限（天数）
        /// </summary>
        public int ValidityDays { get; set; }

        /// <summary>
        /// 过期日期
        /// </summary>
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// MAC地址列表
        /// </summary>
        public List<string> MacAddresses { get; set; } = new List<string>();

        /// <summary>
        /// 是否限定应用程序
        /// </summary>
        public bool RestrictApplication { get; set; }

        /// <summary>
        /// 应用程序信息列表
        /// </summary>
        public List<ApplicationInfo> Applications { get; set; } = new List<ApplicationInfo>();

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// 客户名称（可选）
        /// </summary>
        public string? CustomerName { get; set; }

        /// <summary>
        /// 备注信息（可选）
        /// </summary>
        public string? Notes { get; set; }
    }

    /// <summary>
    /// 应用程序信息
    /// </summary>
    public class ApplicationInfo
    {
        /// <summary>
        /// 应用程序名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 应用程序路径
        /// </summary>
        public string ExePath { get; set; } = string.Empty;

        /// <summary>
        /// 文件版本
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// 文件MD5哈希
        /// </summary>
        public string? FileHash { get; set; }

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }
    }
}
