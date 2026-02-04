
// MARS Compare GUI


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows;
using System.IO;
using System.Xml;
#if !db4SQL
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
#endif
using System.Diagnostics;
using System.Collections.Specialized;
using Excel = Microsoft.Office.Interop.Excel;
using cmdLineCompDir;

using Mars.DataLayer;
using Mars.TestFramework.DataCompare;
using System.Configuration;
using System.Data.SqlClient;

using Parser;
using MARS.CompareGUI.GUI;
using Mars.Securities;

using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Dialogs;
using CustomComboBox;
using Mars.TestFramework.DataCompare.DataCompareBatch;
using ClosedXML.Excel;
using DomUtil;
using Route2NSEx.src.Marquis.systemUtil;

namespace MARS.CompareGUI
{
    public partial class DataCompareForm : Form
    {
        private static string V2_MARKER = "==";
        private static int ComboBoxItemLength = 20;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;  // Turn on WS_EX_COMPOSITED
                return cp;
            }
        }

        public static DataTable MappingTable { get;  set; }

        //string filename = "C:\\Users\\textarvind\\Documents\\SummitProjects\\ConfigForCompare.xml";
        string filename = "DB";
        bool DeleteWasClicked = false;

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
        (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private  string xmlFileName1;
        private  string xmlFileName2;
        private  string excelFileName;
        private  string configFileName;
 
        public DataCompareForm()
        {
            InitializeComponent();

            //Visibility and Enabling settings for selected components
          
            S1ConnectionID_comboBox.Enabled = false;
            S1QueryID_comboBox.Enabled = false;
            S1BrowseButton.Enabled = false;
            S1FileLocationValue.Enabled = false;
            S1DBFieldsButton.Enabled = false;
            S1TestConn_button.Enabled = false;

            S2ConnectionID_comboBox.Enabled = false;
            S2QueryID_comboBox.Enabled = false;
            S2BrowseButton.Enabled = false;
            S2FileLocationValue.Enabled = false;
            S2DBFieldsButton.Enabled = false;
            S2TestConn_button.Enabled = false;

            DBConnPageConnStatus.Visible = false;

            //OFileNameValue.Enabled = false;
            //OFileType_comboBox.Enabled = false;
            //DiffFileSaveButton.Enabled = false;

            //DiffFile.Visible = false;
            //OFileNameValue.Visible = false;
            //DiffFileSaveButton.Visible = false;

            // AF S1FileLocationValue.ReadOnly = true;
            //  AF S2FileLocationValue.ReadOnly = true;

            //OFileLocationValue.ReadOnly = true;

            log4net.Config.BasicConfigurator.Configure();

            openFileDialog1.InitialDirectory = S1FileLocationValue.Text;
            openFileDialog2.InitialDirectory = S2FileLocationValue.Text;


            string configSetting = System.Configuration.ConfigurationManager.AppSettings["CompareResultFolder"];
            if (configSetting != null)
                this.outDirTxt.Text = configSetting;

            configSetting = System.Configuration.ConfigurationManager.AppSettings["BaselineFmtFolder"];
            if (configSetting != null)
                this.BaselineFmtTxt.Text = configSetting;

            configSetting = System.Configuration.ConfigurationManager.AppSettings["BaselineRptFolder"];
            if (configSetting != null)
                this.BaselineRptTxt.Text = configSetting;

            configSetting = System.Configuration.ConfigurationManager.AppSettings["CompareFmtFolder"];
            if (configSetting != null)
                this.CompareFmtTxt.Text = configSetting;

            configSetting = System.Configuration.ConfigurationManager.AppSettings["CompareRptFolder"];
            if (configSetting != null)
                this.CompareFmtTxt.Text = configSetting;

            /*
            configSetting = ConfigurationManager.AppSettings["MappingTableFileName"];

            if (configSetting != null)
            {
                this.MappingTable = DataCompareBatchConfig.ImportExceltoDatatable(configSetting, "Sheet1");
                ExecuteCompare.MappingTable = this.MappingTable;
            }
            */
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label16_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void label12_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void ConnectionNameValue_TextChanged(object sender, EventArgs e)
        {

        }

        private void QueryValue_TextChanged(object sender, EventArgs e)
        {

        }

        private void Type_Click(object sender, EventArgs e)
        {

        }

        private void HostValue_TextChanged(object sender, EventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void checkedListBox3_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void cleanEntriesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ImportButton.PerformClick();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (filename == "")
            {
                using (new CenterWinDialog(this))
                MessageBox.Show("No Config File detected. Please load Config File.");
                return;
            }
            else
            {
                if (tabControl1.SelectedTab == Queries_tabPage)
                {
                    //Adding a new entry 
                    QuerywithID NewQEntry = new QuerywithID();
                    NewQEntry.QueryID = QueryIDValue.Text;
                    NewQEntry.Query = QueryValue.Text;

                    //Save data to ConfigXML
                    SaveConfigXML.SaveQueryData(NewQEntry, filename);

                    //Display on listbox - Query Page
                    if (!SavedQueries_listBox.Items.Contains(NewQEntry.QueryID))
                    {
                        SavedQueries_listBox.Items.Add(NewQEntry.QueryID);
                        SelectListboxItem(SavedQueries_listBox, NewQEntry.QueryID);
                    }

                    //Display on combobox - Compare Page
                    if (S1QueryID_comboBox.Items.Contains(NewQEntry.QueryID) == false)
                        S1QueryID_comboBox.Items.Add(NewQEntry.QueryID);
                    if (S2QueryID_comboBox.Items.Contains(NewQEntry.QueryID) == false)
                        S2QueryID_comboBox.Items.Add(NewQEntry.QueryID);

                }

                if (tabControl1.SelectedTab == DBConnections_tabPage)
                {
                    //Adding a new connection
                    DBConnectionwithID NewDBConnEntry = new DBConnectionwithID();
                    NewDBConnEntry.ConnectionID = ConnectionIDValue.Text;
                    NewDBConnEntry.DatabaseType = DatabaseTypeValue.Text;
                    NewDBConnEntry.Host = HostValue.Text;
                    NewDBConnEntry.Port = PortValue.Text;
                    NewDBConnEntry.Protocol = ProtocolValue.Text;
                    NewDBConnEntry.ServiceName = ServiceNameValue.Text;
                    NewDBConnEntry.UserID = UserIDValue.Text;
                    // NewDBConnEntry.Password = PasswordValue.Text;
                    NewDBConnEntry.Password = MarsEncodePwd.EncodeString(PasswordValue.Text);
                    //Save data to ConfigXML

                    NewDBConnEntry.Sid = SidValue.Text;
                    NewDBConnEntry.ConnString = ConnectionStringValue.Text;

                    SaveConfigXML.SaveDBConnectionData(NewDBConnEntry, filename);

                    //Display on listbox - DBConnections Page
                    if (!SavedConnections_listBox.Items.Contains(NewDBConnEntry.ConnectionID))
                    {
                        SavedConnections_listBox.Items.Add(NewDBConnEntry.ConnectionID);
                        SelectListboxItem(SavedConnections_listBox, NewDBConnEntry.ConnectionID);
                    }
               
                    //Display on combobox - Compare Page
                    if (S1ConnectionID_comboBox.Items.Contains(NewDBConnEntry.ConnectionID) == false)
                        S1ConnectionID_comboBox.Items.Add(NewDBConnEntry.ConnectionID);
                    if (S2ConnectionID_comboBox.Items.Contains(NewDBConnEntry.ConnectionID) == false)
                        S2ConnectionID_comboBox.Items.Add(NewDBConnEntry.ConnectionID);
                    if (TestConnectionsComboBox.Items.Contains(NewDBConnEntry.ConnectionID) == false)
                        TestConnectionsComboBox.Items.Add(NewDBConnEntry.ConnectionID);


                }
                

                if (tabControl1.SelectedTab == setting_page)
                {
                    ProfileWithID NewProfileWithID = new ProfileWithID();
                    NewProfileWithID.ProfileNameID = this.ProfileNameTxt.Text;

                    NewProfileWithID.BaselineRpt = this.BaselineRptTxt.Text;
                    NewProfileWithID.CompareRpt = this.CompareRptTxt.Text;
                    NewProfileWithID.BaselineFmt = this.BaselineFmtTxt.Text;
                    NewProfileWithID.CompareFmt = this.CompareFmtTxt.Text;
                    NewProfileWithID.outDir = this.outDirTxt.Text;

                    SaveConfigXML.SaveProfileData(NewProfileWithID, filename);

                    if (!SavedProfiles_listBox.Items.Contains(NewProfileWithID.ProfileNameID))
                    {
                        SavedProfiles_listBox.Items.Add(NewProfileWithID.ProfileNameID);
                        SelectListboxItem(SavedProfiles_listBox, NewProfileWithID.ProfileNameID);
                    }
                }


                if (tabControl1.SelectedTab == Compare_tabPage)
                {
                    //Adding a new compare 
                    ComparewithID NewCompareEntry = new ComparewithID();
                    NewCompareEntry.CompareID = CompareIDValue.Text;
                    NewCompareEntry.S1Type = S1Type_comboBox.Text;
                    NewCompareEntry.S1DBConn = S1ConnectionID_comboBox.Text;
                    NewCompareEntry.S1QueryID = S1QueryID_comboBox.Text;
                    NewCompareEntry.S1FileLocation = S1FileLocationValue.Text;
                    NewCompareEntry.S1OpicsRepFileLoc = S1OpicsRepFileLoc.Text;
                    NewCompareEntry.S1CSVDelim = S1CSVDelim.Text;

                    NewCompareEntry.S2Type = S2Type_comboBox.Text;
                    NewCompareEntry.S2DBConn = S2ConnectionID_comboBox.Text;
                    NewCompareEntry.S2QueryID = S2QueryID_comboBox.Text;
                    NewCompareEntry.S2FileLocation = S2FileLocationValue.Text;
                    NewCompareEntry.S2OpicsRepFileLoc = S2OpicsRepFileLoc.Text;
                    NewCompareEntry.S2CSVDelim = S2CSVDelim.Text;

                    NewCompareEntry.KeyFields = KeyFieldsValue_comboBox.Text;
                    NewCompareEntry.ShowFields = ShowFieldsValue_comboBox.Text;
                    // NewCompareEntry.CompareFields = CompareFieldsValue_comboBox.Text;

                    NewCompareEntry.CompareFields = ((ToleranceGrid)CompareCustomComboBox.DropDownControl).ItemsToCompare();

                    ///
                    NewCompareEntry.RowFields = RowFieldsValue.Text;
                    NewCompareEntry.ColumnFields = ColumnFieldsValue.Text;

                    NewCompareEntry.OutputFilter = FilterTextBox.Text;
                    NewCompareEntry.OutputFilterApply = this.ApplyCheckBox.Checked;
                    NewCompareEntry.OutputOrderBy = this.OrderByTextBox.Text;
                    NewCompareEntry.S1OpicsRepFileLoc = this.S1OpicsRepFileLoc.Text;
                    NewCompareEntry.S2OpicsRepFileLoc = this.S2OpicsRepFileLoc.Text;

                    //NewCompareEntry.ODiffLocation = OFileNameValue.Text;
                    //NewCompareEntry.OFileType = OFileType_comboBox.Text;
                    // NewCompareEntry.OFileLocation = OFileLocationValue.Text;

                    //Save Data to ConfigXML
                    SaveConfigXML.SaveCompareData(NewCompareEntry, filename);

                    //Display on listbox - Compares Page
                    if (!SavedCompares_listBox.Items.Contains(NewCompareEntry.CompareID))
                    {
                        SavedCompares_listBox.Items.Add(NewCompareEntry.CompareID);
                        SelectListboxItem(SavedCompares_listBox, NewCompareEntry.CompareID);
                    }
                }
            }
            using (new CenterWinDialog(this))
                MessageBox.Show("Save Compleated");
        }

        private static void UpdateSetting(string key, string value)
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (configuration.AppSettings.Settings.AllKeys.Contains(key) == false)
                configuration.AppSettings.Settings.Add(key, value);
            else
                configuration.AppSettings.Settings[key].Value = value;
            configuration.Save();

            ConfigurationManager.RefreshSection("appSettings");
        }

        private void SelectListboxItem(ListBox lb, string itemString)
        {
            for (int index = 0; index < lb.Items.Count; index++)
            {
                string item = lb.Items[index].ToString();
                if (itemString == item)
                {
                    lb.SelectedIndex = index;
                    break;
                }
            }
        }

        private void label22_Click(object sender, EventArgs e)
        {

        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void S1DBFieldsButton_Click(object sender, EventArgs e)
        {
            if (filename == "")
            {
                using (new CenterWinDialog(this))
                MessageBox.Show("No Config File detected. Please load Config File.");
                return;
            }
            else
            {
                KeyFieldsValue_comboBox.Items.Clear();
                ShowFieldsValue_comboBox.Items.Clear();
                CompareFieldsValue_comboBox.Items.Clear();
                RowFieldsValue.Items.Clear();
                ColumnFieldsValue.Items.Clear();
                
                //Converting DB to Xml
                XmlDocument DataSource = new XmlDocument();
                DataTable dt = new DataTable();
                DBConnectionwithID ConnStringForFields = new DBConnectionwithID();
                ConnStringForFields = ReadConfigXML.GetConnectionFromID(S1ConnectionID_comboBox.Text, filename);

                string QueryForFields = ReadConfigXML.GetQueryFromID(S1QueryID_comboBox.Text, filename);
#if !db4SQL
                using (OracleConnection sqlConnection = new OracleConnection(ConnStringForFields.BuildConnectionString()))
                {
                    OracleCommand command = new OracleCommand(QueryForFields, sqlConnection);
                    OracleDataAdapter adapter = new OracleDataAdapter(command);
                    OracleCommandBuilder builder = new OracleCommandBuilder(adapter);
                    DataSet ds = new DataSet();
                    adapter.Fill(ds);
                    dt = ds.Tables[0];
                    Console.WriteLine("\ndbSource ready for access");
                }
#else
                using (SqlConnection sqlConnection = new SqlConnection(ConnStringForFields.BuildConnectionString()))
                {
                    SqlCommand command = new SqlCommand(QueryForFields, sqlConnection);
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    SqlCommandBuilder builder = new SqlCommandBuilder(adapter);
                    DataSet ds = new DataSet();
                    adapter.Fill(ds);
                    dt = ds.Tables[0];
                    Console.WriteLine("\ndbSource ready for access");
                }
#endif
                DataSource = Conversion.DataTableToDom(dt);

                //Generating fields
                String DbFields = GenerateFields.GenDatabaseFields(DataSource);
                DbFields = DbFields.TrimEnd(',');

                //Populating fields
                int i = 0;
                foreach (string singlefield in DbFields.Split(','))
                {
                    CCBoxItem item = new CCBoxItem(singlefield, i);
                    i++;
                    KeyFieldsValue_comboBox.Items.Add(item);
                    ShowFieldsValue_comboBox.Items.Add(item);
                    CompareFieldsValue_comboBox.Items.Add(item);

                    RowFieldsValue.Items.Add(item);
                    ColumnFieldsValue.Items.Add(item);

                    ((ToleranceGrid)CompareCustomComboBox.DropDownControl).AddGridItem(singlefield + "|" + 0 + "||");


                }

                KeyFieldsValue_comboBox.MaxDropDownItems = ComboBoxItemLength;
                ShowFieldsValue_comboBox.MaxDropDownItems = ComboBoxItemLength;
                CompareFieldsValue_comboBox.MaxDropDownItems = ComboBoxItemLength;

                RowFieldsValue.MaxDropDownItems = 5;
                ColumnFieldsValue.MaxDropDownItems = 5;
            }
        }

        private void S1BrowseButton_Click_1(object sender, EventArgs e)
        {
            string file = "";
           
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                file = openFileDialog1.FileName;
                S1FileLocationValue.Text = file;
            }

            if (file != "")
            {
                string S1Fields = "";
                //Generating fields
                if (S1Type_comboBox.Text == "CSV")
                {
                    S1Fields = GenerateFields.GenFieldsCSV(file, S1CSVDelim.Text);
                }

                else if (S1Type_comboBox.Text == "EXCEL")
                {
                    S1Fields = GenerateFields.GenFieldsExcel(file);
                    if (MappingTable != null)
                    {
                        string extraField = MappingTable.Columns[0].ColumnName;
                        S1Fields = extraField + "," + S1Fields;
                    }
                }

                else if (S1Type_comboBox.Text == "REPORT")
                {
                    string fmtLocation = S1OpicsRepFileLoc.Text;
                    try
                    {
                        S1Fields = GenerateFields.GenFieldsREPORT(file, fmtLocation);
                    }

                    catch (System.ArgumentException ex)
                    {
                        MessageBox.Show("Format File not found. Please provide format file location  \n" + ex);
                        return;
                    }
                    
                }

                else if (S1Type_comboBox.Text == "SWIFT")
                {
                    S1Fields = GenerateFields.GenFieldsSWIFT(file);
                }

                else if (S1Type_comboBox.Text == "XML")
                {
                    S1Fields = GenerateFields.GenFields(file);
                    if (MappingTable != null)
                    {
                        string extraField = MappingTable.Columns[0].ColumnName;
                        S1Fields = extraField + "," + S1Fields;
                    }
                }

                S1Fields = S1Fields.TrimEnd(',');

                // Clear ComboBoxes


                //Populating fields
                KeyFieldsValue_comboBox.Items.Clear();
                ShowFieldsValue_comboBox.Items.Clear();
                CompareFieldsValue_comboBox.Items.Clear();
                ((ToleranceGrid)CompareCustomComboBox.DropDownControl).InitGrid(null);
                CompareCustomComboBox.Text = "";

                int i = 0;
                foreach (string singlefield in S1Fields.Split(','))
                {
                    CCBoxItem item = new CCBoxItem(singlefield, i);
                    i++;
                    KeyFieldsValue_comboBox.Items.Add(item);
                    ShowFieldsValue_comboBox.Items.Add(item);
                    CompareFieldsValue_comboBox.Items.Add(item);

                    RowFieldsValue.Items.Add(item);
                    ColumnFieldsValue.Items.Add(item);
                    ((ToleranceGrid)CompareCustomComboBox.DropDownControl).AddGridItem(singlefield + "|" + 0 + "||");
                }

                KeyFieldsValue_comboBox.MaxDropDownItems = ComboBoxItemLength;
                ShowFieldsValue_comboBox.MaxDropDownItems = ComboBoxItemLength;
                CompareFieldsValue_comboBox.MaxDropDownItems = ComboBoxItemLength;

                RowFieldsValue.MaxDropDownItems = 5;
                ColumnFieldsValue.MaxDropDownItems = 5;
            }
        }
        
        private void S2BrowseButton_Click_1(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog2.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                string file = openFileDialog2.FileName;
                S2FileLocationValue.Text = file;
            }
        }

        private void KeyFieldsValue_comboBox_DropDownClosed(object sender, EventArgs e)
        {
            string KFSting = KeyFieldsValue_comboBox.Text;
            if (KeyFieldsValue_comboBox.CheckedItems == null)
            {
                KeyFieldsValue_comboBox.Text = KeyFieldsValue_comboBox.Parent.Text;
            }
        }

        private void KeyFieldsValue_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void ShowFields_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void ShowFieldsValue_comboBox_DropDownClosed(object sender, EventArgs e)
        {

        }

        private void CompareFields_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void CompareFields_comboBox_DropDownClosed(object sender, EventArgs e)
        {

        }

        private void SavedQueries_listBox_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            if (DeleteWasClicked)
            {
                //Reset delete
                DeleteWasClicked = false;

                //Delete Query ID from listbox
                string SelectedQueryIDtoDelete = QueryIDValue.Text;
                try
                {
                    SavedQueries_listBox.Items.Remove(SelectedQueryIDtoDelete);
                }
                catch (Exception ex)
                {

                }

                //Delete Query ID from combobox in compare page
                S1QueryID_comboBox.Items.Remove(SelectedQueryIDtoDelete);
                S2QueryID_comboBox.Items.Remove(SelectedQueryIDtoDelete);

                //Delete Query from Config
                DeleteConfigXML.DeleteQueryData(SelectedQueryIDtoDelete, filename);

                //Clear Page
                ClearDataEntryPanel();

                //Move Selected item on listbox to the next item
                SavedQueries_listBox.SelectedIndex = 0;

                return;
            }
            
            string SelectedQueryID = SavedQueries_listBox.SelectedItem.ToString();

            //Parse through Query Section of Config to find Selected QueryID and Corresponding Query 
            //And display on Queries Page
            QueryIDValue.Text = SelectedQueryID;
            QueryValue.Text = ReadConfigXML.GetQueryFromID(SelectedQueryID, filename);

        }

        private void SavedConnections_listBox_SelectionIndexChanged_1(object sender, EventArgs e)
        {
            if (DeleteWasClicked)
            {
                //Reset delete
                DeleteWasClicked = false;

                //Delete Connection ID from listbox
                string SelectedConnectionIDtoDelete = ConnectionIDValue.Text;
                try
                {
                    SavedConnections_listBox.Items.Remove(SelectedConnectionIDtoDelete);
                }
                catch(Exception ex)
                { }              

                //Delete Connection ID from combobox in compare page
                S1ConnectionID_comboBox.Items.Remove(SelectedConnectionIDtoDelete);
                S2ConnectionID_comboBox.Items.Remove(SelectedConnectionIDtoDelete);

                //Delete Connection from Config
                DeleteConfigXML.DeleteDBConnectionData(SelectedConnectionIDtoDelete, filename);

                //Clear Page
                ClearDataEntryPanel();

                //Move Selected item on listbox to the first item
                SavedConnections_listBox.SelectedIndex = 0;
                return;
               
            }
            if (SavedConnections_listBox.SelectedItem == null) return;

            string SelectedConnectionID = SavedConnections_listBox.SelectedItem.ToString();

            //Parse through Connections Section of Config to find Selected ConnectionID and corresponding Connection
            //And display on Connections Page
            ConnectionIDValue.Text = SelectedConnectionID;
            DBConnectionwithID ReadDBConnEntry = new DBConnectionwithID();
            ReadDBConnEntry = ReadConfigXML.GetConnectionFromID(SelectedConnectionID, filename);

            //Populating corresponding values on the connections page
            DatabaseTypeValue.Text = ReadDBConnEntry.DatabaseType;
            HostValue.Text = ReadDBConnEntry.Host;
            PortValue.Text = ReadDBConnEntry.Port;
            ProtocolValue.Text = ReadDBConnEntry.Protocol;
            ServiceNameValue.Text = ReadDBConnEntry.ServiceName;
            UserIDValue.Text = ReadDBConnEntry.UserID;
            //PasswordValue.Text = ReadDBConnEntry.Password;
            PasswordValue.Text = MarsEncodePwd.DecodeString(ReadDBConnEntry.Password);

            SidValue.Text = MarsEncodePwd.DecodeString(ReadDBConnEntry.Sid);
            ConnectionStringValue.Text = MarsEncodePwd.DecodeString(ReadDBConnEntry.ConnString);
            
        }

        //----------------------------------------------------------------------------------------
        private void SavedCompares_listBox_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            bool useTolerance = false;

            if (SavedCompares_listBox.SelectedItem == null)
                return;

            if (DeleteWasClicked)
            {
                //Reset delete
                DeleteWasClicked = false;

                //Delete Compare ID from listbox
                string SelectedCompareIDtoDelete = CompareIDValue.Text;
                SavedCompares_listBox.Items.Remove(SelectedCompareIDtoDelete);

                //Delete Compare from Config
                DeleteConfigXML.DeleteCompareData(SelectedCompareIDtoDelete, filename);

                //Clear Page
                ClearDataEntryPanel();

                //Move Selected item on listbox to the first item
                SavedCompares_listBox.SelectedIndex = 0;
                return;
              
            } 
            
            KeyFieldsValue_comboBox.Text = "";
            ShowFieldsValue_comboBox.Text = "";
            CompareFieldsValue_comboBox.Text = "";
            RowFieldsValue.Text = "";
            RowFieldsValue.Text = "";
            ColumnFieldsValue.Text = "";

            KeyFieldsValue_comboBox.Items.Clear();
            ShowFieldsValue_comboBox.Items.Clear();
            CompareFieldsValue_comboBox.Items.Clear();
            RowFieldsValue.Items.Clear();
            ColumnFieldsValue.Items.Clear();

            ApplyCheckBox.Checked = false;
            FilterTextBox.Text = "";
            OrderByTextBox.Text = "";

            string SelectedCompareID = SavedCompares_listBox.SelectedItem.ToString();

            //Parse through Compares Section of Config to find Selected CompareID and corresponding Compare data
            //And display on Compare Page
            CompareIDValue.Text = SelectedCompareID;
            ComparewithID ReadCompareEntry = new ComparewithID();
            ReadCompareEntry = ReadConfigXML.GetCompareFromID(SelectedCompareID, filename);

            //Populating corresponding values on the compare page
            S1Type_comboBox.Text = ReadCompareEntry.S1Type;
            S1ConnectionID_comboBox.Text = ReadCompareEntry.S1DBConn;
            S1QueryID_comboBox.Text = ReadCompareEntry.S1QueryID;
            S1FileLocationValue.Text = ReadCompareEntry.S1FileLocation;
            S1OpicsRepFileLoc.Text = ReadCompareEntry.S1OpicsRepFileLoc;
            S1CSVDelim.Text = ReadCompareEntry.S1CSVDelim;

            S2Type_comboBox.Text = ReadCompareEntry.S2Type;
            S2ConnectionID_comboBox.Text = ReadCompareEntry.S2DBConn;
            S2QueryID_comboBox.Text = ReadCompareEntry.S2QueryID;
            S2FileLocationValue.Text = ReadCompareEntry.S2FileLocation;
            S2OpicsRepFileLoc.Text = ReadCompareEntry.S2OpicsRepFileLoc;
            S2CSVDelim.Text = ReadCompareEntry.S2CSVDelim;

            //OFileType_comboBox.Text = ReadCompareEntry.OFileType;
            //OFileNameValue.Text = ReadCompareEntry.ODiffLocation;
            //OFileLocationValue.Text = ReadCompareEntry.OFileLocation;

            ClearFieldItems();

            KeyFieldsValue_comboBox.Text = ReadCompareEntry.KeyFields;
            ShowFieldsValue_comboBox.Text = ReadCompareEntry.ShowFields;

            string compFields = ExtractFields(ReadCompareEntry.CompareFields);
            CompareFieldsValue_comboBox.Text = compFields;
            // Initialize Custom Combo box

            string initialString = "ID|0||;Amount|1|A|1;Rate|1|P|2;Date|1||";
            ToleranceGrid toleranceGrid = new ToleranceGrid(initialString);
            CompareCustomComboBox.DropDownControl = toleranceGrid;
            CompareCustomComboBox.Text = compFields;


            //

            ApplyCheckBox.Checked = ReadCompareEntry.OutputFilterApply;
            FilterTextBox.Text = ReadCompareEntry.OutputFilter;
            OrderByTextBox.Text = ReadCompareEntry.OutputOrderBy;


            RowFieldsValue.Text = ReadCompareEntry.RowFields;
            ColumnFieldsValue.Text = ReadCompareEntry.ColumnFields;

            string strConnectionStr = "";

            //Add the items to the checkedcombobox
            String S1Fields = "";
            if (S1Type_comboBox.Text == "DATABASE")
            {
                try
                {
                    //Converting DB to Xml
                    XmlDocument DataSource = new XmlDocument();
                    DataTable dt = new DataTable();
                    DBConnectionwithID ConnStringForFields = new DBConnectionwithID();
                    ConnStringForFields = ReadConfigXML.GetConnectionFromID(S1ConnectionID_comboBox.Text, filename);
                    string QueryForFields = ReadConfigXML.GetQueryFromID(S1QueryID_comboBox.Text, filename);

                    QueryForFields = adjustQueryForFieldExtraction(QueryForFields);

#if !db4SQL
                    if (ConnStringForFields.DatabaseType.Equals("Oracle"))
                        using (OracleConnection sqlConnection = new OracleConnection(strConnectionStr=ConnStringForFields.BuildConnectionString()))
                        {
                            OracleCommand command = new OracleCommand(QueryForFields, sqlConnection);
                            OracleDataAdapter adapter = new OracleDataAdapter(command);
                            OracleCommandBuilder builder = new OracleCommandBuilder(adapter);
                            DataSet ds = new DataSet();
                            adapter.Fill(ds);
                            dt = ds.Tables[0];
                            Console.WriteLine("\ndbSource ready for access");
                        }
                    else
                        if (ConnStringForFields.DatabaseType.Equals("SQL Server"))
                        using (SqlConnection sqlConnection = new SqlConnection(strConnectionStr = ConnStringForFields.BuildConnectionString()))
                        {
                            SqlCommand command = new SqlCommand(QueryForFields, sqlConnection);
                            SqlDataAdapter adapter = new SqlDataAdapter(command);
                            SqlCommandBuilder builder = new SqlCommandBuilder(adapter);
                            DataSet ds = new DataSet();
                            adapter.Fill(ds);
                            dt = ds.Tables[0];
                            CapitalizeDTColumns(dt);

                            Console.WriteLine("\ndbSource ready for access");
                        }
#else
                    using (SqlConnection sqlConnection = new SqlConnection(ConnStringForFields.BuildConnectionString()))
                    {
                        SqlCommand command = new SqlCommand(QueryForFields, sqlConnection);
                        SqlDataAdapter adapter = new SqlDataAdapter(command);
                        SqlCommandBuilder builder = new SqlCommandBuilder(adapter);
                        DataSet ds = new DataSet();
                        adapter.Fill(ds);
                        dt = ds.Tables[0];
                        Console.WriteLine("\ndbSource ready for access");
                    }
#endif

                    // AF get fields directly from DataTable
                    S1Fields = GetFieldsFromDT(dt);
                    //DataSource = Conversion.DataTableToDom(dt);

                    ////Generating fields
                    //S1Fields = GenerateFields.GenDatabaseFields(DataSource);
                }
                catch (Exception ex)
                {
                    using (new CenterWinDialog(this))
                    {
                        Logger.Error("SavedCompares_listBox_SelectedIndexChanged_1", $"exception|{ex.Message}|Connect string|{strConnectionStr}", ex);
                        MessageBox.Show($"Database Connection Error for connection|{strConnectionStr}\r\n{ex.Message}\r\nPlease check Log file for more details",
                            "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    return;
                }
            }
            if (S1Type_comboBox.Text == "XML")
            {   //Generating fields
                try
                {
                    S1Fields = GenerateFields.GenFields(S1FileLocationValue.Text);
                    if (MappingTable != null)
                    {
                        string extraField = MappingTable.Columns[0].ColumnName;
                        S1Fields = extraField + "," + S1Fields;
                    }
                }
                catch (Exception )
                {
                    using (new CenterWinDialog(this))
                    MessageBox.Show("File Error - The location of the file has changed");// + XMLFileError);
                    return;
                }
            }
            if (S1Type_comboBox.Text == "CSV")
            {   //Generating fields
                try
                {
                    S1Fields = GenerateFields.GenFieldsCSV(S1FileLocationValue.Text, S1CSVDelim.Text);
                }
                catch (Exception )
                {
                    using (new CenterWinDialog(this))
                        MessageBox.Show("File Error - The location of the file has changed");//+ CSVFileError);
                    return;
                }
            }

            if (S1Type_comboBox.Text == "REPORT")
            {   //Generating fields
                try
                {
                    S1Fields = GenerateFields.GenFieldsREPORT(S1FileLocationValue.Text, S1OpicsRepFileLoc.Text);
                }
                catch (Exception ex)
                {
                    using (new CenterWinDialog(this))
                        MessageBox.Show("File Error - The location of the file has changed");//+ CSVFileError);
                    return;
                }
            }

            if (S1Type_comboBox.Text == "EXCEL")
            {   //Generating fields
                try
                {
                    S1Fields = GenerateFields.GenFieldsExcel(S1FileLocationValue.Text);
                    if (MappingTable != null)
                    {
                        string extraField = MappingTable.Columns[0].ColumnName;
                        S1Fields = extraField + "," + S1Fields;
                    }
                }
                catch (Exception ex)
                {
                    using (new CenterWinDialog(this))
                        MessageBox.Show("File Error - The location of the file has changed");//+ CSVFileError);
                    return;
                }
            }

            if (S1Type_comboBox.Text == "SWIFT")
            {   //Generating fields
                try
                {
                    S1Fields = GenerateFields.GenFieldsSWIFT(S1FileLocationValue.Text);
                }
                catch (Exception)
                {
                    using (new CenterWinDialog(this))
                        MessageBox.Show("File Error - The location of the file has changed");//+ CSVFileError);
                    return;
                }
            }

            S1Fields = S1Fields.TrimEnd(',');
            List<string> GeneratedFields = S1Fields.Split(',').ToList();

            string SelectedKF = KeyFieldsValue_comboBox.Text;
            List<string> SelectedKFList = SelectedKF.Split(',').ToList();

            string SelectedSF = ShowFieldsValue_comboBox.Text;
            List<string> SelectedSFList = SelectedSF.Split(',').ToList();

            string SelectedCF = CompareFieldsValue_comboBox.Text;
            List<string> SelectedCFList = SelectedCF.Split(',').ToList();
            List<string> SelectedCFListWithTolerance = ReadCompareEntry.CompareFields.TrimStart('=').Split(';').ToList();

            if (ReadCompareEntry.CompareFields.StartsWith("="))
            {
                useTolerance = true;
            }

            string SelectedRowF = RowFieldsValue.Text;
            List<string> SelectedRowFList = SelectedRowF.Split(',').ToList();

            string SelectedColF = ColumnFieldsValue.Text;
            List<string> SelectedColumnFList = SelectedColF.Split(',').ToList();


            //Populating fields
            int i = 0;

            ((ToleranceGrid)CompareCustomComboBox.DropDownControl).dataGridView1.Rows.Clear();

            foreach (string singlefield in S1Fields.Split(','))
            {
                CCBoxItem item = new CCBoxItem(singlefield, i);
                i++;
                string Newsinglefield = singlefield.Trim();
                
                //KeyFields
                KeyFieldsValue_comboBox.Items.Add(item);
                foreach (string SKF in SelectedKFList)
                {
                    string NewKF = SKF.Trim();
                    if (Newsinglefield == NewKF)
                    {
                        KeyFieldsValue_comboBox.SetItemCheckState(i - 1, CheckState.Checked);
                        break;
                    }
                    continue;
                }

                //ShowFields
                ShowFieldsValue_comboBox.Items.Add(item);
                foreach (string SSF in SelectedSFList)
                {
                    string NewSF = SSF.Trim();
                    if (Newsinglefield == NewSF)
                    {
                        ShowFieldsValue_comboBox.SetItemCheckState(i - 1, CheckState.Checked);
                        break;
                    }
                    continue;
                }

                //CompareFields
                CompareFieldsValue_comboBox.Items.Add(item);
                string foundString = null;
                foreach (string SCF in SelectedCFList)
                {
                    string NewCF = SCF.Trim();
                    if (Newsinglefield == NewCF)
                    {
                        CompareFieldsValue_comboBox.SetItemCheckState(i - 1, CheckState.Checked);
                        foundString = SCF.Trim();
                        break;
                    }
                    continue;
                }

                if (useTolerance == false)
                {
                    if (foundString != null)
                        ((ToleranceGrid)CompareCustomComboBox.DropDownControl).AddGridItem(item + "|" + 1 + "||");
                    else
                        ((ToleranceGrid)CompareCustomComboBox.DropDownControl).AddGridItem(item + "|" + 0 + "||");
                }
                // New Compare Fields with Tolerance
                //  ((ToleranceGrid)CompareCustomComboBox.DropDownControl).AddGridItem("");
                //CompareCustomComboBox.DropDownControl = toleranceGrid;

                foundString = null;
                foreach (string SCF in SelectedCFListWithTolerance)
                {
                    
                    if (SCF.Trim().StartsWith(Newsinglefield))
                    {  
                        foundString = SCF.Trim();
                        break;
                    }
                    continue;
                }

                if (useTolerance == true)
                {
                    if (foundString != null)
                        ((ToleranceGrid)CompareCustomComboBox.DropDownControl).AddGridItem(foundString);
                    else
                        ((ToleranceGrid)CompareCustomComboBox.DropDownControl).AddGridItem(item + "|" + 0 + "||");
                }
                //  //  //


                //RowFields
                RowFieldsValue.Items.Add(item);
                foreach (string SRowF in SelectedRowFList)
                {
                    string NewRowF = SRowF.Trim();
                    if (Newsinglefield == NewRowF)
                    {
                        RowFieldsValue.SetItemCheckState(i - 1, CheckState.Checked);
                        break;
                    }
                    continue;
                }

                //ColumnFields
                ColumnFieldsValue.Items.Add(item);
                foreach (string SColF in SelectedCFList)
                {
                    string NewColF = SColF.Trim();
                    if (Newsinglefield == NewColF)
                    {
                        ColumnFieldsValue.SetItemCheckState(i - 1, CheckState.Checked);
                        break;
                    }
                    continue;
                }

            }

            KeyFieldsValue_comboBox.MaxDropDownItems = ComboBoxItemLength;
            ShowFieldsValue_comboBox.MaxDropDownItems = ComboBoxItemLength;
            CompareFieldsValue_comboBox.MaxDropDownItems = ComboBoxItemLength;
            RowFieldsValue.MaxDropDownItems = 5;
            ColumnFieldsValue.MaxDropDownItems = 5;

            //Status change
            CompareStatuslabel.Text = "Ready for Compare";
            CompareStatuslabel.ForeColor = Color.Black;
        }//-----------------------------------------------------

        private void ClearFieldItems()
        {
            KeyFieldsValue_comboBox.Text = "";
            ShowFieldsValue_comboBox.Text = "";
            CompareFieldsValue_comboBox.Text = "";
            RowFieldsValue.Text = "";
            RowFieldsValue.Text = "";
            ColumnFieldsValue.Text = "";

            KeyFieldsValue_comboBox.Items.Clear();
            ShowFieldsValue_comboBox.Items.Clear();
            CompareFieldsValue_comboBox.Items.Clear();
            RowFieldsValue.Items.Clear();
            ColumnFieldsValue.Items.Clear();
        }

        private static string ExtractFields(string compareFields)
        {
            string returnString = "";
            string workString = "";
            if (string.IsNullOrEmpty(compareFields)) return null;
            if (compareFields.StartsWith(V2_MARKER))
            {
                int index = compareFields.IndexOf(V2_MARKER);
                workString = (index < 0)
                    ? compareFields
                    : compareFields.Remove(index, V2_MARKER.Length);
                string[] rows = workString.Split(';');


                foreach (string row in rows)
                {
                    string[] values = row.Split('|');

                    string fieldName = values[0];

                    returnString += fieldName + ", ";
                }
                returnString = returnString.TrimEnd(' ').TrimEnd(',');
            }
            else
                returnString = compareFields;
            return returnString;
        }

        private string GetFieldsFromDT(DataTable dt)
        {
            string fields = "";

            foreach (DataColumn col in dt.Columns)
            {
                fields += col.ColumnName + ", ";
            }

            int idx = fields.LastIndexOf(",");

            fields = fields.Remove(idx);

            //fields = fields.TrimEnd(',');
            return fields;
        }

        public string adjustQueryForFieldExtraction(string queryForFields)
        {
            string rString = "";

            bool USE_SQL_PARSER = false;

            if (USE_SQL_PARSER)
            {
                SqlParser myParser = new SqlParser();
                myParser.Parse(queryForFields);

                string myOrginalWhereClause = myParser.WhereClause;

                if (string.IsNullOrEmpty(myOrginalWhereClause) == false)
                {
                    myOrginalWhereClause = myOrginalWhereClause.Replace("UNION ALL", "");
                }

                if (string.IsNullOrEmpty(myOrginalWhereClause))
                    myParser.WhereClause = "1=2";
                else
                    myParser.WhereClause = string.Format("({0}) AND ({1})", myOrginalWhereClause, "1=2");

                rString = myParser.ToText();
            }
            else
            {
                string[] separatingStrings = { "WHERE", "where", "Where"  };
                //string[] parts = queryForFields.ToUpper().Split(separatingStrings, System.StringSplitOptions.RemoveEmptyEntries);
                string[] parts = queryForFields.Split(separatingStrings, System.StringSplitOptions.RemoveEmptyEntries);

                rString = parts[0] + " WHERE 1=2";
            }
            
            
            return rString;
        }
        
        private void RunButton_Click_1(object sender, EventArgs e)
        {
            
            if (tabControl1.SelectedTab == Queries_tabPage)
            {
                RunQuery(); 
            }
            else if (tabControl1.SelectedTab == Compare_tabPage)
            {
                compareFile();
            }
            else if (tabControl1.SelectedTab == dirComp_tab)
            {

                compareDir();
                CompareStatuslabel.Text = "Directory Compare Completed";

            }
            else if (tabControl1.SelectedTab == XmlCompare_tabPage)
            {
                xmlCompare();
                CompareStatuslabel.Text = "XML File Compare Completed";
            }
            else if (tabControl1.SelectedTab == this.text_compare)
            {
                textCompare();
                CompareStatuslabel.Text = "Text File Compare Completed";
            }

            else if (tabControl1.SelectedTab == Batch_tabPage)
            {
                RunBatch();
                using (new CenterWinDialog(this))
                    MessageBox.Show("Finished executing Compare Batch");
            }

        }

        private void RunBatch()
        {
            // testing

            DataCompareError error = new DataCompareError();

            MarsCompare mc = new MarsCompare();
            //mc.RunCompare("PLUPD_V2", 
            //                @"C:\MDEV\xmlCompareTest\Data\PLUPDRep53mod.xml", 
            //                @"C:\MDEV\xmlCompareTest\Data\PLUPDRep56mod.xml", 
            //                @"c:\temp\PLUPD_RESULT1.xlsx", out error);

            //mc.RunCompare("PLUPD_V2",
            //               @"C:\MDEV\xmlCompareTest\Data\PLUPDRep53mod.xml",
            //               @"C:\MDEV\xmlCompareTest\Data\PLUPDRep56mod.xml",
            //               @"c:\temp\PLUPD_RESULT2.xlsx", out error);

            //mc.RunCompare("PLUPD_V2",
            //               @"C:\MDEV\xmlCompareTest\Data\PLUPDRep53mod.xml",
            //               @"C:\MDEV\xmlCompareTest\Data\PLUPDRep56mod.xml",
            //               @"c:\temp\PLUPD_RESULT3.xlsx", out error);


            //string batchConfigFileName = @"C:\Users\Alex\Documents\CompareConfigSetup.xlsx";
           
            string batchConfigFileName = BatchConfigFilePath.Text;
            string extension = Path.GetExtension(batchConfigFileName).ToLower();
            string message = "";

            if (File.Exists(batchConfigFileName) == false)
            {
                using (new CenterWinDialog(this))
                    MessageBox.Show("Error: File " + batchConfigFileName + " Not found");
                return;
            }
            else
            if (extension.Equals(".xlsx") == false)
            {
                using (new CenterWinDialog(this))
                    MessageBox.Show("Error: Only files with extension .xlsx are allowed");
                return;
            }
            else
            if (IsFileLocked(new FileInfo(batchConfigFileName)) == true)
            {
                using (new CenterWinDialog(this))
                    MessageBox.Show("Error: File " + batchConfigFileName + " is locked");
                return;
            }
            else
            if (IsOpenByExcel(batchConfigFileName, out message) == true)
            {
                using (new CenterWinDialog(this))
                    MessageBox.Show("Error: Reading File " +  "\n " + message);
                return;
            }

            else
            if (FileContainsRequiredFields(batchConfigFileName) == false)
            {
                using (new CenterWinDialog(this))
                    MessageBox.Show("Error: Please make sure all required fields are included in Batch Configuration File \n" +
                                    "Name, Action, CompareConfigID, File1, File2, OutputFile, Status, Comment, OutputFileLink");
                return;
            }

            DataCompareBatch batch = new DataCompareBatch(batchConfigFileName, this.RunAll_cb.Checked);
            batch.Run(mc);

        }

        string[] requiredFields = { "Name", "Action", "CompareConfigID", "File1", "File2", "OutputFile", "Status", "Comment", "OutputFileLink" };
        private bool FileContainsRequiredFields(string batchConfigFileName)
        {
            bool rc = true;
            List<string> excelFileHeaders = DataCompareBatchConfig.GetHeadersFromExcelFile(batchConfigFileName);

            foreach (string str in requiredFields)
            {
                if (excelFileHeaders.Contains(str) == false)
                {
                    rc = false;
                    break;
                }
            }

            return rc;
        }

        bool IsOpenByExcel(string filePath, out string message)
        {
            bool status = false;
            message = "";
            try
            {
                XLWorkbook workBook = new XLWorkbook(filePath);
            }
            catch (System.IO.IOException ex)
            {
                status = true;
                message = ex.Message;
            }
            return status;
        }

        protected  bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }

        private void textCompare()
        {
            Cursor.Current = Cursors.WaitCursor;
            string file1 = this.textFileCompareTextBox1.Text;
            string file2 = this.textFileCompareTextBox2.Text;

            string outputFile = outDirTxt.Text + @"\TextFileCompareResult.xlsx";
            TextFileCompare tfc = new TextFileCompare();
            //tfc.test();
            //string file1 = @"C:\Users\Marquis\Documents\Compare\Data Files for Compare - Nov 4 - Demo\swift\imtf8.b01";
            //string file2 = @"C:\Users\Marquis\Documents\Compare\Data Files for Compare - Nov 4 - Demo\swift\imtf9.b01";
            tfc.Compare(file1, file2, outputFile);
            Process.Start(outputFile);
            Cursor.Current = Cursors.Default;
        }

        private void compareFile ()
        {
            Cursor.Current = Cursors.WaitCursor;
            if (filename == "")
            {
                using (new CenterWinDialog(this))
                MessageBox.Show("No Config File detected. Please load Config File.");
                return;
            }
            else
            {
                CompareStatuslabel.Text = "Loading Compare Data";

                try
                {
                    ComparewithID CompareData = new ComparewithID();

                    // Compare ID
                    if (CompareIDValue.Text != "")
                    {
                        CompareData.CompareID = CompareIDValue.Text;
                    }
                    else
                    {
                        using (new CenterWinDialog(this))
                        MessageBox.Show("Missing Field: CompareID");
                        return;
                    }

                    CompareData.S1Type = S1Type_comboBox.Text;
                    string message = "";
                    if (CompareData.S1Type == "DATABASE")
                    {
                       
                        // S1 Connection ID
                        if (S1ConnectionID_comboBox.Text != "")
                        {
                            CompareData.S1DBConn = S1ConnectionID_comboBox.Text;
                        }
                        else
                        {
                            using (new CenterWinDialog(this))
                            MessageBox.Show("Missing Field: Source 1 ConnectionID");
                            return;
                        }
                        //S1 Conn String
                        DBConnectionwithID GetS1ConnString = new DBConnectionwithID();
                        GetS1ConnString = ReadConfigXML.GetConnectionFromID(S1ConnectionID_comboBox.Text, filename);
                        CompareData.S1ConnString = GetS1ConnString.BuildConnectionString();
                        CompareData.S1DBType = GetS1ConnString.DatabaseType;
                        // S1 Query ID
                        if (S1QueryID_comboBox.Text != "")
                        {
                            CompareData.S1QueryID = S1QueryID_comboBox.Text;
                        }
                        else
                        {
                            using (new CenterWinDialog(this))
                            MessageBox.Show("Missing Field: Source 1 QueryID");
                            return;
                        }
                        
                        //S1 Query
                        CompareData.S1Query = ReadConfigXML.GetQueryFromID(S1QueryID_comboBox.Text, filename);
                    }
                    
                    // S1 File Location
                    if (CompareData.S1Type != "DATABASE")
                    {
                        if (S1FileLocationValue.Text != "")
                        {
                            CompareData.S1FileLocation = S1FileLocationValue.Text;
                            CompareData.S1OpicsRepFileLoc = this.S1OpicsRepFileLoc.Text;
                            CompareData.S1CSVDelim = S1CSVDelim.Text;
                        }
                        else
                        {
                            using (new CenterWinDialog(this))
                            MessageBox.Show("Missing Field: Source 1 File Location");
                            return;
                        }
                    }

                    CompareData.S2Type = S2Type_comboBox.Text;
                    if (CompareData.S2Type == "DATABASE")
                    {
                        // S2 Connection ID
                        if (S2ConnectionID_comboBox.Text != "")
                        {
                            CompareData.S2DBConn = S2ConnectionID_comboBox.Text;
                        }
                        else
                        {
                            using (new CenterWinDialog(this))
                            MessageBox.Show("Missing Field: Source 2 ConnectionID");
                            return;
                        }
                        
                        //S2 Conn String
                        DBConnectionwithID GetS2ConnString = new DBConnectionwithID();
                        GetS2ConnString = ReadConfigXML.GetConnectionFromID(S2ConnectionID_comboBox.Text, filename);
                        CompareData.S2ConnString = GetS2ConnString.BuildConnectionString();
                        CompareData.S2DBType = GetS2ConnString.DatabaseType;

                        // S2 QueryID
                        if (S2QueryID_comboBox.Text != "")
                        {
                            CompareData.S2QueryID = S2QueryID_comboBox.Text;
                        }
                        else
                        {
                            using (new CenterWinDialog(this))
                            MessageBox.Show("Missing Field: Source 2 QueryID");
                            return;
                        }
                        
                        //S2 Query
                        CompareData.S2Query = ReadConfigXML.GetQueryFromID(S2QueryID_comboBox.Text, filename);
                    }

                    // S2 File Location
                    if (CompareData.S2Type != "DATABASE")
                    {
                        if (S2FileLocationValue.Text != "")
                        {
                            CompareData.S2FileLocation = S2FileLocationValue.Text;
                            CompareData.S2OpicsRepFileLoc = this.S2OpicsRepFileLoc.Text;
                            CompareData.S2CSVDelim = S2CSVDelim.Text;
                        }
                        else
                        {
                            using (new CenterWinDialog(this))
                            MessageBox.Show("Missing Field: Source 2 File Location");
                            return;
                        }
                    }

                    // do field related stuff only if fields are relevant , i.e do not do it for TEXT compare
                    if (CompareData.S1Type != "TEXT")
                    {
                        // Key Fields
                        if (KeyFieldsValue_comboBox.Text != "")
                        {
                            CompareData.KeyFields = KeyFieldsValue_comboBox.Text;
                        }
                        else
                        {
                            using (new CenterWinDialog(this))
                                MessageBox.Show("Missing Field: Key Fields");
                            return;
                        }

                        // Show Fields
                        if (ShowFieldsValue_comboBox.Text != "")
                        {
                            CompareData.ShowFields = ShowFieldsValue_comboBox.Text;
                        }
                        else
                        {
                            using (new CenterWinDialog(this))
                                MessageBox.Show("Missing Field: Show Fields");
                            return;
                        }

                        // Compare Fields
                        if (CompareFieldsValue_comboBox.Text != "")
                        {
                            //CompareData.CompareFields = CompareFieldsValue_comboBox.Text;

                            CompareData.CompareFields = ((ToleranceGrid)CompareCustomComboBox.DropDownControl).ItemsToCompare();

                        }
                        else
                        {
                            using (new CenterWinDialog(this))
                                MessageBox.Show("Missing Field: Compare Fields");
                            return;
                        }

                        // Row Fields
                        if (RowFieldsValue.Text != "")
                        {
                            CompareData.RowFields = RowFieldsValue.Text;
                        }
                        else
                        {
                            CompareData.RowFields = KeyFieldsValue_comboBox.Text;
                            //using (new CenterWinDialog(this))
                            //MessageBox.Show("Missing Field: Row Fields");
                            //return;
                        }

                        // Column Fields
                        if (ColumnFieldsValue.Text != "")
                        {
                            CompareData.ColumnFields = ColumnFieldsValue.Text;
                        }
                        else
                        {
                            CompareData.ColumnFields = CompareFieldsValue_comboBox.Text;
                            //using (new CenterWinDialog(this))
                            //MessageBox.Show("Missing Field: Column Fields");
                            //return;
                        }

                        // Output filtering

                        CompareData.OutputFilter = this.FilterTextBox.Text;
                        CompareData.OutputOrderBy = this.OrderByTextBox.Text;
                        CompareData.OutputFilterApply = this.ApplyCheckBox.Checked;

                        if (FieldSanityCheck(CompareData.ShowFields, CompareData.KeyFields, CompareData.CompareFields, out message) == false)
                        {
                            using (new CenterWinDialog(this))
                                MessageBox.Show(message);
                            return;
                        }

                        //logging
                        log.Info("S1 Type: " + CompareData.S1Type);
                        log.Info("S1 ConnString: " + CompareData.S1ConnString);
                        log.Info("S1 Query: " + CompareData.S1Query);
                        log.Info("S1 FileLocation: " + CompareData.S1FileLocation);
                        log.Info("S2 Type: " + CompareData.S2Type);
                        log.Info("S2 ConnString: " + CompareData.S2ConnString);
                        log.Info("S2 Query: " + CompareData.S2Query);
                        log.Info("S2 FileLocation: " + CompareData.S2FileLocation);
                        log.Info("KeyFields: " + CompareData.KeyFields);
                        log.Info("ShowFields: " + CompareData.ShowFields);
                        log.Info("CompareFields: " + CompareData.CompareFields);
                        log.Info("RowFields: " + CompareData.RowFields);
                        log.Info("ColumnFields: " + CompareData.ColumnFields);
                        log.Info("OutputFileLoc: " + CompareData.OFileLocation);
                    }

                    // Output File
                    if (outDirTxt.Text != "")
                    {
                        CompareData.OFileLocation = outDirTxt.Text;
                    }
                    else
                    {
                        using (new CenterWinDialog(this))
                        MessageBox.Show("Missing Field: Output File Location");
                        return;
                    }

                  

                    //Call ExecuteCompare and Send CompareData
                    CompareStatuslabel.Text = "Running Compare";
                    CompareData.InteractiveMode = true;

                    DataCompareError error = new DataCompareError();
                    ExecuteCompare.ExecuteCompareProgram(CompareData, ref error);

                    //status
                    CompareStatuslabel.Text = "Compare Complete";
                    CompareStatuslabel.ForeColor = Color.Green;

                }
                catch (Exception ex)
                {
                    using (new CenterWinDialog(this))
                        MessageBox.Show("Error in Compare \n Exception: \n" + ex);// + CompareError);
                    return;
                }
            }
        }

        public static bool FieldSanityCheck(string showFields, string keyFields, string compareFields, out string message)
        {
            bool checkPassed = true;
            message = "";

            string keyFieldMessage = "";
            string compareFieldMessage = "";

            var showFieldsList = showFields.Split(',').ToList().Select(s => s.Trim()).ToList();


            var keyFieldsList = keyFields.Split(',').ToList().Select(s => s.Trim()).ToList();

            // var compareFieldsList = compareFields.Split(',').ToList().Select(s => s.Trim()).ToList();
            var compareFieldsList = (ExtractFields(compareFields)).Split(',').ToList().Select(s => s.Trim()).ToList();

            foreach (var key1 in keyFieldsList)
            {
                if (showFieldsList.Contains(key1) == false)
                {
                    keyFieldMessage += "<" + key1 + "> ";
                    checkPassed = false;
                }
            }

            foreach (var comp1 in compareFieldsList)
            {
                if (!string.IsNullOrWhiteSpace(comp1))
                {
                    if (showFieldsList.Contains(comp1) == false)
                    {
                        compareFieldMessage += "<" + comp1 + "> ";
                        checkPassed = false;
                    }
                }
            }

            if (keyFieldMessage.Trim().Length != 0)
                message += "Warning: following key fields are not present in 'Show Fields' list: " + keyFieldMessage + "\n";

            if (compareFieldMessage.Trim().Length != 0)
                message += "Warning: following compare fields are not present in 'Show Fields' list: " + compareFieldMessage + "\n";

            return checkPassed;
        }

        private bool FieldSanityCheck()
        {
            throw new NotImplementedException();
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {

        }

        private void S1TypeDropDownClosed(object sender, EventArgs e)
        {
            
        }

        private void S1TypeSelectedIndexChanged(object sender, EventArgs e)
        {
            S2Type_comboBox.Text = S1Type_comboBox.Text;


            if (S1Type_comboBox.Text == "DATABASE")
            {
             
                S1ConnectionID_comboBox.Enabled = true;
                S1QueryID_comboBox.Enabled = true;
                //S1DBFieldsButton.Enabled = true;
                S1TestConn_button.Enabled = true;

                S1BrowseButton.Enabled = false;
                S1FileLocationValue.Enabled = false;
                S1OpicsRepFileLoc.Enabled = false;
            }
            else
            {
                S1BrowseButton.Enabled = true;
                S1FileLocationValue.Enabled = true;

                S1ConnectionID_comboBox.Enabled = false;
                S1QueryID_comboBox.Enabled = false;
                S1DBFieldsButton.Enabled = false;
                S1TestConn_button.Enabled = false;
               
            }
        }

        private void S2TypeSelectedIndexChanged(object sender, EventArgs e)
        {
            if (S2Type_comboBox.Text == "DATABASE")
            {
                S2ConnectionID_comboBox.Enabled = true;
                S2QueryID_comboBox.Enabled = true;
                //S2DBFieldsButton.Enabled = true;
                S2TestConn_button.Enabled = true;

                S2BrowseButton.Enabled = false;
                S2FileLocationValue.Enabled = false;
                S2OpicsRepFileLoc.Enabled = false;
            }
            else
            {
                S2BrowseButton.Enabled = true;
                S2FileLocationValue.Enabled = true;

                S2ConnectionID_comboBox.Enabled = false;
                S2QueryID_comboBox.Enabled = false;
                S2DBFieldsButton.Enabled = false;
                S2TestConn_button.Enabled = false;
            }
        }


        private void S1TestConn_button_Click(object sender, EventArgs e)
        {
            if (filename == "")
            {
                using (new CenterWinDialog(this))
                MessageBox.Show("No Config File detected. Please load Config File.");
                return;
            }
            else
            {
                DBConnectionwithID TestConnEntry = new DBConnectionwithID();
                TestConnEntry = ReadConfigXML.GetConnectionFromID(S1ConnectionID_comboBox.Text, filename);
                string TestConnString = TestConnEntry.BuildConnectionString();

                //DBConnPageConnStatus.Visible = true;
                try
                {
#if db4SQL
                    SqlConnection testconn = new SqlConnection(TestConnString);
#else
                    if (TestConnEntry.DatabaseType.Equals("Oracle"))
                    {
                        OracleConnection testconn = new OracleConnection(TestConnString);
                    }
                    else if (TestConnEntry.DatabaseType.Equals("SQL Server"))
                    {
                        SqlConnection testconn = new SqlConnection(TestConnString);
                    }
                                        
#endif
                    CompareStatuslabel.Text = "Successful database Connection";
                }
                catch
                {
                    //DBConnPageConnStatus.Text = "Connection Unsuccessful";
                    CompareStatuslabel.Text = "Failed database Connection";
                    log.Error("Failed database connection: " + TestConnString);
                }
            }
        }
        private static MLogger Logger = MLogger.GetLogger(typeof(DataCompareForm));
        private void TestConnection_button_Click(object sender, EventArgs e)
        {
            Logger.Info("TestConnection_button_Click","Begin");
            DBConnectionwithID TestConnEntry = new DBConnectionwithID();
            TestConnEntry.ConnectionID = ConnectionIDValue.Text;
            TestConnEntry.DatabaseType = DatabaseTypeValue.Text;
            TestConnEntry.Host = HostValue.Text;
            TestConnEntry.Port = PortValue.Text;
            TestConnEntry.Protocol = ProtocolValue.Text;
            TestConnEntry.ServiceName = ServiceNameValue.Text;
            TestConnEntry.UserID = UserIDValue.Text;
            TestConnEntry.Password = MarsEncodePwd.DecodeString(PasswordValue.Text);

            TestConnEntry.Sid = SidValue.Text;
            TestConnEntry.ConnString = ConnectionStringValue.Text;

            String TestConnString = TestConnEntry.BuildConnectionString();

          //  TestConnString = "Data Source = (DESCRIPTION = (ADDRESS_LIST = (ADDRESS = (PROTOCOL = TCP)(Host = 192.168.2.99)(PORT = 1521))) (CONNECT_DATA = (SERVICE_NAME = orcl.internal.marquis.nyc)));User Id=marquis; Password=marquis;";

          //  TestConnString = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=192.168.2.99)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=orcl.internal.marquis.nyc)));User Id=marquis;Password=marquis;";

           // Debug.WriteLine(TestConnString);
           
            if (TestDBConnection(TestConnEntry, TestConnString) == false)
            {
                DBConnPageConnStatus.Text = "Connection Unsuccessful";
                DBConnPageConnStatus.Visible = true;
            }
            else
            {
                DBConnPageConnStatus.Text = "Connection Successful";
                DBConnPageConnStatus.Visible = true;
            }
        }


        private bool TestDBConnection(DBConnectionwithID TestConnEntry, String TestConnString)
        {
            bool rc = true;
            
            try
            {
                if (TestConnEntry.DatabaseType.Equals("Oracle"))
                {

                    OracleConnection testconn = new OracleConnection(TestConnString);

                    testconn.Open();
                    testconn.Close();
                    DBConnPageConnStatus.Visible = true;
                }

                if (TestConnEntry.DatabaseType.Equals("SQL Server"))
                {
                    SqlConnection testconn = new SqlConnection(TestConnString);
                    testconn.Open();
                    testconn.Close();
                    DBConnPageConnStatus.Visible = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("TestDBConnection",string.Format("Exception:[{0}] stackTrace:[{1}] \r\ninnerMessage:[{2}]",ex.Message,ex.StackTrace,ex.InnerException==null?"No InnerInfo":ex.InnerException.Message),ex);
                rc = false;
            }

            return rc;
        }

        private bool GetTestDataTable(DBConnectionwithID TestConnEntry, String TestConnString, string query, out DataTable dt)
        {
            bool rc = true;
            dt = null;
            
            try
            {
                DataTable resultDt = ExecuteCompare.GetDataFromDatabase(TestConnEntry.DatabaseType, TestConnString, query);
                dt = resultDt;
            }
            catch (Exception ex)
            {
                string error = string.Format("Exception:[{0}] stackTrace:[{1}] \r\ninnerMessage:[{2}]", 
                    ex.Message, 
                    ex.StackTrace, ex.InnerException == null ? "No InnerInfo" : ex.InnerException.Message);
                Logger.Error("TestDBConnection", string.Format("Exception:[{0}] stackTrace:[{1}] \r\ninnerMessage:[{2}]", ex.Message, ex.StackTrace, ex.InnerException == null ? "No InnerInfo" : ex.InnerException.Message), ex);
                Console.WriteLine("TestDBConnection  " + error + "" + ex);
                rc = false;
            }

            return rc;
        }



        public bool ReadConfigFromDB = true;

        private void OpenButton_Click(object sender, EventArgs e)
        {
            LoadCompareConfig();
        }


        // private void OpenButton_Click(object sender, EventArgs e)
        private void LoadCompareConfig()
        {

            XmlDocument xmlDoc = null;

            if (ReadConfigFromDB == true)
            {
                xmlDoc = DomHelper.ReadXmlDoc();
            }
            else
            {
                if (filename == "")
                {
                    //Loading Config File
                    //string filename = "";
                    DialogResult result = openFileDialog3.ShowDialog(); // Show the dialog.
                    if (result == DialogResult.OK) // Test result.
                    {
                        filename = openFileDialog3.FileName;
                    }

                    xmlDoc = new XmlDocument();

                    if (filename != "")
                    {
                        xmlDoc.Load(@filename);
                    }
                }
            }
            

            //Populating list boxes and combo boxes
            //Queries
            var nodeRegion1 = xmlDoc.SelectNodes("//configuration/Queries/Query");
            foreach (XmlElement EachQuery in nodeRegion1)
            {
                try
                {
                    SavedQueries_listBox.Items.Add(EachQuery.Attributes["ID"].Value);
                    S1QueryID_comboBox.Items.Add(EachQuery.Attributes["ID"].Value);
                    S2QueryID_comboBox.Items.Add(EachQuery.Attributes["ID"].Value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception " + ex);
                }
               
            }

            /*
            if (SavedQueries_listBox.Items.Count == 0)
            {
                SavedQueries_listBox.Hide();
            }
            else
            {
                SavedQueries_listBox.SelectedIndex = 0;
            }
            */

            //Connections
            var nodeRegion2 = xmlDoc.SelectNodes("//configuration/Connections/DBConn");
            foreach (XmlElement EachConnection in nodeRegion2)
            {
                SavedConnections_listBox.Items.Add(EachConnection.Attributes["ID"].Value);
                S1ConnectionID_comboBox.Items.Add(EachConnection.Attributes["ID"].Value);
                S2ConnectionID_comboBox.Items.Add(EachConnection.Attributes["ID"].Value);
                TestConnectionsComboBox.Items.Add(EachConnection.Attributes["ID"].Value);
            }

            /*
            if (SavedConnections_listBox.Items.Count == 0)
            {
                SavedConnections_listBox.Hide();
            }
            else
            {
                SavedConnections_listBox.SelectedIndex = 0;
            }
            */


            //Compares
            XmlNodeList nodeRegion3 = xmlDoc.SelectNodes("//configuration/Compares/Compare");
                    
            foreach (XmlElement EachCompare in nodeRegion3)
            {
                SavedCompares_listBox.Items.Add(EachCompare.Attributes["ID"].Value);
            }

            /*
            if (SavedCompares_listBox.Items.Count == 0)
            {
                SavedCompares_listBox.Hide();
            }
            else
            {
                //SavedCompares_listBox.SelectedIndex = 0;
            }
            */

            //Profiles
            XmlNodeList nodeRegion4 = xmlDoc.SelectNodes("//configuration/Profiles/Profile");

            foreach (XmlElement EachProfile in nodeRegion4)
            {
                SavedProfiles_listBox.Items.Add(EachProfile.Attributes["ID"].Value);
            }

            if (SavedProfiles_listBox.Items.Count == 0)
            {
                SavedProfiles_listBox.Hide();
            }
            else
            {
                //SavedCompares_listBox.SelectedIndex = 0;
            }





            // output directory path 

            XmlNodeList opdpname = xmlDoc.GetElementsByTagName("OutPutDirPath");

            for (int i = 0; i < opdpname.Count; i++)
            {
                outDirTxt.Text = opdpname[i].InnerText;
                break;

            }
            CompareStatuslabel.Text = "Ready";
            
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
           
            //delete
            DeleteWasClicked = true;

            if (tabControl1.SelectedTab == Queries_tabPage)
            {
                if (ConfirmDeletion("Database Query " + QueryIDValue.Text))
                    SavedQueries_listBox_SelectedIndexChanged_1(null, EventArgs.Empty);
            }

            if (tabControl1.SelectedTab == DBConnections_tabPage)
            {
                if (ConfirmDeletion("Database Connection " + ConnectionIDValue.Text))
                    SavedConnections_listBox_SelectionIndexChanged_1(null, EventArgs.Empty);
            }

            if (tabControl1.SelectedTab == Compare_tabPage)
            {
                if (ConfirmDeletion("Compare Setting " + CompareIDValue.Text))
                    SavedCompares_listBox_SelectedIndexChanged_1(null, EventArgs.Empty);
            }

            if (tabControl1.SelectedTab == setting_page)
            {
                if (ConfirmDeletion("Profile Setting " + ProfileNameTxt.Text))
                    SavedProfiles_listBox_SelectedIndexChanged(null, EventArgs.Empty);
            }

        }

        private bool ConfirmDeletion(string itemType)
        {
            bool rc = false;

            DialogResult dialogResult = MessageBox.Show("Are you sure you want to delete " + itemType + " ?", "Confirm delete of " + itemType, MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                rc = true;
            }
            else if (dialogResult == DialogResult.No)
            {
                rc = false;
            }

            return rc;
        }

        private void OutputFileBrowseButton_Click(object sender, EventArgs e)
        {
            saveFileDialog1.ShowDialog();
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            //OFileLocationValue.Text = saveFileDialog1.FileName;
        }

        private void S2TestConn_button_Click(object sender, EventArgs e)
        {
            if (filename == "")
            {
                using (new CenterWinDialog(this))
                MessageBox.Show("No Config File detected. Please load Config File.");
                return;
            }
            else
            {
                DBConnectionwithID TestConnEntry = new DBConnectionwithID();
                TestConnEntry = ReadConfigXML.GetConnectionFromID(S2ConnectionID_comboBox.Text, filename);
                string TestConnString = TestConnEntry.BuildConnectionString();

                if (TestDBConnection(TestConnEntry, TestConnString) == true)
                    CompareStatuslabel.Text = "Successful database Connection";
                else
                    CompareStatuslabel.Text = "Failed database Connection";
            }
        }

        private void S2DBFieldsButton_Click(object sender, EventArgs e)
        {
            if (filename == "")
            {
                using (new CenterWinDialog(this))
                MessageBox.Show("No Config File detected. Please load Config File.");
                return;
            }
            else
            {
                KeyFieldsValue_comboBox.Text = "";
                ShowFieldsValue_comboBox.Text = "";
                CompareFieldsValue_comboBox.Text = "";

                //Converting DB to Xml
                XmlDocument DataSource = new XmlDocument();
                DataTable dt = new DataTable();
                DBConnectionwithID ConnStringForFields = new DBConnectionwithID();
                ConnStringForFields = ReadConfigXML.GetConnectionFromID(S2ConnectionID_comboBox.Text, filename);
                string QueryForFields = ReadConfigXML.GetQueryFromID(S2QueryID_comboBox.Text, filename);

                if (ConnStringForFields.DatabaseType.Equals("Oracle"))
                {
#if db4SQL
                    using (SqlConnection sqlConnection = new SqlConnection(ConnStringForFields.BuildConnectionString()))
                    {
                        SqlCommand command = new SqlCommand(QueryForFields, sqlConnection);
                        SqlDataAdapter adapter = new SqlDataAdapter(command);
                        SqlCommandBuilder builder = new SqlCommandBuilder(adapter);
                        DataSet ds = new DataSet();
                        adapter.Fill(ds);
                        dt = ds.Tables[0];
                        Console.WriteLine("\ndbSource ready for access");
                    }
#else
                    using (OracleConnection sqlConnection = new OracleConnection(ConnStringForFields.BuildConnectionString()))
                    {
                        OracleCommand command = new OracleCommand(QueryForFields, sqlConnection);
                        OracleDataAdapter adapter = new OracleDataAdapter(command);
                        OracleCommandBuilder builder = new OracleCommandBuilder(adapter);
                        DataSet ds = new DataSet();
                        adapter.Fill(ds);
                        dt = ds.Tables[0];
                        Console.WriteLine("\ndbSource ready for access");
                    }
#endif
                }

                else
                    if (ConnStringForFields.DatabaseType.Equals("Oracle"))
                {
                    using (SqlConnection sqlConnection = new SqlConnection(ConnStringForFields.BuildConnectionString()))
                    {
                        SqlCommand command = new SqlCommand(QueryForFields, sqlConnection);
                        SqlDataAdapter adapter = new SqlDataAdapter(command);
                        SqlCommandBuilder builder = new SqlCommandBuilder(adapter);
                        DataSet ds = new DataSet();
                        adapter.Fill(ds);
                        dt = ds.Tables[0];
                        Console.WriteLine("\ndbSource ready for access");
                    }
                }

                DataSource = Conversion.DataTableToDom(dt);

                //Generating fields
                String DbFields = GenerateFields.GenDatabaseFields(DataSource);

                DbFields = DbFields.TrimEnd(',');

                //Populating fields
                int i = 0;
                foreach (string singlefield in DbFields.Split(','))
                {
                    CCBoxItem item = new CCBoxItem(singlefield, i);
                    i++;
                    KeyFieldsValue_comboBox.Items.Add(item);
                    ShowFieldsValue_comboBox.Items.Add(item);
                    CompareFieldsValue_comboBox.Items.Add(item);

                    RowFieldsValue.Items.Add(item);
                    ColumnFieldsValue.Items.Add(item);
                }
                KeyFieldsValue_comboBox.MaxDropDownItems = 15;
                ShowFieldsValue_comboBox.MaxDropDownItems = 15;
                CompareFieldsValue_comboBox.MaxDropDownItems = 15;

                RowFieldsValue.MaxDropDownItems = 15;
                ColumnFieldsValue.MaxDropDownItems =15;
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveButton.PerformClick();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void queyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = Queries_tabPage;
        }

        private void compareToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = Compare_tabPage;
        }

        private void DBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = DBConnections_tabPage;
        }

        private void delToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteButton.PerformClick();
        }

        private void RunCompareToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            RunButton.PerformClick();
        }

        /*private void userGuideToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process mydoc = new Process();

            mydoc.StartInfo.FileName = "C:\\Users\\textarvind\\Desktop\\MARSCompareGUI\\MARSCompare-UserGuide.pdf";

            mydoc.Start();
        }*/

        private void MarquisCompare_Load(object sender, EventArgs e)
        {
            //string initialString = "ID|0||;Amount|1|A|1;Rate|1|P|2;Date|1||";
            ToleranceGrid toleranceGrid = new ToleranceGrid();
            CompareCustomComboBox.DropDownControl = toleranceGrid;
        }

        /*
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenButton.PerformClick();
        }
        */
        private void dir1_button_Click(object sender, EventArgs e)
        {
            dir1_folderBrowserDialog.Description="Select Directory 1";
            if (System.IO.Directory.Exists(dir1_textBox.Text))
            {
                dir1_folderBrowserDialog.SelectedPath = dir1_textBox.Text;
                
            }
            else if (System.IO.Directory.Exists(dir2_textBox.Text))
            {
                dir1_folderBrowserDialog.SelectedPath = dir2_textBox.Text;
            }

            else
            {
                dir1_folderBrowserDialog.SelectedPath = "";
                //dir1_folderBrowserDialog.RootFolder = Environment.SpecialFolder.MyComputer;
            }

            DialogResult result = dir1_folderBrowserDialog.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                string dirPath = dir1_folderBrowserDialog.SelectedPath;
                dir1_textBox.Text = dirPath;
            }
        }

        private void dir2_button_Click(object sender, EventArgs e)
        {
            dir2_folderBrowserDialog.Description = "Select Directory 2";
            if (System.IO.Directory.Exists(dir2_textBox.Text))
            {
                dir2_folderBrowserDialog.SelectedPath = dir2_textBox.Text;

            }
            else if (System.IO.Directory.Exists(dir1_textBox.Text))
            {
                dir2_folderBrowserDialog.SelectedPath = dir1_textBox.Text;
            }
            else
            {
                dir2_folderBrowserDialog.SelectedPath = "";
                //dir2_folderBrowserDialog.RootFolder = Environment.SpecialFolder.MyComputer;
            }

            DialogResult result = dir2_folderBrowserDialog.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                string dirPath = dir2_folderBrowserDialog.SelectedPath;
                dir2_textBox.Text = dirPath;
            }
        }

             
        private void compareDir ()
        {
            CompareStatuslabel.Text = "Directory Compare Running";
            startTime_label.Text = DateTime.Now.ToString("G");
            endTime_label.Text = "";
            // make sure all the dir are calid
            if (!System.IO.Directory.Exists(outDirTxt.Text))
            {
                endTime_label.Text = DateTime.Now.ToString("G");
                MessageBox.Show(this, "Invalid Output Directory: " + outDirTxt.Text, "Directory Compare", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return;
            }

            if (!System.IO.Directory.Exists(dir1_textBox.Text))
            {
                endTime_label.Text = DateTime.Now.ToString("G");
                MessageBox.Show(this, "Invalid Directory 1: " + dir1_textBox.Text, "Directory Compare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                return;
            }
            if (!System.IO.Directory.Exists(dir2_textBox.Text))
            {
                endTime_label.Text = DateTime.Now.ToString("G");
                MessageBox.Show(this, "Invalid Directory 2: " + dir2_textBox.Text, "Directory Compare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                return;
            }

            CompareDirs dirComp = new CompareDirs();
            
            dirComp.init(dir1_textBox.Text, dir2_textBox.Text, outDirTxt.Text);
            Cursor.Current = Cursors.WaitCursor;
            CompareDirs.COMP_ERR retErr = dirComp.doDirCompare();
            Cursor.Current = Cursors.Default;
            /*Show(Window, String, String, MessageBoxButton, MessageBoxImage) */
            endTime_label.Text = DateTime.Now.ToString("G");

            if (retErr == CompareDirs.COMP_ERR.COMP_ERR_SUCCESS)
            {
                if (MessageBox.Show(this, "Successfully Completed " +  dirComp.getErrStr(CompareDirs.COMP_ERR.COMP_OUT_FILENAME) +"\n Would you like to open the file?",
                    "MARS Directory Compare", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == System.Windows.Forms.DialogResult.Yes)
                {
                    //open excel file.
                    Process.Start(dirComp.getErrStr(CompareDirs.COMP_ERR.COMP_OUT_FILENAME));
                }
            }
            else
            {
                MessageBox.Show(this, "Failed " + dirComp.getErrStr(retErr), "Directory Compare", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
                return;
        }

        private void xmlFileBrowseButton1_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "Select Input File Name for First XML file";
           
            openFileDialog1.Filter = "xml files (*.xml)|*.xml|All files (*.*)|*.*";
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                xmlCompareInputFileNameTextBox1.Text = openFileDialog1.FileName;
                xmlFileName1 = openFileDialog1.FileName;
            }
        }

        private void xmlFileBrowseButton2_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "Select Input File Name for Second XML file";
            openFileDialog1.Filter = "xml files (*.xml)|*.xml|All files (*.*)|*.*";
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                xmlCompareInputFileNameTextBox2.Text = openFileDialog1.FileName;
                xmlFileName2 = openFileDialog1.FileName;
            }
        }

        private void xmlConfigFileBrowseButton_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "Select Config File Name";
            openFileDialog1.Filter = "config files (*.config)|*.config|All files (*.*)|*.*";
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                xmlCompareConfigTextBox.Text = openFileDialog1.FileName;
                configFileName = openFileDialog1.FileName;
            }
        }

        /*
        private void xmlOutputFileBrowseButton1_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();

            dialog.Title = "Select Output File Name";
            dialog.Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*";
            DialogResult result = dialog.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                xmlCompareOutputFileNameTextBox.Text = dialog.FileName;
                excelFileName = openFileDialog1.FileName;
            }
        }
        */

        private void xmlCompare()
        { 
            try
            {

                string filePath = outDirTxt.Text;
                if (!System.IO.Directory.Exists(filePath))
                {
                    MessageBox.Show(this, "invalid output directory", "Directory Compare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                String timeStamp = DateTime.Now.ToString("yyMMddHHmmss");
                excelFileName = filePath + "\\MARS_XML_Comp_" + timeStamp + ".xlsx";

                xmlFileName1 = xmlCompareInputFileNameTextBox1.Text;
                xmlFileName2 = xmlCompareInputFileNameTextBox2.Text;
                //excelFileName = xmlCompareOutputFileNameTextBox.Text;
                configFileName = xmlCompareConfigTextBox.Text;
                Cursor.Current = Cursors.WaitCursor;
                CompareStatuslabel.Text = "XML Compare Started";
                Stopwatch stopWatch = JobContext.CreateStopwatch();
                XmlCompareConfigurator.Configure(configFileName);
                XmlCompareConfigurator.SetIgnoreState(this.ignoreCheckBox.Checked);
                XmlCompareJob job = new XmlCompareJob(xmlFileName1, xmlFileName2, excelFileName);
                job.Execute();

                // timing related stuff
                stopWatch.Stop();
                // Get the elapsed time as a TimeSpan value.
                JobContext.DisplayStopwatch("End Of job ");
                CompareStatuslabel.Text = "XML Compare completed successfully";
                if (MessageBox.Show(this, "XML Compare completed successfully \n Would you like to open the file?",
                    "MARS XML Compare", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == System.Windows.Forms.DialogResult.Yes)
                {
                    //open excel file.
                    Process.Start(excelFileName);
                }
            }
            catch (DivideByZeroException ex)
            {
                log.Fatal(ex);
                string logFileName = "";


                log4net.Repository.Hierarchy.Logger logger = (log4net.Repository.Hierarchy.Logger)log.Logger.Repository.GetCurrentLoggers()[0];
                foreach (log4net.Appender.IAppender appender in logger.Parent.Appenders)
                {
                    //Console.WriteLine("  Appender: {0}", appender.Name);
                    // just checking for RollingFile here, but could just as well check for all other appenders
                    if (appender.GetType() == typeof(log4net.Appender.RollingFileAppender))
                    {
                        log4net.Appender.RollingFileAppender rolling
                            = (log4net.Appender.RollingFileAppender)appender;
                        logFileName = rolling.File;
                        break;
                    }
                }
                CompareStatuslabel.Text = "XML Compare failed. Check log file for details. Log file: " + logFileName;
                Console.WriteLine("Caught exception.");
                Console.WriteLine(ex);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private void OutputFolderButton_Click(object sender, EventArgs e)
        {
            //dirout_folderBrowserDialog.Description = "Select output directory";
            //if (System.IO.Directory.Exists(outDirTxt.Text))
            //{
            //    dirout_folderBrowserDialog.SelectedPath = outDirTxt.Text;

            //}
            //else
            //{
            //    dirout_folderBrowserDialog.SelectedPath = "";
            //}

            //DialogResult result = dirout_folderBrowserDialog.ShowDialog(); // Show the dialog.
            //if (result == DialogResult.OK) // Test result.
            //{
            //    string dirPath = dirout_folderBrowserDialog.SelectedPath;
            //    outDirTxt.Text = dirPath;
            //}

            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = outDirTxt.Text;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                outDirTxt.Text = dialog.FileName;
            }
            BringToFront();

        }

        private void tESTToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //DataCompare.test();

            TextFileCompare tfc = new TextFileCompare();
            //tfc.test();
            string file1 = @"C:\Users\Marquis\Documents\Compare\Data Files for Compare - Nov 4 - Demo\swift\imtf8.b01";
            string file2 = @"C:\Users\Marquis\Documents\Compare\Data Files for Compare - Nov 4 - Demo\swift\imtf9.b01";
            tfc.Compare(file1, file2, @"c:\temp\result.xlsx");


        }

        private void MarquisCompare_Shown(object sender, EventArgs e)
        {
            LoadCompareConfig();
        }

        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ImportCompareConfig();
        }

        private void ImportButton_Click(object sender, EventArgs e)
        {
            ImportCompareConfig();
        }

        private void ImportCompareConfig()
        {
            string configFileName;
            openFileDialog1.Title = "Select Compare Configuration File";

            openFileDialog1.Filter = "config files (*.config)|*.config|All files (*.*)|*.*";
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                xmlCompareInputFileNameTextBox1.Text = openFileDialog1.FileName;
                configFileName = openFileDialog1.FileName;

                ComparewithID cfg = ConfigFileToCfg(configFileName);
                PopulateData(cfg);
            }
        }

        private void PopulateData(ComparewithID cfg)
        {
            CompareIDValue.Text = cfg.CompareID;
            S1Type_comboBox.Text = cfg.S1Type;
            S1ConnectionID_comboBox.Text = cfg.S1DBConn;
            S1QueryID_comboBox.Text = cfg.S1QueryID;
            S1FileLocationValue.Text = cfg.S1FileLocation;
            S2Type_comboBox.Text = cfg.S2Type;
            S2ConnectionID_comboBox.Text = cfg.S2DBConn;
            S2QueryID_comboBox.Text = cfg.S2QueryID;
            S2FileLocationValue.Text = cfg.S2FileLocation;

            KeyFieldsValue_comboBox.Text = cfg.KeyFields;
            ShowFieldsValue_comboBox.Text = cfg.ShowFields;
            CompareFieldsValue_comboBox.Text = cfg.CompareFields;
        }

        private ComparewithID ConfigFileToCfg(string configFileName)
        {
            ComparewithID cfg = new ComparewithID();
            AppConfig.Change(configFileName);

            string id = Path.GetFileNameWithoutExtension(configFileName);

            cfg.CompareID = id;
            cfg.S1Type = "XML";
            cfg.S1DBConn = "";
            cfg.S1QueryID = "";
            cfg.S1FileLocation = ConfigurationManager.AppSettings["xmlFileName1"];

            cfg.S2Type = "XML"; 
            cfg.S2DBConn = "";
            cfg.S2QueryID = "";
            cfg.S2FileLocation = ConfigurationManager.AppSettings["xmlFileName2"];

            cfg.KeyFields = ConfigurationManager.AppSettings["KeyFields"];
            cfg.ShowFields = ConfigurationManager.AppSettings["ShowFields"];
            cfg.CompareFields = ConfigurationManager.AppSettings["CompareFields"];

            cfg.RowFields = "";
            cfg.ColumnFields = "";

            return cfg;
        }

        private static DataCompareForm instance = null;


        public static DataCompareForm GetInstance()
        {
            if (instance == null)
                instance = new DataCompareForm();

            return instance;
        }

        private void DataCompareForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            /*
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
            */
        }

        private void button3_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "Select Input File Name for First Text file";

            openFileDialog1.Filter = "All files (*.*)|*.*";
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                this.textFileCompareTextBox1.Text = openFileDialog1.FileName;
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            openFileDialog1.Title = "Select Input File Name for Second Text file";

            openFileDialog1.Filter = "All files (*.*)|*.*";
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                this.textFileCompareTextBox2.Text = openFileDialog1.FileName;
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {

        }

        private void NewButton_Click(object sender, EventArgs e)
        {
            ClearDataEntryPanel();
        }




        private void ClearDataEntryPanel()
        {
            if (tabControl1.SelectedTab == Compare_tabPage)
            {
                CompareIDValue.Clear();
                S1Type_comboBox.SelectedIndex = -1;
                S2Type_comboBox.SelectedIndex = -1;
                S1ConnectionID_comboBox.SelectedIndex = -1;
                S2ConnectionID_comboBox.SelectedIndex = -1;
                S1QueryID_comboBox.SelectedIndex = -1;
                S2QueryID_comboBox.SelectedIndex = -1;
                S1FileLocationValue.Clear();
                S2FileLocationValue.Clear();
                S1OpicsRepFileLoc.Clear();
                S2OpicsRepFileLoc.Clear();

                S1CSVDelim.Clear();
                S2CSVDelim.Clear();

                KeyFieldsValue_comboBox.Items.Clear();
                KeyFieldsValue_comboBox.Text = "";

                ShowFieldsValue_comboBox.Items.Clear();
                ShowFieldsValue_comboBox.Text = "";

                CompareFieldsValue_comboBox.Items.Clear();
                CompareFieldsValue_comboBox.Text = "";

                CompareCustomComboBox.Text = "";
                ToleranceGrid toleranceGrid = (ToleranceGrid)CompareCustomComboBox.DropDownControl;
                toleranceGrid.InitGrid(null);

            }
            else if (tabControl1.SelectedTab == Queries_tabPage)
            {
                QueryIDValue.Clear();
                QueryValue.Clear();
            }

            else if (tabControl1.SelectedTab == this.DBConnections_tabPage)
            {
                ConnectionIDValue.Clear();
                DatabaseTypeValue.SelectedIndex = -1;
                HostValue.Clear();
                ProtocolValue.Clear();
                ServiceNameValue.Clear();
                PortValue.Clear();
                UserIDValue.Clear();
                PasswordValue.Clear();
            }

            else if (tabControl1.SelectedTab == this.setting_page)
            {
                outDirTxt.Clear();

                BaselineRptTxt.Clear();
                CompareRptTxt.Clear();
                BaselineFmtTxt.Clear();
                CompareFmtTxt.Clear();
                ProfileNameTxt.Clear();
            }

        }

        private void S1QueryID_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
           // AF changed on 1/28/2019
           // Only do this when combobox is empty
           if (KeyFieldsValue_comboBox.Items.Count == 0)
            {
                GetFieldsFromDB();
                S2QueryID_comboBox.SelectedIndex = S1QueryID_comboBox.SelectedIndex;
        }

        }

        private void GetFieldsFromDB()
        {
            KeyFieldsValue_comboBox.Text = "";
            ShowFieldsValue_comboBox.Text = "";
            CompareFieldsValue_comboBox.Text = "";
            RowFieldsValue.Text = "";
            ColumnFieldsValue.Text = "";

            KeyFieldsValue_comboBox.Items.Clear();
            ShowFieldsValue_comboBox.Items.Clear();
            CompareFieldsValue_comboBox.Items.Clear();
            RowFieldsValue.Items.Clear();
            ColumnFieldsValue.Items.Clear();
            String S1Fields = "";
            string strConnectionStr = "";

            if (S1Type_comboBox.Text == "DATABASE")
            {
                try
                {
                    //Converting DB to Xml
                    XmlDocument DataSource = new XmlDocument();
                    DataTable dt = new DataTable();
                    DBConnectionwithID ConnStringForFields = new DBConnectionwithID();
                    ConnStringForFields = ReadConfigXML.GetConnectionFromID(S1ConnectionID_comboBox.Text, filename);
                    string QueryForFields = ReadConfigXML.GetQueryFromID(S1QueryID_comboBox.Text, filename);

                    QueryForFields = adjustQueryForFieldExtraction(QueryForFields);

#if !db4SQL
                    if (ConnStringForFields.DatabaseType.Equals("Oracle"))
                        using (OracleConnection sqlConnection = new OracleConnection(strConnectionStr=ConnStringForFields.BuildConnectionString()))
                        {
                            OracleCommand command = new OracleCommand(QueryForFields, sqlConnection);
                            OracleDataAdapter adapter = new OracleDataAdapter(command);
                            OracleCommandBuilder builder = new OracleCommandBuilder(adapter);
                            DataSet ds = new DataSet();
                            adapter.Fill(ds);
                            dt = ds.Tables[0];
                            Console.WriteLine("\ndbSource ready for access");
                        }
                    else
                        if (ConnStringForFields.DatabaseType.Equals("SQL Server"))
                        using (SqlConnection sqlConnection = new SqlConnection(strConnectionStr = ConnStringForFields.BuildConnectionString()))
                        {
                            SqlCommand command = new SqlCommand(QueryForFields, sqlConnection);
                            SqlDataAdapter adapter = new SqlDataAdapter(command);
                            SqlCommandBuilder builder = new SqlCommandBuilder(adapter);
                            DataSet ds = new DataSet();
                            adapter.Fill(ds);
                            dt = ds.Tables[0];
                            CapitalizeDTColumns(dt);
                            Console.WriteLine("\ndbSource ready for access");
                        }
#else
                    using (SqlConnection sqlConnection = new SqlConnection(ConnStringForFields.BuildConnectionString()))
                    {
                        SqlCommand command = new SqlCommand(QueryForFields, sqlConnection);
                        SqlDataAdapter adapter = new SqlDataAdapter(command);
                        SqlCommandBuilder builder = new SqlCommandBuilder(adapter);
                        DataSet ds = new DataSet();
                        adapter.Fill(ds);
                        dt = ds.Tables[0];
                        Console.WriteLine("\ndbSource ready for access");
                    }
#endif

                    // AF get fields directly from DataTable
                    S1Fields = GetFieldsFromDT(dt);
                    //DataSource = Conversion.DataTableToDom(dt);

                    ////Generating fields
                    //S1Fields = GenerateFields.GenDatabaseFields(DataSource);
                }
                catch (Exception ex)
                {
                    using (new CenterWinDialog(this))
                    {
                        Logger.Error("SavedCompares_listBox_SelectedIndexChanged_1", $"exception|{ex.Message}|Connect string|{strConnectionStr}", ex);
                        MessageBox.Show($"Database Connection Error for connection|{strConnectionStr}\r\n{ex.Message}\r\nPlease check Log file for more details",
                            "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    return;
                }
            }

            S1Fields = S1Fields.TrimEnd(',');
            List<string> GeneratedFields = S1Fields.Split(',').ToList();

            string SelectedKF = KeyFieldsValue_comboBox.Text;
            List<string> SelectedKFList = SelectedKF.Split(',').ToList();

            string SelectedSF = ShowFieldsValue_comboBox.Text;
            List<string> SelectedSFList = SelectedSF.Split(',').ToList();

            string SelectedCF = CompareFieldsValue_comboBox.Text;
            List<string> SelectedCFList = SelectedCF.Split(',').ToList();

            string SelectedRowF = RowFieldsValue.Text;
            List<string> SelectedRowFList = SelectedRowF.Split(',').ToList();

            string SelectedColF = ColumnFieldsValue.Text;
            List<string> SelectedColumnFList = SelectedColF.Split(',').ToList();


            //Populating fields
            int i = 0;

            foreach (string singlefield in S1Fields.Split(','))
            {
                CCBoxItem item = new CCBoxItem(singlefield, i);
                i++;
                string Newsinglefield = singlefield.Trim();

                //KeyFields
                KeyFieldsValue_comboBox.Items.Add(item);
                ((ToleranceGrid)CompareCustomComboBox.DropDownControl).AddGridItem(singlefield + "|" + 0 + "||");
                foreach (string SKF in SelectedKFList)
                {
                    string NewKF = SKF.Trim();
                    if (Newsinglefield == NewKF)
                    {
                        KeyFieldsValue_comboBox.SetItemCheckState(i - 1, CheckState.Checked);

                        
                        break;
                    }
                    continue;
                }

                //ShowFields
                ShowFieldsValue_comboBox.Items.Add(item);
                foreach (string SSF in SelectedSFList)
                {
                    string NewSF = SSF.Trim();
                    if (Newsinglefield == NewSF)
                    {
                        ShowFieldsValue_comboBox.SetItemCheckState(i - 1, CheckState.Checked);
                        break;
                    }
                    continue;
                }

                //CompareFields
                CompareFieldsValue_comboBox.Items.Add(item);
                foreach (string SCF in SelectedCFList)
                {
                    string NewCF = SCF.Trim();
                    if (Newsinglefield == NewCF)
                    {
                        CompareFieldsValue_comboBox.SetItemCheckState(i - 1, CheckState.Checked);
                        break;
                    }
                    continue;
                }

                //RowFields
                RowFieldsValue.Items.Add(item);
                foreach (string SRowF in SelectedRowFList)
                {
                    string NewRowF = SRowF.Trim();
                    if (Newsinglefield == NewRowF)
                    {
                        RowFieldsValue.SetItemCheckState(i - 1, CheckState.Checked);
                        break;
                    }
                    continue;
                }

                //ColumnFields
                ColumnFieldsValue.Items.Add(item);
                foreach (string SColF in SelectedCFList)
                {
                    string NewColF = SColF.Trim();
                    if (Newsinglefield == NewColF)
                    {
                        ColumnFieldsValue.SetItemCheckState(i - 1, CheckState.Checked);
                        break;
                    }
                    continue;
                }

            }

            KeyFieldsValue_comboBox.MaxDropDownItems = 15;
            ShowFieldsValue_comboBox.MaxDropDownItems = 15;
            CompareFieldsValue_comboBox.MaxDropDownItems = 15;
            RowFieldsValue.MaxDropDownItems = 15;
            ColumnFieldsValue.MaxDropDownItems = 15;

            //Status change
            CompareStatuslabel.Text = "Ready for Compare";
            CompareStatuslabel.ForeColor = Color.Black;

        }

        private void TestQueryButton_Click(object sender, EventArgs e)
        {
            Logger.Info("TestQueryButton_Click", "Begin");
            RunQuery();
        }

        private void RunQuery()
        {
            if (TestConnectionsComboBox.SelectedItem == null)
                return;

            string SelectedConnectionID = TestConnectionsComboBox.SelectedItem.ToString();

            DBConnectionwithID TestConnEntry = ReadConfigXML.GetConnectionFromID(SelectedConnectionID, filename);
            TestConnEntry.Password = MarsEncodePwd.DecodeString(TestConnEntry.Password);
            //  NewDBConnEntry.Password = MarsEncodePwd.EncodeString(PasswordValue.Text);

            String TestConnString = TestConnEntry.BuildConnectionString();

            //  TestConnString = "Data Source = (DESCRIPTION = (ADDRESS_LIST = (ADDRESS = (PROTOCOL = TCP)(Host = 192.168.2.99)(PORT = 1521))) (CONNECT_DATA = (SERVICE_NAME = orcl.internal.marquis.nyc)));User Id=marquis; Password=marquis;";

            //  TestConnString = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=192.168.2.99)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=orcl.internal.marquis.nyc)));User Id=marquis;Password=marquis;";

            Debug.WriteLine(TestConnString);

            if (TestDBConnection(TestConnEntry, TestConnString) == false)
            {
                TestConnectionStatus.Text = "Connection Unsuccessful";
                TestConnectionStatus.Visible = true;
            }
            else
            {
                TestConnectionStatus.Text = "Connection Successful";
                TestConnectionStatus.Visible = true;
            }

            DataTable dt;

            bool rc = GetTestDataTable(TestConnEntry, TestConnString, QueryValue.Text, out dt);

            CapitalizeDTColumns(dt);

            if (rc)
            {
                QueryResultForm form = QueryResultForm.Create(dt);
                form.Text = "Query: " + QueryValue.Text;
                form.Show();
            }
        }

        private void CapitalizeDTColumns(DataTable dt)
        {
            if (dt == null) return;
            foreach (DataColumn col in dt.Columns)
            {
                col.ColumnName = col.ColumnName.ToUpper();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                string file = openFileDialog1.FileName;
                this.S1OpicsRepFileLoc.Text = file;
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                string file = openFileDialog1.FileName;
                this.S2OpicsRepFileLoc.Text = file;
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            DataCompare.test();
        }

        private void label21_Click(object sender, EventArgs e)
        {

        }

        private void BaselineFmtFolderButton_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = BaselineFmtTxt.Text;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                BaselineFmtTxt.Text = dialog.FileName;
            }
            BringToFront();
        }

        private void BaselineRptFolderButton_Click(object sender, EventArgs e)
        {
       
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = BaselineRptTxt.Text;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                BaselineRptTxt.Text = dialog.FileName;
            }
            BringToFront();
        }

        private void CompareFmtFolderButton_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = CompareFmtTxt.Text;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                CompareFmtTxt.Text = dialog.FileName;
            }
            BringToFront();
        }

        private void CompareRptFolderButton_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = CompareRptTxt.Text;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                CompareRptTxt.Text = dialog.FileName;
            }
            BringToFront();
        }

        private void label33_Click(object sender, EventArgs e)
        {

        }

        private void SetDefaultBtn_Click(object sender, EventArgs e)
        {

            UpdateSetting("CompareResultFolder", this.outDirTxt.Text);
            UpdateSetting("BaselineFmtFolder", this.BaselineFmtTxt.Text);
            UpdateSetting("BaselineRptFolder", this.BaselineRptTxt.Text);
            UpdateSetting("CompareFmtFolder", this.CompareFmtTxt.Text);
            UpdateSetting("CompareRptFolder", this.CompareFmtTxt.Text);
        }

        private void SavedProfiles_listBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DeleteWasClicked)
            {
                //Reset delete
                DeleteWasClicked = false;

                //Delete Query ID from listbox
                string SelectedProfileIdToDelete = ProfileNameTxt.Text;
                try
                {
                    SavedProfiles_listBox.Items.Remove(SelectedProfileIdToDelete);
                }
                catch (Exception ex)
                {

                }

                
                //Delete Query from Config
                DeleteConfigXML.DeleteProfileData(SelectedProfileIdToDelete, filename);

                //Clear Page
                ClearDataEntryPanel();

                //Move Selected item on listbox to the next item
                SavedProfiles_listBox.SelectedIndex = 0;

                return;
            }

            string SelectedProfileID = SavedProfiles_listBox.SelectedItem.ToString();

            //Parse through Profile Section of Config to find SelectedProfileID and Corresponding Profile 
            //And display on Settings Page
            ProfileNameTxt.Text = SelectedProfileID;
            ProfileWithID NewProfile = ReadConfigXML.GetProfileFromID(SelectedProfileID, filename);

            outDirTxt.Text = NewProfile.outDir;
            BaselineRptTxt.Text = NewProfile.BaselineRpt;
            CompareRptTxt.Text = NewProfile.CompareRpt;
            BaselineFmtTxt.Text = NewProfile.BaselineFmt;
            CompareFmtTxt.Text = NewProfile.CompareFmt;
        }

        private void CompareCustomComboBox_DropDown(object sender, EventArgs e)
        {

        }

        private void CompareCustomComboBox_DropDownClosed(object sender, EventArgs e)
        {

            ToleranceGrid grid = (ToleranceGrid)((CustomComboBox.CustomComboBox)sender).DropDownControl;
            grid.dataGridView1.EndEdit();
            CompareCustomComboBox.Text = grid.ItemsToDisplayForComboBox();
        }

        private void BatchFileNameSearch_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Excel files (*.*)|*.xlsx";
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                BatchConfigFilePath.Text = openFileDialog1.FileName;
            }
        }

        private void BatchConfigFilePath_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void BatchConfigFilePath_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            //foreach (string file in files) Console.WriteLine(file);
            BatchConfigFilePath.Text = files[0];

        }
    }
}
