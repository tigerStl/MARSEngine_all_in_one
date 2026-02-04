using System;
using System.Windows;
using System.Windows.Media;
using NLog;

namespace MarsSpyTool.Utility.License
{
    /// <summary>
    /// License 激活窗口
    /// </summary>
    public partial class LicenseActivationWindow : Window
    {
        private static readonly Logger logger = LogManager.GetLogger("MarsSpyLog");
        private MarsLicenseManager _licenseManager;

        public LicenseActivationWindow()
        {
            InitializeComponent();
            _licenseManager = MarsLicenseManager.Instance;
            LoadCurrentLicenseInfo();
            LoadMachineInfo();
        }

        /// <summary>
        /// 加载机器信息
        /// </summary>
        private void LoadMachineInfo()
        {
            try
            {
                HardwareIdTextBox.Text = MarsLicenseManager.GetHardwareId();
                UsernameTextBox.Text = Environment.UserName;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "LoadMachineInfo\tException");
                MessageBox.Show($"获取机器信息失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 加载当前 License 信息
        /// </summary>
        private void LoadCurrentLicenseInfo()
        {
            try
            {
                var currentLicense = _licenseManager.CurrentLicense;
                
                if (currentLicense == null)
                {
                    LicensedToText.Text = "未激活";
                    LicenseTypeText.Text = "N/A";
                    ActivationStatusText.Text = "未激活";
                    ActivationStatusText.Foreground = Brushes.Red;
                    ActivationDateText.Text = "N/A";
                    ExpirationDateText.Text = "N/A";
                    RemainingDaysText.Text = "N/A";
                    return;
                }

                LicensedToText.Text = currentLicense.LicensedTo;
                LicenseTypeText.Text = GetLicenseTypeDisplayName(currentLicense.Type);

                // 激活状态
                if (currentLicense.IsActivated && currentLicense.IsValid())
                {
                    ActivationStatusText.Text = "已激活";
                    ActivationStatusText.Foreground = Brushes.Green;
                }
                else if (currentLicense.IsActivated && !currentLicense.IsValid())
                {
                    ActivationStatusText.Text = "已过期";
                    ActivationStatusText.Foreground = Brushes.Red;
                }
                else
                {
                    ActivationStatusText.Text = "未激活";
                    ActivationStatusText.Foreground = Brushes.Orange;
                }

                // 激活日期
                ActivationDateText.Text = currentLicense.IsActivated 
                    ? currentLicense.ActivationDate.ToString("yyyy-MM-dd HH:mm:ss") 
                    : "未激活";

                // 过期日期
                if (currentLicense.Type == LicenseType.Perpetual)
                {
                    ExpirationDateText.Text = "永久";
                    ExpirationDateText.Foreground = Brushes.Green;
                    RemainingDaysText.Text = "永久";
                    RemainingDaysText.Foreground = Brushes.Green;
                }
                else
                {
                    ExpirationDateText.Text = currentLicense.ExpirationDate.ToString("yyyy-MM-dd");
                    
                    int remainingDays = currentLicense.GetRemainingDays();
                    RemainingDaysText.Text = $"{remainingDays} 天";
                    
                    if (remainingDays <= 0)
                    {
                        RemainingDaysText.Foreground = Brushes.Red;
                    }
                    else if (remainingDays <= 7)
                    {
                        RemainingDaysText.Foreground = Brushes.Orange;
                    }
                    else
                    {
                        RemainingDaysText.Foreground = Brushes.Green;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "LoadCurrentLicenseInfo\tException");
                MessageBox.Show($"加载 License 信息失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 获取 License 类型显示名称
        /// </summary>
        private string GetLicenseTypeDisplayName(LicenseType type)
        {
            switch (type)
            {
                case LicenseType.Trial:
                    return "试用版";
                case LicenseType.Standard:
                    return "标准版";
                case LicenseType.Professional:
                    return "专业版";
                case LicenseType.Enterprise:
                    return "企业版";
                case LicenseType.Perpetual:
                    return "永久版";
                default:
                    return "未知";
            }
        }

        /// <summary>
        /// 激活按钮点击事件
        /// </summary>
        private void ActivateButton_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("ActivateButton_Click\tBegin");
            try
            {
                string licenseKey = LicenseKeyTextBox.Text.Trim();
                
                if (string.IsNullOrWhiteSpace(licenseKey))
                {
                    MessageBox.Show("请输入 License Key", "提示", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    LicenseKeyTextBox.Focus();
                    return;
                }

                // 激活
                string errorMessage = "";
                bool success = _licenseManager.ActivateLicense(licenseKey, ref errorMessage);
                
                if (success)
                {
                    logger.Info("ActivateButton_Click\tActivation successful");
                    MessageBox.Show(
                        "License 激活成功！\n\n" +
                        $"类型: {GetLicenseTypeDisplayName(_licenseManager.CurrentLicense.Type)}\n" +
                        $"授权给: {_licenseManager.CurrentLicense.LicensedTo}\n" +
                        $"过期日期: {(_licenseManager.CurrentLicense.Type == LicenseType.Perpetual ? "永久" : _licenseManager.CurrentLicense.ExpirationDate.ToString("yyyy-MM-dd"))}",
                        "激活成功",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    
                    // 刷新显示
                    LoadCurrentLicenseInfo();
                    LicenseKeyTextBox.Clear();
                }
                else
                {
                    logger.Error($"ActivateButton_Click\tActivation failed: {errorMessage}");
                    MessageBox.Show(
                        $"License 激活失败:\n\n{errorMessage}\n\n" +
                        "请检查:\n" +
                        "1. License Key 是否正确\n" +
                        "2. License 是否已被使用\n" +
                        "3. 是否超过最大激活次数\n\n" +
                        "如有疑问，请联系技术支持。",
                        "激活失败",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"ActivateButton_Click\tException: {ex.Message}");
                MessageBox.Show($"激活过程出现异常:\n{ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                logger.Info("ActivateButton_Click\tEnd");
            }
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

