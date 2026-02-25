using Mars.Inter.MQCenter.interProcess.Wpf.ObjectSpy.Support;
using Mars.Inter.MQCenter.MSAASupport;
using Mars.Inter.MQCenter.spyHelper;
using Mars.Inter.MQCenter.windowsControlsHelpers;
using Mars.message.AutoTestingDriver.interProcess;
using Mars.message.AutoTestingDriver.message;
using Mars.message.Inter.MQCenter.interProcess;
using Mars.message.Inter.MQCenter.objectTypeMapping;
using Mars.message.Inter.MQCenter.simpleLog;
using Mars.message.Utility.visualObjects.objectSpyer;
using Mars.message.windowsWrapper.SystemUtil;
using MarsUFTAddins.IMars.tiger;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;

using System.Threading.Tasks;
//using System.Windows.Controls;
using System.Windows.Forms;
using Accessibility;
using System.Windows.Automation;
using Mars.Inter.MQCenter.MarsUtility;


namespace Mars.message.Inter.MQCenter.objectSpy
{
    public partial class MarsObjSpyForm : Form
    {
        private static MarsObjSpyForm objectSpyForm = null;

        /// <summary>
        /// 静态开关：是否显示高度和宽度都为0的IAccessible对象
        /// </summary>
        public static bool ShowZeroSizeObjects { get; set; } = false;

        /// <summary>
        /// 设置是否显示零尺寸对象（同时更新MARSAccessibleHelper的设置）
        /// </summary>
        /// <param name="show">是否显示零尺寸对象</param>
        public static void SetShowZeroSizeObjects(bool show)
        {
            ShowZeroSizeObjects = show;
            MARSAccessibleHelper.ShowZeroSizeObjects = show;
        }

        public string targetControlWndId = null;
        /// <summary>
        /// 目标对象信息,即，鼠标选中的对象
        /// </summary>
        public MarsSpiedObjectInfo TargetSpiedObject { get; set; } = null;
        private TreeNode targetControlNode = null;

        private List<TreeNode> searchedNodes = new List<TreeNode>();
        private int currentSearchId = 0;
        private List<MarsSpiedObjectInfo> allObjects = null;
        private List<IntPtr> allObjectsFromTargetProcess = new List<IntPtr>();
        private List<System.Windows.Forms.Control> allControls = new List<System.Windows.Forms.Control>();

        public enSpyMode CurrentSpyMode { get; set; } = enSpyMode.spyMode_net_winform_frameWork;
        private MarsObjSpyForm()
        {


            InitializeComponent();
            this.winformObjectTreeview.AfterSelect += winformObjectTreeview_AfterSelect;
            this.winformObjectTreeview.BeforeSelect += winformObjectTreeview_BeforeSelect;
            this.winformObjectTreeview.DoubleClick += winformObjectTreeview_DoubleClick;

            // Initialize zero size objects button state
            toolStripButtonShowZeroSize.Checked = ShowZeroSizeObjects;
            toolStripButtonShowZeroSize.BackColor = ShowZeroSizeObjects ? System.Drawing.Color.LightGreen : System.Drawing.SystemColors.Control;

            // Set ToolStrip height to 32
            this.toolStrip1.Height = 32;

            // 设置 Context Menu 的字体和大小
            this.contextMenuStrip1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.contextMenuStripSelectedObjects.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);

            // 调整菜单项高度以适应 9pt 字体
            //foreach (ToolStripItem item in this.contextMenuStripSelectedObjects.Items)
            //{
            //    if (item is ToolStripMenuItem menuItem)
            //    {
            //        menuItem.Size = new System.Drawing.Size(menuItem.Size.Width, 24);
            //    }
            //}

            //treeView1.ImageList = this.imageList1;
            this.StartPosition = FormStartPosition.CenterParent;
            SetTopLevel(true);
            SetObjectProperGridColumnType();
            Hide();
        }

        private void SetObjectProperGridColumnType()
        {
            DataGridViewComboBoxColumn dropListColumn = (this.targetObjPropertyGrid.Columns[0] as DataGridViewComboBoxColumn);
            dropListColumn.Items.Clear();
            dropListColumn.Items.Add(MarsSpyGeneratedQuickAccess.cnst_objectHappyName);
            dropListColumn.Items.Add(MarsSpyGeneratedQuickAccess.cnst_swfname);
            dropListColumn.Items.Add(MarsSpyGeneratedQuickAccess.cnst_swfnamePath);
            dropListColumn.Items.Add(MarsSpyGeneratedQuickAccess.cnst_text);
            dropListColumn.Items.Add(MarsSpyGeneratedQuickAccess.cnst_type);
            dropListColumn.Items.Add(MarsSpyGeneratedQuickAccess.cnst_typePath);
            dropListColumn.Items.Add(MarsSpyGeneratedQuickAccess.cnst_index);
            dropListColumn.Items.Add(MarsSpyGeneratedQuickAccess.cnst_objectPegWindow);
            dropListColumn.Items.Add(MarsSpyGeneratedQuickAccess.cnst_appliedApp); //shot name
            dropListColumn.Items.Add(MarsSpyGeneratedQuickAccess.cnst_isPegwindow);
        }

        public TreeView GetTreeView()
        {
            return this.winformObjectTreeview;
        }
        internal static MarsObjSpyForm getInstance(List<MarsSpiedObjectInfo> lstOfObjs,
            enSpyMode spyMode = enSpyMode.spyMode_net_winform_frameWork,
            MarsSpiedObjectInfo targetSpiedObj = null)
        {

            if (objectSpyForm == null)
            {
                objectSpyForm = new MarsObjSpyForm();
            }
            objectSpyForm.TargetSpiedObject = targetSpiedObj;
            objectSpyForm.CurrentSpyMode = spyMode;
            objectSpyForm.SetAllObjects(lstOfObjs); // reloadObjects(lstOfObjs);
            return objectSpyForm;
        }

        public void SetAllObjects(List<MarsSpiedObjectInfo> objs)
        {
            this.allObjects = objs;
        }

        private bool isTargetObjectFind = false;
        private void reloadObjects(List<MarsSpiedObjectInfo> lstOfObjs)
        {
            //this.treeView1.BeginUpdate();
            try
            {

                allObjectsFromTargetProcess.Clear();
                allControls.Clear();

                IntPtr targetHwnd = IntPtr.Zero;
                int tmpHwnd;
                /// 对于以UIA方式获得的对象，需要用TargetSpiedObject作为定位对象
                ///
                bool isComMode = false;
                if (TargetSpiedObject != null)
                {
                    isComMode = true;
                }
                else
                {
                    if ((!string.IsNullOrEmpty(this.targetControlWndId))
                            && (int.TryParse(this.targetControlWndId, out tmpHwnd)))
                    {
                        targetHwnd = new IntPtr(tmpHwnd);
                    }
                }
                this.winformObjectTreeview.Nodes.Clear();
                if (lstOfObjs == null) return;
                for (int i = 0; i < lstOfObjs.Count; i++)
                {
                    var itm = lstOfObjs[i];
                    if (itm == null) continue;

                    TreeNode objNode = CreateNodeFromObjInfo(itm, targetUserControlId: targetHwnd);
                    if (objNode == null) continue;
                    winformObjectTreeview.Invoke(new Action(() =>
                    {
                        this.winformObjectTreeview.Nodes.Add(objNode);
                    }
                    ));

                }
                if (!isTargetObjectFind)
                {
                    try
                    {
                        System.Windows.Forms.Control c = System.Windows.Forms.Control.FromHandle(targetHwnd);
                        MarsObjectOp op = new MarsObjectOp();
                        System.Windows.Forms.Control cTop = op.GetTopParent(c);
                        //if (c.Parent != null)
                        if (cTop != null)
                        {
                            var objNew = new MarsSpiedObjectInfo()
                            {
                                x = cTop.Left,
                                y = cTop.Top,
                                w = cTop.Width,
                                h = cTop.Height,
                                relatedX = cTop.Left,
                                relatedY = cTop.Top,
                                referenceToObj = cTop,
                                objectName = cTop.Name,
                                objectNamePath = ";",
                                Text = cTop.Text,
                                objectType = cTop.GetType().FullName,
                                objectTypePath = cTop.GetType().FullName,
                                hwnd = cTop.Handle.ToInt32(),
                            };
                            objNew.buildChildren();
                            //重新获得其所有的子对象
                            TreeNode objNode = CreateNodeFromObjInfo(objNew, targetUserControlId: targetHwnd);
                            if (objNode != null)
                            {
                                //treeView1.Invoke(new Action(() =>
                                {
                                    this.winformObjectTreeview.Nodes.Add(objNode);
                                }
                                //));
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("reloadObjects", e.Message, e);
            }
            finally
            {
                //this.treeView1.EndUpdate();
            }
        }

        private void SetTargetObject(TreeNode targetTreeNode)
        {
            targetTreeNode.ForeColor = System.Drawing.Color.Blue;
            targetTreeNode.NodeFont = new Font(winformObjectTreeview.Font, FontStyle.Bold);
            isTargetObjectFind = true;
            this.targetControlNode = targetTreeNode;
        }

        private TreeNode CreateNodeFromObjInfo(MarsSpiedObjectInfo itm, IntPtr targetUserControlId, TreeNode ndParent = null, int imageIndx = 0)
        {
            if (itm == null) return null;
            TreeNode nd = new TreeNode(itm.getDisplayId() ?? "N/A");
            //nd.ImageIndex = imageIndx;
            nd.Tag = itm;
            Color nodeColor = Color.Black;
            if (itm.controlClassTypeFromAPI == null)
                nodeColor = Color.DarkBlue;
            else if (itm.controlClassTypeFromAPI.Equals("standard", StringComparison.OrdinalIgnoreCase))
                nodeColor = ColorTranslator.FromHtml("#ba4a00");
            else if (itm.controlClassTypeFromAPI.Equals("afx", StringComparison.OrdinalIgnoreCase))
                nodeColor = Color.DarkGreen;
            else if (itm.controlClassTypeFromAPI.Equals("winforms", StringComparison.OrdinalIgnoreCase))
                nodeColor = ColorTranslator.FromHtml("#512e5f");
            if (!itm.isVisible)
                nodeColor = Color.Red;
            nd.ForeColor = nodeColor;

            if (this.TargetSpiedObject != null)
            {
                if (this.TargetSpiedObject == itm)
                    SetTargetObject(nd);
            }
            else
            {
                /// for .net inject mode
                if ((itm.referenceToObj != null) && ((itm.referenceToObj as System.Windows.Forms.Control) != null))
                {
                    System.Windows.Forms.Control cntrl = itm.referenceToObj as System.Windows.Forms.Control;

                    if (allObjectsFromTargetProcess.IndexOf(cntrl.Handle) > 0) return null;//说明该对象已经添加过
                    allObjectsFromTargetProcess.Add(cntrl.Handle);
                    allControls.Add(cntrl);
                    if (!cntrl.Visible)
                    {
                        nd.ForeColor = nodeColor; //System.Drawing.Color.Red;
                        nd.NodeFont = new Font(winformObjectTreeview.Font, FontStyle.Italic);
                    }
                    if (cntrl.Handle == targetUserControlId)
                    {
                        //targetControlNode = nd;
                        SetTargetObject(nd);
                    }

                }
                else
                    allObjectsFromTargetProcess.Add(new IntPtr(itm.hwnd));
            }
            if (itm.children != null)
            {
                foreach (var itmSub in itm.children)
                {
                    if (itmSub == null) continue;
                    TreeNode subNode = CreateNodeFromObjInfo(itmSub, targetUserControlId, imageIndx: 1);
                    if (subNode != null)
                        nd.Nodes.Add(subNode);
                }
            }
            return nd;
        }

        /// <summary>
        /// 获取类型标签（简化版本）
        /// </summary>
        /// <param name="fullTypeName">完整类型名称</param>
        /// <returns>简化的类型标签</returns>
        private string GetTypeLabel(string fullTypeName)
        {
            if (string.IsNullOrEmpty(fullTypeName))
                return "unknown";

            try
            {
                // 提取类型名称（去掉命名空间）
                var typeName = fullTypeName.Split('.').LastOrDefault();
                if (string.IsNullOrEmpty(typeName))
                    return "unknown";

                // 转换为小写
                return typeName.ToLower();
            }
            catch
            {
                return "unknown";
            }
        }

        /// <summary>
        /// 添加Snoop风格的信息到DataGridView
        /// </summary>
        /// <param name="objInfo">MarsSpiedObjectInfo对象</param>
        private void AddSnoopStyleInfoToGrid(MarsSpiedObjectInfo objInfo)
        {
            try
            {
                // 添加依赖属性信息
                if (objInfo.dependencyProperties != null && objInfo.dependencyProperties.Count > 0)
                {
                    var rid = dataGridView1.Rows.Add();
                    var row = dataGridView1.Rows[rid];
                    CreateRow(row, "Dependency Properties", $"Count: {objInfo.dependencyProperties.Count}");
                    row.DefaultCellStyle.BackColor = Color.LightGreen;
                }

                // 添加事件信息
                if (objInfo.events != null && objInfo.events.Count > 0)
                {
                    var rid = dataGridView1.Rows.Add();
                    var row = dataGridView1.Rows[rid];
                    CreateRow(row, "Events", $"Count: {objInfo.events.Count}");
                    row.DefaultCellStyle.BackColor = Color.LightPink;
                }

                // 添加样式信息
                if (!string.IsNullOrEmpty(objInfo.style))
                {
                    var rid = dataGridView1.Rows.Add();
                    var row = dataGridView1.Rows[rid];
                    CreateRow(row, "Style", objInfo.style);
                    row.DefaultCellStyle.BackColor = Color.LightCyan;
                }

                // 添加模板信息
                if (!string.IsNullOrEmpty(objInfo.template))
                {
                    var rid = dataGridView1.Rows.Add();
                    var row = dataGridView1.Rows[rid];
                    CreateRow(row, "Template", objInfo.template);
                    row.DefaultCellStyle.BackColor = Color.LightCyan;
                }

                // 添加资源信息
                if (objInfo.resources != null && objInfo.resources.Count > 0)
                {
                    var rid = dataGridView1.Rows.Add();
                    var row = dataGridView1.Rows[rid];
                    CreateRow(row, "Resources", $"Count: {objInfo.resources.Count}");
                    row.DefaultCellStyle.BackColor = Color.LightYellow;
                }

                // 添加绑定信息
                if (objInfo.bindings != null && objInfo.bindings.Count > 0)
                {
                    var rid = dataGridView1.Rows.Add();
                    var row = dataGridView1.Rows[rid];
                    CreateRow(row, "Bindings", $"Count: {objInfo.bindings.Count}");
                    row.DefaultCellStyle.BackColor = Color.LightBlue;
                }

                // 添加触发器信息
                if (objInfo.triggers != null && objInfo.triggers.Count > 0)
                {
                    var rid = dataGridView1.Rows.Add();
                    var row = dataGridView1.Rows[rid];
                    CreateRow(row, "Triggers", $"Count: {objInfo.triggers.Count}");
                    row.DefaultCellStyle.BackColor = Color.LightSalmon;
                }

                // 添加渲染信息
                if (!string.IsNullOrEmpty(objInfo.renderInfo))
                {
                    var rid = dataGridView1.Rows.Add();
                    var row = dataGridView1.Rows[rid];
                    CreateRow(row, "Render Info", objInfo.renderInfo);
                    row.DefaultCellStyle.BackColor = Color.LightGray;
                }

                // 添加布局信息
                if (!string.IsNullOrEmpty(objInfo.layoutInfo))
                {
                    var rid = dataGridView1.Rows.Add();
                    var row = dataGridView1.Rows[rid];
                    CreateRow(row, "Layout Info", objInfo.layoutInfo);
                    row.DefaultCellStyle.BackColor = Color.LightSteelBlue;
                }

                // 添加输入信息
                if (!string.IsNullOrEmpty(objInfo.inputInfo))
                {
                    var rid = dataGridView1.Rows.Add();
                    var row = dataGridView1.Rows[rid];
                    CreateRow(row, "Input Info", objInfo.inputInfo);
                    row.DefaultCellStyle.BackColor = Color.LightGoldenrodYellow;
                }

                // 添加焦点信息
                if (!string.IsNullOrEmpty(objInfo.focusInfo))
                {
                    var rid = dataGridView1.Rows.Add();
                    var row = dataGridView1.Rows[rid];
                    CreateRow(row, "Focus Info", objInfo.focusInfo);
                    row.DefaultCellStyle.BackColor = Color.LightCoral;
                }

                // 添加可见性信息
                if (!string.IsNullOrEmpty(objInfo.visibilityInfo))
                {
                    var rid = dataGridView1.Rows.Add();
                    var row = dataGridView1.Rows[rid];
                    CreateRow(row, "Visibility Info", objInfo.visibilityInfo);
                    row.DefaultCellStyle.BackColor = Color.LightSeaGreen;
                }

                // 添加变换信息
                if (!string.IsNullOrEmpty(objInfo.transformInfo))
                {
                    var rid = dataGridView1.Rows.Add();
                    var row = dataGridView1.Rows[rid];
                    CreateRow(row, "Transform Info", objInfo.transformInfo);
                    row.DefaultCellStyle.BackColor = Color.LightSkyBlue;
                }

                // 添加动画信息
                if (!string.IsNullOrEmpty(objInfo.animationInfo))
                {
                    var rid = dataGridView1.Rows.Add();
                    var row = dataGridView1.Rows[rid];
                    CreateRow(row, "Animation Info", objInfo.animationInfo);
                    row.DefaultCellStyle.BackColor = Color.LightPink;
                }

                // 添加上下文信息
                if (!string.IsNullOrEmpty(objInfo.contextInfo))
                {
                    var rid = dataGridView1.Rows.Add();
                    var row = dataGridView1.Rows[rid];
                    CreateRow(row, "Context Info", objInfo.contextInfo);
                    row.DefaultCellStyle.BackColor = Color.LightSlateGray;
                }

                // 添加调试信息
                if (!string.IsNullOrEmpty(objInfo.debugInfo))
                {
                    var rid = dataGridView1.Rows.Add();
                    var row = dataGridView1.Rows[rid];
                    CreateRow(row, "Debug Info", objInfo.debugInfo);
                    row.DefaultCellStyle.BackColor = Color.LightSteelBlue;
                }

                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
                dataGridView1.AutoResizeColumns();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding Snoop style info to grid: {ex.Message}");
            }
        }

        internal static void showModuleInThread()
        {
            var t = new System.Threading.Thread(() =>
            {
                if ((objectSpyForm != null) && (objectSpyForm.Modal))
                {

                }
                else
                    //objectSpyForm.Close();
                    objectSpyForm.ShowDialog();
                objectSpyForm = null;
            });
            t.SetApartmentState(System.Threading.ApartmentState.STA);
            t.Start();
        }

        private void treeView1_DoubleClick(object sender, EventArgs e)
        {
            // added to selectedObjectsListView            
        }

        //private void InitObjectAccessItems(MarsSpiedObjectInfo objInfo)
        //{

        //}

        private void LoadBasicInfo(MarsSpiedObjectInfo objInfo)
        {
            dataGridView1.Rows.Clear();
            var rid = dataGridView1.Rows.Add();
            var row = dataGridView1.Rows[rid];
            CreateRow(row, "Catalog", objInfo.controlClassTypeFromAPI);
            
            // Add Catalog Extension row
            rid = dataGridView1.Rows.Add();
            row = dataGridView1.Rows[rid];
            string catalogExtension = GetCatalogExtension(objInfo);
            CreateRow(row, "Catalog Extension", catalogExtension);
            
            rid = dataGridView1.Rows.Add();
            row = dataGridView1.Rows[rid];
            CreateRow(row, "X(Absolute)", objInfo.x);
            rid = dataGridView1.Rows.Add();
            row = dataGridView1.Rows[rid];
            CreateRow(row, "y(Absolute)", objInfo.y);
            rid = dataGridView1.Rows.Add();
            row = dataGridView1.Rows[rid];
            CreateRow(row, "Height", objInfo.h);
            rid = dataGridView1.Rows.Add();
            row = dataGridView1.Rows[rid];
            CreateRow(row, "Width", objInfo.w);
            rid = dataGridView1.Rows.Add();
            row = dataGridView1.Rows[rid];
            CreateRow(row, "Text/Tile", objInfo.Text);
            rid = dataGridView1.Rows.Add();
            row = dataGridView1.Rows[rid];
            CreateRow(row, "Object Type", objInfo.objectType);
            rid = dataGridView1.Rows.Add();
            row = dataGridView1.Rows[rid];
            CreateRow(row, "Object Name", objInfo.objectName);
            rid = dataGridView1.Rows.Add();
            row = dataGridView1.Rows[rid];
            CreateRow(row, "Object Name Path", objInfo.objectNamePath);
            rid = dataGridView1.Rows.Add();
            row = dataGridView1.Rows[rid];
            CreateRow(row, "Control ID", objInfo.controlId);
            rid = dataGridView1.Rows.Add();
            row = dataGridView1.Rows[rid];
            CreateRow(row, "Z-Order", objInfo.zorder);

            rid = dataGridView1.Rows.Add();
            row = dataGridView1.Rows[rid];
            CreateRow(row, "MARS_Type", objInfo.controlMarsType);

            rid = dataGridView1.Rows.Add();
            row = dataGridView1.Rows[rid];
            CreateRow(row, "HWND", $"0x{objInfo.hwnd:X}");

            if (objInfo.hwnd != 0)
            {
                // 测试Windows API获取窗口信息
                var className = new StringBuilder(256);
                MarsWindowsAPIs.GetClassName((IntPtr)objInfo.hwnd, className, 255);
                string windowClass = className.ToString();
                rid = dataGridView1.Rows.Add();
                row = dataGridView1.Rows[rid];
                CreateRow(row, "WinClass", windowClass);
                
                // Add Class Ext row
                rid = dataGridView1.Rows.Add();
                row = dataGridView1.Rows[rid];
                string classExt = GetClassExtension(objInfo);
                CreateRow(row, "Class Ext", classExt);
            }

            rid = dataGridView1.Rows.Add();
            row = dataGridView1.Rows[rid];
            CreateRow(row, "Visible", objInfo.isVisible);
            if (!objInfo.isVisible)
                row.DefaultCellStyle.BackColor = Color.LightCyan;

            // 添加启用状态显示（如果是WPF对象）
            if (CurrentSpyMode == Mars.Inter.MQCenter.spyHelper.enSpyMode.spyMode_net_winform_wpf ||
                CurrentSpyMode == Mars.Inter.MQCenter.spyHelper.enSpyMode.sypMode_net_core_wpf)
            {
                rid = dataGridView1.Rows.Add();
                row = dataGridView1.Rows[rid];
                CreateRow(row, "Enabled", objInfo.isEnabled);
                if (!objInfo.isEnabled)
                    row.DefaultCellStyle.BackColor = Color.LightYellow;

                // 添加Snoop风格的信息显示
                AddSnoopStyleInfoToGrid(objInfo);
            }

            /// display tmp Image Name
            /// 
            rid = dataGridView1.Rows.Add();
            row = dataGridView1.Rows[rid];
            CreateRow(row, "SnapShot Path", objInfo.snapshotFileNameWithPath ?? "");
            row.Tag = "image";

            rid = dataGridView1.Rows.Add();
            row = dataGridView1.Rows[rid];
            CreateRow(row, "Control", objInfo.referenceToObj);
            row.DefaultCellStyle.BackColor = Color.LightBlue;

            row.Tag = objInfo.referenceToObj;
            this.dataGridView1.Tag = objInfo;

            // 如果是WPF模式，尝试通过VisualTreeHelper捕获图像
            if ((CurrentSpyMode == Mars.Inter.MQCenter.spyHelper.enSpyMode.spyMode_net_winform_wpf ||
                 CurrentSpyMode == Mars.Inter.MQCenter.spyHelper.enSpyMode.sypMode_net_core_wpf) &&
                string.IsNullOrEmpty(objInfo.snapshotFileNameWithPath))
            {
                try
                {
                    // 使用WPF图像捕获辅助类
                    var imagePath = WpfVisualCaptureHelper.CaptureMarsObjectImage(objInfo);
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        objInfo.snapshotFileNameWithPath = imagePath;
                        // 更新SnapShot Path行的值
                        foreach (DataGridViewRow gridRow in dataGridView1.Rows)
                        {
                            if (gridRow.Tag != null && gridRow.Tag.ToString() == "image")
                            {
                                gridRow.Cells["objPropValue"].Value = imagePath;
                                break;
                            }
                        }
                        /// load to preview picture
                        /// 
                        this.controlPicturePreview.Image = Image.FromFile(imagePath);
                    }
                }
                catch (Exception ex)
                {
                    // 记录错误但不影响其他功能
                    System.Diagnostics.Debug.WriteLine($"WPF image capture failed: {ex.Message}");
                }
            }

            // load image to preview
            if (System.IO.File.Exists(objInfo.snapshotFileNameWithPath))
            {
                this.controlPicturePreview.Image = null;
                //this.controlPicturePreview.SizeMode = PictureBoxSizeMode.CenterImage;
                string strFilePath = objInfo.snapshotFileNameWithPath;
                this.controlPicturePreview.Image = Bitmap.FromFile(strFilePath);
                this.controlPicturePreview.Width = this.controlPicturePreview.Width < this.controlPicturePreview.Image.Width ? this.controlPicturePreview.Image.Width : this.controlPicturePreview.Width;
                this.controlPicturePreview.Height = this.controlPicturePreview.Height < this.controlPicturePreview.Image.Height ? this.controlPicturePreview.Image.Height : this.controlPicturePreview.Height;
                //this.controlPicturePreview.Dock = DockStyle.Fill;
                this.controlPicturePreview.Load(strFilePath);
            }
            else
            {
                var pic = StandardWindowsEnumerator.GetControlImage(objInfo);

                if (this.controlPicturePreview.Image != null)
                {
                    this.controlPicturePreview.Image.Dispose();
                    this.controlPicturePreview.Image = null;
                }
                if (pic == null) return;
                this.controlPicturePreview.Width = pic.Width;
                this.controlPicturePreview.Height = pic.Height;
                this.controlPicturePreview.SizeMode = PictureBoxSizeMode.Normal;
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => this.controlPicturePreview.Image = pic));
                }
                else
                    this.controlPicturePreview.Image = pic;


            }

        }

        private void CreateRow(DataGridViewRow row, string v, object x)
        {
            if (row == null) return;
            row.Cells["objectProperty"].Value = v;
            row.Cells["objPropValue"].Value = x == null ? "" : x.ToString();
            row.Cells["objPropertyType"].Value = x == null ? "" : x.GetType().FullName;
        }

        /// <summary>
        /// 获取 Catalog Extension 信息
        /// 检查 .NET 对象的基类型中是否包含 DevExpress 或 Infragistics
        /// </summary>
        /// <param name="objInfo">MarsSpiedObjectInfo 对象</param>
        /// <returns>Catalog Extension 字符串</returns>
        private string GetCatalogExtension(MarsSpiedObjectInfo objInfo)
        {
            try
            {
                if (objInfo == null || objInfo.referenceToObj == null)
                    return "";

                // 检查是否为 .NET 对象
                Type objType = objInfo.referenceToObj.GetType();
                if (objType == null)
                    return "";

                // 遍历继承链查找 DevExpress 或 Infragistics
                Type currentType = objType;
                while (currentType != null && currentType != typeof(object))
                {
                    string typeName = currentType.FullName ?? "";
                    string typeNamespace = currentType.Namespace ?? "";

                    // 检查是否包含 DevExpress
                    if (typeName.IndexOf("DevExpress", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        typeNamespace.IndexOf("DevExpress", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return "DevExpress";
                    }

                    // 检查是否包含 Infragistics
                    if (typeName.IndexOf("Infragistics", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        typeNamespace.IndexOf("Infragistics", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return "Infragistics";
                    }

                    // 移动到基类型
                    currentType = currentType.BaseType;
                }

                return "";
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("GetCatalogExtension", $"Error getting catalog extension: {ex.Message}", ex);
                return "";
            }
        }

        /// <summary>
        /// 获取 Class Extension 信息
        /// 判断对象是 winform、wpf 还是 standard
        /// </summary>
        /// <param name="objInfo">MarsSpiedObjectInfo 对象</param>
        /// <returns>Class Extension 字符串</returns>
        private string GetClassExtension(MarsSpiedObjectInfo objInfo)
        {
            try
            {
                if (objInfo == null)
                    return "";

                // 首先检查 WinClass，如果以 hwndwrapper 开头，则为 WPF
                if (objInfo.hwnd != 0)
                {
                    try
                    {
                        var className = new StringBuilder(256);
                        MarsWindowsAPIs.GetClassName((IntPtr)objInfo.hwnd, className, 255);
                        string windowClass = className.ToString();
                        if (!string.IsNullOrEmpty(windowClass) && 
                            windowClass.StartsWith("hwndwrapper", StringComparison.OrdinalIgnoreCase))
                        {
                            return "wpf";
                        }
                    }
                    catch (Exception ex)
                    {
                        simpleLog.MarsLoggerSimple.Error("GetClassExtension", $"Error getting WinClass: {ex.Message}", ex);
                    }
                }

                // 检查 controlClassTypeFromAPI
                if (!string.IsNullOrEmpty(objInfo.controlClassTypeFromAPI))
                {
                    string apiType = objInfo.controlClassTypeFromAPI.ToLower();
                    if (apiType == "standard")
                        return "standard";
                    if (apiType == "winforms" || apiType == "winform")
                        return "winform";
                    if (apiType == "wpf")
                        return "wpf";
                }

                // 检查 referenceToObj 类型
                if (objInfo.referenceToObj != null)
                {
                    Type objType = objInfo.referenceToObj.GetType();
                    if (objType != null)
                    {
                        string typeName = objType.FullName ?? "";
                        string typeNamespace = objType.Namespace ?? "";

                        // 检查是否为 WinForm
                        if (typeNamespace.IndexOf("System.Windows.Forms", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            typeName.IndexOf("WindowsForms", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return "winform";
                        }

                        // 检查是否为 WPF
                        if (typeNamespace.IndexOf("System.Windows", StringComparison.OrdinalIgnoreCase) >= 0 &&
                            typeNamespace.IndexOf("System.Windows.Forms", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            // 排除 System.Windows.Forms，但包含其他 System.Windows 命名空间
                            if (typeNamespace.IndexOf("System.Windows.Controls", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                typeNamespace.IndexOf("System.Windows.Media", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                typeNamespace.IndexOf("System.Windows.Shapes", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                return "wpf";
                            }
                        }
                    }
                }

                // 检查 objectType 或 objectTypePath
                if (!string.IsNullOrEmpty(objInfo.objectType))
                {
                    string objectType = objInfo.objectType.ToLower();
                    if (objectType.IndexOf("winform", StringComparison.OrdinalIgnoreCase) >= 0)
                        return "winform";
                    if (objectType.IndexOf("wpf", StringComparison.OrdinalIgnoreCase) >= 0)
                        return "wpf";
                }

                if (!string.IsNullOrEmpty(objInfo.objectTypePath))
                {
                    string objectTypePath = objInfo.objectTypePath.ToLower();
                    if (objectTypePath.IndexOf("system.windows.forms", StringComparison.OrdinalIgnoreCase) >= 0)
                        return "winform";
                    if (objectTypePath.IndexOf("system.windows", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        objectTypePath.IndexOf("system.windows.forms", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        return "wpf";
                    }
                }

                // 检查 CurrentSpyMode
                if (CurrentSpyMode == Mars.Inter.MQCenter.spyHelper.enSpyMode.spyMode_net_winform_wpf ||
                    CurrentSpyMode == Mars.Inter.MQCenter.spyHelper.enSpyMode.sypMode_net_core_wpf)
                {
                    return "wpf";
                }

                if (CurrentSpyMode == Mars.Inter.MQCenter.spyHelper.enSpyMode.spyMode_net_winform_frameWork)
                {
                    return "winform";
                }

                // 默认为 standard
                return "standard";
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("GetClassExtension", $"Error getting class extension: {ex.Message}", ex);
                return "standard";
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (this.targetControlNode == null) return;
            this.winformObjectTreeview.SelectedNode = this.targetControlNode;
            if (this.targetControlNode.Parent != null)
            {
                if (!this.targetControlNode.Parent.IsExpanded)
                    this.targetControlNode.Parent.Expand();
            }
            this.targetControlNode.EnsureVisible();
        }

        private void toolStripTextBox1_Click(object sender, EventArgs e)
        {

        }
        private bool SearchRecursive(IEnumerable nodes, string searchFor)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Text.IndexOf(searchFor, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    winformObjectTreeview.SelectedNode = node;
                    node.BackColor = Color.Yellow;
                    searchedNodes.Add(node);
                }
                if (SearchRecursive(node.Nodes, searchFor))
                    return true;
            }
            return false;
        }
        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            if (searchedNodes != null)
            {
                searchedNodes.ForEach(p => p.BackColor = Color.Transparent);

            }
            if (string.IsNullOrEmpty(searchText.Text))
            {
                MessageBox.Show("Please input any you want to search in the next text box", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SearchRecursive(winformObjectTreeview.Nodes, searchText.Text);

            if ((searchedNodes != null) && (searchedNodes.Count > 0))
            {
                searchPre.Enabled = true;
                searchNext.Enabled = true;
                this.currentSearchId = 0;
            }
            else
            {
                searchPre.Enabled = false;
                searchNext.Enabled = false;
                this.currentSearchId = -1;
            }
        }

        private void toolStripLabel2_Click(object sender, EventArgs e)
        {
            if (sender == searchPre)
            {
                if (this.currentSearchId - 1 >= 0)
                {
                    this.currentSearchId--;
                    winformObjectTreeview.SelectedNode = searchedNodes[this.currentSearchId];

                }
                else
                {
                    searchPre.Enabled = false;
                }
            }
            else
            {
                if (this.currentSearchId + 1 <= this.searchedNodes.Count)
                {
                    this.currentSearchId++;
                    winformObjectTreeview.SelectedNode = searchedNodes[this.currentSearchId];

                }
                else
                {
                    searchNext.Enabled = false;
                }
            }
        }

        private void MarsObjSpyForm_Shown(object sender, EventArgs e)
        {
            reloadObjects(this.allObjects);
            //定位到目标对象
            if (targetControlNode != null)
            {
                winformObjectTreeview.SelectedNode = targetControlNode;
                targetControlNode.EnsureVisible();
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            //refresh
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {

        }

        private void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void dataGridView1_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex != this.dataGridView1.Rows.Count - 1)
            {
                ////check whether the row is tagged with img
                //var clickedRowx = this.dataGridView1.Rows[e.RowIndex];
                //object rowTag = clickedRowx.Tag;
                //if (rowTag != null)
                //{
                //    if (rowTag is string)
                //    {
                //        string tagInfoOfRow = (string)rowTag;
                //        if (string.Compare("image", tagInfoOfRow, true) == 0)
                //        {

                //            if (this.controlPicturePreview.Image != null)
                //                this.controlPicturePreview.Image.Dispose();
                //            this.controlPicturePreview.InitialImage = null;
                //            this.controlPicturePreview.SizeMode = PictureBoxSizeMode.CenterImage;
                //            string strFilePath = clickedRowx.Cells["objPropValue"].Value as string;
                //            if ((!string.IsNullOrEmpty(strFilePath)) && (System.IO.File.Exists(strFilePath)))
                //            {
                //                this.controlPicturePreview.Load(strFilePath);
                //            }

                //        }
                //    }
                //}
                return;
            }
            var clickedRow = this.dataGridView1.Rows[e.RowIndex];
            if (clickedRow == null) return;
            string strType = (string)clickedRow.Cells[e.ColumnIndex].Value;
            if (string.IsNullOrEmpty(strType)) return;
            if (strType.StartsWith("System", StringComparison.OrdinalIgnoreCase)
                || strType.StartsWith("Window", StringComparison.OrdinalIgnoreCase))
                return;

            MarsObjectNavigate frmNav = new MarsObjectNavigate();
            frmNav.SetNaviInfo(clickedRow.Tag, clickedRow.Cells["objPropValue"].Value as string);
            frmNav.ShowDialog();
        }

        private void button2_Click(object sender, EventArgs e)
        {

        }



        private void button3_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {

        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void toolStripButtonGen_Click(object sender, EventArgs e)
        {
            /// add object to pool
            /// 
            int idMsgbox = 0;
            try
            {
                if (this.winformObjectTreeview.SelectedNode == null) return;
                MarsSpiedObjectInfo objInfo = this.winformObjectTreeview.SelectedNode.Tag as MarsSpiedObjectInfo;
                if (objInfo == null) return;
                String strItemText = objInfo.getDisplayId();

                BuildObj(this.winformObjectTreeview.SelectedNode, "UNSET");
                for (int i = 0; i < (this.selectedObjectsListView.Items.Count); i++)
                {
                    string itmTxt = this.selectedObjectsListView.Items[i].Text;
                    if (string.IsNullOrEmpty(itmTxt)) continue;
                    if (string.Compare(itmTxt, strItemText, true) == 0)
                    {
                        return;
                    }
                }
                idMsgbox = 1;
                var newItm = this.selectedObjectsListView.Items.Add(strItemText);
                newItm.Tag = objInfo;
            }
            finally
            {
                if (idMsgbox == 0)
                {
                    MessageBox.Show("please select an object from left tree first.", "Message");
                }
            }
        }

        private void ValidateSpiedObjectInfo(List<MarsSpyGeneratedQuickAccess> objQuickAccessItems)
        {
            if (objQuickAccessItems == null) return;
            if (!objQuickAccessItems.Any(p => p.PropertyName.Equals(MarsSpyGeneratedQuickAccess.cnst_objectPegWindow)))
            {
                objQuickAccessItems.Add(new MarsSpyGeneratedQuickAccess()
                {
                    PropertyName = MarsSpyGeneratedQuickAccess.cnst_objectPegWindow,
                    PropertyValue = MarsSpyGeneratedQuickAccess.cnst_defaultPegwindowHint
                });
            }
            if (!objQuickAccessItems.Any(p => p.PropertyName.Equals(MarsSpyGeneratedQuickAccess.cnst_appliedApp)))
            {
                objQuickAccessItems.Add(new MarsSpyGeneratedQuickAccess()
                {
                    PropertyName = MarsSpyGeneratedQuickAccess.cnst_appliedApp,
                    PropertyValue = string.IsNullOrEmpty(targetApplicationShortName.Text.Trim()) ? MarsSpyGeneratedQuickAccess.cnst_defaultAppNameHint : targetApplicationShortName.Text.Trim()
                });
            }

            if (!objQuickAccessItems.Any(p => p.PropertyName.Equals(MarsSpyGeneratedQuickAccess.cnst_isPegwindow)))
            {
                objQuickAccessItems.Add(new MarsSpyGeneratedQuickAccess()
                {
                    PropertyName = MarsSpyGeneratedQuickAccess.cnst_isPegwindow,
                    PropertyValue = "False"
                });
            }
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            // 产生目标quickaccess
            if (this.selectedObjectsListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select only ONE item from the left list.", "Message",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var selectedItm = selectedObjectsListView.SelectedItems[0];
            MarsSpiedObjectInfo objInfo = selectedItm.Tag as MarsSpiedObjectInfo;
            if (objInfo == null)
            {
                MessageBox.Show("No assigned object attached to tag, the item will be removed.", "Message",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.selectedObjectsListView.Items.Remove(selectedItm);
                return;
            }
            /**
             * 算法：
             * 1，是否generatedQuickAccessItems为空，如果为空，自动产生数据
             * 2，将产生的数据写到旁边的table中
             * 3，
             * */
            if ((objInfo.generatedQuickAccessItems == null)
                || (objInfo.generatedQuickAccessItems.Count == 0))
            {
                //调用 generate properties
                generateIds_Click(this.generateIds, null);
            }
            else
            {
                // 判断是否存在系统所需的pegwindow和 happyname项目
                ValidateSpiedObjectInfo(objInfo.generatedQuickAccessItems);
            }
            this.targetObjPropertyGrid.Rows.Clear();
            DataGridViewComboBoxColumn bxcolumn = this.targetObjPropertyGrid.Columns["marsObjIdName"] as DataGridViewComboBoxColumn;
            foreach (var itm in objInfo.generatedQuickAccessItems)
            {
                if (itm == null) continue;
                int idxValue = bxcolumn.Items.IndexOf(itm.PropertyName);
                if (idxValue < 0) continue;

                int iRowIdx = this.targetObjPropertyGrid.Rows.Add();
                var row = this.targetObjPropertyGrid.Rows[iRowIdx];
                row.Cells["marsObjIdName"].Value = bxcolumn.Items[idxValue];
                row.Tag = itm;
                row.Cells["marsObjectValue"].Value = itm.PropertyValue;
            }
        }

        private void newProperBtn_Click(object sender, EventArgs e)
        {
            if (this.targetObjPropertyGrid.Rows.Count == 0)
            {
                this.targetObjPropertyGrid.Rows.Add();
                return;
            }
            var lastRow = this.targetObjPropertyGrid.Rows[this.targetObjPropertyGrid.Rows.Count - 1];
            if ((lastRow.Tag == null) || (lastRow.IsNewRow))
            {
                // just a new row
                MessageBox.Show("A new row is already created.", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            this.targetObjPropertyGrid.Rows.Add();
        }

        private void targetObjPropertiesGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void savePropertiesButton_Click(object sender, EventArgs e)
        {
            //

        }
        /// <summary>
        /// 修正name和namePath
        /// 算法：
        /// 1，先判断是否在列表中存在重名
        /// 2，如果重名，将使用name path
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void generateIds_Click(object sender, EventArgs e)
        {

            if (selectedObjectsListView.SelectedItems.Count != 1)
            {
                MessageBox.Show("please select only ONE object you want to generate MARS properties.", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var t = selectedObjectsListView.SelectedItems[0].Tag;
            if (!(t is MarsSpiedObjectInfo))
            {
                MessageBox.Show("tag is not 'MarsSpiedObjectInfo'.", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            MarsSpiedObjectInfo o = t as MarsSpiedObjectInfo;
            ValidateSpiedObjectInfo(o.generatedQuickAccessItems);
            /**
             * 算法：
             * 1，是否存在名称
             * 2，如果存在名称，是否存在相同名称的object
             * 3，获得该对像的 name path
             * */
            if ((o.referenceToObj == null) || (
                o.referenceToObj as System.Windows.Forms.Control == null))
            {
                MessageBox.Show("tag is not Control.", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string strTargetName = ((System.Windows.Forms.Control)o.referenceToObj).Name;
            bool isPathRequired = false;
            if (string.IsNullOrEmpty(strTargetName))
            {
                //名称为空，必须使用namepath
                isPathRequired = true;
            }
            else
            {
                int iCnt = allControls.Count(p => (p != null) && (p.Name.Equals(strTargetName, StringComparison.OrdinalIgnoreCase)));
                //先增加name的
                MarsSpyGeneratedQuickAccess objNameInfo = o.generatedQuickAccessItems.Where(p => p.PropertyName != null && p.PropertyName.Equals(MarsSpyGeneratedQuickAccess.cnst_swfname, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (objNameInfo == null)
                {
                    o.generatedQuickAccessItems.Add(new MarsSpyGeneratedQuickAccess()
                    {
                        PropertyName = MarsSpyGeneratedQuickAccess.cnst_swfname,
                        PropertyValue = strTargetName
                    });
                }
                else
                {
                    objNameInfo.PropertyValue = strTargetName;
                }
                isPathRequired = true;
            }
            if (isPathRequired)
            {
                /// 判断是否存在swfname path
                /// 
                var namePath = o.generatedQuickAccessItems.Where(p => p.PropertyName.Equals(MarsSpyGeneratedQuickAccess.cnst_swfnamePath, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                string strNamePath = ReflectorForCSharp.MarsGetParentsNames(((System.Windows.Forms.Control)o.referenceToObj).Parent);
                if (namePath == null)
                {
                    o.generatedQuickAccessItems.Add(namePath = new MarsSpyGeneratedQuickAccess()
                    {
                        PropertyName = MarsSpyGeneratedQuickAccess.cnst_swfnamePath,
                        PropertyValue = strNamePath
                    });
                }
                else
                {
                    namePath.PropertyValue = strNamePath;
                }
            }
            return;
            //if (isPathRequired)
            //    List<System.Windows.Forms.Control> lstCtrl = new List<System.Windows.Forms.Control>();

            //    allControls.ForEach(c=> {
            //    if (c == null) return;
            //    if (c.Name.Equals(strTargetName, StringComparison.OrdinalIgnoreCase))
            //    {
            //        lstCtrl.Add(c);
            //    }
            //});

            //if (lstCtrl.Count > 1)
            //{
            //    //多個对象，需要使用 name path
            //    var reflector = new ReflectorForCSharp();
            //    Dictionary<System.Windows.Forms.Control,List<MarsSpyGeneratedQuickAccess>> lstOfNamePath 
            //        = new Dictionary<System.Windows.Forms.Control, List<MarsSpyGeneratedQuickAccess>>();
            //    //List<string> lstTmp = new List<string>();
            //    lstCtrl.ForEach(p =>
            //    {
            //        if (p == null) return;                    
            //        List<MarsSpyGeneratedQuickAccess> tmpListSpyInfos = new List<MarsSpyGeneratedQuickAccess>();
            //        MarsSpyGeneratedQuickAccess tmpName = null;
            //        tmpListSpyInfos.Add(tmpName = new interProcess.MarsSpyGeneratedQuickAccess());
            //        bool isOk = tmpName.gen_DataForObj(p, MarsSpyGeneratedQuickAccess.cnst_swfnamePath);
            //        if (!isOk) return;
            //        tmpListSpyInfos.Add(tmpName);
            //        lstOfNamePath.Add(p, tmpListSpyInfos);
            //    });
            //    /**
            //     * 判断是否存在相同的
            //     * */
            //    List<MarsSpyGeneratedQuickAccess> targetObjPrep = lstOfNamePath[o.referenceToObj as System.Windows.Forms.Control];
            //    if (targetObjPrep == null)
            //    {
            //        // 没有path？这个可能吗？
            //        return;
            //    }
            //    List<System.Windows.Forms.Control> duplicatedControl = new List<System.Windows.Forms.Control>();
            //    foreach (var p in lstOfNamePath.Keys)
            //    {
            //        if (p == null) continue;
            //        if (p.Equals(o.referenceToObj as System.Windows.Forms.Control)) continue;

            //        if (targetObjPrep[0].PropertyValue.Equals(lstOfNamePath[p][0].PropertyValue, StringComparison.OrdinalIgnoreCase))
            //            duplicatedControl.Add(p);
            //    }
            //    duplicatedControl.ForEach(p=>
            //    {
            //        lstOfNamePath.Remove(p);
            //    });
            //    duplicatedControl.Clear();
            //    if (lstOfNamePath.Keys.Count==1) // 确定了
            //    {
            //        //lstOfNamePath[lstOfNamePath.Keys[0]];
            //        // 将List<MarsSpyGeneratedQuickAccess> 对象写到datagridview中
            //        LoadQuickAcessToDataGridView(strTargetName, lstOfNamePath[lstOfNamePath.Keys.FirstOrDefault()]); 
            //        return;
            //    }

            //    var tmpo = o.referenceToObj as System.Windows.Forms.Control;

            //    string strNamePath = reflector.getNamePath(tmpo);

            //}
            //else
            //{                
            //    LoadQuickAcessToDataGridView(strTargetName,o.generatedQuickAccessItems = new List<MarsSpyGeneratedQuickAccess>()
            //    {
            //        new MarsSpyGeneratedQuickAccess()
            //        {
            //            PropertyName = MarsSpyGeneratedQuickAccess.cnst_swfname,
            //            PropertyValue = strTargetName
            //        }
            //    });
            //}

        }

        private bool insertIntoObjectPropertyAndValue(string strPro, string strV, Color c = default(Color))
        {
            int idx = this.targetObjPropertyGrid.Rows.Add();
            var row = this.targetObjPropertyGrid.Rows[idx];
            DataGridViewComboBoxCell c0 = row.Cells[0] as DataGridViewComboBoxCell;

            if (!c.Equals(default(Color)))
            {
                row.DefaultCellStyle.BackColor = c;
                c0.Style.BackColor = c;
            }

            int itmIdx = c0.Items.IndexOf(strPro);
            if (itmIdx < 0)
            {
                this.targetObjPropertyGrid.Rows.Remove(row);
                return false;
            }
            c0.Value = c0.Items[itmIdx];
            row.Cells[1].Value = strV;

            row.Tag = new MarsSpyGeneratedQuickAccess()
            {
                PropertyName = c0.Value as string,
                PropertyValue = strV
            };

            return true;
        }

        private void LoadQuickAcessToDataGridView(string strHappyName, List<MarsSpyGeneratedQuickAccess> lists)
        {
            this.targetObjPropertyGrid.Rows.Clear();
            if (lists == null) return;
            // 增加happy name and default name
            bool isInserted = insertIntoObjectPropertyAndValue(MarsSpyGeneratedQuickAccess.cnst_objectHappyName, "Set object Name here", Color.LightYellow);

            lists.ForEach(p =>
            {
                if (p == null) return;
                insertIntoObjectPropertyAndValue(p.PropertyName, p.PropertyValue);
            });

            // 增加default的pegwindw
            insertIntoObjectPropertyAndValue(MarsSpyGeneratedQuickAccess.cnst_objectPegWindow, MarsSpyGeneratedQuickAccess.cnst_defaultPegwindowHint, Color.LightYellow);
            insertIntoObjectPropertyAndValue(MarsSpyGeneratedQuickAccess.cnst_appliedApp, MarsSpyGeneratedQuickAccess.cnst_defaultAppNameHint, Color.LightYellow);
            this.targetObjPropertyGrid.Tag = lists;
        }

        private void toolStripButtonAddPool_Click(object sender, EventArgs e)
        {
            if (this.winformObjectTreeview.SelectedNode == null)
            {
                MessageBox.Show("Please select an item from left treeview, before you want to add it to object pool", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var itmNew = selectedObjectsListView.Items.Add(this.winformObjectTreeview.SelectedNode.Text);

            itmNew.Tag = this.winformObjectTreeview.SelectedNode.Tag;
            if (itmNew.Tag == null) return;
            if ((itmNew.Tag as Mars.message.Inter.MQCenter.interProcess.MarsSpiedObjectInfo) == null) return;

            Mars.message.Inter.MQCenter.interProcess.MarsSpiedObjectInfo spyer = itmNew.Tag as Mars.message.Inter.MQCenter.interProcess.MarsSpiedObjectInfo;
            if (spyer.referenceToObj == null) return;
            if ((spyer.referenceToObj as System.Windows.Forms.Control) == null) return;
            if (spyer.generatedQuickAccessItems == null)
            {
                spyer.generatedQuickAccessItems = new List<MarsSpyGeneratedQuickAccess>();
                spyer.generatedQuickAccessItems.Add(new MarsSpyGeneratedQuickAccess()
                {
                    PropertyName = MarsSpyGeneratedQuickAccess.cnst_swfname,
                    PropertyValue = (spyer.referenceToObj as System.Windows.Forms.Control).Name
                });
            }

        }
        /// <summary>
        /// Save from List pool to sever
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            /// save jason to file 
            /// 
            string strError = "";
            if (selectedObjectsListView.Items.Count == 0)
            {
                MessageBox.Show("Please select at least one object into Object Pool", "Message", MessageBoxButtons.OK);
                return;
            }

            //组成一个数组的message，然后回写到消息队列中，采用keywords为"createNewObjectsFromSpyer"
            /// 算法:
            /// 1, 将所有的数据放到一个array【】中，
            /// 2，采用javascript组建json string
            /// 3，send keyword "InsertOrUpdateObjects to sever via 
            /// 
            var enmerable = selectedObjectsListView.Items.Cast<System.Windows.Forms.ListViewItem>();
            var lst = enmerable.Where(p => (p.Tag as Mars.message.Inter.MQCenter.interProcess.MarsSpiedObjectInfo) != null)
                .Select(p => ((Mars.message.Inter.MQCenter.interProcess.MarsSpiedObjectInfo)p.Tag)) //.generatedQuickAccessItems)
                .ToList();
            System.Web.Script.Serialization.JavaScriptSerializer javaScript = new System.Web.Script.Serialization.JavaScriptSerializer();
            string strDataToSendBack = javaScript.Serialize(lst);

            // save to special file
            var pth = System.IO.Path.GetDirectoryName(typeof(MarsSpiedObjectInfo).Assembly.Location);
            var currentSystemUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            pth = System.IO.Path.Combine(pth, $"data\\obj\\{currentSystemUser}");
            if (!System.IO.Directory.Exists(pth))
            {
                System.IO.Directory.CreateDirectory(pth);
            }
            /// get uuid from file
            /// not necessary to get the uuid, as only objects info should send to back via tool, otherwise
            /// the mq info modal could be heave
            //var targetJsonFile = System.IO.Path.Combine(pth,$"marsObjUUID.uuid");
            //if (!System.IO.File.Exists(targetJsonFile))
            //{
            //    MessageBox.Show("no uuid file is created, please check.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    return;
            //}
            ///// get last row of the file
            ///// 
            //var lines = System.IO.File.ReadAllLines(targetJsonFile);
            //if (!((lines != null) && (lines.Length > 0)))
            //{
            //    MessageBox.Show("marsObjUUID.uuid file is empty", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    return;
            //}
            //var currentUUID = lines[lines.Length - 1].Trim();

            /// write to target file
            /// 
            string targetJsonFile = System.IO.Path.Combine(pth, Utility.MarsConstants.CNST_SYPTOOL_JSONOBJ_FILENAME);
            if (System.IO.File.Exists(targetJsonFile))
            {
                try
                {
                    System.IO.File.Delete(targetJsonFile);
                }
                catch (Exception ex)
                {
                    simpleLog.MarsLoggerSimple.Error("button1_Click", ex.Message, ex);
                    MessageBox.Show($"can't delete object transfer file|{targetJsonFile}", "Error", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
            }
            // add an end mark 
            strDataToSendBack = $"{strDataToSendBack}\r\n{Utility.MarsConstants.CNST_SPYTOOL_OBJ_FILE_ENDMARK}";
            System.IO.File.WriteAllText(targetJsonFile, strDataToSendBack);
            MessageBox.Show($"MARS Object tools is going to send back|{lst.Count}| object(s)");
            ////bool isAllObjectValiate = ValiateObjects(allObjects, ref strError);
            ////if (!isAllObjectValiate)
            ////{
            ////    MessageBox.Show(strError, "Message", MessageBoxButtons.OK);
            ////    return;
            ////}
            ////System.Web.Script.Serialization.JavaScriptSerializer javaScript = new System.Web.Script.Serialization.JavaScriptSerializer();
            //MARSTestStep objNewMarsObjes = new MARSTestStep();
            //objNewMarsObjes.AckTime = DateTime.Now;
            //objNewMarsObjes.advice2User = "new objects from spyer";

            //objNewMarsObjes.AttachInfo = strDataToSendBack; //s实际的json格式的数据
            //objNewMarsObjes.DataToSet = "";
            //objNewMarsObjes.Keyword = "createNewObjectsFromSpyer";
            //objNewMarsObjes.MessageType = MARSMessageType.e_CreateObjectFromSpyer;
            //objNewMarsObjes.TestResult = new MARSStepResult();

            //simpleLog.MarsLoggerSimple.Info("button1_Click", objNewMarsObjes.AttachInfo);
        }

        private bool ValiateObjects(List<MarsSpiedObjectInfo> allObjects, ref string strError)
        {
            int idx = 0;
            foreach (var itm in allObjects)
            {
                idx++;
                if (itm == null) continue;
                if ((itm.generatedQuickAccessItems == null) || (itm.generatedQuickAccessItems.Count <= 0))
                {
                    strError = $"#{idx} object's quick access information is empty or null";
                    return false;
                }
                var peg = (from o in itm.generatedQuickAccessItems
                           where o.PropertyName.Equals(MarsSpyGeneratedQuickAccess.cnst_objectPegWindow, StringComparison.OrdinalIgnoreCase)
                           select o
                          ).FirstOrDefault();
                if (peg == null)
                {
                    strError = $"#{idx} object's pegwindow information is empty or null";
                    return false;
                }
                var app = (from o in itm.generatedQuickAccessItems
                           where o.PropertyName.Equals(MarsSpyGeneratedQuickAccess.cnst_appliedApp, StringComparison.OrdinalIgnoreCase)
                           select o
                          ).FirstOrDefault();
                if (peg == null)
                {
                    strError = $"#{idx} object's pegwindow information is empty or null";
                    return false;
                }
            }
            return true;

        }

        private void targetObjPropertyGrid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            /// 将修改的数据回写到assigned object
            /// 
            var row = this.targetObjPropertyGrid.Rows[e.RowIndex];
            if (row == null) return;
            if ((row.Tag == null) || (!(row.Tag is MarsSpyGeneratedQuickAccess)))
            {
                MessageBox.Show("No quick access object is assigned to this row.");
                return;
            }
            MarsSpyGeneratedQuickAccess itmQuickAccess = row.Tag as MarsSpyGeneratedQuickAccess;
            itmQuickAccess.PropertyName = row.Cells["marsObjIdName"].Value as string;
            itmQuickAccess.PropertyValue = row.Cells["marsObjectValue"].Value as string;

        }

        private void removeObjectFromPool_Click(object sender, EventArgs e)
        {
            if ((this.selectedObjectsListView.SelectedItems == null)
                || (this.selectedObjectsListView.SelectedItems.Count == 0))
            {
                MessageBox.Show("Please Select an item from Object Pool first", "Message", MessageBoxButtons.OK);
                return;
            }
            for (int i = this.selectedObjectsListView.SelectedItems.Count - 1; i >= 0; i--)
            {
                this.selectedObjectsListView.Items.Remove(this.selectedObjectsListView.SelectedItems[i]);
            }
        }

        private void selectedObjectsListView_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void selectedObjectsListView_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            try
            {
                if (e.Label == null) return; // 用户取消了编辑

                // 只允许编辑 MARS Object Name 列（第二列，索引为1）
                if (e.Item >= 0 && e.Item < selectedObjectsListView.Items.Count)
                {
                    var item = selectedObjectsListView.Items[e.Item];
                    if (item.SubItems.Count > 1)
                    {
                        // 更新 MARS Object Name
                        item.SubItems[1].Text = e.Label;

                        simpleLog.MarsLoggerSimple.Info("selectedObjectsListView_AfterLabelEdit",
                            $"Updated MARS Object Name to: {e.Label}");
                    }
                }
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("selectedObjectsListView_AfterLabelEdit",
                    $"Error handling label edit: {ex.Message}", ex);
            }
        }

        private void deleteSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectedObjectsListView.SelectedItems.Count == 0)
                {
                    MessageBox.Show(
                        GetLocalizedString("NoItemSelected", "Please select an item to delete"),
                        GetLocalizedString("Information", "Information"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                var result = MessageBox.Show(
                    GetLocalizedString("ConfirmDeleteSelected", "Are you sure you want to delete the selected item(s)?"),
                    GetLocalizedString("ConfirmDelete", "Confirm Delete"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    for (int i = selectedObjectsListView.SelectedItems.Count - 1; i >= 0; i--)
                    {
                        selectedObjectsListView.Items.Remove(selectedObjectsListView.SelectedItems[i]);
                    }

                    simpleLog.MarsLoggerSimple.Info("deleteSelectedToolStripMenuItem_Click",
                        "Deleted selected items from object pool");
                }
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("deleteSelectedToolStripMenuItem_Click",
                    $"Error deleting selected items: {ex.Message}", ex);
                MessageBox.Show(
                    GetLocalizedString("ErrorDeletingItems", $"Error deleting items: {ex.Message}"),
                    GetLocalizedString("Error", "Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void deleteAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectedObjectsListView.Items.Count == 0)
                {
                    MessageBox.Show(
                        GetLocalizedString("NoItemsToDelete", "No items to delete"),
                        GetLocalizedString("Information", "Information"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                var result = MessageBox.Show(
                    GetLocalizedString("ConfirmDeleteAll", "Are you sure you want to delete ALL items?"),
                    GetLocalizedString("ConfirmDeleteAll", "Confirm Delete All"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    selectedObjectsListView.Items.Clear();

                    simpleLog.MarsLoggerSimple.Info("deleteAllToolStripMenuItem_Click",
                        "Cleared all items from object pool");
                }
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("deleteAllToolStripMenuItem_Click",
                    $"Error deleting all items: {ex.Message}", ex);
                MessageBox.Show(
                    GetLocalizedString("ErrorDeletingAllItems", $"Error deleting all items: {ex.Message}"),
                    GetLocalizedString("Error", "Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void detailsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectedObjectsListView.SelectedItems.Count == 0)
                {
                    MessageBox.Show(
                        GetLocalizedString("NoItemSelected", "Please select an item to view details"),
                        GetLocalizedString("Information", "Information"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                var selectedItem = selectedObjectsListView.SelectedItems[0];
                var objInfo = selectedItem.Tag as MarsSpiedObjectInfo;

                if (objInfo == null)
                {
                    MessageBox.Show(
                        GetLocalizedString("NoObjectInfo", "No object information available"),
                        GetLocalizedString("Information", "Information"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                // 显示详细信息
                var details = new StringBuilder();
                details.AppendLine($"{GetLocalizedString("ObjectName", "Object Name")}: {objInfo.objectName ?? "N/A"}");
                details.AppendLine($"{GetLocalizedString("ObjectType", "Object Type")}: {objInfo.objectType ?? "N/A"}");
                details.AppendLine($"{GetLocalizedString("ControlClass", "Control Class")}: {objInfo.controlClassTypeFromAPI ?? "N/A"}");
                details.AppendLine($"{GetLocalizedString("Location", "Location")}: X={objInfo.x}, Y={objInfo.y}, W={objInfo.w}, H={objInfo.h}");

                if (!string.IsNullOrEmpty(objInfo.marsNamePath))
                {
                    details.AppendLine($"{GetLocalizedString("MarsNamePath", "Mars Name Path")}: {objInfo.marsNamePath}");
                }

                if (!string.IsNullOrEmpty(objInfo.marsMSAARoleNamePath))
                {
                    details.AppendLine($"{GetLocalizedString("MSAARolePath", "MSAA Role Path")}: {objInfo.marsMSAARoleNamePath}");
                }

                MessageBox.Show(
                    details.ToString(),
                    GetLocalizedString("ObjectDetails", "Object Details"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("detailsToolStripMenuItem_Click",
                    $"Error showing details: {ex.Message}", ex);
                MessageBox.Show(
                    GetLocalizedString("ErrorShowingDetails", $"Error showing details: {ex.Message}"),
                    GetLocalizedString("Error", "Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void selectedObjectsListView_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            // 在OwnerDraw模式下，行高由ListView自动管理
            // 我们只需要确保绘制正确的内容
        }

        private void selectedObjectsListView_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            try
            {
                // 设置背景色
                Color backColor = e.Item.Selected ? SystemColors.Highlight : SystemColors.Window;
                using (SolidBrush backBrush = new SolidBrush(backColor))
                {
                    e.Graphics.FillRectangle(backBrush, e.Bounds);
                }

                // 设置文本颜色
                Color textColor = e.Item.Selected ? SystemColors.HighlightText : SystemColors.WindowText;

                string text = e.SubItem.Text;

                if (e.ColumnIndex == 2) // Object IDs 列
                {
                    // 多行显示 Object IDs
                    DrawMultilineText(e.Graphics, text, e.Bounds, textColor, e.Item.Font);
                }
                else
                {
                    // 单行显示其他列
                    TextRenderer.DrawText(e.Graphics, text, e.Item.Font, e.Bounds, textColor,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.WordEllipsis);
                }

                // 绘制网格线
                using (Pen gridPen = new Pen(SystemColors.ControlDark))
                {
                    e.Graphics.DrawLine(gridPen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                }
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("selectedObjectsListView_DrawSubItem",
                    $"Error drawing subitem: {ex.Message}", ex);
            }
        }

        private void selectedObjectsListView_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            // 绘制列标题
            using (SolidBrush backBrush = new SolidBrush(SystemColors.Control))
            {
                e.Graphics.FillRectangle(backBrush, e.Bounds);
            }

            using (SolidBrush textBrush = new SolidBrush(SystemColors.ControlText))
            {
                TextRenderer.DrawText(e.Graphics, e.Header.Text, e.Font, e.Bounds, SystemColors.ControlText,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
            }

            // 绘制列标题边框
            using (Pen borderPen = new Pen(SystemColors.ControlDark))
            {
                e.Graphics.DrawRectangle(borderPen, e.Bounds);
            }
        }

        private void selectedObjectsListView_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Right)
                {
                    // 右键点击处理
                    return;
                }

                // 左键点击处理
                ListViewHitTestInfo hitTest = selectedObjectsListView.HitTest(e.Location);

                if (hitTest.Item != null && hitTest.SubItem != null)
                {
                    // 获取点击的列索引
                    int columnIndex = GetColumnIndexFromSubItem(hitTest.Item, hitTest.SubItem);

                    // 检查是否点击在 Object IDs 列（第3列，索引为2）
                    if (columnIndex == 2) // Object IDs 列
                    {
                        // 复制 Object IDs 内容到剪贴板
                        string objectIds = hitTest.SubItem.Text;
                        if (!string.IsNullOrEmpty(objectIds))
                        {
                            Clipboard.SetText(objectIds);

                            // 显示复制成功提示
                            MessageBox.Show(
                                GetLocalizedString("ObjectIdsCopied", "Object IDs copied to clipboard"),
                                GetLocalizedString("Information", "Information"),
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);

                            simpleLog.MarsLoggerSimple.Info("selectedObjectsListView_MouseClick",
                                $"Copied Object IDs to clipboard: {objectIds}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("selectedObjectsListView_MouseClick",
                    $"Error handling mouse click: {ex.Message}", ex);
            }
        }

        private int GetColumnIndexFromSubItem(ListViewItem item, ListViewItem.ListViewSubItem subItem)
        {
            try
            {
                // 遍历所有子项找到匹配的索引
                for (int i = 0; i < item.SubItems.Count; i++)
                {
                    if (item.SubItems[i] == subItem)
                    {
                        return i;
                    }
                }
                return -1; // 未找到
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("GetColumnIndexFromSubItem",
                    $"Error getting column index: {ex.Message}", ex);
                return -1;
            }
        }


        private void DrawMultilineText(Graphics graphics, string text, Rectangle bounds, Color textColor, Font font)
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                    return;

                // 将文本按分号分割并换行
                string[] lines = text.Split(';');
                StringBuilder formattedText = new StringBuilder();

                for (int i = 0; i < lines.Length; i++)
                {
                    if (i > 0)
                        formattedText.AppendLine();
                    formattedText.Append(lines[i].Trim());
                }

                // 使用 TextRenderer 绘制多行文本
                TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak;
                TextRenderer.DrawText(graphics, formattedText.ToString(), font, bounds, textColor, flags);
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("DrawMultilineText",
                    $"Error drawing multiline text: {ex.Message}", ex);
            }
        }

        private void batchAddObjects_Click(object sender, EventArgs e)
        {
            /// 算法：
            /// 1，是否选择了根节点？否则提示
            /// 2，选择根节点和叶节点
            /// 3，初始化所有节点的信息
            /// 
            string strError = "";
            if (!SelectedNodeIsRoot(ref strError))
            {
                MessageBox.Show(strError, "Message", MessageBoxButtons.OK);
                return;
            }

            List<TreeNode> allNodes = new List<TreeNode>();
            allNodes.Add(winformObjectTreeview.SelectedNode);
            FetchLeafNodes(winformObjectTreeview.SelectedNode, allNodes);

            /// 3，初始化所有节点的信息
            /// 
            CreateAllNodesPropertiesInfoAndLoadToPool(allNodes);
            /// 添加到objectpool
            /// 
            selectedObjectsListView.Items.Clear();
            allNodes.ForEach(p =>
            {
                MarsSpiedObjectInfo objInfo = p.Tag as MarsSpiedObjectInfo;
                if (objInfo == null) return;
                var itm = selectedObjectsListView.Items.Add(objInfo.getDisplayId());
                itm.Tag = objInfo;
            });
        }

        private void CreateAllNodesPropertiesInfoAndLoadToPool(List<TreeNode> allNodes)
        {
            selectedObjectsListView.Items.Clear();

            /// 算法：
            /// 1， 第一个是window
            /// 2， 其他作为该window的子对象
            /// 
            bool isOk = false;
            string strPegName = "";
            for (int i = 0; i < allNodes.Count; i++)
            {
                if (i == 0)
                {
                    isOk = BuildPegObj(allNodes[0], ref strPegName);
                    if (!isOk)
                    {
                        simpleLog.MarsLoggerSimple.Error("CreateAllNodesPropertiesInfoAndLoadToPool", "Can't create pegwindow objects, node could be null?");
                        MessageBox.Show("Can't Build Pegobjects", "Message", MessageBoxButtons.OK);
                        break;
                    }

                    continue;
                }

                // 构建其他数据
                BuildObj(allNodes[i], strPegName);
            }
        }

        private MarsSpyGeneratedQuickAccess getObjProertyByName(List<MarsSpyGeneratedQuickAccess> lstPro,
            string strPro,
            string strV,
            bool isCreate = true)
        {
            if (lstPro == null) return null;
            var target = lstPro.Where(p => p.PropertyName.Equals(strPro, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
            if ((isCreate) && (target == null))
            {
                lstPro.Add(target = new MarsSpyGeneratedQuickAccess()
                {
                    PropertyName = strPro,
                    PropertyValue = strV
                }); ;
            }
            return target;
        }

        private string getHappyName(string pegPrefix, string srcHappyName, bool isPeg = true)
        {
            System.Windows.Forms.RadioButton radioBtn = null;
            if (isPeg)
                radioBtn = pegwindNameFormatGroup.Controls.OfType<System.Windows.Forms.RadioButton>().FirstOrDefault(p => p.Checked);
            else
                radioBtn = objectsNameFormatGroup.Controls.OfType<System.Windows.Forms.RadioButton>().FirstOrDefault(p => p.Checked);
            if (radioBtn == null)
                radioBtn = pegNameFormat_uppercase;
            switch (radioBtn.Text.ToUpper())
            {
                case "UPPERCASE":
                    return string.IsNullOrEmpty(pegPrefix) ? srcHappyName.ToUpper() : $"{srcHappyName}_{srcHappyName}".ToUpper();
                case "LOWERCASE":
                    return string.IsNullOrEmpty(pegPrefix) ? srcHappyName.ToLower() : $"{srcHappyName}_{srcHappyName}".ToLower();

                default:
                    return string.IsNullOrEmpty(pegPrefix) ? srcHappyName : $"{srcHappyName}_{srcHappyName}";
            }
        }

        private bool BuildObj(TreeNode nd, string strPegName)
        {
            if (nd == null) return false;
            var objTmp = nd.Tag as MarsSpiedObjectInfo;
            if (objTmp == null) return false;

            System.Windows.Forms.Control c = objTmp.referenceToObj as System.Windows.Forms.Control;
            if (c == null) return false;
            /// 算法：
            /// 1，添加名称,
            /// 2，添加pegname, isPegwindow
            /// 3，添加happyName
            /// 4，判断是否添加name path
            /// 

            /// 1，添加名称
            bool isNamePathRequired = string.IsNullOrEmpty(c.Name);
            MarsSpyGeneratedQuickAccess swfName = null;
            if (!isNamePathRequired)
                swfName = getObjProertyByName(objTmp.generatedQuickAccessItems,
                MarsSpyGeneratedQuickAccess.cnst_swfname,
                c.Name);
            /// 2，添加pegname, isPegwindow
            var pegName = getObjProertyByName(objTmp.generatedQuickAccessItems,
                MarsSpyGeneratedQuickAccess.cnst_objectPegWindow,
                strPegName);
            var isPegwindow = getObjProertyByName(objTmp.generatedQuickAccessItems,
                MarsSpyGeneratedQuickAccess.cnst_isPegwindow,
                "False");
            /// 3，添加happyName
            /// 
            string pegPreFix = pegwindowNamePrefix.Text.Trim();
            string strHappyName = getHappyName(pegPreFix, c.Name, false);
            var happyName = getObjProertyByName(objTmp.generatedQuickAccessItems,
                MarsSpyGeneratedQuickAccess.cnst_objectHappyName,
                strHappyName);
            /// 4，判断是否添加name path
            if (!isNamePathRequired)
            {
                // 可能有重复的
                int iCnt = allControls.Count(p => (p != null) && (p.Name.Equals(c.Name, StringComparison.OrdinalIgnoreCase)));
                isNamePathRequired = iCnt > 1;
            }
            MarsSpyGeneratedQuickAccess swfNamePath = null;
            if (isNamePathRequired)
            {
                string strNamePath = ReflectorForCSharp.MarsGetParentsNames(c.Parent);
                swfNamePath = getObjProertyByName(objTmp.generatedQuickAccessItems,
                    MarsSpyGeneratedQuickAccess.cnst_swfnamePath,
                    strNamePath);
            }
            return true;
        }

        private bool BuildPegObj(TreeNode nd, ref string strPegName)
        {
            if (nd == null) return false;
            var pegTmp = nd.Tag as MarsSpiedObjectInfo;
            if (pegTmp == null) return false;

            System.Windows.Forms.Control c = pegTmp.referenceToObj as System.Windows.Forms.Control;
            if (c == null) return false;

            if (pegTmp.generatedQuickAccessItems == null)
                pegTmp.generatedQuickAccessItems = new List<MarsSpyGeneratedQuickAccess>();


            /// 添加name
            /// 
            //objTmp.generatedQuickAccessItems.Clear();
            // 判断是否存在 swf name
            var swfname = getObjProertyByName(pegTmp.generatedQuickAccessItems,
                MarsSpyGeneratedQuickAccess.cnst_swfname,
                c.Name);

            var isPegwindow = getObjProertyByName(pegTmp.generatedQuickAccessItems,
                MarsSpyGeneratedQuickAccess.cnst_isPegwindow,
                "True");
            // pegwindow 不需要进行path，最多使用一个Text,暂时不添加

            // 处理pegwidnow name
            var radioBtn = pegwindNameFormatGroup.Controls.OfType<System.Windows.Forms.RadioButton>().FirstOrDefault(p => p.Checked);
            if (radioBtn == null)
                radioBtn = pegNameFormat_uppercase;
            string pegPreFix = pegwindowNamePrefix.Text.Trim();
            string tmpPegName = string.IsNullOrEmpty(pegPreFix) ? $"{c.Name}" : $"{pegwindowNamePrefix.Text}_{c.Name}";
            switch (radioBtn.Text.ToUpper())
            {
                case "UPPERCASE":
                    tmpPegName = tmpPegName.ToUpper();
                    break;
                case "LOWERCASE":
                    tmpPegName = tmpPegName.ToLower();
                    break;
                default:
                    break;
            }
            var happyName = getObjProertyByName(pegTmp.generatedQuickAccessItems,
                MarsSpyGeneratedQuickAccess.cnst_objectHappyName,
                tmpPegName);
            var pegName = getObjProertyByName(pegTmp.generatedQuickAccessItems,
                MarsSpyGeneratedQuickAccess.cnst_objectPegWindow,
                tmpPegName);

            strPegName = tmpPegName;
            return true;
        }

        private void FetchLeafNodes(TreeNode root, List<TreeNode> targetNodes)
        {
            if ((root.Nodes == null)
                || (root.Nodes.Count == 0))
            {
                targetNodes.Add(root);
                return;
            }
            for (int i = 0; i < root.Nodes.Count; i++)
            {
                var n = root.Nodes[i];
                FetchLeafNodes(n, targetNodes);
            }

        }

        private bool SelectedNodeIsRoot(ref string strError)
        {
            if ((winformObjectTreeview.SelectedNode == null)
                || (winformObjectTreeview.SelectedNode.Parent != null))
            {
                strError = "Please select ONLY A root node first";
                return false;
            }

            return true;
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            int x = -1, y = -1;
            if (!((int.TryParse(mouseX.Text.Trim(), out x)) && int.TryParse(mouseY.Text.Trim(), out y)))
            {
                MessageBox.Show("please set integer");
                return;
            }

            Cursor.Position = new Point(x, y);
            System.Threading.Thread.Sleep(2000);
            Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RightMouseClick(x, y);
        }

        private void toolStripButton4_Click_1(object sender, EventArgs e)
        {
            this.TopMost = !this.TopMost;
        }

        private void tabPage2_Click(object sender, EventArgs e)
        {

        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }

        private void createObjectsFromTreeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(pegwindowNamePrefix.Text))
            {
                MessageBox.Show("Please set Pegwindow information first");
                return;
            }

            string strPegName = pegwindowNamePrefix.Text;
            if (this.selectedObjectsListView.Items.Count <= 0)
            {
                MessageBox.Show("Please set object to Item first");
                return;
            }
            List<MarsSpiedObjectInfo> lst = new List<MarsSpiedObjectInfo>();
            for (int i = 0; i < selectedObjectsListView.Items.Count; i++)
            {
                var itm = selectedObjectsListView.Items[i];
                if (itm == null) continue;
                //if (itm == null) continue;
                if (((System.Windows.Forms.ListViewItem)itm).Tag == null) continue;
                if (!(itm.Tag is MarsSpiedObjectInfo)) continue;
                ((MarsSpiedObjectInfo)itm.Tag).PegName = strPegName;
                lst.Add(itm.Tag as MarsSpiedObjectInfo);
            }
            if (lst.Count == 0)
            {
                MessageBox.Show("Please set object to Item first, no available spied object info");
                return;
            }
            /// save tofile 
            /// 
            System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();
            string jsonstr = js.Serialize(lst);
            string strFileName = "c:\\temp\\marsSpyObjects.json";
            if (System.IO.File.Exists(strFileName)) System.IO.File.Delete(strFileName);

            System.IO.File.WriteAllText("c:\\temp\\marsSpyObjects.json", jsonstr);

        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {

        }




        private void refreshObjectButton_Click_1(object sender, EventArgs e)
        {
            int iMark = new Random().Next();
            simpleLog.MarsLoggerSimple.logBegin($"{iMark}|refreshObjectButton_Click_1");
            try
            {   //var lstwindows = ProcessAllControlsEnumerator.GetSpyWindowTree(curP.Id);
                //LoadObjectsToTreeView(lstwindows, this.winformObjectTreeview);
                // 检查是否为MFC应用程序
                if (ProcessAllControlsEnumerator.IsMfcApplication())
                {
                    // 如果是MFC应用程序，使用ProcessAllControlsEnumerator的方法
                    var allControls = ProcessAllControlsEnumerator.GetControlsByTopWindowClass();
                    if (allControls != null && allControls.Count > 0)
                    {
                        // 分类控件到不同的树视图
                        var winformObjects = new List<MarsSpiedObjectInfo>();
                        var standardObjects = new List<MarsSpiedObjectInfo>();

                        foreach (var obj in allControls)
                        {
                            if (obj == null) continue;

                            // 根据对象类型分类
                            if (IsWinFormObject(obj))
                            {
                                winformObjects.Add(obj);
                            }
                            else
                            {
                                standardObjects.Add(obj);
                            }
                        }

                        // 加载到相应的树视图
                        if (winformObjects.Count > 0)
                        {
                            LoadObjectsToTreeView(winformObjects, this.winformObjectTreeview);
                        }

                        if (standardObjects.Count > 0)
                        {
                            LoadStandardObjectsToTreeView(standardObjects);
                        }
                    }
                    else
                    {
                        MessageBox.Show("No objects found in the MFC application");
                    }
                }
                else
                {
                    // 如果不是MFC应用程序，使用原有的检测逻辑
                    bool hasWinForms = System.Windows.Forms.Application.OpenForms.Count > 0;
                    bool hasWpf = false;
                    if (System.Windows.Application.Current != null)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            hasWpf = System.Windows.Application.Current != null && System.Windows.Application.Current.Windows.Count > 0;
                        });
                    }
                    bool hasStandardControls = StandardWindowsEnumerator.GetStandardChildWindows(MarsWindowsAPIs.GetForegroundWindow()).Any();

                    if (hasWinForms)
                    {
                        var lstOfObjs = MarsWinformSpy.getCurrentAllObjects();
                        if (lstOfObjs == null)
                        {
                            MessageBox.Show("Can't load all object");
                            return;
                        }
                        this.reloadObjects(lstOfObjs);
                    }
                    if (hasWpf)
                    {
                        //var lstOfWpfObjs = MarsWpfSpy.getCurrentAllObjects();
                        //if (lstOfWpfObjs == null)
                        //{
                        //    MessageBox.Show("Can't load all WPF object");
                        //    return;
                        //}
                        //this.reloadWpfObjects(lstOfWpfObjs);
                    }
                    if (hasStandardControls)
                    {
                        try
                        {
                            var foregroundWindow = MarsWindowsAPIs.GetForegroundWindow();
                            var standardObjects = StandardWindowsEnumerator.BuildStandardObjectsTree(foregroundWindow);
                            if (standardObjects != null && standardObjects.Count > 0)
                            {
                                LoadStandardObjectsToTreeView(standardObjects);
                            }
                        }
                        catch (Exception ex)
                        {
                            simpleLog.MarsLoggerSimple.Error("refreshObjectButton_Click_1",
                                $"Error loading standard controls: {ex.Message}", ex);
                        }
                    }
                }

            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd($"{iMark}|refreshObjectButton_Click_1");
            }
        }

        /// <summary>
        /// 检测是否为WinForm对象
        /// </summary>
        /// <param name="obj">要检测的对象</param>
        /// <returns>如果是WinForm对象返回true</returns>
        private bool IsWinFormObject(MarsSpiedObjectInfo obj)
        {
            if (obj == null || string.IsNullOrEmpty(obj.objectType)) return false;

            // 检查类型路径中是否包含WinForm相关命名空间
            string typePath = obj.objectTypePath ?? obj.objectType;
            return typePath.Contains("System.Windows.Forms") ||
                   typePath.Contains("WindowsForms10.") ||
                   (obj.referenceToObj != null && obj.referenceToObj is System.Windows.Forms.Control);
        }

        /// <summary>
        /// 将对象列表加载到指定的树视图中
        /// </summary>
        /// <param name="objects">要加载的对象列表</param>
        /// <param name="treeView">目标树视图</param>
        private void LoadObjectsToTreeView(List<MarsSpiedObjectInfo> objects, TreeView treeView)
        {
            if (objects == null || objects.Count == 0 || treeView == null) return;
            allObjectsFromTargetProcess.Clear();
            try
            {
                IntPtr targetHwnd = IntPtr.Zero;
                int tmpHwnd;
                if ((!string.IsNullOrEmpty(this.targetControlWndId)) &&
                    (int.TryParse(this.targetControlWndId, out tmpHwnd)))
                {
                    targetHwnd = new IntPtr(tmpHwnd);
                }

                foreach (var obj in objects)
                {
                    if (obj == null) continue;

                    TreeNode objNode = CreateNodeFromObjInfo(obj, targetUserControlId: targetHwnd);
                    if (objNode == null) continue;

                    treeView.Invoke(new Action(() =>
                    {
                        treeView.Nodes.Add(objNode);
                    }));
                }

                // 展开所有节点
                treeView.Invoke(new Action(() =>
                {
                    treeView.ExpandAll();
                }));
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("LoadObjectsToTreeView", $"Error loading objects to tree view: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 加载WPF对象到WPF树视图
        /// </summary>
        /// <param name="lstOfWpfObjs">WPF对象列表</param>
        private void reloadWpfObjects(List<MarsSpiedObjectInfo> lstOfWpfObjs)
        {
            try
            {
                this.winformObjectTreeview.Nodes.Clear();
                if (lstOfWpfObjs == null || lstOfWpfObjs.Count == 0) return;

                IntPtr targetHwnd = IntPtr.Zero;
                int tmpHwnd;
                if ((!string.IsNullOrEmpty(this.targetControlWndId)) &&
                    (int.TryParse(this.targetControlWndId, out tmpHwnd)))
                {
                    targetHwnd = new IntPtr(tmpHwnd);
                }

                foreach (var obj in lstOfWpfObjs)
                {
                    if (obj == null) continue;

                    TreeNode objNode = CreateNodeFromObjInfo(obj, targetUserControlId: targetHwnd);
                    if (objNode == null) continue;

                    winformObjectTreeview.Invoke(new Action(() =>
                    {
                        this.winformObjectTreeview.Nodes.Add(objNode);
                    }));
                }

                // 展开所有节点
                winformObjectTreeview.Invoke(new Action(() =>
                {
                    this.winformObjectTreeview.ExpandAll();
                }));
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("reloadWpfObjects", $"Error loading WPF objects: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 加载标准对象到标准对象树视图
        /// </summary>
        /// <param name="standardObjects">标准对象列表</param>
        private void LoadStandardObjectsToTreeView(List<MarsSpiedObjectInfo> standardObjects)
        {
            try
            {
                this.winformObjectTreeview.Nodes.Clear();
                if (standardObjects == null || standardObjects.Count == 0) return;

                IntPtr targetHwnd = IntPtr.Zero;
                int tmpHwnd;
                if ((!string.IsNullOrEmpty(this.targetControlWndId)) &&
                    (int.TryParse(this.targetControlWndId, out tmpHwnd)))
                {
                    targetHwnd = new IntPtr(tmpHwnd);
                }

                foreach (var obj in standardObjects)
                {
                    if (obj == null) continue;

                    TreeNode objNode = CreateNodeFromObjInfo(obj, targetUserControlId: targetHwnd);
                    if (objNode == null) continue;

                    winformObjectTreeview.Invoke(new Action(() =>
                    {
                        this.winformObjectTreeview.Nodes.Add(objNode);
                    }));
                }

                // 展开所有节点
                winformObjectTreeview.Invoke(new Action(() =>
                {
                    this.winformObjectTreeview.ExpandAll();
                }));
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("LoadStandardObjectsToTreeView",
                    $"Error loading standard objects: {ex.Message}", ex);
            }
        }

        private void createTestcaseFromHereToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void dataGridView1_CellContentClick_1(object sender, DataGridViewCellEventArgs e)
        {

        }

        /// <summary>
        /// 加载标准Windows控件到winformObjectTreeview
        /// </summary>
        /// <param name="parentHandle">父窗口句柄，如果为IntPtr.Zero则使用当前活动窗口</param>
        public void LoadStandardObjects(IntPtr parentHandle = default(IntPtr))
        {
            try
            {
                // 如果没有指定父窗口句柄，尝试获取当前活动窗口
                if (parentHandle == IntPtr.Zero)
                {
                    parentHandle = MarsWindowsAPIs.GetForegroundWindow();
                }

                if (parentHandle == IntPtr.Zero)
                {
                    MessageBox.Show("无法获取目标窗口句柄", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 清空现有的标准对象树
                this.winformObjectTreeview.Nodes.Clear();

                // 获取标准对象树
                var standardObjects = StandardWindowsEnumerator.BuildStandardObjectsTree(parentHandle);

                if (standardObjects == null || standardObjects.Count == 0)
                {
                    MessageBox.Show("未找到标准Windows控件", "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 将标准对象添加到树视图中
                foreach (var standardObj in standardObjects)
                {
                    var treeNode = CreateStandardObjectTreeNode(standardObj);
                    if (treeNode != null)
                    {
                        this.winformObjectTreeview.Nodes.Add(treeNode);
                    }
                }

                // 展开所有节点
                this.winformObjectTreeview.ExpandAll();
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("LoadStandardObjects", $"加载标准对象时发生错误: {ex.Message}", ex);
                MessageBox.Show($"加载标准对象时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 从MarsSpiedObjectInfo创建TreeNode
        /// </summary>
        /// <param name="marsInfo">MarsSpiedObjectInfo对象</param>
        /// <returns>TreeNode对象</returns>
        private TreeNode CreateStandardObjectTreeNode(MarsSpiedObjectInfo marsInfo)
        {
            if (marsInfo == null)
                return null;

            // 构建显示文本：显示格式为 (子节点数)-[类型]:对象名称
            var typeLabel = GetTypeLabel(marsInfo.objectType);
            var objectName = !string.IsNullOrEmpty(marsInfo.objectName) ? marsInfo.objectName : "N/A";
            var displayText = $"({marsInfo.allChildrenCount})-[{typeLabel}]:{objectName}";

            TreeNode node = new TreeNode(displayText);
            node.Tag = marsInfo;

            // 设置节点样式
            if (!marsInfo.isVisible)
            {
                node.ForeColor = System.Drawing.Color.Red;
                node.NodeFont = new Font(winformObjectTreeview.Font, FontStyle.Italic);
            }

            // 递归添加子节点
            if (marsInfo.children != null && marsInfo.children.Count > 0)
            {
                foreach (var child in marsInfo.children)
                {
                    var childNode = CreateStandardObjectTreeNode(child);
                    if (childNode != null)
                    {
                        node.Nodes.Add(childNode);
                    }
                }
            }

            return node;
        }


        /// <summary>
        /// 刷新标准对象树
        /// </summary>
        public void RefreshStandardObjects()
        {
            LoadStandardObjects();
        }

        /// <summary>
        /// 从指定窗口句柄加载标准对象
        /// </summary>
        /// <param name="windowHandle">窗口句柄</param>
        public void LoadStandardObjectsFromHandle(IntPtr windowHandle)
        {
            LoadStandardObjects(windowHandle);
        }

        /// <summary>
        /// 从当前选中的.NET对象对应的窗口加载标准对象
        /// </summary>
        public void LoadStandardObjectsFromSelectedObject()
        {
            if (winformObjectTreeview.SelectedNode?.Tag is MarsSpiedObjectInfo selectedObj)
            {
                if (selectedObj.referenceToObj is System.Windows.Forms.Control control)
                {
                    LoadStandardObjects(control.Handle);
                }
                else if (selectedObj.hwnd != 0)
                {
                    LoadStandardObjects(new IntPtr(selectedObj.hwnd));
                }
                else
                {
                    MessageBox.Show("选中的对象没有有效的窗口句柄", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("请先选择一个.NET对象", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// 测试标准对象加载功能
        /// </summary>
        public void TestStandardObjectsLoading()
        {
            try
            {
                // 获取当前活动窗口
                IntPtr foregroundWindow = MarsWindowsAPIs.GetForegroundWindow();

                if (foregroundWindow == IntPtr.Zero)
                {
                    MessageBox.Show("无法获取当前活动窗口", "测试", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 测试获取标准子窗口
                var standardWindows = StandardWindowsEnumerator.GetStandardChildWindows(foregroundWindow);

                MessageBox.Show($"找到 {standardWindows.Count} 个标准Windows控件", "测试结果", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // 测试构建树状结构
                var marsObjects = StandardWindowsEnumerator.BuildStandardObjectsTree(foregroundWindow);

                MessageBox.Show($"构建了 {marsObjects.Count} 个顶级Mars对象", "测试结果", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // 加载到树视图
                LoadStandardObjects(foregroundWindow);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"测试失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 使用示例：演示如何加载标准Windows控件到winformObjectTreeview
        /// </summary>
        public void ExampleUsage()
        {
            // 示例1：加载当前活动窗口的标准控件
            LoadStandardObjects();

            // 示例2：从指定窗口句柄加载标准控件
            IntPtr windowHandle = MarsWindowsAPIs.GetForegroundWindow();
            LoadStandardObjectsFromHandle(windowHandle);

            // 示例3：从选中的.NET对象加载标准控件
            LoadStandardObjectsFromSelectedObject();

            // 示例4：刷新标准对象树
            RefreshStandardObjects();

            // 示例5：测试功能
            TestStandardObjectsLoading();
        }

        /// <summary>
        /// 显示standard object对象内容的使用示例
        /// </summary>
        public void ExampleShowStandardObjectContent()
        {
            // 示例1：设置事件处理（在初始化时调用）
            SetupStandardObjectsTreeViewEvents();

            // 示例2：加载标准对象
            LoadStandardObjects();

            // 示例3：显示选中的对象详情（双击树节点时自动调用）
            // ShowStandardObjectDetails();

            // 示例4：在DataGridView中显示对象信息（单击树节点时自动调用）
            // ShowStandardObjectInDataGrid();

            // 示例5：显示所有对象的摘要信息
            ShowAllStandardObjectsSummary();

            // 示例6：显示指定对象的详细信息
            if (winformObjectTreeview.Nodes.Count > 0)
            {
                var firstNode = winformObjectTreeview.Nodes[0];
                if (firstNode.Tag is MarsSpiedObjectInfo marsInfo)
                {
                    ShowStandardObjectDetails(marsInfo);
                }
            }
        }

        /// <summary>
        /// 示例：验证class、type、text和controlMarsType功能
        /// </summary>
        public void ExampleVerifyClassTypeTextControlMarsType()
        {
            try
            {
                // 1. 验证class、type、text获取功能
                VerifyClassTypeTextFunction();

                // 2. 验证control ID获取功能
                VerifyControlIdFunction();

                // 3. 显示所有标准对象的详细信息
                ShowAllStandardObjectsSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"示例执行失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 验证allChildrenCount属性是否正常工作
        /// </summary>
        public void VerifyAllChildrenCountProperty()
        {
            try
            {
                // 创建一个测试对象
                var testObj = new MarsSpiedObjectInfo
                {
                    objectName = "TestObject",
                    objectType = "TestType",
                    allChildrenCount = 5
                };

                // 验证属性可以正常访问
                MessageBox.Show($"测试对象 allChildrenCount: {testObj.allChildrenCount}", "验证结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"验证失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 验证control ID获取功能
        /// </summary>
        public void VerifyControlIdFunction()
        {
            try
            {
                // 获取当前活动窗口
                IntPtr foregroundWindow = MarsWindowsAPIs.GetForegroundWindow();

                if (foregroundWindow == IntPtr.Zero)
                {
                    MessageBox.Show("无法获取当前活动窗口", "测试", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 测试GetDlgCtrlID API
                int controlId = MarsWindowsAPIs.GetDlgCtrlID(foregroundWindow);
                MessageBox.Show($"当前窗口 Control ID: {controlId}", "API测试", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // 测试GetParent API
                IntPtr parentWindow = MarsWindowsAPIs.GetParent(foregroundWindow);
                MessageBox.Show($"当前窗口 Parent: 0x{parentWindow:X8}", "API测试", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // 加载标准对象并显示control ID信息
                LoadStandardObjects(foregroundWindow);

                if (winformObjectTreeview.Nodes.Count > 0)
                {
                    var firstNode = winformObjectTreeview.Nodes[0];
                    if (firstNode.Tag is MarsSpiedObjectInfo marsInfo)
                    {
                        MessageBox.Show($"第一个标准对象 Control ID: {marsInfo.controlId}", "标准对象测试", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"验证Control ID功能失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 验证class、type、text获取和controlMarsType设置功能
        /// </summary>
        public void VerifyClassTypeTextFunction()
        {
            try
            {
                // 获取当前活动窗口
                IntPtr foregroundWindow = MarsWindowsAPIs.GetForegroundWindow();

                if (foregroundWindow == IntPtr.Zero)
                {
                    MessageBox.Show("无法获取当前活动窗口", "测试", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 测试Windows API获取窗口信息
                var className = new StringBuilder(256);
                MarsWindowsAPIs.GetClassName(foregroundWindow, className, 255);
                string windowClass = className.ToString();

                var windowText = new StringBuilder(256);
                MarsWindowsAPIs.GetWindowText(foregroundWindow, windowText, 255);
                string text = windowText.ToString();

                MessageBox.Show($"当前窗口信息:\nClass: {windowClass}\nText: {text}", "API测试", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // 加载标准对象并显示详细信息
                LoadStandardObjects(foregroundWindow);

                if (winformObjectTreeview.Nodes.Count > 0)
                {
                    var details = new StringBuilder();
                    details.AppendLine("=== Standard Objects Class/Type/Text 信息 ===");

                    int count = 0;
                    foreach (TreeNode node in winformObjectTreeview.Nodes)
                    {
                        if (node.Tag is MarsSpiedObjectInfo marsInfo)
                        {
                            count++;
                            details.AppendLine($"\n--- 对象 {count} ---");
                            details.AppendLine($"Class: {marsInfo.objectType ?? "N/A"}");
                            details.AppendLine($"Type: {marsInfo.controlMarsType ?? "N/A"}");
                            details.AppendLine($"Text: {marsInfo.Text ?? "N/A"}");
                            details.AppendLine($"Name: {marsInfo.objectName ?? "N/A"}");
                            details.AppendLine($"Control ID: {marsInfo.controlId}");

                            if (count >= 5) // 只显示前5个对象
                                break;
                        }
                    }

                    details.AppendLine($"\n总共找到 {winformObjectTreeview.Nodes.Count} 个标准对象");

                    ShowDetailsWindow("Standard Objects 详细信息", details.ToString());
                }
                else
                {
                    MessageBox.Show("未找到标准对象", "测试结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"验证Class/Type/Text功能失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 显示选中的standard object对象的详细信息
        /// </summary>
        public void ShowStandardObjectDetails()
        {
            if (winformObjectTreeview.SelectedNode?.Tag is MarsSpiedObjectInfo selectedObj)
            {
                ShowStandardObjectDetails(selectedObj);
            }
            else
            {
                MessageBox.Show("请先选择一个standard object", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// 显示指定standard object对象的详细信息
        /// </summary>
        /// <param name="marsInfo">MarsSpiedObjectInfo对象</param>
        public void ShowStandardObjectDetails(MarsSpiedObjectInfo marsInfo)
        {
            if (marsInfo == null)
            {
                MessageBox.Show("对象信息为空", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // 构建详细信息字符串
                var details = new StringBuilder();
                details.AppendLine("=== Standard Object Details ===");
                details.AppendLine($"Object Name: {marsInfo.objectName ?? "N/A"}");
                details.AppendLine($"Window Class: {marsInfo.objectType ?? "N/A"}");
                details.AppendLine($"Mars Control Type: {marsInfo.controlMarsType ?? "N/A"}");
                details.AppendLine($"Text: {marsInfo.Text ?? "N/A"}");
                string controlId = marsInfo.controlId != -1 ? marsInfo.controlId.ToString() : GetControlIdFromMarsInfo(marsInfo);
                details.AppendLine($"Control ID: {controlId}");
                details.AppendLine($"Handle: 0x{marsInfo.hwnd:X8}");
                details.AppendLine($"Position: ({marsInfo.x}, {marsInfo.y})");
                details.AppendLine($"Size: {marsInfo.w} x {marsInfo.h}");
                details.AppendLine($"Visible: {marsInfo.isVisible}");
                details.AppendLine($"Enabled: {marsInfo.isEnabled}");
                details.AppendLine($"All Children Count: {marsInfo.allChildrenCount}");
                details.AppendLine($"Children Count: {marsInfo.children?.Count ?? 0}");
                details.AppendLine($"Object UUID: {marsInfo.obj_uuid}");

                if (!string.IsNullOrEmpty(marsInfo.PegWindUUID))
                {
                    details.AppendLine($"Parent Window UUID: {marsInfo.PegWindUUID}");
                }

                // 显示子对象信息
                if (marsInfo.children != null && marsInfo.children.Count > 0)
                {
                    details.AppendLine("\n=== Children Objects ===");
                    for (int i = 0; i < marsInfo.children.Count; i++)
                    {
                        var child = marsInfo.children[i];
                        details.AppendLine($"[{i}] {child.objectName ?? "N/A"} ({child.objectType ?? "N/A"})");
                    }
                }

                // 显示详细信息窗口
                ShowDetailsWindow("Standard Object Details", details.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"显示对象详情时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 从MarsSpiedObjectInfo获取控件ID
        /// </summary>
        /// <param name="marsInfo">MarsSpiedObjectInfo对象</param>
        /// <returns>控件ID字符串</returns>
        private string GetControlIdFromMarsInfo(MarsSpiedObjectInfo marsInfo)
        {
            try
            {
                // 首先尝试使用已存储的controlId
                if (marsInfo.controlId != -1)
                {
                    return marsInfo.controlId.ToString();
                }

                // 如果controlId未设置，尝试通过Windows API获取
                if (marsInfo.hwnd != 0)
                {
                    IntPtr handle = new IntPtr(marsInfo.hwnd);
                    int controlId = MarsWindowsAPIs.GetDlgCtrlID(handle);
                    if (controlId != 0)
                    {
                        // 更新marsInfo中的controlId
                        marsInfo.controlId = controlId;
                        return controlId.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("GetControlIdFromMarsInfo", $"Error getting control ID: {ex.Message}");
            }
            return "N/A";
        }

        /// <summary>
        /// 显示详细信息窗口
        /// </summary>
        /// <param name="title">窗口标题</param>
        /// <param name="content">内容</param>
        private void ShowDetailsWindow(string title, string content)
        {
            try
            {
                // 创建详细信息窗口
                var detailsForm = new Form
                {
                    Text = title,
                    Size = new Size(600, 500),
                    StartPosition = FormStartPosition.CenterParent,
                    ShowInTaskbar = false,
                    TopMost = true
                };

                // 创建文本框
                var textBox = new TextBox
                {
                    Multiline = true,
                    ScrollBars = ScrollBars.Both,
                    Dock = DockStyle.Fill,
                    Text = content,
                    ReadOnly = true,
                    Font = new Font("Consolas", 9),
                    BackColor = Color.White
                };

                // 创建按钮面板
                var buttonPanel = new Panel
                {
                    Height = 40,
                    Dock = DockStyle.Bottom
                };

                var closeButton = new Button
                {
                    Text = "关闭",
                    Size = new Size(80, 30),
                    Location = new Point(detailsForm.Width - 100, 5),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                closeButton.Click += (s, e) => detailsForm.Close();

                var copyButton = new Button
                {
                    Text = "复制",
                    Size = new Size(80, 30),
                    Location = new Point(detailsForm.Width - 190, 5),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                copyButton.Click += (s, e) =>
                {
                    Clipboard.SetText(content);
                    MessageBox.Show("内容已复制到剪贴板", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };

                buttonPanel.Controls.Add(closeButton);
                buttonPanel.Controls.Add(copyButton);
                detailsForm.Controls.Add(textBox);
                detailsForm.Controls.Add(buttonPanel);

                // 显示窗口
                detailsForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建详细信息窗口时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 在DataGridView中显示standard object的详细信息
        /// </summary>
        public void ShowStandardObjectInDataGrid()
        {
            if (winformObjectTreeview.SelectedNode?.Tag is MarsSpiedObjectInfo selectedObj)
            {
                ShowStandardObjectInDataGrid(selectedObj);
            }
            else
            {
                MessageBox.Show("请先选择一个standard object", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// 在DataGridView中显示指定standard object的详细信息
        /// </summary>
        /// <param name="marsInfo">MarsSpiedObjectInfo对象</param>
        public void ShowStandardObjectInDataGrid(MarsSpiedObjectInfo marsInfo)
        {
            if (marsInfo == null)
            {
                MessageBox.Show("对象信息为空", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // 清空现有的数据
                this.dataGridView1.Rows.Clear();

                // 添加基本信息
                AddPropertyToGrid("Object Name", marsInfo.objectName ?? "N/A");
                AddPropertyToGrid("Window Class", marsInfo.objectType ?? "N/A");
                AddPropertyToGrid("Mars Control Type", marsInfo.controlMarsType ?? "N/A");
                AddPropertyToGrid("Text", marsInfo.Text ?? "N/A");
                AddPropertyToGrid("Control ID", marsInfo.controlId != -1 ? marsInfo.controlId.ToString() : GetControlIdFromMarsInfo(marsInfo));
                AddPropertyToGrid("Handle", $"0x{marsInfo.hwnd:X8}");
                AddPropertyToGrid("Position", $"({marsInfo.x}, {marsInfo.y})");
                AddPropertyToGrid("Size", $"{marsInfo.w} x {marsInfo.h}");
                AddPropertyToGrid("Visible", marsInfo.isVisible.ToString());
                AddPropertyToGrid("Enabled", marsInfo.isEnabled.ToString());
                AddPropertyToGrid("All Children Count", marsInfo.allChildrenCount.ToString());
                AddPropertyToGrid("Children Count", (marsInfo.children?.Count ?? 0).ToString());
                AddPropertyToGrid("Object UUID", marsInfo.obj_uuid ?? "N/A");

                if (!string.IsNullOrEmpty(marsInfo.PegWindUUID))
                {
                    AddPropertyToGrid("Parent Window UUID", marsInfo.PegWindUUID);
                }

                // 显示子对象信息
                if (marsInfo.children != null && marsInfo.children.Count > 0)
                {
                    AddPropertyToGrid("", "=== Children Objects ===");
                    for (int i = 0; i < marsInfo.children.Count; i++)
                    {
                        var child = marsInfo.children[i];
                        AddPropertyToGrid($"Child [{i}]", $"{child.objectName ?? "N/A"} ({child.objectType ?? "N/A"})");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"显示对象详情时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 向DataGridView添加属性行
        /// </summary>
        /// <param name="propertyName">属性名</param>
        /// <param name="propertyValue">属性值</param>
        private void AddPropertyToGrid(string propertyName, string propertyValue)
        {
            int rowIndex = this.dataGridView1.Rows.Add();
            this.dataGridView1.Rows[rowIndex].Cells[0].Value = propertyName;
            this.dataGridView1.Rows[rowIndex].Cells[1].Value = propertyValue;
        }

        /// <summary>
        /// 为winformObjectTreeview添加双击事件处理
        /// </summary>
        public void SetupStandardObjectsTreeViewEvents()
        {
            if (winformObjectTreeview != null)
            {
                winformObjectTreeview.DoubleClick += StandardObjectsTreeView_DoubleClick;
                winformObjectTreeview.Click += StandardObjectsTreeView_Click;
            }
        }

        /// <summary>
        /// winformObjectTreeview双击事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void StandardObjectsTreeView_DoubleClick(object sender, EventArgs e)
        {
            ShowStandardObjectDetails();
        }

        /// <summary>
        /// winformObjectTreeview单击事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void StandardObjectsTreeView_Click(object sender, EventArgs e)
        {
            ShowStandardObjectInDataGrid();
        }

        /// <summary>
        /// 显示所有standard objects的摘要信息
        /// </summary>
        public void ShowAllStandardObjectsSummary()
        {
            try
            {
                var summary = new StringBuilder();
                summary.AppendLine("=== All Standard Objects Summary ===");

                int totalObjects = 0;
                int totalChildren = 0;
                var objectTypes = new Dictionary<string, int>();

                // 遍历所有节点
                foreach (TreeNode node in winformObjectTreeview.Nodes)
                {
                    if (node.Tag is MarsSpiedObjectInfo marsInfo)
                    {
                        totalObjects++;
                        totalChildren += marsInfo.allChildrenCount;

                        string type = marsInfo.objectType ?? "Unknown";
                        if (objectTypes.ContainsKey(type))
                            objectTypes[type]++;
                        else
                            objectTypes[type] = 1;

                        // 递归统计子节点
                        CountChildNodes(node, ref totalObjects, ref totalChildren, ref objectTypes);
                    }
                }

                summary.AppendLine($"Total Objects: {totalObjects}");
                summary.AppendLine($"Total Children: {totalChildren}");
                summary.AppendLine($"Root Objects: {winformObjectTreeview.Nodes.Count}");
                summary.AppendLine("\n=== Object Types ===");

                foreach (var kvp in objectTypes.OrderByDescending(x => x.Value))
                {
                    summary.AppendLine($"{kvp.Key}: {kvp.Value}");
                }

                ShowDetailsWindow("Standard Objects Summary", summary.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"显示摘要信息时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 递归统计子节点
        /// </summary>
        /// <param name="node">树节点</param>
        /// <param name="totalObjects">总对象数</param>
        /// <param name="totalChildren">总子节点数</param>
        /// <param name="objectTypes">对象类型统计</param>
        private void CountChildNodes(TreeNode node, ref int totalObjects, ref int totalChildren, ref Dictionary<string, int> objectTypes)
        {
            foreach (TreeNode childNode in node.Nodes)
            {
                if (childNode.Tag is MarsSpiedObjectInfo marsInfo)
                {
                    totalObjects++;
                    totalChildren += marsInfo.allChildrenCount;

                    string type = marsInfo.objectType ?? "Unknown";
                    if (objectTypes.ContainsKey(type))
                        objectTypes[type]++;
                    else
                        objectTypes[type] = 1;

                    // 递归处理子节点
                    CountChildNodes(childNode, ref totalObjects, ref totalChildren, ref objectTypes);
                }
            }
        }

        private void toolStripButton5_Click_1(object sender, EventArgs e)
        {
            this.winformObjectTreeview.Nodes.Clear();
            //this.winformObjectTreeview.Nodes.Clear();
            //this.winformObjectTreeview.Nodes.Clear();

            /// 获得窗口树
            /// 
            var curP = Process.GetCurrentProcess();

            /// 获得list模式
            /// 
            var lstControls = ProcessAllControlsEnumerator.GetSpyWindowControlList(curP.Id);
            /// 过滤 高度为0的控件
            /// 
            simpleLog.MarsLoggerSimple.Info("toolStripButton5_Click_1", $"Before Filter Count={lstControls.Count}");
            lstControls = lstControls.Where(c => c.h > 0 && c.w > 0).ToList();
            simpleLog.MarsLoggerSimple.Info("toolStripButton5_Click_1", $"After Filter Count={lstControls.Count}");
            var treeStyleObjects = ProcessAllControlsEnumerator.BuildTreeFromListByParentHwnd(lstControls);
            LoadObjectsToTreeView(treeStyleObjects, this.winformObjectTreeview);

            /// 增加MSAA的对象处理
            /// 

        }

        /// <summary>
        /// 加载标准对象按钮点击事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void loadStandardObjectsButton_Click(object sender, EventArgs e)
        {
            try
            {
                // 切换到标准对象标签页
                this.objectTabcontrols.SelectedTab = this.tabpageStandardsObj;

                // 加载标准对象
                LoadStandardObjects();
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("loadStandardObjectsButton_Click", $"加载标准对象时发生错误: {ex.Message}", ex);
                MessageBox.Show($"加载标准对象时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 从选中对象加载标准对象的右键菜单事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void loadStandardObjectsFromSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // 切换到标准对象标签页
                this.objectTabcontrols.SelectedTab = this.tabpageStandardsObj;

                // 从选中的.NET对象加载标准对象
                LoadStandardObjectsFromSelectedObject();
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("loadStandardObjectsFromSelectedToolStripMenuItem_Click", $"从选中对象加载标准对象时发生错误: {ex.Message}", ex);
                MessageBox.Show($"从选中对象加载标准对象时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void genObjectsPropertisBtn_Click(object sender, EventArgs e)
        {
            try
            {
                // 保留现有代码 - 检查是否有选中的节点
                if (winformObjectTreeview.SelectedNode?.Tag == null)
                {
                    MessageBox.Show(
                        GetLocalizedString("NoNodeSelected", "Please select a node first"),
                        GetLocalizedString("Information", "Information"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                var selectedInfo = winformObjectTreeview.SelectedNode.Tag as MarsSpiedObjectInfo;
                if (selectedInfo == null) return;

                // 检查对象类型是否为 UIA 或 IAccessible
                bool isUIAObject = selectedInfo.referenceToObj is System.Windows.Automation.AutomationElement;
                bool isIAccessibleObject = selectedInfo.referenceToObj is Accessibility.IAccessible;

                if (!isUIAObject && !isIAccessibleObject)
                {
                    MessageBox.Show(
                        GetLocalizedString("UnsupportedObjectType", "Only UIA and IAccessible objects are supported for this operation"),
                        GetLocalizedString("Information", "Information"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                // 生成 MARS Object Name 和 Object IDs
                string marsObjectName = GenerateMarsObjectName(selectedInfo);
                string objectIds = GenerateObjectIds(selectedInfo, isUIAObject, isIAccessibleObject);

                // 添加到 selectedObjectsListView
                AddObjectToSelectedList(selectedInfo, marsObjectName, objectIds);

                MessageBox.Show(
                    GetLocalizedString("ObjectAddedSuccessfully", "Object added to pool successfully"),
                    GetLocalizedString("Success", "Success"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("genObjectsPropertisBtn_Click", $"Error generating object properties: {ex.Message}", ex);
                MessageBox.Show(
                    GetLocalizedString("ErrorGeneratingProperties", $"Error generating object properties: {ex.Message}"),
                    GetLocalizedString("Error", "Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 生成 MARS Object Name
        /// </summary>
        private string GenerateMarsObjectName(MarsSpiedObjectInfo info)
        {
            try
            {
                // 使用对象名称作为默认的 MARS Object Name
                string baseName = !string.IsNullOrEmpty(info.objectName) ? info.objectName : info.objectType;

                // 清理名称，移除特殊字符
                baseName = System.Text.RegularExpressions.Regex.Replace(baseName, @"[^\w\s]", "");
                baseName = baseName.Trim();

                // 如果名称为空，使用默认名称
                if (string.IsNullOrEmpty(baseName))
                {
                    baseName = "MarsObject";
                }

                return baseName;
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("GenerateMarsObjectName", $"Error generating MARS object name: {ex.Message}", ex);
                return "MarsObject";
            }
        }

        /// <summary>
        /// 生成 Object IDs
        /// </summary>
        private string GenerateObjectIds(MarsSpiedObjectInfo info, bool isUIAObject, bool isIAccessibleObject)
        {
            try
            {
                var objectIds = new List<string>();

                // Catalog
                if (isUIAObject && isIAccessibleObject)
                {
                    objectIds.Add("Catalog:=uia+IAcc");
                }
                else if (isUIAObject)
                {
                    objectIds.Add("Catalog:=uia");
                }
                else if (isIAccessibleObject)
                {
                    objectIds.Add("Catalog:=IAcc");
                }

                // marsMixObjectNamePath
                if (!string.IsNullOrEmpty(info.marsNamePath))
                {
                    objectIds.Add($"marsMixObjectNamePath:={info.marsNamePath}");
                }

                // attachText
                if (isIAccessibleObject && info.referenceToObj is Accessibility.IAccessible accObj)
                {
                    try
                    {
                        string accName = accObj.get_accName(0);
                        if (!string.IsNullOrEmpty(accName))
                        {
                            objectIds.Add($"attachText:={accName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        simpleLog.MarsLoggerSimple.Error("GenerateObjectIds", $"Error getting accName: {ex.Message}", ex);
                    }
                }

                // roleName (for IAccessible objects)
                if (isIAccessibleObject && info.referenceToObj is Accessibility.IAccessible accObj2)
                {
                    try
                    {
                        object roleObj = accObj2.get_accRole(0);
                        int role = roleObj is int i ? i : Convert.ToInt32(roleObj);
                        string roleName = MARSAccessibleProvider.GetRoleName(role);
                        if (!string.IsNullOrEmpty(roleName))
                        {
                            objectIds.Add($"roleName:={roleName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        simpleLog.MarsLoggerSimple.Error("GenerateObjectIds", $"Error getting role name: {ex.Message}", ex);
                    }
                }

                return string.Join("; ", objectIds);
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("GenerateObjectIds", $"Error generating object IDs: {ex.Message}", ex);
                return "Error generating IDs";
            }
        }

        /// <summary>
        /// 添加对象到选中列表
        /// </summary>
        private void AddObjectToSelectedList(MarsSpiedObjectInfo info, string marsObjectName, string objectIds)
        {
            try
            {
                // 检查是否已存在相同的对象
                for (int i = 0; i < selectedObjectsListView.Items.Count; i++)
                {
                    var existingItem = selectedObjectsListView.Items[i];
                    if (existingItem.Tag == info)
                    {
                        // 更新现有项目
                        existingItem.SubItems[0].Text = info.getDisplayId();
                        existingItem.SubItems[1].Text = marsObjectName;
                        existingItem.SubItems[2].Text = objectIds;
                        return;
                    }
                }

                // 添加新项目
                var newItem = selectedObjectsListView.Items.Add(info.getDisplayId());
                newItem.Tag = info;

                // 添加子项目
                newItem.SubItems.Add(marsObjectName);
                newItem.SubItems.Add(objectIds);

                simpleLog.MarsLoggerSimple.Info("AddObjectToSelectedList", $"Added object: {marsObjectName}, IDs: {objectIds}");
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("AddObjectToSelectedList", $"Error adding object to list: {ex.Message}", ex);
            }
        }

        private void toolStripButtonGeneratePath_Click(object sender, EventArgs e)
        {
            try
            {
                // Check if there's a selected node
                if (winformObjectTreeview.SelectedNode?.Tag == null)
                {
                    MessageBox.Show(
                        GetLocalizedString("NoNodeSelected", "Please select a node first"),
                        GetLocalizedString("Information", "Information"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                var selectedInfo = winformObjectTreeview.SelectedNode.Tag as MarsSpiedObjectInfo;
                if (selectedInfo == null) return;

                // Generate Mars Name Path
                string marsNamePath = GenerateMarsNamePath(selectedInfo);
                string masrTypePath = GenerateMarsTypePath(selectedInfo);

                // Add to dataGridView1
                AddMarsNamePathToDataGrid(marsNamePath);
                AddMarsTypePathToDataGrid(masrTypePath);

                // Display the result
                MessageBox.Show(
                    GetLocalizedString("MarsNamePathGenerated", $"Mars Name Path: {marsNamePath}"),
                    GetLocalizedString("MarsNamePath", "Mars Name Path"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                // Copy to clipboard
                System.Windows.Forms.Clipboard.SetText(marsNamePath);
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("toolStripButtonGeneratePath_Click", $"Error generating Mars Name Path: {ex.Message}", ex);
                MessageBox.Show(
                    GetLocalizedString("ErrorGeneratingPath", $"Error generating Mars Name Path: {ex.Message}"),
                    GetLocalizedString("Error", "Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void toolStripButtonShowZeroSize_Click(object sender, EventArgs e)
        {
            try
            {
                // Toggle the setting
                bool newValue = !ShowZeroSizeObjects;
                SetShowZeroSizeObjects(newValue);

                // Update button appearance
                toolStripButtonShowZeroSize.Checked = newValue;
                toolStripButtonShowZeroSize.BackColor = newValue ? System.Drawing.Color.LightGreen : System.Drawing.SystemColors.Control;

                // Show status message
                string status = newValue ? "Enabled" : "Disabled";
                string message = GetLocalizedString("ZeroSizeToggle", $"Show Zero Size Objects: {status}");
                MessageBox.Show(message, GetLocalizedString("Settings", "Settings"), MessageBoxButtons.OK, MessageBoxIcon.Information);

                simpleLog.MarsLoggerSimple.Info("toolStripButtonShowZeroSize_Click", $"Zero size objects display: {status}");
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("toolStripButtonShowZeroSize_Click", $"Error toggling zero size objects setting: {ex.Message}", ex);
                MessageBox.Show(
                    GetLocalizedString("ErrorTogglingSetting", $"Error toggling setting: {ex.Message}"),
                    GetLocalizedString("Error", "Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void AddMarsTypePathToDataGrid(string marsTypePath)
        {
            try
            {
                // Insert a new row at position 1 (second row, 0-based index)
                int insertIndex = Math.Min(1, dataGridView1.Rows.Count);
                dataGridView1.Rows.Insert(insertIndex);
                DataGridViewRow newRow = dataGridView1.Rows[insertIndex];

                // Set the property name
                newRow.Cells["objectProperty"].Value = "MarsUITypePath";

                // Set the property value (the generated path)
                newRow.Cells["objPropValue"].Value = marsTypePath;

                // Set the property type
                newRow.Cells["objPropertyType"].Value = "String";

                // Make the row visible and select it
                newRow.Selected = true;
                dataGridView1.FirstDisplayedScrollingRowIndex = insertIndex;

                // Log the addition
                simpleLog.MarsLoggerSimple.Info("AddMarsNamePathToDataGrid", $"Added Mars Name Path to data grid at row {insertIndex}: {marsTypePath}");
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("AddMarsNamePathToDataGrid", $"Error adding Mars Name Path to data grid: {ex.Message}", ex);
            }
        }

        private void AddMarsNamePathToDataGrid(string marsNamePath)
        {
            try
            {
                // Insert a new row at position 1 (second row, 0-based index)
                int insertIndex = Math.Min(1, dataGridView1.Rows.Count);
                dataGridView1.Rows.Insert(insertIndex);
                DataGridViewRow newRow = dataGridView1.Rows[insertIndex];

                // Set the property name
                newRow.Cells["objectProperty"].Value = "Mars Name Path";

                // Set the property value (the generated path)
                newRow.Cells["objPropValue"].Value = marsNamePath;

                // Set the property type
                newRow.Cells["objPropertyType"].Value = "String";

                // Make the row visible and select it
                newRow.Selected = true;
                dataGridView1.FirstDisplayedScrollingRowIndex = insertIndex;

                // Log the addition
                simpleLog.MarsLoggerSimple.Info("AddMarsNamePathToDataGrid", $"Added Mars Name Path to data grid at row {insertIndex}: {marsNamePath}");
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("AddMarsNamePathToDataGrid", $"Error adding Mars Name Path to data grid: {ex.Message}", ex);
            }
        }

        private void toolStripButtonAddPool_Click_1(object sender, EventArgs e)
        {

        }
        ///
        /// 单节点模式。主要算法如下：
        /// 1，通过x,y获得IAccessible接口，如果没有，返回错误
        /// 2，通过IAccessible接口一直回溯parent IAccessible对象到顶层，
        /// 3，将树在Mars.Inter.MQCenter中的MarsObjSpyForm界面显示
        /// 
        public static void StartAccessibleModeFromXYXX(string strProcessName, int pid, IntPtr hwnd, int x, int y)
        {
            simpleLog.MarsLoggerSimple.logBegin("StartAccessibleModeFromXY", $"start from Process|{strProcessName}|{hwnd}|{x},{y}");
            try
            {
                IAccessible targetAcc = null;
                bool isOk = false;
                string strError = "";
                var objTree = MARSAccessibleHelper.GetInitAccessibleObjectTreeFromPosition(strProcessName, x, y, ref targetAcc, ref isOk, ref strError);
                simpleLog.MarsLoggerSimple.Info("StartAccessibleModeFromXY", $"{objTree?.Count}");

                if (!isOk || objTree == null || objTree.Count == 0)
                {
                    simpleLog.MarsLoggerSimple.Error("StartAccessibleModeFromXY", $"Failed to get accessible chain: {strError}");
                    return;
                }

                // 将 IAccessible 链转换为 MarsSpiedObjectInfo 的单链树（root->...->target）
                MarsSpiedObjectInfo rootInfo = ConvertAccessibleChainToMarsInfo(objTree);
                var roots = new List<MarsSpiedObjectInfo>();
                roots.Add(rootInfo);

                // 打开对象窥探窗体并加载树
                var frm = MarsObjSpyForm.getInstance(roots, enSpyMode.spyMode_net_winform_frameWork);
                MarsObjSpyForm.showModuleInThread();
            }
            finally
            {
                MarsLoggerSimple.logEnd("StartAccessibleModeFromXY");
            }

        }

        public static MarsSpiedObjectInfo GetUIAInfoFromHwnd(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
                return null;

            try
            {
                // 1. 获取 AutomationElement
                AutomationElement element = AutomationElement.FromHandle(hwnd);
                if (element == null)
                    return null;

                // 2. 递归构建 MarsSpiedObjectInfo
                MarsSpiedObjectInfo info = CreateMarsInfoFromAutomation(element);

                // 3. 递归获取所有子元素
                AddUIAChildrenRecursive(element, info);

                return info;
            }
            catch (Exception ex)
            {
                // 可根据需要记录日志
                return null;
            }
        }

        // 递归添加子元素
        private static void AddUIAChildrenRecursive(AutomationElement parentElement, MarsSpiedObjectInfo parentInfo)
        {
            try
            {
                var walker = TreeWalker.ControlViewWalker;
                var child = walker.GetFirstChild(parentElement);
                while (child != null)
                {
                    var childInfo = CreateMarsInfoFromAutomation(child);
                    if (childInfo != null)
                    {
                        if (parentInfo.children == null)
                            parentInfo.children = new System.Collections.Generic.List<MarsSpiedObjectInfo>();
                        parentInfo.children.Add(childInfo);

                        // 递归
                        AddUIAChildrenRecursive(child, childInfo);
                    }
                    child = walker.GetNextSibling(child);
                }
            }
            catch { }
        }

        /// <summary>
        /// 混合模式：使用UIA获得元素链，如果UIA失败，使用MSAA获得元素链，判断hwnd是否有afx
        /// </summary>
        /// <param name="strProcessName"></param>
        /// <param name="pid"></param>
        /// <param name="hwnd"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public static void StartAccessibleModeFromXY(string strProcessName, int pid, IntPtr hwnd, int x, int y)
        {
            int iMark = new Random().Next(10000);
            simpleLog.MarsLoggerSimple.logBegin("StartAccessibleModeFromXY", $"{iMark}|start from Process|{strProcessName}|{hwnd}|{x},{y}");
            try
            {
                // 使用 UIA 辅助：从屏幕坐标获取元素链
                List<AutomationElement> lstSiblings = new();
                List<(AutomationElement, IAccessible)> accessibleLst = new();
                AutomationElement targetUIObject = null;
                MarsLoggerSimple.Info("StartAccessibleModeFromXY", $"{iMark}|before invoke MarsMARSUIHelper.GetElementChainFromPoint");
                var uiaChain = Mars.Inter.MQCenter.MSAASupport.MarsMARSUIHelper.GetElementChainFromPoint(x, y,
                    lstSiblings, accessibleLst, ref targetUIObject);
                if (uiaChain == null || uiaChain.Count == 0)
                {
                    simpleLog.MarsLoggerSimple.Error("StartAccessibleModeFromXY", $"{iMark}|Failed to get UIA chain from point");
                    return;
                }

                List<(AutomationElement, MarsMSAABasicInfo)> lstAccessibleObjs = new();
                if (accessibleLst.Count > 0)
                {
                    simpleLog.MarsLoggerSimple.Info("StartAccessibleModeFromXY", $"going to get data from Accessible|count={accessibleLst.Count}");
                    foreach (var (ae, acc) in accessibleLst)
                    {
                        var msaaInfo = MarsMSAABasicInfo.FromAccessible(acc);
                        if (msaaInfo == null) continue;
                        lstAccessibleObjs.Add((ae, msaaInfo));
                        simpleLog.MarsLoggerSimple.Info("StartAccessibleModeFromXY", msaaInfo.ToString());
                    }
                }

                // 转换为 MarsSpiedObjectInfo 单链树
                MarsSpiedObjectInfo targetSpiedObj = null;
                MarsSpiedObjectInfo rootInfo = ConvertAutomationChainToMarsInfo(uiaChain, lstSiblings, lstAccessibleObjs, targetUIObject, ref targetSpiedObj);
                var roots = new List<MarsSpiedObjectInfo> { rootInfo };

                // 打开对象窥探窗体并加载树
                var frm = MarsObjSpyForm.getInstance(roots, enSpyMode.spyMode_net_winform_frameWork, targetSpiedObj);
                MarsObjSpyForm.showModuleInThread();
            }
            finally
            {
                MarsLoggerSimple.logEnd("StartAccessibleModeFromXY");
            }

        }
        /// <summary>
        /// 通过uia元素链，转换为MarsSpiedObjectInfo单链树。在转换过程中，处理兄弟对象，和因为无法使用UIA获得的MSAA信息 
        /// MSAA链中，第一个元素就是uia对象。需要将MSAA信息补充到uia对象中
        /// </summary>
        /// <param name="chain"></param>
        /// <param name="siblings"></param>
        /// <param name="msaaObjectInfo"></param>
        /// <returns></returns>
        private static MarsSpiedObjectInfo ConvertAutomationChainToMarsInfo(List<AutomationElement> chain,
                List<AutomationElement> siblings,
                List<(AutomationElement, MarsMSAABasicInfo)> msaaObjectInfo,
                AutomationElement targetUIObject,
                ref MarsSpiedObjectInfo targetSpiedObj)
        {
            // Ensure order: parent (top window) -> ... -> target element
            //try
            //{
            //    if (chain != null && chain.Count > 1)
            //    {
            //        bool firstIsWindow = false, lastIsWindow = false;
            //        try { firstIsWindow = (chain[0].Current.ControlType != null && chain[0].Current.ControlType.Id == 50032); } catch { }
            //        try { lastIsWindow = (chain[chain.Count - 1].Current.ControlType != null && chain[chain.Count - 1].Current.ControlType.Id == 50032); } catch { }
            //        if (!firstIsWindow && lastIsWindow)
            //        {
            //            chain.Reverse();
            //        }
            //    }
            //}
            //catch { }

            MarsSpiedObjectInfo root = null;
            MarsSpiedObjectInfo prev = null;
            string rolePath = string.Empty;
            bool isTargetUI = false;
            for (int i = 0; i < chain.Count; i++)
            {
                //foreach (var ae in chain)
                //{
                var ae = chain[i];
                if (ae == null) continue;
                isTargetUI = (ae == targetUIObject);
                var node = CreateMarsInfoFromAutomation(ae);
                if (isTargetUI)
                {
                    targetSpiedObj = node;
                }
                string roleName = node.objectType ?? string.Empty;
                rolePath = string.IsNullOrEmpty(rolePath) ? roleName : ($"{rolePath}/{roleName}");
                node.marsMSAARoleNamePath = rolePath;
                if (root == null) root = node;
                if (i == chain.Count - 2)
                {
                    /// 处理兄弟对象
                    /// 
                    foreach (var s in siblings)
                    {
                        if (s == null) continue;
                        var saeNode = CreateMarsInfoFromAutomation(s);
                        if (node.children == null)
                        {
                            node.children = new List<MarsSpiedObjectInfo>();
                        }
                        node.children.Add(saeNode);
                    }
                }
                if (prev != null)
                {
                    if (prev.children == null) prev.children = new List<MarsSpiedObjectInfo>();
                    prev.children.Add(node);
                }
                prev = node;
            }
            return root;
        }

        /// <summary>
        /// 获取更友好的显示名称，避免显示纯数字
        /// </summary>
        private static string GetDisplayName(string name, string automationId, string controlTypeName)
        {
            // 如果有名称且不是纯数字，直接使用
            if (!string.IsNullOrEmpty(name) && !System.Text.RegularExpressions.Regex.IsMatch(name, @"^\d+$"))
            {
                return name;
            }

            // 如果名称为空或纯数字，直接返回空字符串
            // 不使用 AutomationId，因为它会变化
            return string.Empty;
        }

        private static int GetAEControlId(AutomationElement ae)
        {
            if (ae == null) return -1;
            return MarsWindowsAPIs.GetDlgCtrlID(new IntPtr(ae.Current.NativeWindowHandle));
        }

        private static MarsSpiedObjectInfo CreateMarsInfoFromAutomation(AutomationElement ae)
        {
            string name = string.Empty;
            string automationId = string.Empty;
            string className = string.Empty;
            string frameworkId = string.Empty;
            string controlTypeName = string.Empty;
            System.Drawing.Rectangle rect = System.Drawing.Rectangle.Empty;
            try { name = ae.Current.Name ?? string.Empty; } catch { }
            try { automationId = ae.Current.AutomationId ?? string.Empty; } catch { }
            try { className = ae.Current.ClassName ?? string.Empty; } catch { }
            try { frameworkId = ae.Current.FrameworkId ?? string.Empty; } catch { }
            try { controlTypeName = ae.Current.ControlType != null ? ae.Current.ControlType.ProgrammaticName : string.Empty; } catch { }
            try
            {
                var r = ae.Current.BoundingRectangle;
                rect = new System.Drawing.Rectangle((int)r.Left, (int)r.Top, Math.Max(0, (int)r.Width), Math.Max(0, (int)r.Height));
            }
            catch { }
            string strControlId = string.Empty;

            int cur_hwnd = 0;
            try { cur_hwnd = (int)(ae.Current.NativeWindowHandle); } catch { }

            var info = new MarsSpiedObjectInfo()
            {
                referenceToObj = ae,
                objectName = GetDisplayName(name, automationId, controlTypeName),
                Text = name,
                objectType = controlTypeName,
                x = rect.X,
                y = rect.Y,
                w = rect.Width,
                h = rect.Height,
                isVisible = rect.Width > 0 && rect.Height > 0,
                hwnd = cur_hwnd,
                controlId = GetAEControlId(ae),
                children = new List<MarsSpiedObjectInfo>()
            };
            try { info.controlClassTypeFromAPI = "uia"; } catch { }
            return info;
        }

        private static MarsSpiedObjectInfo ConvertAccessibleChainToMarsInfo(List<IAccessible> chain)
        {
            MarsSpiedObjectInfo root = null;
            MarsSpiedObjectInfo prev = null;
            string rolePath = string.Empty;
            foreach (var acc in chain)
            {
                if (acc == null) continue;
                var node = CreateMarsInfoFromAccessible(acc);

                // 如果节点为null（被零尺寸过滤），跳过此节点
                if (node == null) continue;

                // build role name path from root to current
                string roleName = node.objectType ?? "";
                rolePath = string.IsNullOrEmpty(rolePath) ? roleName : ($"{rolePath}/{roleName}");
                node.marsMSAARoleNamePath = rolePath;
                if (root == null) root = node;
                if (prev != null)
                {
                    if (prev.children == null) prev.children = new List<MarsSpiedObjectInfo>();
                    prev.children.Add(node);
                }
                prev = node;
            }
            return root;
        }

        private static MarsSpiedObjectInfo CreateMarsInfoFromAccessible(IAccessible acc)
        {
            string name = SafeGet(() => acc.get_accName(0));
            string value = SafeGet(() => acc.get_accValue(0));
            int left = 0, top = 0, width = 0, height = 0;
            try { acc.accLocation(out left, out top, out width, out height, 0); } catch { }
            string roleName = "";
            try
            {
                object roleObj = acc.get_accRole(0);
                int role = roleObj is int i ? i : Convert.ToInt32(roleObj);
                roleName = MARSAccessibleProvider.GetRoleName(role);
            }
            catch { }

            // 检查是否应该跳过零尺寸对象
            if (!ShowZeroSizeObjects && width == 0 && height == 0)
            {
                // 返回一个标记为隐藏的对象，或者返回null让调用者处理
                return null;
            }

            var info = new MarsSpiedObjectInfo()
            {
                referenceToObj = acc,
                objectName = name,
                Text = string.IsNullOrEmpty(value) ? name : value,
                objectType = roleName,
                x = left,
                y = top,
                w = width,
                h = height,
                isVisible = width > 0 && height > 0,
                children = new List<MarsSpiedObjectInfo>()
            };
            try { info.controlClassTypeFromAPI = "standard"; } catch { }
            return info;
        }

        private static string SafeGet(Func<string> getter)
        {
            try { return getter() ?? string.Empty; } catch { return string.Empty; }
        }

        /// <summary>
        /// 递归设置所有控件的字体
        /// </summary>
        /// <param name="control">要设置字体的控件</param>
        /// <param name="font">要设置的字体</param>
        private void SetAllControlsFont(Control control, Font font)
        {
            try
            {
                // 设置当前控件的字体
                control.Font = font;

                // 递归设置所有子控件的字体
                foreach (Control childControl in control.Controls)
                {
                    SetAllControlsFont(childControl, font);
                }
            }
            catch (Exception ex)
            {
                // 忽略设置字体时的异常，继续处理其他控件
                simpleLog.MarsLoggerSimple.Warning("SetAllControlsFont", $"Error setting font for control {control.Name}: {ex.Message}");
            }
        }

        private void winformObjectTreeview_AfterSelect(object sender, TreeViewEventArgs e)
        {
            try
            {
                if (e?.Node?.Tag == null) return;
                var info = e.Node.Tag as MarsSpiedObjectInfo;
                if (info == null) return;

                // 1. Clear data grid view
                this.dataGridView1.Rows.Clear();

                // 2. Update control picture preview
                UpdateControlPicturePreview(info);

                // 3. Check if it's a highlightable node
                bool isHighlightableNode = (info.referenceToObj is Accessibility.IAccessible) ||
                    (info.referenceToObj is AutomationElement) ||
                    (string.Equals(info.controlClassTypeFromAPI, "standard", StringComparison.OrdinalIgnoreCase)) ||
                    (string.Equals(info.controlClassTypeFromAPI, "uia", StringComparison.OrdinalIgnoreCase));
                if (!isHighlightableNode)
                {
                    if (info.referenceToObj is System.Windows.Forms.Control c)
                    {
                        /// 获得 rect From hwnd
                        /// 
                        MarsWindowsAPIs.RECT rect = default(MarsWindowsAPIs.RECT);
                        if (MarsWindowsAPIs.GetWindowRect(c.Handle, out rect))
                        {
                            info.x = rect.Left;
                            info.y = rect.Top;
                            info.w = rect.Right - rect.Left;
                            info.h = rect.Bottom - rect.Top;
                            isHighlightableNode = true;
                        }
                    }
                }
                if (isHighlightableNode)
                {
                    var rect = new System.Drawing.Rectangle(info.x, info.y, info.w, info.h);
                    if (rect.Width > 0 && rect.Height > 0)
                    {
                        // Use HighlightWindow for persistent highlight
                        this.Invoke(new Action(() =>
                        {
                            HighlightWindow.HideAndDestroy();
                            var frm = HighlightWindow.getInstance();
                            frm.Show();
                            frm.Left = info.x - 1;
                            frm.Top = info.y - 1;
                            frm.Width = info.w + 1;
                            frm.Height = info.h + 1;
                            frm.ActiveControl = null;

                            // Debug: Log the highlight window size
                            simpleLog.MarsLoggerSimple.Info("HighlightWindow", $"Highlight window set to: Left={frm.Left}, Top={frm.Top}, Width={frm.Width}, Height={frm.Height}, Object size: x={info.x}, y={info.y}, w={info.w}, h={info.h}");
                        }));

                        // Also show flash highlight for immediate feedback
                        // FlashHighlight(rect, 2); // Commented out, may be needed in the future
                    }
                }

                // 4. Load basic information
                LoadBasicInfo(info);
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("winformObjectTreeview_AfterSelect", $"Error in AfterSelect: {ex.Message}", ex);
            }
        }

        private void winformObjectTreeview_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            try
            {
                // Hide highlight window when deselecting a node
                // This is called before a new node is selected, so we can clean up the current highlight
                HighlightWindow.HideAndDestroy();
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("winformObjectTreeview_BeforeSelect", $"Error in BeforeSelect: {ex.Message}", ex);
            }
        }

        private void UpdateControlPicturePreview(MarsSpiedObjectInfo info)
        {
            try
            {
                if (info == null) return;

                // Clear existing image properly
                ClearPreviewImage();

                Image newImage = null;

                // Try to get image from snapshot file first
                if (System.IO.File.Exists(info.snapshotFileNameWithPath))
                {
                    newImage = Bitmap.FromFile(info.snapshotFileNameWithPath);
                    simpleLog.MarsLoggerSimple.Info("UpdateControlPicturePreview", $"Loaded snapshot image: {info.snapshotFileNameWithPath}");
                }
                else
                {
                    // For IAccessible objects, try to get hwnd from the object itself
                    if (info.referenceToObj is Accessibility.IAccessible accObj)
                    {
                        // Try to get hwnd from IAccessible object
                        IntPtr hwnd = GetHwndFromIAccessible(accObj);
                        if (hwnd != IntPtr.Zero)
                        {
                            info.hwnd = hwnd.ToInt64();
                            simpleLog.MarsLoggerSimple.Info("UpdateControlPicturePreview", $"Got hwnd from IAccessible: 0x{hwnd.ToInt64():X}");
                        }
                    }
                    else if (info.referenceToObj is System.Windows.Forms.Control)
                    {

                    }

                    // Try to get control image
                    newImage = StandardWindowsEnumerator.GetControlImage(info);
                    if (newImage != null)
                    {
                        simpleLog.MarsLoggerSimple.Info("UpdateControlPicturePreview", $"Captured control image: {newImage.Width}x{newImage.Height}");
                    }
                    else
                    {
                        // If no image could be captured, show a placeholder
                        newImage = CreatePlaceholderImage(info);
                        simpleLog.MarsLoggerSimple.Info("UpdateControlPicturePreview", $"Created placeholder image: {newImage.Width}x{newImage.Height}");
                    }
                }

                // Set the new image and adjust size
                if (newImage != null)
                {
                    SetPreviewImage(newImage);
                }
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("UpdateControlPicturePreview", $"Error updating picture preview: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 清除预览图片
        /// </summary>
        private void ClearPreviewImage()
        {
            try
            {
                if (this.controlPicturePreview.Image != null)
                {
                    var oldImage = this.controlPicturePreview.Image;
                    this.controlPicturePreview.Image = null;
                    oldImage.Dispose();
                }
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("ClearPreviewImage", $"Error clearing preview image: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 设置预览图片并调整大小
        /// </summary>
        private void SetPreviewImage(Image image)
        {
            try
            {
                if (image == null) return;

                // Set the image with original size (no scaling)
                this.controlPicturePreview.Image = image;
                this.controlPicturePreview.SizeMode = PictureBoxSizeMode.AutoSize;

                // Set the PictureBox size to match the image size
                this.controlPicturePreview.Size = new System.Drawing.Size(image.Width, image.Height);

                // Enable scroll bars on the parent container if it's a Panel
                if (this.controlPicturePreview.Parent is Panel parentPanel)
                {
                    parentPanel.AutoScroll = true;
                    parentPanel.AutoScrollMinSize = new System.Drawing.Size(image.Width, image.Height);

                    // Center the image in the scrollable area
                    //this.controlPicturePreview.Location = new System.Drawing.Point(
                    //    Math.Max(0, (parentPanel.ClientSize.Width - image.Width) / 2),
                    //    Math.Max(0, (parentPanel.ClientSize.Height - image.Height) / 2)
                    //);
                }

                simpleLog.MarsLoggerSimple.Info("SetPreviewImage", $"Set preview image size: {image.Width}x{image.Height} (no scaling)");
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("SetPreviewImage", $"Error setting preview image: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 从 IAccessible 对象获取窗口句柄
        /// </summary>
        private IntPtr GetHwndFromIAccessible(Accessibility.IAccessible accObj)
        {
            try
            {
                // 方法1: 通过 accLocation 获取位置，然后使用 WindowFromPoint
                accObj.accLocation(out int left, out int top, out int width, out int height, 0);
                if (width > 0 && height > 0)
                {
                    var centerPoint = new System.Drawing.Point(left + width / 2, top + height / 2);
                    IntPtr hwnd = MarsWindowsAPIs.WindowFromPoint(centerPoint);
                    if (hwnd != IntPtr.Zero)
                    {
                        return hwnd;
                    }
                }

                //// 方法2: 尝试通过 MARSAccessibleProvider 获取
                //var provider = new MARSAccessibleProvider();
                //var accessibleObj = provider.GetAccessibleObject(accObj);
                //if (accessibleObj != null)
                //{
                //    // 这里可能需要根据 MARSAccessibleProvider 的实现来获取 hwnd
                //    // 具体实现取决于 MARSAccessibleProvider 的接口
                //}

                return IntPtr.Zero;
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Warning("GetHwndFromIAccessible", $"Error getting hwnd from IAccessible: {ex.Message}", ex.StackTrace);
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// 创建占位符图像
        /// </summary>
        private Image CreatePlaceholderImage(MarsSpiedObjectInfo info)
        {
            try
            {
                int width = Math.Max(info.w, 100);
                int height = Math.Max(info.h, 50);

                Bitmap placeholder = new Bitmap(width, height);
                using (Graphics g = Graphics.FromImage(placeholder))
                {
                    g.Clear(Color.LightGray);
                    g.DrawRectangle(Pens.Black, 0, 0, width - 1, height - 1);

                    string text = $"IAccessible\n{info.objectName}\n{info.objectType}";
                    using (Font font = new Font("Arial", 8))
                    {
                        StringFormat sf = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center
                        };
                        g.DrawString(text, font, Brushes.Black, new RectangleF(0, 0, width, height), sf);
                    }
                }

                return placeholder;
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("CreatePlaceholderImage", $"Error creating placeholder image: {ex.Message}", ex);
                return null;
            }
        }

        private void winformObjectTreeview_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                if (winformObjectTreeview.SelectedNode?.Tag == null) return;
                var info = winformObjectTreeview.SelectedNode.Tag as MarsSpiedObjectInfo;
                if (info == null) return;

                if (info.referenceToObj is System.Windows.Forms.Control)
                {
                    return;
                }

                // 1. Show dialog asking if user wants to get child nodes (multilingual)
                var result = MessageBox.Show(
                    GetLocalizedString("GetChildNodesQuestion", "Do you want to get child nodes under this node?"),
                    GetLocalizedString("GetChildNodesTitle", "Get Child Nodes"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes) return;

                // 2. Check reference object type and get child objects
                var children = GetChildrenForNode(info);
                if (children == null || children.Count == 0)
                {
                    MessageBox.Show(
                        GetLocalizedString("NoChildNodesFound", "No child nodes found"),
                        GetLocalizedString("Information", "Information"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                // 3. Add child objects to selected node
                AddChildrenToTreeNode(winformObjectTreeview.SelectedNode, children);

                MessageBox.Show(
                    GetLocalizedString("SuccessfullyAddedChildNodes", $"Successfully added {children.Count} child nodes"),
                    GetLocalizedString("Completed", "Completed"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("winformObjectTreeview_DoubleClick", $"Error in DoubleClick: {ex.Message}", ex);
                MessageBox.Show(
                    GetLocalizedString("ErrorGettingChildNodes", $"Error getting child nodes: {ex.Message}"),
                    GetLocalizedString("Error", "Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private string GetLocalizedString(string key, string defaultValue)
        {
            try
            {
                // Force English as default language for dialog messages
                // This can be changed to use system culture if needed
                return defaultValue;

                // Uncomment below code if you want to use system culture
                /*
                var culture = System.Threading.Thread.CurrentThread.CurrentCulture;
                switch (culture.TwoLetterISOLanguageName.ToLower())
                {
                    case "zh":
                    case "zh-cn":
                    case "zh-tw":
                        return GetChineseString(key);
                    case "en":
                    default:
                        return defaultValue;
                }
                */
            }
            catch
            {
                return defaultValue;
            }
        }

        private string GetChineseString(string key)
        {
            switch (key)
            {
                case "GetChildNodesQuestion":
                    return "是否要获取该节点下的子节点？";
                case "GetChildNodesTitle":
                    return "获取子节点";
                case "NoChildNodesFound":
                    return "未找到子节点";
                case "Information":
                    return "信息";
                case "SuccessfullyAddedChildNodes":
                    return "成功添加子节点";
                case "Completed":
                    return "完成";
                case "ErrorGettingChildNodes":
                    return "获取子节点时发生错误";
                case "Error":
                    return "错误";
                case "NoNodeSelected":
                    return "请先选择一个节点";
                case "MarsNamePathGenerated":
                    return "Mars Name Path: {0}";
                case "MarsNamePath":
                    return "Mars Name Path";
                case "ErrorGeneratingPath":
                    return "生成Mars Name Path时发生错误: {0}";

                // 新增的弹出菜单字符串
                case "UnsupportedObjectType":
                    return "仅支持 UIA 和 IAccessible 对象进行此操作";
                case "ObjectAddedSuccessfully":
                    return "对象已成功添加到池中";
                case "Success":
                    return "成功";
                case "ErrorGeneratingProperties":
                    return "生成对象属性时发生错误";
                case "NoItemSelected":
                    return "请选择要删除的项目";
                case "ConfirmDeleteSelected":
                    return "确定要删除选中的项目吗？";
                case "ConfirmDelete":
                    return "确认删除";
                case "NoItemsToDelete":
                    return "没有要删除的项目";
                case "ConfirmDeleteAll":
                    return "确定要删除所有项目吗？";
                case "ErrorDeletingItems":
                    return "删除项目时发生错误";
                case "ErrorDeletingAllItems":
                    return "删除所有项目时发生错误";
                case "NoObjectInfo":
                    return "没有可用的对象信息";
                case "ObjectName":
                    return "对象名称";
                case "ObjectType":
                    return "对象类型";
                case "ControlClass":
                    return "控件类";
                case "Location":
                    return "位置";
                case "MSAARolePath":
                    return "MSAA 角色路径";
                case "ObjectDetails":
                    return "对象详情";
                case "ErrorShowingDetails":
                    return "显示详情时发生错误";
                case "ObjectIdsCopied":
                    return "Object IDs 已复制到剪贴板";

                default:
                    return key;
            }
        }

        private string GenerateMarsNamePath(MarsSpiedObjectInfo selectedInfo)
        {
            try
            {
                var pathComponents = new List<string>();
                var currentNode = winformObjectTreeview.SelectedNode;

                // Traverse up to the root node
                while (currentNode != null && currentNode.Tag is MarsSpiedObjectInfo info)
                {
                    string component = GetPathComponent(info);
                    if (!string.IsNullOrEmpty(component))
                    {
                        pathComponents.Insert(0, component); // Insert at beginning to maintain root-to-target order
                    }
                    currentNode = currentNode.Parent;
                }

                string marsNamePath = string.Join(";", pathComponents);

                // Store the path in the selected object
                selectedInfo.marsNamePath = marsNamePath;

                return marsNamePath;
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("GenerateMarsNamePath", $"Error generating path: {ex.Message}", ex);
                throw;
            }
        }

        private string GenerateMarsTypePath(MarsSpiedObjectInfo selectedInfo)
        {
            try
            {
                var typeComponents = new List<string>();
                var currentNode = winformObjectTreeview.SelectedNode;

                // Traverse up to the root node
                while (currentNode != null && currentNode.Tag is MarsSpiedObjectInfo info)
                {
                    string controlType = "";

                    // Only get ControlType if referenceToObj is AutomationElement
                    if (info.referenceToObj is System.Windows.Automation.AutomationElement uiaElement)
                    {
                        try
                        {
                            controlType = uiaElement.Current.ControlType?.ProgrammaticName ?? "";
                        }
                        catch (Exception ex)
                        {
                            simpleLog.MarsLoggerSimple.Error("GenerateMarsTypePath", $"Failed to get ControlType from AutomationElement: {ex.Message}");
                            controlType = "";
                        }
                    }
                    else
                    {
                        // If referenceToObj is not AutomationElement, terminate traversal
                        break;
                    }

                    if (!string.IsNullOrEmpty(controlType))
                    {
                        typeComponents.Insert(0, controlType); // Insert at beginning to maintain root-to-target order
                    }

                    currentNode = currentNode.Parent;
                }

                string marsTypePath = string.Join(";", typeComponents);

                // Store the path in the selected object
                selectedInfo.marsTypePath = marsTypePath;

                return marsTypePath;
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("GenerateMarsTypePath", $"Error generating type path: {ex.Message}", ex);
                throw;
            }
        }

        private string GetPathComponent(MarsSpiedObjectInfo info)
        {
            try
            {
                if (info == null) return "";

                string objectName = info.objectName ?? "";
                if (string.IsNullOrEmpty(objectName))
                {
                    objectName = info.Text ?? "";
                }


                // Determine the technology type
                string techType = "";
                if (info.referenceToObj is AutomationElement uiae)
                {
                    techType = "UIA";
                    objectName = uiae.Current.Name;
                }
                else if (info.referenceToObj is Accessibility.IAccessible iacc)
                {
                    techType = "IAcc";
                    objectName = iacc.get_accName(0);
                }
                else if (string.Equals(info.controlClassTypeFromAPI, "uia", StringComparison.OrdinalIgnoreCase))
                {
                    techType = "UIA";
                }
                else if (string.Equals(info.controlClassTypeFromAPI, "standard", StringComparison.OrdinalIgnoreCase))
                {
                    techType = "IAcc";
                }
                else
                {
                    // Try to determine from other properties
                    if (!string.IsNullOrEmpty(info.controlClassTypeFromAPI))
                    {
                        techType = info.controlClassTypeFromAPI.ToUpper();
                    }
                    else
                    {
                        techType = "Unknown";
                    }
                }
                if (string.IsNullOrEmpty(objectName))
                {
                    objectName = "Unknown";
                }
                return $"{techType}:{objectName}";
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("GetPathComponent", $"Error getting path component: {ex.Message}", ex);
                return "Error";
            }
        }

        private List<MarsSpiedObjectInfo> GetChildrenForNode(MarsSpiedObjectInfo nodeInfo)
        {
            var children = new List<MarsSpiedObjectInfo>();

            try
            {
                // 判断reference对象类型
                if (nodeInfo.referenceToObj is AutomationElement uiaElement)
                {
                    // UIA方式获取子对象
                    children = GetUIAChildren(uiaElement);

                    // 如果UIA没有子对象，尝试IAccessible
                    if (children.Count == 0)
                    {
                        children = GetIAccessibleChildren(nodeInfo);
                    }
                }
                else if (nodeInfo.referenceToObj is Accessibility.IAccessible accessible)
                {
                    // IAccessible方式获取子对象
                    children = GetIAccessibleChildren(nodeInfo);
                }
                else
                {
                    // 尝试通过hwnd获取IAccessible
                    children = GetIAccessibleChildren(nodeInfo);
                }
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("GetChildrenForNode", $"Error getting children: {ex.Message}", ex);
            }

            return children;
        }

        private List<MarsSpiedObjectInfo> GetUIAChildren(AutomationElement element)
        {
            var children = new List<MarsSpiedObjectInfo>();

            try
            {
                //string pattersnAndInfo = MarsUIInspector.InspectElementPatterns(element);
                //simpleLog.MarsLoggerSimple.Info("GetUIAChildren", $"Inspecting UIA element: {pattersnAndInfo}");

                //var all = element.FindAll(TreeScope.Descendants, Condition.TrueCondition);
                //Dictionary<string, string>  allItems = MarsUIInspector.DumpAllElements(all);
                //Console.WriteLine($"Descendants={all.Count}");

                var walker = TreeWalker.ContentViewWalker;
                var child = walker.GetFirstChild(element);

                while (child != null)
                {
                    var childInfo = CreateMarsInfoFromAutomation(child);
                    if (childInfo != null)
                    {
                        children.Add(childInfo);

                        // 如果需要高亮显示
                        if (ShouldHighlightDuringProcessing())
                        {
                            var rect = new System.Drawing.Rectangle(childInfo.x, childInfo.y, childInfo.w, childInfo.h);
                            if (rect.Width > 0 && rect.Height > 0)
                            {
                                FlashHighlight(rect, 1);
                                System.Threading.Thread.Sleep(100); // 短暂显示高亮
                            }
                        }
                    }

                    child = walker.GetNextSibling(child);
                }
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("GetUIAChildren", $"Error getting UIA children: {ex.Message}", ex);
            }

            return children;
        }

        private List<MarsSpiedObjectInfo> GetIAccessibleChildren(MarsSpiedObjectInfo nodeInfo)
        {
            var children = new List<MarsSpiedObjectInfo>();

            try
            {
                Accessibility.IAccessible accessible = null;
                bool isOk = false; string strError = "";

                // 尝试从referenceToObj获取IAccessible
                if (nodeInfo.referenceToObj is Accessibility.IAccessible acc)
                {
                    accessible = acc;
                }
                else if (nodeInfo.hwnd != 0)
                {
                    // 通过hwnd获取IAccessible
                    var provider = new MARSAccessibleProvider();
                    accessible = provider.GetAccessibleObject(new IntPtr(nodeInfo.hwnd)) as IAccessible;
                }

                if (accessible == null) return children;

                // 获取子对象
                int childCount = 0;
                try
                {
                    childCount = accessible.accChildCount;
                }
                catch
                {
                    return children;
                }

                if (childCount <= 0) return children;

                object[] childObjects = new object[childCount];
                int obtained = MARSAccessibleProvider.AccessibleChildren(accessible, 0, childCount, childObjects, out int nObtained);
                int iRole = MARSAccessibleProvider.Get_Role(accessible);//.get_accRole(0) as int;
                if (obtained != 0 || nObtained <= 0) return children;

                for (int i = 0; i < nObtained; i++)
                {
                    var childObj = childObjects[i];
                    if (childObj is not IAccessible)
                    {
                        if (childObj is int childIdx)
                        {
                            if ((iRole == MARSAccessibleConstans.ROLE_SYSTEM_PAGETABLIST)
                                || (iRole == MARSAccessibleConstans.ROLE_SYSTEM_PAGETAB))
                            {
                                // 这些控件的子对象是索引，需要特殊处理
                                var rect = MARSAccessibleProvider.getPageSubItemRect(accessible, childIdx, ref isOk, ref strError);
                                var subItmName = accessible.get_accName(childIdx);
                                FlashControlHelper.FlashRect(rect);
                                continue;
                            }
                            var childItm = accessible.get_accChild(childIdx);
                            if (childItm is IAccessible cAcc)
                            {
                                /// 没有实现accLocation的对象，直接忽略
                                /// 
                                simpleLog.MarsLoggerSimple.Error("GetIAccessibleChildren", $"==========NOT IMPLEMENT======\r\nProcessing child index {childIdx} of role {iRole}\r\n|{Environment.StackTrace} ");
                            }
                        }
                        continue;
                    }
                    var childInfo = CreateMarsInfoFromAccessible(childObj as IAccessible);
                    if (childInfo != null)
                    {
                        children.Add(childInfo);

                        // 如果需要高亮显示
                        if (ShouldHighlightDuringProcessing())
                        {
                            var rect = new System.Drawing.Rectangle(childInfo.x, childInfo.y, childInfo.w, childInfo.h);
                            if (rect.Width > 0 && rect.Height > 0)
                            {
                                FlashHighlight(rect, 1);
                                System.Threading.Thread.Sleep(100); // 短暂显示高亮
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("GetIAccessibleChildren", $"Error getting IAccessible children: {ex.Message}", ex);
            }

            return children;
        }

        private void AddChildrenToTreeNode(TreeNode parentNode, List<MarsSpiedObjectInfo> children)
        {
            try
            {
                // 清除现有子节点（如果存在）
                parentNode.Nodes.Clear();

                // 添加新的子节点
                foreach (var childInfo in children)
                {
                    var childNode = CreateNodeFromObjInfo(childInfo, IntPtr.Zero);
                    if (childNode != null)
                    {
                        parentNode.Nodes.Add(childNode);
                    }
                }

                // 展开父节点以显示子节点
                parentNode.Expand();
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("AddChildrenToTreeNode", $"Error adding children to tree node: {ex.Message}", ex);
            }
        }

        private bool ShouldHighlightDuringProcessing()
        {
            // 这里可以根据设置或配置来决定是否需要高亮
            // 暂时返回true，实际使用时可以通过配置或设置来控制
            return true;
        }

        private void FlashHighlight(System.Drawing.Rectangle rect, int times)
        {
            // Clip to screen to avoid huge off-screen rectangles
            var screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(rect.X, rect.Y));
            var screenBounds = screen.Bounds;
            var clipped = System.Drawing.Rectangle.Intersect(rect, screenBounds);
            if (clipped.Width <= 0 || clipped.Height <= 0) return;

            // Draw multiple concentric reversible frames to increase visibility and apparent thickness
            for (int i = 0; i < times; i++)
            {
                // Use more layers to simulate a thicker border
                var layers = new[] { 0, 1, 2, 3, 4 };
                foreach (var off in layers)
                {
                    var r = new System.Drawing.Rectangle(
                        clipped.X - off,
                        clipped.Y - off,
                        clipped.Width + (off * 2),
                        clipped.Height + (off * 2));
                    var color = (off % 3 == 0) ? System.Drawing.Color.Red : (off % 3 == 1) ? System.Drawing.Color.Yellow : System.Drawing.Color.Lime;
                    System.Windows.Forms.ControlPaint.DrawReversibleFrame(r, color, FrameStyle.Dashed);
                }

                System.Threading.Thread.Sleep(220);

                // erase in reverse order for clean revert
                for (int idx = layers.Length - 1; idx >= 0; idx--)
                {
                    int off = layers[idx];
                    var r = new System.Drawing.Rectangle(
                        clipped.X - off,
                        clipped.Y - off,
                        clipped.Width + (off * 2),
                        clipped.Height + (off * 2));
                    var color = (off % 3 == 0) ? System.Drawing.Color.Red : (off % 3 == 1) ? System.Drawing.Color.Yellow : System.Drawing.Color.Lime;
                    System.Windows.Forms.ControlPaint.DrawReversibleFrame(r, color, FrameStyle.Dashed);
                }

                System.Threading.Thread.Sleep(100);
            }
        }

        private void contextMenuStripSelectedObjects_Opening(object sender, CancelEventArgs e)
        {
            /// 根据当前选中节点，动态设置菜单项的可见性和启用状态
            /// 测试popup menu高度
            /// 
            //simpleLog.MarsLoggerSimple.Info("contextMenuStripSelectedObjects_Opening", $"{this.contextMenuStripSelectedObjects.Size}");

        }
    }
}
