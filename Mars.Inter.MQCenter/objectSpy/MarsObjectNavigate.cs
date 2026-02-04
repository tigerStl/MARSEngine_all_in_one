using MarsUFTAddins.IMars.tiger;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mars.message.Inter.MQCenter.objectSpy
{
    public partial class MarsObjectNavigate : Form
    {

        public MarsObjectNavigate(bool isArray=false)
        {
            InitializeComponent();
            if (!isArray)
            {
                this.splitContainer1.Panel1Collapsed = true;
                this.splitContainer1.Panel1.Hide();
            }
            else
            {
                this.splitContainer1.Panel1Collapsed = false;
                //this.splitContainer1.Panel1.Hide();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }  

        private void NaviageObjects(string propertyName ,Object objToNavigate)
        {
            /// 算法：
            /// 1, 写对象名称或者在上级对象的属性名称和类别
            /// 2, load 对象信息到表中
            /// 3，添加双击事件处理
            /// 
            this.objectNameLbl.Text = $"Property Name :{propertyName}";
            this.objectTypeLbl.Text = $"Type:{objToNavigate.GetType().FullName}";

            /// 2, load 对象信息到表中
            /// 2.1 load property
            /// 
            ReflectorForCSharp reflect = new ReflectorForCSharp();
            var propertiesInfo = reflect.GetAllPropertiesWithValues(objToNavigate);
            this.dataGridView1.Rows.Clear();
            if (propertiesInfo!=null)
            {                
                foreach (var itm in propertiesInfo.Keys) {
                    try
                    {
                        if (itm == null) continue;
                        int iRowId = this.dataGridView1.Rows.Add();
                        var oneRow = this.dataGridView1.Rows[iRowId];
                        oneRow.Cells[0].Value = "Property";
                        //oneRow.DefaultCellStyle.BackColor = Color.LightBlue;
                        MethodInfo getMthd = itm.GetGetMethod();
                        MethodInfo setMthd = itm.GetSetMethod();
                        string strGetPub = getMthd == null ? "GET.N/A" : getMthd.IsPublic ? "Pub.Get" : "Priv.Get";
                        string strSetPub = setMthd == null ? "SET.N/A" : getMthd.IsPublic ? "Pub.Set" : "Priv.Set";
                        oneRow.Cells[1].Value = $"[{strGetPub}]/[{strSetPub}]"; //public or private
                        oneRow.Cells[2].Value = itm.Name;
                        oneRow.Cells[3].Value = propertiesInfo[itm];
                        oneRow.Cells[4].Value = itm.GetType().FullName;

                        oneRow.Tag = itm.GetValue(objToNavigate);
                        if (oneRow.Tag != null)
                        {
                            if ((oneRow.Tag is System.Object[])
                                || (oneRow.Tag.GetType().GetInterfaces()
                                        .Any(x =>
                                            x.IsGenericType &&
                                            x.GetGenericTypeDefinition() == typeof(ICollection<>))))
                            {
                                oneRow.DefaultCellStyle.BackColor = Color.LightGreen;
                            }
                        }
                        oneRow.Cells[4].Value = oneRow.Tag == null ? "N/A" : oneRow.Tag.GetType().FullName;
                    }
                    catch(Exception e)
                    {
                        simpleLog.MarsLoggerSimple.Error("NaviageObjects", e.Message, e);
                        continue;
                    }
                }
            }
            var memberInfo = reflect.getAllMemberInfo(objToNavigate);

            if (memberInfo != null)
            {
                foreach (var itm in memberInfo.Keys)
                {
                    try
                    {
                        if (itm == null) continue;
                        int iRowId = this.dataGridView1.Rows.Add();
                        var oneRow = this.dataGridView1.Rows[iRowId];
                        oneRow.Cells[0].Value = "Member";
                        //oneRow.DefaultCellStyle.BackColor = Color.LightBlue;
                        oneRow.Cells[1].Value = "Member"; //public or private
                        oneRow.Cells[2].Value = itm.Name;
                        oneRow.Cells[3].Value = memberInfo[itm];
                        oneRow.Cells[4].Value = itm.GetType().FullName;
                        if ((itm as FieldInfo)==null) continue;
                        oneRow.Tag = ((FieldInfo)itm).GetValue(objToNavigate);
                        if (oneRow.Tag != null)
                        {
                            if ((oneRow.Tag is System.Object[])
                                || (oneRow.Tag.GetType().GetInterfaces()
                                        .Any(x =>
                                            x.IsGenericType &&
                                            x.GetGenericTypeDefinition() == typeof(ICollection<>))))
                            {
                                oneRow.DefaultCellStyle.BackColor = Color.LightGreen;
                            }
                        }
                        oneRow.Cells[4].Value = oneRow.Tag == null ? "N/A" : oneRow.Tag.GetType().FullName;
                    }
                    catch (Exception e)
                    {
                        simpleLog.MarsLoggerSimple.Error("NaviageObjects", e.Message, e);
                        continue;
                    }
                }
            }
        }

        internal void SetNaviInfo(Object objToNavigate, String propertyName)
        {
            if (objToNavigate is System.Object[])
            {
                System.Object[] arrObj = objToNavigate as System.Object[];
                LoadArrayToListView(arrObj, propertyName);
            }
            else if (objToNavigate is System.Collections.ArrayList)
            {
                LoadArrayToListView(objToNavigate as System.Collections.ArrayList, propertyName);
            }
            else {
                NaviageObjects(propertyName, objToNavigate);
            }
            
        }

        private void LoadArrayToListView(ArrayList lstObj, string propertyName)
        {
            this.listView1.Items.Clear();
            for (int i = 0; i < lstObj.Count; i++)
            {
                try
                {
                    string strId = i + "";
                    string strValue = lstObj[i] == null ? "N/A" : lstObj[i].ToString();
                    string strType = lstObj[i] == null ? "N/A" : lstObj[i].GetType().FullName;
                    var itm = this.listView1.Items.Add(strId);
                    itm.SubItems.Add(strValue);
                    itm.SubItems.Add(strType);
                    itm.Tag = lstObj[i];


                    //this.listView1.Items.Add (new ListViewItem(new string[] { strId, strValue, strType })
                    //{
                    //    Tag = arrObj[i]
                    //}) ;
                }
                catch (Exception e)
                {
                    this.listView1.Items.Add(new ListViewItem(new string[] { i + "", "exception", e.Message }));
                }
            }
        }

        private void LoadArrayToListView(object[] arrObj, string propertyName)
        {
            this.listView1.Items.Clear();
            for (int i = 0; i < arrObj.Length; i++)
            {
                try
                {
                    string strId = i + "";
                    string strValue = arrObj[i] == null ? "N/A" : arrObj[i].ToString();
                    string strType = arrObj[i] == null ? "N/A" : arrObj[i].GetType().FullName;
                    var itm = this.listView1.Items.Add(strId);
                    itm.SubItems.Add(strValue);
                    itm.SubItems.Add(strType);
                    itm.Tag = arrObj[i];


                    //this.listView1.Items.Add (new ListViewItem(new string[] { strId, strValue, strType })
                    //{
                    //    Tag = arrObj[i]
                    //}) ;
                }
                catch(Exception e)
                {
                    this.listView1.Items.Add(new ListViewItem(new string[] {i+"", "exception", e.Message }));
                }
                
            }
        }

        private void dataGridView1_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void dataGridView1_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            try
            {
                var oneRow = this.dataGridView1.Rows[e.RowIndex];
                if (oneRow.Tag == null) return;
                string strObjType = oneRow.Cells["objType"].Value as string;
                string[] typsLevel = strObjType.Split('.');
                if (string.IsNullOrEmpty(strObjType)) return;
                MarsObjectNavigate frm = null;
                string proName = oneRow.Cells[2].Value as string;

                if (oneRow.Tag is ArrayList)
                {
                    frm = new MarsObjectNavigate(true);
                    frm.SetNaviInfo(oneRow.Tag, proName);
                    frm.ShowDialog();
                    return;
                }

                bool isArray = oneRow.Tag is System.Object[];                
                if (!isArray)
                {                    
                    if ((strObjType.StartsWith("System", StringComparison.OrdinalIgnoreCase)) && (typsLevel.Length <= 2)) //basic types
                        return;
                }
                if (
                    //(strObjType.StartsWith("System", StringComparison.OrdinalIgnoreCase)&&(!strObjType.StartsWith("System.Object[]")))  || 
                    strObjType.StartsWith("Window", StringComparison.OrdinalIgnoreCase) || 
                    strObjType.Equals("N/A",StringComparison.OrdinalIgnoreCase)
                    ) return;
                
                
                if ((oneRow.Tag is System.Object[])
                    //||(typeof(System.Collections.ICollection).IsAssignableFrom(oneRow.Tag.GetType()))
                    //||(typeof(System.Collections.IEnumerable).IsAssignableFrom(oneRow.Tag.GetType()))
                    )
                {
                    frm = new MarsObjectNavigate(true);
                }
                else
                {
                    frm = new MarsObjectNavigate();
                }
                
                frm.SetNaviInfo(oneRow.Tag, proName);
                frm.ShowDialog();
                frm = null;
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("dataGridView1_CellMouseDoubleClick", ex.Message, ex);
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void dataGridView1_CellContentClick_1(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            this.Close();
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            //
            if (listView1.SelectedItems == null) return;
            if (listView1.SelectedItems.Count == 0) return;
            if (listView1.SelectedItems[0] == null) return;
            if (listView1.SelectedItems[0].Tag == null) return;

            NaviageObjects(listView1.SelectedItems[0].Tag.ToString(), listView1.SelectedItems[0].Tag);
        }

        private void dataGridView1_CellContentClick_2(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            //try
            //{

            //    for (int i = 0; i < this.dataGridView1.Rows.Count; i++)
            //    {
            //        DataGridViewCell c = this.dataGridView1.Rows[i].Cells["propertyName"];
            //        if (c == null) continue;
            //        if (c.Value == null) continue;
            //        string strCell = c.Value.ToString();
            //        //this.dataGridView1.Rows[i].Visible = true;
            //        if (this.textBox1.Text.Length < 3) continue;
            //        if (this.textBox1.Text == "") continue;

            //        if (strCell.IndexOf(this.textBox1.Text) >= 0)
            //        {
            //            this.dataGridView1.Rows[i].DefaultCellStyle.BackColor = Color.LightYellow;
            //            //this.dataGridView1.Rows[i].Visible = true;
            //        }
            //        else
            //        {
            //            this.dataGridView1.Rows[i].DefaultCellStyle.BackColor = Color.Transparent;
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{

            //}
            //finally
            //{
            //    this.dataGridView1.Update();

            //}
        }


        private void searchText_TextChanged(object sender, EventArgs e)
        {
            string strSrchTxt = "";
            if ((strSrchTxt=searchText.Text).Trim().Length < 3) return;
            int firstRow = -1;
            for (int i = 0; i < this.dataGridView1.Rows.Count; i++)
            {
                string strCellProperNameValue = this.dataGridView1.Rows[i].Cells["propertyName"].Value as string;
                string strCellValue = this.dataGridView1.Rows[i].Cells["objectValue"].Value as string;
                if ((string.IsNullOrEmpty(strCellProperNameValue))&&(string.IsNullOrEmpty(strCellValue))) continue;

                if (((strCellProperNameValue!=null)&&(strCellProperNameValue.IndexOf(strSrchTxt)>=0))
                    ||(((strCellValue != null) && (strCellValue.IndexOf(strSrchTxt) >= 0)))
                    )
                {
                    this.dataGridView1.Rows[i].DefaultCellStyle.BackColor = Color.LightBlue;
                    if (firstRow == -1)
                        firstRow = this.dataGridView1.Rows[i].Index;
                }
                else
                {
                    this.dataGridView1.Rows[i].DefaultCellStyle.BackColor = Color.White;
                }
            }
            if (firstRow >=0)
            {
                //使第一条在最上方
                this.dataGridView1.FirstDisplayedScrollingRowIndex = firstRow;
            }
        }
    }
}
