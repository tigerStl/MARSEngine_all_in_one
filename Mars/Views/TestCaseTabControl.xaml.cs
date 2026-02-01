using Mars.Views.baseView;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Mars.Views
{
    /// <summary>
    /// Interaction logic for TestCaseTabControl.xaml
    /// </summary>
    public partial class TestCaseTabControl : MarsBaseViewControl
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestCaseTabControl));
        private List<TabItem> _tabItems;
        private TabItem _tabAdd = null;

        public TestCaseTabControl()
        {
            InitializeComponent();
            try
            {
                // initialize tabItem array
                _tabItems = new List<TabItem>();

                // add a tabItem with + in header 
                //   _tabAdd = new TabItem();
                //   _tabAdd.Header = "+";
                // tabAdd.MouseLeftButtonUp += new MouseButtonEventHandler(tabAdd_MouseLeftButtonUp);

                //   _tabItems.Add(_tabAdd);

                // add first tab
                //   this.AddTabItem();

                // bind tab control
                tabDynamic.DataContext = _tabItems;

                tabDynamic.SelectedIndex = 0;

                base.Title = "TCs";

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private TabItem AddTabItem()
        {
            int count = _tabItems.Count;

            // create new tab item
            TabItem tab = new TabItem();

            tab.Header = string.Format("Tab {0}", count);
            tab.Name = string.Format("tab{0}", count);
            tab.HeaderTemplate = tabDynamic.FindResource("TabHeader") as DataTemplate;

            tab.MouseDoubleClick += new MouseButtonEventHandler(tab_MouseDoubleClick);

            // add controls to tab item, this case I added just a textbox
            TextBox txt = new TextBox();
            txt.Name = "txt";

            tab.Content = txt;

            // insert tab item right before the last (+) tab item
            _tabItems.Insert(count - 1, tab);

            return tab;
        }

        private void tabAdd_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // clear tab control binding
            tabDynamic.DataContext = null;

            TabItem tab = this.AddTabItem();

            // bind tab control
            tabDynamic.DataContext = _tabItems;

            // select newly added tab item
            tabDynamic.SelectedItem = tab;
        }

        private void tab_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TabItem tab = sender as TabItem;
            /*
            TabProperty dlg = new TabProperty();

            // get existing header text
            dlg.txtTitle.Text = tab.Header.ToString();

            if (dlg.ShowDialog() == true)
            {
                // change header text
                tab.Header = dlg.txtTitle.Text.Trim();
            }
             */
        }

        private void tabDynamic_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabItem tab = tabDynamic.SelectedItem as TabItem;
            if (tab == null) return;

            if (tab.Equals(_tabAdd))
            {
                // clear tab control binding
                tabDynamic.DataContext = null;

                TabItem newTab = this.AddTabItem();

                // bind tab control
                tabDynamic.DataContext = _tabItems;

                // select newly added tab item
                tabDynamic.SelectedItem = newTab;
            }
            else
            {
                // your code here...
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            string tabName = (sender as Button).CommandParameter.ToString();

            var item = tabDynamic.Items.Cast<TabItem>().Where(i => i.Name.Equals(tabName)).SingleOrDefault();

            TabItem tab = item as TabItem;

            if (tab != null)
            {
                if (_tabItems.Count < 1)
                {
                    MessageBox.Show("Cannot remove last tab.");
                }
                else
                /*
                if (MessageBox.Show(string.Format("Are you sure you want to remove the tab '{0}'?", tab.Header.ToString()),
                "Remove Tab", MessageBoxButton.YesNo) == MessageBoxResult.Yes)*/
                {
                    // get selected tab
                    TabItem selectedTab = tabDynamic.SelectedItem as TabItem;

                    // clear tab control binding
                    tabDynamic.DataContext = null;

                    _tabItems.Remove(tab);

                    // bind tab control
                    tabDynamic.DataContext = _tabItems;

                    // select previously selected tab. if that is removed then select first tab
                    if (selectedTab == null || selectedTab.Equals(tab))
                    {
                        if (_tabItems.Count > 0)
                            selectedTab = _tabItems[0];
                    }
                    tabDynamic.SelectedItem = selectedTab;
                }
            }
        }


        internal void addTestCaseEdit(TestCaseEdit testCaseEdit, string tabName)
        {
            // prevent creation of identical tabs
            try
            {
                var existingTab = (from tt in _tabItems
                                   where tt.Header.Equals(tabName)
                                   select tt).FirstOrDefault();

                if (existingTab != null)
                {
                    existingTab.IsSelected = true;
                    //tabDynamic.SelectedTab = existingTab.inde;
                    return;
                }


                int count = _tabItems.Count;

                // create new tab item
                TabItem tab = new TabItem();

                tab.Header = tabName;
                tab.Name = string.Format("tab{0}", count);
                tab.HeaderTemplate = tabDynamic.FindResource("TabHeader") as DataTemplate;

                tab.MouseDoubleClick += new MouseButtonEventHandler(tab_MouseDoubleClick);


                tab.Content = testCaseEdit;

                // insert tab item right before the last (+) tab item
                //_tabItems.Insert(count , tab);
                tabDynamic.DataContext = null;
                tab.Visibility = System.Windows.Visibility.Visible;
                _tabItems.Add(tab);
                tabDynamic.DataContext = _tabItems;


                tabDynamic.SelectedItem = _tabItems[count]; ;

                base.Title = string.Format("TCs Count:[{0}]", count);
            }catch(Exception e)
            {
                Logger.Error("addTestCaseEdit", e.Message, e);
            }
        }

        private void TabItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            /*
            var tabItem = e.Source as TabItem;

            if (tabItem == null)
                return;

            if (Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed)
            {
                DragDrop.DoDragDrop(tabItem, tabItem, DragDropEffects.All);
            }
             */
        }


        private void TabItem_Drop(object sender, DragEventArgs e)
        {
            /*
            var tabItemTarget = e.Source as TabItem;

            var tabItemSource = e.Data.GetData(typeof(TabItem)) as TabItem;

            if (!tabItemTarget.Equals(tabItemSource))
            {
             //   var tabControl = tabItemTarget.Parent as TabControl;
                var tabControl = tabDynamic;
                int sourceIndex = tabControl.Items.IndexOf(tabItemSource);
                int targetIndex = tabControl.Items.IndexOf(tabItemTarget);



                tabControl.Items.Remove(tabItemSource);
                tabControl.Items.Insert(targetIndex, tabItemSource);

                tabControl.Items.Remove(tabItemTarget);
                tabControl.Items.Insert(sourceIndex, tabItemTarget);
            }
             */
        }

        #region RoutedEvent
        public void HandleChildSignal(object sender, System.Windows.RoutedEventArgs e)
        {
            Console.WriteLine("RoutedEvent!!!");
        }
        #endregion
    }
}
