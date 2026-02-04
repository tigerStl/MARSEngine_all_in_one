using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarsSpyTool.Utility.License
{
    /// <summary>
    /// License 类型枚举
    /// </summary>
    public enum LicenseType
    {
        /// <summary>试用版（30天）</summary>
        Trial = 0,
        /// <summary>标准版</summary>
        Standard = 1,
        /// <summary>专业版</summary>
        Professional = 2,
        /// <summary>企业版</summary>
        Enterprise = 3,
        /// <summary>永久版</summary>
        Perpetual = 99
    }

    /// <summary>
    /// License 功能权限枚举
    /// </summary>
    [Flags]
    public enum LicenseFeatures
    {
        None = 0,
        /// <summary>基础对象识别</summary>
        BasicObjectSpy = 1 << 0,
        /// <summary>单对象模式</summary>
        SingleObjectMode = 1 << 1,
        /// <summary>自动生成测试用例</summary>
        AutoGenerateTestCase = 1 << 2,
        /// <summary>录制回放</summary>
        RecordReplay = 1 << 3,
        /// <summary>多数据库支持</summary>
        MultiDatabase = 1 << 4,
        /// <summary>高级对象识别（Java/Qt）</summary>
        AdvancedObjectRecognition = 1 << 5,
        /// <summary>批量操作</summary>
        BatchOperation = 1 << 6,
        /// <summary>云端同步</summary>
        CloudSync = 1 << 7,
        /// <summary>全部功能</summary>
        All = ~0
    }

    /// <summary>
    /// License 信息类
    /// </summary>
    [Serializable]
    public class MarsLicenseInfo
    {
        /// <summary>
        /// License 密钥（加密后的）
        /// </summary>
        public string LicenseKey { get; set; }

        /// <summary>
        /// 用户名/公司名
        /// </summary>
        public string LicensedTo { get; set; }

        /// <summary>
        /// License 类型
        /// </summary>
        public LicenseType Type { get; set; }

        /// <summary>
        /// 激活日期
        /// </summary>
        public DateTime ActivationDate { get; set; }

        /// <summary>
        /// 过期日期
        /// </summary>
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// 功能权限（位标志）
        /// </summary>
        public LicenseFeatures Features { get; set; }

        /// <summary>
        /// 最大并发用户数
        /// </summary>
        public int MaxConcurrentUsers { get; set; }

        /// <summary>
        /// 硬件ID（用于机器绑定）
        /// </summary>
        public string HardwareId { get; set; }

        /// <summary>
        /// 是否已激活
        /// </summary>
        public bool IsActivated { get; set; }

        /// <summary>
        /// 激活次数（限制激活次数）
        /// </summary>
        public int ActivationCount { get; set; }

        /// <summary>
        /// 最大激活次数
        /// </summary>
        public int MaxActivations { get; set; }

        /// <summary>
        /// 版本限制（仅支持特定版本）
        /// </summary>
        public string SupportedVersions { get; set; }

        /// <summary>
        /// 扩展属性（JSON格式，用于未来扩展）
        /// </summary>
        public string ExtendedProperties { get; set; }

        /// <summary>
        /// 数字签名（防篡改）
        /// </summary>
        public string Signature { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public MarsLicenseInfo()
        {
            Type = LicenseType.Trial;
            Features = LicenseFeatures.BasicObjectSpy;
            MaxConcurrentUsers = 1;
            MaxActivations = 1;
            IsActivated = false;
            ActivationCount = 0;
        }

        /// <summary>
        /// 检查 License 是否有效
        /// </summary>
        public bool IsValid()
        {
            if (!IsActivated) return false;
            if (DateTime.Now > ExpirationDate) return false;
            if (Type != LicenseType.Perpetual && (ExpirationDate - DateTime.Now).TotalDays <= 0)
                return false;
            return true;
        }

        /// <summary>
        /// 检查 License 是否即将过期（7天内）
        /// </summary>
        public bool IsExpiringSoon()
        {
            if (Type == LicenseType.Perpetual) return false;
            return (ExpirationDate - DateTime.Now).TotalDays <= 7 && (ExpirationDate - DateTime.Now).TotalDays > 0;
        }

        /// <summary>
        /// 获取剩余天数
        /// </summary>
        public int GetRemainingDays()
        {
            if (Type == LicenseType.Perpetual) return int.MaxValue;
            return Math.Max(0, (int)(ExpirationDate - DateTime.Now).TotalDays);
        }

        /// <summary>
        /// 检查是否有某个功能的权限
        /// </summary>
        public bool HasFeature(LicenseFeatures feature)
        {
            return (Features & feature) == feature;
        }
    }
}

