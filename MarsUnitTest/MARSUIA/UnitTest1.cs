using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Windows;
using Mars.AutoTestingDriver.ExecuteTestcase.MarsProcess;
using Mars.AutoTestingDriver.MarsUISupport;
using Mars.Inter.MQCenter.MSAASupport;
using Mars.message.AutoTestingDriver.interProcess;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarsUnitTest.MARSUIA
{
	[TestClass]
	public class MarsUIAUnitTest
	{
		private Dictionary<string, string> objPegProperties ;
        private Dictionary<string, string> objOtherPegProperties;
        private int sophisPId;
        private string strError="";
        private bool isOk;
        private MARSDealResult result = new MARSDealResult();

        [TestInitialize]
        public void TestInitialize()
        {
            objPegProperties = new Dictionary<string, string>();
        //Catalog:= MARSUI Object Type:= ControlType.Tab
        //Catalog:= MARSUI Text:= Fusion Invest 2025 - manager ObjectType:= ControlType.Window
            objPegProperties.Add("Catalog", "MARSUI");
            objPegProperties.Add("Text", "Fusion Invest 2025 - manager");
            objPegProperties.Add("ObjectType", "ControlType.Window");

            var processes = System.Diagnostics.Process.GetProcessesByName("sophisvalue");
            if (processes.Length > 0)
            {
                int sophisvalueProcessId = processes[0].Id;
                MARSTestProcess.CurrentTestProcessId = sophisPId = sophisvalueProcessId;
                Console.WriteLine($"sophisvalue.exe 进程ID: {sophisvalueProcessId}");
            }
            else
            {
                Console.WriteLine("未找到 sophisvalue.exe 进程。");
            }
        }

        [TestMethod]
		public void TestPegwindow()
		{
            isOk = MarsMARSUIHelper.MARSUI_Pegwindow(0, objPegProperties, objPegProperties, "","", "Pegwindow", "", 
                "TEST_UIA_PAGE", "", ref strError, ref result);

        }

        /// <summary>
        /// Catalog:=MARSUI 
        /// Text:=^Swap 
        /// Object Type:=ControlType.Window 
        /// MarsNamePath:=UIA:Fusion Invest 2025;UIA:Unknown;UIA:^Swap	
        /// </summary>
        [TestMethod]
        public void Test_SwapEntryPegwindow()
        {
            Dictionary<string, string > peg_TradeEntry = new Dictionary<string, string>();
            peg_TradeEntry.Add("Catalog", "MARSUI");
            peg_TradeEntry.Add("Text", "^Swap");
            peg_TradeEntry.Add("Object Type", "ControlType.Window");
            peg_TradeEntry.Add("MarsNamePath", "UIA:Fusion Invest 2025;UIA:Unknown;UIA:^Swap");
            objOtherPegProperties = peg_TradeEntry;
            isOk = MarsMARSUIHelper.MARSUI_Pegwindow(0, peg_TradeEntry, objPegProperties, "", "", "Pegwindow", "",
               "SOPHIS_SWAP_TRADE_ENTRY", "", ref strError, ref result);
        }

        /// <summary>
        /// Catalog:=MARSUI
        /// Object Type:=ControlType.Tab
        /// MarsNamePath:=UIA:Fusion Invest 2025 - manager;UIA:Unknown;UIA:Unknown
        /// </summary>
        [TestMethod]
        public void Test_SelectTab()
        {
            TestPegwindow();

            Dictionary<string, string> tabItem = new Dictionary<string, string>();
            tabItem.Add("Catalog", "MARSUI");
            //tabItem.Add("Text", "Trade Entry");
            tabItem.Add("Object Type", "ControlType.TabItem");
            tabItem.Add("MarsNamePath", "UIA:Fusion Invest 2025 - manager;UIA:Unknown;UIA:Unknown;UIA:Interest Rate Swap");
            tabItem.Add("MarsNamePathType", "abs"); //相对路径
            string strData = "^swap";
            isOk = MarsMARSUIHelper.MARSUI_SelectTab(0,  this.objPegProperties, tabItem, "", strData, "winTab", "",
               "SOPHIS_SWAP_TRADE_ENTRY", "", ref strError, ref result);

        }
        [TestMethod]
        public void Test_FillEdit()
        {
            Test_SwapEntryPegwindow();
            if (!MARSUIAppSideVariables.IsPegwindowsSet)
            {
                MessageBox.Show("未设置PEG窗口");
                return;
            }

            Dictionary<string, string> filledit= new Dictionary<string, string>();
            filledit.Add("Catalog", "MARSUI");
            filledit.Add("Object Type", "ControlType.Document");
            filledit.Add("attachText", "Gen: End Date Edit");
            //filledit.Add("MarsNamePath", "UIA:Fusion Invest 2025 - manager;UIA:Unknown;UIA:^Swap;UIA:Unknown;UIA:Unknown;UIA:Gen: End Date Edit");
            string strData = "100d";
            isOk = MarsMARSUIHelper.MARSUI_FillEdit(0, this.objOtherPegProperties, filledit, "", strData, "FillEdit", "",
               "SOPHIS_SWAP_TRADE_ENTRY", "", ref strError, ref result);
        }

        [TestMethod]
        public void Test_SelectDropList()
        {
            Test_SwapEntryPegwindow();
            if (!MARSUIAppSideVariables.IsPegwindowsSet)
            {
                MessageBox.Show("未设置PEG窗口");
                return;
            }

            Dictionary<string, string> droplsit = new Dictionary<string, string>();
            droplsit.Add("Catalog", "MARSUI");
            droplsit.Add("Object Type", "ControlType.ComboBox");
            droplsit.Add("attachText", "^Model$");
            //filledit.Add("MarsNamePath", "UIA:Fusion Invest 2025 - manager;UIA:Unknown;UIA:^Swap;UIA:Unknown;UIA:Unknown;UIA:Gen: End Date Edit");
            string strData = "^CDO$";
            isOk = MarsMARSUIHelper.MARSUI_SelectDropdown(0, this.objOtherPegProperties, droplsit, "", strData, "_SelectDropdown", "",
               "SOPHIS_SWAP_TRADE_ENTRY", "", ref strError, ref result);
        }


        [TestMethod]
        public void Test_SelectTradeEntryTab()
        {
            Test_SwapEntryPegwindow();
            if (!MARSUIAppSideVariables.IsPegwindowsSet)
            {
                MessageBox.Show("未设置PEG窗口");
                return;
            }

            Dictionary<string, string> tabItem = new Dictionary<string, string>();
            tabItem.Add("Catalog", "MARSUI");
            //tabItem.Add("Text", "Trade Entry");
            tabItem.Add("Object Type", "ControlType.TabItem");
            //tabItem.Add("ObjectName", "Tab1");
            tabItem.Add("MarsNamePath", "UIA:Fusion Invest 2025 - manager;UIA:Unknown;UIA:Swap;UIA:Unknown;UIA:Tab1;UIA:Received leg cash flow");
            //tabItem.Add("MarsNamePathType", "abs"); //相对路径
            string strData = "Paid leg cash flow";
            isOk = MarsMARSUIHelper.MARSUI_SelectTab(0, this.objPegProperties, tabItem, "", strData, "winTab", "",
               "SOPHIS_SWAP_TRADE_ENTRY", "", ref strError, ref result);

        }

        [TestMethod]
        public void Test_CaptureValueTable()
        {
            Test_SwapEntryPegwindow();
            if (!MARSUIAppSideVariables.IsPegwindowsSet)
            {
                MessageBox.Show("未设置PEG窗口");
                return;
            }
            Dictionary<string, string> tabItem = new Dictionary<string, string>();
            tabItem.Add("Catalog", "MARSUI");
            //tabItem.Add("Text", "Trade Entry");
            tabItem.Add("Object Type", "ControlType.Pane");
            tabItem.Add("WinClass", "CSCtrlGrille");
            //tabItem.Add("ObjectName", "Tab1");
            tabItem.Add("MarsNamePath", "UIA:Fusion Invest 2025 - manager;UIA:Unknown;UIA:Swap;UIA:Unknown;UIA:Tab1;UIA:Received leg cash flow");
            string strPara = "ALLROWS;PAYMENT DATE";
            string strData = "Payment_date";
            isOk = MarsMARSUIHelper.MARSUI_CaptureValue(0, this.objOtherPegProperties, tabItem, strPara, strData, "winTable", "",
               "SOPHIS_SWAP_CASH_FLOW_TABLE", "", ref strError, ref result);
            if (isOk) { 
                MessageBox.Show(result.ReturnedData);
            }
            else
            {
                MessageBox.Show(strError, result.ErrorMessage);
            }

        }

        [TestMethod]
        public void Test_CaptureAndCompareTextbox()
        {
            Test_SwapEntryPegwindow();
            if (!MARSUIAppSideVariables.IsPegwindowsSet)
            {
                MessageBox.Show("未设置PEG窗口");
                return;
            }

            Dictionary<string, string> filledit = new Dictionary<string, string>();
            filledit.Add("Catalog", "MARSUI");
            filledit.Add("Object Type", "ControlType.Document");
            filledit.Add("attachText", "Gen: End Date Edit");
            //filledit.Add("MarsNamePath", "UIA:Fusion Invest 2025 - manager;UIA:Unknown;UIA:^Swap;UIA:Unknown;UIA:Unknown;UIA:Gen: End Date Edit");
            string strData = "100d";
            isOk = MarsMARSUIHelper.MARSUI_CaptureValue(0, this.objOtherPegProperties, filledit, "", strData, "winEdit", "",
               "SOPHIS_SWAP_TRADE_ENTRY", "", ref strError, ref result);
            if (isOk) {
                MessageBox.Show(result.ReturnedData);
            }
            else
            {
                MessageBox.Show(strError, result.ErrorMessage);
            }
        }

        [TestMethod]
        public void Test_CaptureAndCompareCombobox()
        {
            Test_SwapEntryPegwindow();
            if (!MARSUIAppSideVariables.IsPegwindowsSet)
            {
                MessageBox.Show("未设置PEG窗口");
                return;
            }

            Dictionary<string, string> droplsit = new Dictionary<string, string>();
            droplsit.Add("Catalog", "MARSUI");
            droplsit.Add("Object Type", "ControlType.ComboBox");
            droplsit.Add("attachText", "^Model$");

            isOk = MarsMARSUIHelper.MARSUI_CaptureValue(0, this.objOtherPegProperties, droplsit, "", "", "winCombobox", "",
               "SOPHIS_SWAP_TRADE_ENTRY", "", ref strError, ref result);
            if (isOk)
            {
                MessageBox.Show(result.ReturnedData);
            }
            else
            {
                MessageBox.Show(strError, result.ErrorMessage);
            }
        }

        [TestMethod]
        public void Test_Snapshot()
        {
            Test_SwapEntryPegwindow();
            if (!MARSUIAppSideVariables.IsPegwindowsSet)
            {
                MessageBox.Show("未设置PEG窗口");
                return;
            }
            Dictionary<string, string> droplsit = new Dictionary<string, string>();
            droplsit.Add("Catalog", "MARSUI");
            droplsit.Add("Object Type", "ControlType.ComboBox");
            droplsit.Add("attachText", "^Model$");
            string strData = "testSnapshot";
            isOk = MarsMARSUIHelper.MARSUI_Snapshot(0, this.objOtherPegProperties, droplsit, "", strData, "Snapshot", "",
               "SOPHIS_SWAP_TRADE_ENTRY", "", ref strError, ref result);
            if (isOk)
            {
                MessageBox.Show(result.ReturnedData);
            }
            else
            {
                MessageBox.Show(strError, result.ErrorMessage);
            }
        }

        [TestMethod]
        public void Test_ClickButton()
        {
            Test_SwapEntryPegwindow();
            if (!MARSUIAppSideVariables.IsPegwindowsSet)
            {
                MessageBox.Show("未设置PEG窗口");
                return;
            }
            Dictionary<string, string> droplsit = new Dictionary<string, string>();
            droplsit.Add("Catalog", "MARSUI");
            droplsit.Add("Object Type", "ControlType.Button");
            droplsit.Add("attachText", "^Calculate$");
            string strData = "testSnapshot";
            isOk = MarsMARSUIHelper.MARSUI_ClickButton(0, this.objOtherPegProperties, droplsit, "", strData, "clickButton", "",
               "SOPHIS_SWAP_TRADE_ENTRY", "", ref strError, ref result);
            if (isOk)
            {
                MessageBox.Show(result.ReturnedData);
            }
            else
            {
                MessageBox.Show(strError, result.ErrorMessage);
            }
        }

        /// <summary>
        /// 选中ribbon的菜单项，不是内部的button
        /// </summary>
        [TestMethod]
        public void Test_SelectMenuItem()
        {
            TestPegwindow();
            if (!MARSUIAppSideVariables.IsPegwindowsSet)
            {
                MessageBox.Show("未设置PEG窗口");
                return;
            }
            Dictionary<string, string> ribbon = new Dictionary<string, string>();
            ribbon.Add("Catalog", "MARSUI");
            ribbon.Add("Object Type", "ControlType.Pane");
            ribbon.Add("attachText", "^Fusion Invest 2025$");
            ribbon.Add("winClass", "^Afx:RibbonBar");
            string strData = "Derivatives";
            isOk = MarsMARSUIHelper.MARSUI_SelectMenuItem(0, this.objOtherPegProperties, ribbon, "", strData, "WinRibbon", "",
               "SOPHIS_SWAP_TRADE_ENTRY", "", ref strError, ref result);
            System.Threading.Thread.Sleep(2000);
            if (isOk)
            {
                MessageBox.Show(result.ReturnedData);
            }
            else
            {
                MessageBox.Show(strError, result.ErrorMessage);
            }
        }

        [TestMethod]
        public void Test_ClickMenuIcon()
        {
            TestPegwindow();
            if (!MARSUIAppSideVariables.IsPegwindowsSet)
            {
                MessageBox.Show("未设置PEG窗口");
                return;
            }
            Dictionary<string, string> ribbon = new Dictionary<string, string>();
            ribbon.Add("Catalog", "MARSUI");
            ribbon.Add("Object Type", "ControlType.Pane");
            ribbon.Add("attachText", "^Fusion Invest 2025$");
            ribbon.Add("winClass", "^Afx:RibbonBar");
            ribbon.Add("MarsRibbonType","SplitButton");/// SplitButton 或者 PushButton
            string strData = "Interest Rates;Swaps";
            isOk = MarsMARSUIHelper.MARSUI_ClickMenuIcon(0, this.objOtherPegProperties, ribbon, "", strData, "WinRibbon", "",
               "SOPHIS_SWAP_TRADE_ENTRY", "", ref strError, ref result);
            if (isOk)
            {
                MessageBox.Show(result.ReturnedData);
            }
            else
            {
                MessageBox.Show(strError, result.ErrorMessage);
            }
        }

        [TestMethod]
        public void Test_InterestSwapWindow()
        {
            objPegProperties = new Dictionary<string, string>();
            //Catalog:= MARSUI Object Type:= ControlType.Tab
            //Catalog:= MARSUI Text:= Fusion Invest 2025 - manager ObjectType:= ControlType.Window
            objPegProperties.Add("Catalog", "MARSUI");
            objPegProperties.Add("Text", "^Interest Rate Swaps");
            objPegProperties.Add("ObjectType", "ControlType.Window");

            isOk = MarsMARSUIHelper.MARSUI_Pegwindow(0, objPegProperties, objPegProperties, "", "", "Pegwindow", "",
                "TEST_UIA_PAGE", "", ref strError, ref result);
            //if (isOk)
            //{
            //    MessageBox.Show(result.ReturnedData);
            //}
            //else
            //{
            //    MessageBox.Show(strError, result.ErrorMessage);
            //}


        }

        [TestMethod]
        public void Test_SearchAndClick()
        {
            Test_InterestSwapWindow();
            if (!MARSUIAppSideVariables.IsPegwindowsSet)
            {
                MessageBox.Show("未设置PEG窗口");
                return;
            }
            Dictionary<string, string> testGrid = new Dictionary<string, string>();
            testGrid.Add("Catalog", "MARSUI");
            testGrid.Add("Object Type", "ControlType.Pane");
            //testGrid.Add("attachText", "DataGrid");
            testGrid.Add("winClass", "CSCtrlGrille");
            string strPara = "MarsAddins;Reference;Action:LEFT_DBL_CLICK;";
            string strData = "sanjay1";
            isOk = MarsMARSUIHelper.MARSUI_SearchAndClick(0, objPegProperties, testGrid, strPara, strData, "winTable",attachText:"",
                "TEST_UIA_PAGE", objName:"", ref strError, ref result);
            //winClass:=CSCtrlGrille attachText:=DataGrid	
            //testGrid.
        }

        [TestMethod]
        public void Test_PressKeys()
        {
            Test_InterestSwapWindow();
            if (!MARSUIAppSideVariables.IsPegwindowsSet)
            {
                MessageBox.Show("未设置PEG窗口");
                return;
            }
            Dictionary<string, string> testGrid = new Dictionary<string, string>();
            testGrid.Add("Catalog", "MARSUI");
            testGrid.Add("Object Type", "ControlType.Pane");
            //testGrid.Add("attachText", "DataGrid");
            testGrid.Add("winClass", "CSCtrlGrille");
            string strPara = "CURRENT_POS_NO_CLICK";
            string strData = "^N";
            isOk = MarsMARSUIHelper.MARSUI_PressKeys(0, objPegProperties, testGrid, strPara, strData, "winTable", attachText: "",
                "TEST_UIA_PAGE", objName: "", ref strError, ref result);
            //winClass:=CSCtrlGrille attachText:=DataGrid	
            //testGrid.
        }

        //[TestMethod]
        //public void Test_CreateNewTradeByClickButtonInDlg()
        //{
        //    Test_InterestSwapWindow();
        //    if (!MARSUIAppSideVariables.IsPegwindowsSet)
        //    {
        //        MessageBox.Show("未设置PEG窗口");
        //        return;
        //    }
        //    Dictionary<string, string> testDlg = new Dictionary<string, string>();
        //    testDlg.Add("Catalog", "MARSUI");
        //    testDlg.Add("Object Type", "ControlType.Pane");
        //    //testGrid.Add("attachText", "DataGrid");
        //    testDlg.Add("winClass", "CSCtrlGrille");
        //    string strPara = "CURRENT_POS_NO_CLICK";
        //    string strData = "^N";
        //    isOk = MarsMARSUIHelper.MARSUI_CreateNewTradeByClickButtonInDlg(0, objPegProperties, testGrid, strPara, strData, "winTable", attachText: "",
        //        "TEST_UIA_PAGE", objName: "", ref strError, ref result);
        //}
    }
}
