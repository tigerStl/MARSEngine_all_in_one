using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using LicenseEnCodeAndDecode.Models;
using LicenseEnCodeAndDecode.Services;
using MarsLicenseManager.Services;

namespace MarsLicenseManager
{
    public partial class Form1 : Form
    {
        private readonly LicenseEncryptionService _encryptionService;
        private readonly List<ApplicationInfo> _applications;
        private readonly ConfigurationService _configService;
        private readonly DllEncryptionService _dllEncryptionService;

        public Form1()
        {
            InitializeComponent();
            _encryptionService = new LicenseEncryptionService();
            _applications = new List<ApplicationInfo>();
            _configService = new ConfigurationService();
            _dllEncryptionService = new DllEncryptionService();

            // 加载RSA私钥（用于生成License）
            LoadRsaPrivateKey();

            // 初始化到期日期
            dateTimePickerExpiration.Value = DateTime.Now.AddDays(365);

            // 加载语言设置
            string language = _configService.GetLanguage();
            if (language == "zh-CN")
            {
                comboBoxLanguage.SelectedIndex = 1;
            }
            else
            {
                comboBoxLanguage.SelectedIndex = 0;
            }
            ApplyLanguage(language);
        }

        private void ApplyLanguage(string language)
        {
            // 设置文化
            CultureInfo culture = new CultureInfo(language);
            Thread.CurrentThread.CurrentUICulture = culture;

            // 更新所有控件文本
            this.Text = "MARS License Manager";

            // Basic Info Group
            groupBoxBasic.Text = "Basic Information";
            labelLicenseCount.Text = "License Count:";
            labelValidityDays.Text = "Validity (Days):";
            labelExpirationDate.Text = "Expiration Date:";
            labelCustomerName.Text = "Customer Name:";
            labelNotes.Text = "Notes:";
            labelLanguage.Text = "Language:";

            // MAC Address Group
            groupBoxMac.Text = "MAC Address Management";
            buttonAddMac.Text = "Add MAC Address";
            buttonRemoveMac.Text = "Remove Selected MAC";

            // Application Group
            groupBoxApplication.Text = "Application Restriction";
            checkBoxRestrictApp.Text = "Enable Application Restriction";
            buttonAddApp.Text = "Add Application";
            buttonRemoveApp.Text = "Remove Selected App";

            // Main Buttons
            buttonGenerateLicense.Text = "Generate License File";
            buttonValidateLicense.Text = "Validate License File";
            
            // DLL Encryption Group
            groupBoxDllEncryption.Text = "DLL Encryption/Decryption";
            labelDllFilePath.Text = "DLL File Path:";
            buttonSelectDllFile.Text = "Select DLL";
            buttonEncryptDll.Text = "Encrypt DLL";
            labelEncryptedFilePath.Text = "Encrypted File Path:";
            buttonSelectEncryptedFile.Text = "Select File";
            buttonDecryptDll.Text = "Decrypt to DLL";
            buttonDecryptToStream.Text = "Decrypt to Stream";
        }

        private void NumericUpDownValidityDays_ValueChanged(object? sender, EventArgs e)
        {
            // 自动更新到期日期
            dateTimePickerExpiration.Value = DateTime.Now.AddDays((double)numericUpDownValidityDays.Value);
        }

        private void ComboBoxLanguage_SelectedIndexChanged(object? sender, EventArgs e)
        {
            string language = comboBoxLanguage.SelectedIndex == 1 ? "zh-CN" : "en-US";
            _configService.SetLanguage(language);
            ApplyLanguage(language);
        }

        private void ButtonAddMac_Click(object? sender, EventArgs e)
        {
            string macAddress = textBoxMacAddress.Text.Trim();
            if (string.IsNullOrEmpty(macAddress))
            {
                MessageBox.Show("Please enter a MAC address!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!IsValidMacAddress(macAddress))
            {
                MessageBox.Show("Invalid MAC address format!\nSupported formats:\n- 00-11-22-33-44-55\n- 00:11:22:33:44:55\n- 001122334455", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 检查是否超过许可证数量限制
            if (listBoxMacAddresses.Items.Count >= numericUpDownLicenseCount.Value)
            {
                MessageBox.Show($"MAC address count cannot exceed License count ({numericUpDownLicenseCount.Value})!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 检查是否重复
            if (listBoxMacAddresses.Items.Contains(macAddress))
            {
                MessageBox.Show("This MAC address already exists!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            listBoxMacAddresses.Items.Add(macAddress);
            textBoxMacAddress.Clear();
        }

        private void ButtonRemoveMac_Click(object? sender, EventArgs e)
        {
            if (listBoxMacAddresses.SelectedItem == null)
            {
                MessageBox.Show("Please select a MAC address to remove!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            listBoxMacAddresses.Items.Remove(listBoxMacAddresses.SelectedItem);
        }

        private void CheckBoxRestrictApp_CheckedChanged(object? sender, EventArgs e)
        {
            bool enabled = checkBoxRestrictApp.Checked;
            listBoxApplications.Enabled = enabled;
            buttonAddApp.Enabled = enabled;
            buttonRemoveApp.Enabled = enabled;
        }

        private void ButtonAddApp_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*";
                openFileDialog.Title = "Select Application";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string appPath = openFileDialog.FileName;
                    string appName = Path.GetFileNameWithoutExtension(appPath);

                    // 检查是否重复
                    if (listBoxApplications.Items.Cast<string>().Any(item => item.StartsWith(appName)))
                    {
                        MessageBox.Show("This application already exists!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    listBoxApplications.Items.Add($"{appName} - {appPath}");
                }
            }
        }

        private void ButtonRemoveApp_Click(object? sender, EventArgs e)
        {
            if (listBoxApplications.SelectedItem == null)
            {
                MessageBox.Show("Please select an application to remove!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            listBoxApplications.Items.Remove(listBoxApplications.SelectedItem);
        }

        private void ButtonGenerateLicense_Click(object? sender, EventArgs e)
        {
            try
            {
                // 验证MAC地址数量
                if (listBoxMacAddresses.Items.Count == 0)
                {
                    MessageBox.Show("Please add at least one MAC address!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (listBoxMacAddresses.Items.Count != numericUpDownLicenseCount.Value)
                {
                    var result = MessageBox.Show($"MAC address count ({listBoxMacAddresses.Items.Count}) does not match License count ({numericUpDownLicenseCount.Value}). Continue anyway?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (result != DialogResult.Yes)
                        return;
                }

                // 创建许可证信息
                var licenseInfo = new LicenseInfo
                {
                    LicenseCount = (int)numericUpDownLicenseCount.Value,
                    ExpirationDate = dateTimePickerExpiration.Value,
                    CustomerName = textBoxCustomerName.Text.Trim(),
                    Notes = textBoxNotes.Text.Trim(),
                    MacAddresses = listBoxMacAddresses.Items.Cast<string>().ToList(),
                    RestrictApplication = checkBoxRestrictApp.Checked,
                    Applications = _applications
                };

                // 生成许可证文件
                string licenseContent = _encryptionService.GenerateLicense(licenseInfo);

                // 保存文件
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "License Files (*.lic)|*.lic|All Files (*.*)|*.*";
                    saveFileDialog.Title = "Save License File";
                    saveFileDialog.FileName = $"MARS_License_{DateTime.Now:yyyyMMdd_HHmmss}.lic";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(saveFileDialog.FileName, licenseContent);
                        MessageBox.Show($"License file generated successfully!\nFile: {saveFileDialog.FileName}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating license: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonValidateLicense_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "License Files (*.lic)|*.lic|All Files (*.*)|*.*";
                openFileDialog.Title = "Select License File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string licenseContent = File.ReadAllText(openFileDialog.FileName);
                        var validationResult = _encryptionService.ValidateLicense(licenseContent);

                        string message = $"License Validation Result:\n\n" +
                                       $"Valid: {(validationResult.IsValid ? "Yes" : "No")}\n" +
                                       $"License Count: {validationResult.LicenseCount}\n" +
                                       $"Expiration Date: {validationResult.ExpirationDate:yyyy-MM-dd HH:mm:ss}\n" +
                                       $"Customer Name: {validationResult.CustomerName}\n" +
                                       $"Notes: {validationResult.Notes}\n" +
                                       $"MAC Addresses: {string.Join(", ", validationResult.MacAddresses)}\n" +
                                       $"Restrict Application: {(validationResult.RestrictApplication ? "Yes" : "No")}\n" +
                                       $"Applications: {string.Join(", ", validationResult.Applications.Select(a => a.Name))}\n\n";

                        if (!validationResult.IsValid)
                        {
                            message += $"Error: {validationResult.ErrorMessage}";
                        }

                        MessageBox.Show(message, "License Validation", MessageBoxButtons.OK, validationResult.IsValid ? MessageBoxIcon.Information : MessageBoxIcon.Error);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error validating license: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void LoadRsaPrivateKey()
        {
            try
            {
                string[] possibleKeyPaths = new[]
                {
                    "mars_private.key",
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mars_private.key"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "LicenseEnCodeAndDecode", "mars_private.key")
                };

                foreach (var keyPath in possibleKeyPaths)
                {
                    if (File.Exists(keyPath))
                    {
                        _encryptionService.LoadPrivateKey(keyPath);
                        return;
                    }
                }

                MessageBox.Show("RSA private key file not found. Please ensure mars_private.key exists.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading RSA private key: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool IsValidMacAddress(string macAddress)
        {
            // 支持多种MAC地址格式
            string[] patterns = {
                @"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$",  // 00-11-22-33-44-55 或 00:11:22:33:44:55
                @"^([0-9A-Fa-f]{12})$"  // 001122334455
            };

            return patterns.Any(pattern => Regex.IsMatch(macAddress, pattern));
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        #region DLL Encryption Event Handlers

        private void ButtonSelectDllFile_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "DLL Files (*.dll)|*.dll|All Files (*.*)|*.*";
                openFileDialog.Title = "Select DLL File to Encrypt";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    textBoxDllFilePath.Text = openFileDialog.FileName;
                }
            }
        }

        private void ButtonEncryptDll_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBoxDllFilePath.Text))
            {
                MessageBox.Show("Please select a DLL file first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 加载私钥
                string[] possiblePrivateKeyPaths = new[]
                {
                    "mars_private.key",
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mars_private.key"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "LicenseEnCodeAndDecode", "mars_private.key")
                };

                string? privateKeyPath = null;
                foreach (var path in possiblePrivateKeyPaths)
                {
                    if (File.Exists(path))
                    {
                        privateKeyPath = path;
                        break;
                    }
                }

                if (privateKeyPath == null)
                {
                    MessageBox.Show("Private key file not found. Please ensure mars_private.key exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 创建加密服务实例
                var dllEncryptionService = new DllEncryptionService(privateKeyPath);

                // 选择输出文件
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "Encrypted DLL Files (*.encdll)|*.encdll|All Files (*.*)|*.*";
                    saveFileDialog.Title = "Save Encrypted DLL File";
                    saveFileDialog.FileName = Path.GetFileNameWithoutExtension(textBoxDllFilePath.Text) + ".encdll";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        // 加密DLL文件
                        bool success = dllEncryptionService.EncryptDllFile(textBoxDllFilePath.Text, saveFileDialog.FileName);

                        if (success)
                        {
                            MessageBox.Show($"DLL file encrypted successfully!\nOutput: {saveFileDialog.FileName}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("Failed to encrypt DLL file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error encrypting DLL file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonSelectEncryptedFile_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Encrypted DLL Files (*.encdll)|*.encdll|All Files (*.*)|*.*";
                openFileDialog.Title = "Select Encrypted File to Decrypt";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    textBoxEncryptedFilePath.Text = openFileDialog.FileName;
                }
            }
        }

        private void ButtonDecryptDll_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBoxEncryptedFilePath.Text))
            {
                MessageBox.Show("Please select an encrypted file first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 加载公钥
                string[] possiblePublicKeyPaths = new[]
                {
                    "mars_public.key",
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mars_public.key"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "LicenseEnCodeAndDecode", "mars_public.key")
                };

                string? publicKeyPath = null;
                foreach (var path in possiblePublicKeyPaths)
                {
                    if (File.Exists(path))
                    {
                        publicKeyPath = path;
                        break;
                    }
                }

                if (publicKeyPath == null)
                {
                    MessageBox.Show("Public key file not found. Please ensure mars_public.key exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 创建解密服务实例
                var dllDecryptionService = new DllEncryptionService(publicKeyPath, true);

                // 选择输出文件
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "DLL Files (*.dll)|*.dll|All Files (*.*)|*.*";
                    saveFileDialog.Title = "Save Decrypted DLL File";
                    saveFileDialog.FileName = Path.GetFileNameWithoutExtension(textBoxEncryptedFilePath.Text) + "_decrypted.dll";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        // 解密DLL文件
                        bool success = dllDecryptionService.DecryptDllFile(textBoxEncryptedFilePath.Text, saveFileDialog.FileName);

                        if (success)
                        {
                            MessageBox.Show($"DLL file decrypted successfully!\nOutput: {saveFileDialog.FileName}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("Failed to decrypt DLL file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error decrypting DLL file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonDecryptToStream_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBoxEncryptedFilePath.Text))
            {
                MessageBox.Show("Please select an encrypted file first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 加载公钥
                string[] possiblePublicKeyPaths = new[]
                {
                    "mars_public.key",
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mars_public.key"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "LicenseEnCodeAndDecode", "mars_public.key")
                };

                string? publicKeyPath = null;
                foreach (var path in possiblePublicKeyPaths)
                {
                    if (File.Exists(path))
                    {
                        publicKeyPath = path;
                        break;
                    }
                }

                if (publicKeyPath == null)
                {
                    MessageBox.Show("Public key file not found. Please ensure mars_public.key exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 创建解密服务实例
                var dllDecryptionService = new DllEncryptionService(publicKeyPath, true);

                // 解密到流
                byte[] decryptedDllBytes = dllDecryptionService.DecryptDllToStream(textBoxEncryptedFilePath.Text);

                // 显示解密结果信息
                string message = $"DLL decrypted to stream successfully!\n" +
                               $"Original size: {decryptedDllBytes.Length} bytes\n" +
                               $"The decrypted DLL data is ready for use in memory.";

                MessageBox.Show(message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // 可选：将解密的数据保存到临时文件供用户查看
                var result = MessageBox.Show("Would you like to save the decrypted DLL to a file?", "Save Decrypted DLL", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                    {
                        saveFileDialog.Filter = "DLL Files (*.dll)|*.dll|All Files (*.*)|*.*";
                        saveFileDialog.Title = "Save Decrypted DLL File";
                        saveFileDialog.FileName = Path.GetFileNameWithoutExtension(textBoxEncryptedFilePath.Text) + "_decrypted.dll";

                        if (saveFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            File.WriteAllBytes(saveFileDialog.FileName, decryptedDllBytes);
                            MessageBox.Show($"Decrypted DLL saved to: {saveFileDialog.FileName}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error decrypting DLL to stream: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion
    }
}
