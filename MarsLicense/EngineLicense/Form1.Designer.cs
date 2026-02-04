namespace MarsLicenseManager;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.groupBoxBasic = new System.Windows.Forms.GroupBox();
        this.numericUpDownLicenseCount = new System.Windows.Forms.NumericUpDown();
        this.numericUpDownValidityDays = new System.Windows.Forms.NumericUpDown();
        this.dateTimePickerExpiration = new System.Windows.Forms.DateTimePicker();
        this.textBoxCustomerName = new System.Windows.Forms.TextBox();
        this.textBoxNotes = new System.Windows.Forms.TextBox();
        this.labelLicenseCount = new System.Windows.Forms.Label();
        this.labelValidityDays = new System.Windows.Forms.Label();
        this.labelExpirationDate = new System.Windows.Forms.Label();
        this.labelCustomerName = new System.Windows.Forms.Label();
        this.labelNotes = new System.Windows.Forms.Label();
        this.labelLanguage = new System.Windows.Forms.Label();
        this.comboBoxLanguage = new System.Windows.Forms.ComboBox();
        
        this.groupBoxMac = new System.Windows.Forms.GroupBox();
        this.listBoxMacAddresses = new System.Windows.Forms.ListBox();
        this.textBoxMacAddress = new System.Windows.Forms.TextBox();
        this.buttonAddMac = new System.Windows.Forms.Button();
        this.buttonRemoveMac = new System.Windows.Forms.Button();
        
        this.groupBoxApplication = new System.Windows.Forms.GroupBox();
        this.checkBoxRestrictApp = new System.Windows.Forms.CheckBox();
        this.listBoxApplications = new System.Windows.Forms.ListBox();
        this.buttonAddApp = new System.Windows.Forms.Button();
        this.buttonRemoveApp = new System.Windows.Forms.Button();
        
        this.buttonGenerateLicense = new System.Windows.Forms.Button();
        this.buttonValidateLicense = new System.Windows.Forms.Button();
        
        this.groupBoxDllEncryption = new System.Windows.Forms.GroupBox();
        this.buttonSelectDllFile = new System.Windows.Forms.Button();
        this.textBoxDllFilePath = new System.Windows.Forms.TextBox();
        this.labelDllFilePath = new System.Windows.Forms.Label();
        this.buttonEncryptDll = new System.Windows.Forms.Button();
        this.buttonDecryptDll = new System.Windows.Forms.Button();
        this.buttonSelectEncryptedFile = new System.Windows.Forms.Button();
        this.textBoxEncryptedFilePath = new System.Windows.Forms.TextBox();
        this.labelEncryptedFilePath = new System.Windows.Forms.Label();
        this.buttonDecryptToStream = new System.Windows.Forms.Button();
        
        this.groupBoxBasic.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.numericUpDownLicenseCount)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.numericUpDownValidityDays)).BeginInit();
        this.groupBoxMac.SuspendLayout();
        this.groupBoxApplication.SuspendLayout();
        this.groupBoxDllEncryption.SuspendLayout();
        this.SuspendLayout();
        
        // 
        // groupBoxBasic
        // 
        this.groupBoxBasic.Controls.Add(this.labelLicenseCount);
        this.groupBoxBasic.Controls.Add(this.numericUpDownLicenseCount);
        this.groupBoxBasic.Controls.Add(this.labelValidityDays);
        this.groupBoxBasic.Controls.Add(this.numericUpDownValidityDays);
        this.groupBoxBasic.Controls.Add(this.labelExpirationDate);
        this.groupBoxBasic.Controls.Add(this.dateTimePickerExpiration);
        this.groupBoxBasic.Controls.Add(this.labelCustomerName);
        this.groupBoxBasic.Controls.Add(this.textBoxCustomerName);
        this.groupBoxBasic.Controls.Add(this.labelNotes);
        this.groupBoxBasic.Controls.Add(this.textBoxNotes);
        this.groupBoxBasic.Controls.Add(this.labelLanguage);
        this.groupBoxBasic.Controls.Add(this.comboBoxLanguage);
        this.groupBoxBasic.Location = new System.Drawing.Point(12, 12);
        this.groupBoxBasic.Name = "groupBoxBasic";
        this.groupBoxBasic.Size = new System.Drawing.Size(760, 160);
        this.groupBoxBasic.TabIndex = 0;
        this.groupBoxBasic.TabStop = false;
        this.groupBoxBasic.Text = "Basic Information";
        
        // 
        // labelLicenseCount
        // 
        this.labelLicenseCount.AutoSize = true;
        this.labelLicenseCount.Location = new System.Drawing.Point(20, 30);
        this.labelLicenseCount.Name = "labelLicenseCount";
        this.labelLicenseCount.Size = new System.Drawing.Size(80, 17);
        this.labelLicenseCount.TabIndex = 0;
        this.labelLicenseCount.Text = "License Count:";
        
        // 
        // numericUpDownLicenseCount
        // 
        this.numericUpDownLicenseCount.Location = new System.Drawing.Point(120, 28);
        this.numericUpDownLicenseCount.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
        this.numericUpDownLicenseCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        this.numericUpDownLicenseCount.Name = "numericUpDownLicenseCount";
        this.numericUpDownLicenseCount.Size = new System.Drawing.Size(150, 23);
        this.numericUpDownLicenseCount.TabIndex = 1;
        this.numericUpDownLicenseCount.Value = new decimal(new int[] { 1, 0, 0, 0 });
        
        // 
        // labelValidityDays
        // 
        this.labelValidityDays.AutoSize = true;
        this.labelValidityDays.Location = new System.Drawing.Point(300, 30);
        this.labelValidityDays.Name = "labelValidityDays";
        this.labelValidityDays.Size = new System.Drawing.Size(80, 17);
        this.labelValidityDays.TabIndex = 2;
        this.labelValidityDays.Text = "Validity (Days):";
        
        // 
        // numericUpDownValidityDays
        // 
        this.numericUpDownValidityDays.Location = new System.Drawing.Point(390, 28);
        this.numericUpDownValidityDays.Maximum = new decimal(new int[] { 3650, 0, 0, 0 });
        this.numericUpDownValidityDays.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        this.numericUpDownValidityDays.Name = "numericUpDownValidityDays";
        this.numericUpDownValidityDays.Size = new System.Drawing.Size(150, 23);
        this.numericUpDownValidityDays.TabIndex = 3;
        this.numericUpDownValidityDays.Value = new decimal(new int[] { 365, 0, 0, 0 });
        this.numericUpDownValidityDays.ValueChanged += new System.EventHandler(this.NumericUpDownValidityDays_ValueChanged);
        
        // 
        // labelExpirationDate
        // 
        this.labelExpirationDate.AutoSize = true;
        this.labelExpirationDate.Location = new System.Drawing.Point(20, 65);
        this.labelExpirationDate.Name = "labelExpirationDate";
        this.labelExpirationDate.Size = new System.Drawing.Size(68, 17);
        this.labelExpirationDate.TabIndex = 4;
        this.labelExpirationDate.Text = "Expiration Date:";
        
        // 
        // dateTimePickerExpiration
        // 
        this.dateTimePickerExpiration.Location = new System.Drawing.Point(120, 63);
        this.dateTimePickerExpiration.Name = "dateTimePickerExpiration";
        this.dateTimePickerExpiration.Size = new System.Drawing.Size(420, 23);
        this.dateTimePickerExpiration.TabIndex = 5;
        
        // 
        // labelCustomerName
        // 
        this.labelCustomerName.AutoSize = true;
        this.labelCustomerName.Location = new System.Drawing.Point(20, 100);
        this.labelCustomerName.Name = "labelCustomerName";
        this.labelCustomerName.Size = new System.Drawing.Size(68, 17);
        this.labelCustomerName.TabIndex = 6;
        this.labelCustomerName.Text = "Customer Name:";
        
        // 
        // textBoxCustomerName
        // 
        this.textBoxCustomerName.Location = new System.Drawing.Point(120, 97);
        this.textBoxCustomerName.Name = "textBoxCustomerName";
        this.textBoxCustomerName.Size = new System.Drawing.Size(420, 23);
        this.textBoxCustomerName.TabIndex = 7;
        
        // 
        // labelNotes
        // 
        this.labelNotes.AutoSize = true;
        this.labelNotes.Location = new System.Drawing.Point(560, 30);
        this.labelNotes.Name = "labelNotes";
        this.labelNotes.Size = new System.Drawing.Size(44, 17);
        this.labelNotes.TabIndex = 8;
        this.labelNotes.Text = "Notes:";
        
        // 
        // labelLanguage
        // 
        this.labelLanguage.AutoSize = true;
        this.labelLanguage.Location = new System.Drawing.Point(20, 130);
        this.labelLanguage.Name = "labelLanguage";
        this.labelLanguage.Size = new System.Drawing.Size(68, 17);
        this.labelLanguage.TabIndex = 10;
        this.labelLanguage.Text = "Language:";
        
        // 
        // comboBoxLanguage
        // 
        this.comboBoxLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.comboBoxLanguage.FormattingEnabled = true;
        this.comboBoxLanguage.Items.AddRange(new object[] { "English", "中文" });
        this.comboBoxLanguage.Location = new System.Drawing.Point(120, 127);
        this.comboBoxLanguage.Name = "comboBoxLanguage";
        this.comboBoxLanguage.Size = new System.Drawing.Size(150, 25);
        this.comboBoxLanguage.TabIndex = 11;
        this.comboBoxLanguage.SelectedIndexChanged += new System.EventHandler(this.ComboBoxLanguage_SelectedIndexChanged);
        
        // 
        // textBoxNotes
        // 
        this.textBoxNotes.Location = new System.Drawing.Point(560, 50);
        this.textBoxNotes.Multiline = true;
        this.textBoxNotes.Name = "textBoxNotes";
        this.textBoxNotes.Size = new System.Drawing.Size(180, 90);
        this.textBoxNotes.TabIndex = 9;
        
        // 
        // groupBoxMac
        // 
        this.groupBoxMac.Controls.Add(this.listBoxMacAddresses);
        this.groupBoxMac.Controls.Add(this.textBoxMacAddress);
        this.groupBoxMac.Controls.Add(this.buttonAddMac);
        this.groupBoxMac.Controls.Add(this.buttonRemoveMac);
        this.groupBoxMac.Location = new System.Drawing.Point(12, 180);
        this.groupBoxMac.Name = "groupBoxMac";
        this.groupBoxMac.Size = new System.Drawing.Size(370, 250);
        this.groupBoxMac.TabIndex = 1;
        this.groupBoxMac.TabStop = false;
        this.groupBoxMac.Text = "MAC Address Management";
        
        // 
        // listBoxMacAddresses
        // 
        this.listBoxMacAddresses.FormattingEnabled = true;
        this.listBoxMacAddresses.ItemHeight = 17;
        this.listBoxMacAddresses.Location = new System.Drawing.Point(15, 25);
        this.listBoxMacAddresses.Name = "listBoxMacAddresses";
        this.listBoxMacAddresses.Size = new System.Drawing.Size(340, 140);
        this.listBoxMacAddresses.TabIndex = 0;
        
        // 
        // textBoxMacAddress
        // 
        this.textBoxMacAddress.Location = new System.Drawing.Point(15, 175);
        this.textBoxMacAddress.Name = "textBoxMacAddress";
        //this.textBoxMacAddress.PlaceholderText = "Enter MAC address (e.g., 00-11-22-33-44-55)";
        this.textBoxMacAddress.Size = new System.Drawing.Size(340, 23);
        this.textBoxMacAddress.TabIndex = 1;
        
        // 
        // buttonAddMac
        // 
        this.buttonAddMac.Location = new System.Drawing.Point(15, 210);
        this.buttonAddMac.Name = "buttonAddMac";
        this.buttonAddMac.Size = new System.Drawing.Size(160, 30);
        this.buttonAddMac.TabIndex = 2;
        this.buttonAddMac.Text = "Add MAC Address";
        this.buttonAddMac.UseVisualStyleBackColor = true;
        this.buttonAddMac.Click += new System.EventHandler(this.ButtonAddMac_Click);
        
        // 
        // buttonRemoveMac
        // 
        this.buttonRemoveMac.Location = new System.Drawing.Point(195, 210);
        this.buttonRemoveMac.Name = "buttonRemoveMac";
        this.buttonRemoveMac.Size = new System.Drawing.Size(160, 30);
        this.buttonRemoveMac.TabIndex = 3;
        this.buttonRemoveMac.Text = "Remove Selected MAC";
        this.buttonRemoveMac.UseVisualStyleBackColor = true;
        this.buttonRemoveMac.Click += new System.EventHandler(this.ButtonRemoveMac_Click);
        
        // 
        // groupBoxApplication
        // 
        this.groupBoxApplication.Controls.Add(this.checkBoxRestrictApp);
        this.groupBoxApplication.Controls.Add(this.listBoxApplications);
        this.groupBoxApplication.Controls.Add(this.buttonAddApp);
        this.groupBoxApplication.Controls.Add(this.buttonRemoveApp);
        this.groupBoxApplication.Location = new System.Drawing.Point(402, 180);
        this.groupBoxApplication.Name = "groupBoxApplication";
        this.groupBoxApplication.Size = new System.Drawing.Size(370, 250);
        this.groupBoxApplication.TabIndex = 2;
        this.groupBoxApplication.TabStop = false;
        this.groupBoxApplication.Text = "Application Restriction";
        
        // 
        // checkBoxRestrictApp
        // 
        this.checkBoxRestrictApp.AutoSize = true;
        this.checkBoxRestrictApp.Location = new System.Drawing.Point(15, 30);
        this.checkBoxRestrictApp.Name = "checkBoxRestrictApp";
        this.checkBoxRestrictApp.Size = new System.Drawing.Size(123, 21);
        this.checkBoxRestrictApp.TabIndex = 0;
        this.checkBoxRestrictApp.Text = "Enable Application Restriction";
        this.checkBoxRestrictApp.UseVisualStyleBackColor = true;
        this.checkBoxRestrictApp.CheckedChanged += new System.EventHandler(this.CheckBoxRestrictApp_CheckedChanged);
        
        // 
        // listBoxApplications
        // 
        this.listBoxApplications.FormattingEnabled = true;
        this.listBoxApplications.ItemHeight = 17;
        this.listBoxApplications.Location = new System.Drawing.Point(15, 60);
        this.listBoxApplications.Name = "listBoxApplications";
        this.listBoxApplications.Size = new System.Drawing.Size(340, 140);
        this.listBoxApplications.TabIndex = 1;
        this.listBoxApplications.Enabled = false;
        
        // 
        // buttonAddApp
        // 
        this.buttonAddApp.Enabled = false;
        this.buttonAddApp.Location = new System.Drawing.Point(15, 210);
        this.buttonAddApp.Name = "buttonAddApp";
        this.buttonAddApp.Size = new System.Drawing.Size(160, 30);
        this.buttonAddApp.TabIndex = 2;
        this.buttonAddApp.Text = "Add Application";
        this.buttonAddApp.UseVisualStyleBackColor = true;
        this.buttonAddApp.Click += new System.EventHandler(this.ButtonAddApp_Click);
        
        // 
        // buttonRemoveApp
        // 
        this.buttonRemoveApp.Enabled = false;
        this.buttonRemoveApp.Location = new System.Drawing.Point(195, 210);
        this.buttonRemoveApp.Name = "buttonRemoveApp";
        this.buttonRemoveApp.Size = new System.Drawing.Size(160, 30);
        this.buttonRemoveApp.TabIndex = 3;
        this.buttonRemoveApp.Text = "Remove Selected App";
        this.buttonRemoveApp.UseVisualStyleBackColor = true;
        this.buttonRemoveApp.Click += new System.EventHandler(this.ButtonRemoveApp_Click);
        
        // 
        // buttonGenerateLicense
        // 
        this.buttonGenerateLicense.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
        this.buttonGenerateLicense.Location = new System.Drawing.Point(12, 445);
        this.buttonGenerateLicense.Name = "buttonGenerateLicense";
        this.buttonGenerateLicense.Size = new System.Drawing.Size(370, 45);
        this.buttonGenerateLicense.TabIndex = 3;
        this.buttonGenerateLicense.Text = "Generate License File";
        this.buttonGenerateLicense.UseVisualStyleBackColor = true;
        this.buttonGenerateLicense.Click += new System.EventHandler(this.ButtonGenerateLicense_Click);
        
        // 
        // buttonValidateLicense
        // 
        this.buttonValidateLicense.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
        this.buttonValidateLicense.Location = new System.Drawing.Point(402, 445);
        this.buttonValidateLicense.Name = "buttonValidateLicense";
        this.buttonValidateLicense.Size = new System.Drawing.Size(370, 45);
        this.buttonValidateLicense.TabIndex = 4;
        this.buttonValidateLicense.Text = "Validate License File";
        this.buttonValidateLicense.UseVisualStyleBackColor = true;
        this.buttonValidateLicense.Click += new System.EventHandler(this.ButtonValidateLicense_Click);
        
        // 
        // groupBoxDllEncryption
        // 
        this.groupBoxDllEncryption.Controls.Add(this.labelDllFilePath);
        this.groupBoxDllEncryption.Controls.Add(this.textBoxDllFilePath);
        this.groupBoxDllEncryption.Controls.Add(this.buttonSelectDllFile);
        this.groupBoxDllEncryption.Controls.Add(this.buttonEncryptDll);
        this.groupBoxDllEncryption.Controls.Add(this.labelEncryptedFilePath);
        this.groupBoxDllEncryption.Controls.Add(this.textBoxEncryptedFilePath);
        this.groupBoxDllEncryption.Controls.Add(this.buttonSelectEncryptedFile);
        this.groupBoxDllEncryption.Controls.Add(this.buttonDecryptDll);
        this.groupBoxDllEncryption.Controls.Add(this.buttonDecryptToStream);
        this.groupBoxDllEncryption.Location = new System.Drawing.Point(12, 500);
        this.groupBoxDllEncryption.Name = "groupBoxDllEncryption";
        this.groupBoxDllEncryption.Size = new System.Drawing.Size(760, 180);
        this.groupBoxDllEncryption.TabIndex = 5;
        this.groupBoxDllEncryption.TabStop = false;
        this.groupBoxDllEncryption.Text = "DLL Encryption/Decryption";
        
        // 
        // labelDllFilePath
        // 
        this.labelDllFilePath.AutoSize = true;
        this.labelDllFilePath.Location = new System.Drawing.Point(15, 30);
        this.labelDllFilePath.Name = "labelDllFilePath";
        this.labelDllFilePath.Size = new System.Drawing.Size(80, 17);
        this.labelDllFilePath.TabIndex = 0;
        this.labelDllFilePath.Text = "DLL File Path:";
        
        // 
        // textBoxDllFilePath
        // 
        this.textBoxDllFilePath.Location = new System.Drawing.Point(15, 50);
        this.textBoxDllFilePath.Name = "textBoxDllFilePath";
        this.textBoxDllFilePath.ReadOnly = true;
        this.textBoxDllFilePath.Size = new System.Drawing.Size(500, 23);
        this.textBoxDllFilePath.TabIndex = 1;
        
        // 
        // buttonSelectDllFile
        // 
        this.buttonSelectDllFile.Location = new System.Drawing.Point(530, 49);
        this.buttonSelectDllFile.Name = "buttonSelectDllFile";
        this.buttonSelectDllFile.Size = new System.Drawing.Size(100, 25);
        this.buttonSelectDllFile.TabIndex = 2;
        this.buttonSelectDllFile.Text = "Select DLL";
        this.buttonSelectDllFile.UseVisualStyleBackColor = true;
        this.buttonSelectDllFile.Click += new System.EventHandler(this.ButtonSelectDllFile_Click);
        
        // 
        // buttonEncryptDll
        // 
        this.buttonEncryptDll.Location = new System.Drawing.Point(650, 49);
        this.buttonEncryptDll.Name = "buttonEncryptDll";
        this.buttonEncryptDll.Size = new System.Drawing.Size(100, 25);
        this.buttonEncryptDll.TabIndex = 3;
        this.buttonEncryptDll.Text = "Encrypt DLL";
        this.buttonEncryptDll.UseVisualStyleBackColor = true;
        this.buttonEncryptDll.Click += new System.EventHandler(this.ButtonEncryptDll_Click);
        
        // 
        // labelEncryptedFilePath
        // 
        this.labelEncryptedFilePath.AutoSize = true;
        this.labelEncryptedFilePath.Location = new System.Drawing.Point(15, 90);
        this.labelEncryptedFilePath.Name = "labelEncryptedFilePath";
        this.labelEncryptedFilePath.Size = new System.Drawing.Size(120, 17);
        this.labelEncryptedFilePath.TabIndex = 4;
        this.labelEncryptedFilePath.Text = "Encrypted File Path:";
        
        // 
        // textBoxEncryptedFilePath
        // 
        this.textBoxEncryptedFilePath.Location = new System.Drawing.Point(15, 110);
        this.textBoxEncryptedFilePath.Name = "textBoxEncryptedFilePath";
        this.textBoxEncryptedFilePath.ReadOnly = true;
        this.textBoxEncryptedFilePath.Size = new System.Drawing.Size(500, 23);
        this.textBoxEncryptedFilePath.TabIndex = 5;
        
        // 
        // buttonSelectEncryptedFile
        // 
        this.buttonSelectEncryptedFile.Location = new System.Drawing.Point(530, 109);
        this.buttonSelectEncryptedFile.Name = "buttonSelectEncryptedFile";
        this.buttonSelectEncryptedFile.Size = new System.Drawing.Size(100, 25);
        this.buttonSelectEncryptedFile.TabIndex = 6;
        this.buttonSelectEncryptedFile.Text = "Select File";
        this.buttonSelectEncryptedFile.UseVisualStyleBackColor = true;
        this.buttonSelectEncryptedFile.Click += new System.EventHandler(this.ButtonSelectEncryptedFile_Click);
        
        // 
        // buttonDecryptDll
        // 
        this.buttonDecryptDll.Location = new System.Drawing.Point(650, 109);
        this.buttonDecryptDll.Name = "buttonDecryptDll";
        this.buttonDecryptDll.Size = new System.Drawing.Size(100, 25);
        this.buttonDecryptDll.TabIndex = 7;
        this.buttonDecryptDll.Text = "Decrypt to DLL";
        this.buttonDecryptDll.UseVisualStyleBackColor = true;
        this.buttonDecryptDll.Click += new System.EventHandler(this.ButtonDecryptDll_Click);
        
        // 
        // buttonDecryptToStream
        // 
        this.buttonDecryptToStream.Location = new System.Drawing.Point(15, 145);
        this.buttonDecryptToStream.Name = "buttonDecryptToStream";
        this.buttonDecryptToStream.Size = new System.Drawing.Size(150, 25);
        this.buttonDecryptToStream.TabIndex = 8;
        this.buttonDecryptToStream.Text = "Decrypt to Stream";
        this.buttonDecryptToStream.UseVisualStyleBackColor = true;
        this.buttonDecryptToStream.Click += new System.EventHandler(this.ButtonDecryptToStream_Click);
        
        // 
        // Form1
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(784, 700);
        this.Controls.Add(this.groupBoxBasic);
        this.Controls.Add(this.groupBoxMac);
        this.Controls.Add(this.groupBoxApplication);
        this.Controls.Add(this.buttonGenerateLicense);
        this.Controls.Add(this.buttonValidateLicense);
        this.Controls.Add(this.groupBoxDllEncryption);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.Name = "Form1";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "MARS License Manager";
        this.groupBoxBasic.ResumeLayout(false);
        this.groupBoxBasic.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.numericUpDownLicenseCount)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.numericUpDownValidityDays)).EndInit();
        this.groupBoxMac.ResumeLayout(false);
        this.groupBoxMac.PerformLayout();
        this.groupBoxApplication.ResumeLayout(false);
        this.groupBoxApplication.PerformLayout();
        this.groupBoxDllEncryption.ResumeLayout(false);
        this.groupBoxDllEncryption.PerformLayout();
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.GroupBox groupBoxBasic;
    private System.Windows.Forms.NumericUpDown numericUpDownLicenseCount;
    private System.Windows.Forms.NumericUpDown numericUpDownValidityDays;
    private System.Windows.Forms.DateTimePicker dateTimePickerExpiration;
    private System.Windows.Forms.TextBox textBoxCustomerName;
    private System.Windows.Forms.TextBox textBoxNotes;
    private System.Windows.Forms.Label labelLicenseCount;
    private System.Windows.Forms.Label labelValidityDays;
    private System.Windows.Forms.Label labelExpirationDate;
    private System.Windows.Forms.Label labelCustomerName;
    private System.Windows.Forms.Label labelNotes;

    private System.Windows.Forms.GroupBox groupBoxMac;
    private System.Windows.Forms.ListBox listBoxMacAddresses;
    private System.Windows.Forms.TextBox textBoxMacAddress;
    private System.Windows.Forms.Button buttonAddMac;
    private System.Windows.Forms.Button buttonRemoveMac;

    private System.Windows.Forms.GroupBox groupBoxApplication;
    private System.Windows.Forms.CheckBox checkBoxRestrictApp;
    private System.Windows.Forms.ListBox listBoxApplications;
    private System.Windows.Forms.Button buttonAddApp;
    private System.Windows.Forms.Button buttonRemoveApp;

    private System.Windows.Forms.Button buttonGenerateLicense;
    private System.Windows.Forms.Button buttonValidateLicense;
    private System.Windows.Forms.Label labelLanguage;
    private System.Windows.Forms.ComboBox comboBoxLanguage;
    
    private System.Windows.Forms.GroupBox groupBoxDllEncryption;
    private System.Windows.Forms.Label labelDllFilePath;
    private System.Windows.Forms.TextBox textBoxDllFilePath;
    private System.Windows.Forms.Button buttonSelectDllFile;
    private System.Windows.Forms.Button buttonEncryptDll;
    private System.Windows.Forms.Label labelEncryptedFilePath;
    private System.Windows.Forms.TextBox textBoxEncryptedFilePath;
    private System.Windows.Forms.Button buttonSelectEncryptedFile;
    private System.Windows.Forms.Button buttonDecryptDll;
    private System.Windows.Forms.Button buttonDecryptToStream;
}
