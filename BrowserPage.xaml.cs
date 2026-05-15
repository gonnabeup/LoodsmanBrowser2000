using System;
using System.Collections.Generic;
using System.Data;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Ascon.Plm.Loodsman.PluginSDK;

namespace LoodsmanBrowser2000
{
    public partial class BrowserPage : Page
    {
        private readonly INetPluginCall _npc;

        public class AttributeItem
        {
            public string Name { get; set; }

            public string Value { get; set; }
        }

        public class TreeItemData
        {
            public int IdVersion { get; set; }

            public string TypeName { get; set; }
        }

        public BrowserPage(INetPluginCall npc)
        {
            InitializeComponent();
            _npc = npc;

            Loaded += BrowserPage_Loaded;
            tvObjects.SelectedItemChanged += TvObjects_SelectedItemChanged;
        }

        private void BrowserPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadRootObjects();
        }

        private void LoadRootObjects()
        {
            tvObjects.Items.Clear();

            try
            {
                var dt = _npc.GetDataTable("GetTree", new object[] { "", "", "", 0, "", false });

                foreach (DataRow row in dt.Rows)
                {
                    int idVersion = Convert.ToInt32(row["_ID_VERSION"]);

                    var tvi = CreateTreeItem(row, idVersion);

                    tvi.Items.Add(null);

                    tvObjects.Items.Add(tvi);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        /*private void LoadRootObjects() // получение корневых объектов через GetProjectList2
        {
            tvObjects.Items.Clear();

            try
            {
                var dtRootObjects = _npc.GetDataTable("GetProjectList2", new object[] { 0 });

                if (dtRootObjects == null || dtRootObjects.Rows.Count == 0)
                {
                    tvObjects.Items.Add(new TreeViewItem { Header = "Проекты не найдены" });
                    return;
                }

                foreach (DataRow row in dtRootObjects.Rows)
                {
                    int idVersion = Convert.ToInt32(row["_ID_VERSION"]);
                    
                    var tvi = CreateTreeItem(row, idVersion);
                    tvi.Items.Add(null);
                    tvObjects.Items.Add(tvi);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка:\n" + ex.Message);
            }
        }*/

        private TreeViewItem CreateTreeItem(DataRow row, int idVersion)
        {
            string product = row["_PRODUCT"]?.ToString().Trim() ?? "";
            string version = row["_VERSION"]?.ToString().Trim() ?? "";
            string type = row["_TYPE"]?.ToString().Trim() ?? "";

            var tvi = new TreeViewItem
            {
                Header = $"{product} [{version}]",
                Tag = new TreeItemData { IdVersion = idVersion, TypeName = type },
            };
            // Делаем расширяемым
            tvi.Expanded += TreeItem_Expanded;

            return tvi;
        }

        private void TreeItem_Expanded(object sender, RoutedEventArgs e)
        {
            var tvi = sender as TreeViewItem;
            if (tvi.Items.Count == 1 && tvi.Items[0] == null) // Загружаем только при первом раскрытии
            {
                tvi.Items.Clear();
                LoadChildren(tvi);
            }
        }

        private void TvObjects_SelectedItemChanged(
            object sender,
            RoutedPropertyChangedEventArgs<object> e
        )
        {
            try
            {
                var selectedItem = tvObjects.SelectedItem as TreeViewItem;

                if (selectedItem == null)
                    return;

                var data = (TreeItemData)selectedItem.Tag;
                
                int idVersion = data.IdVersion;
                string typeName = data.TypeName;
                
                LoadAttributes(idVersion, typeName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void LoadAttributes(int idVersion, string typeName)
        {
            try
            {
                var dtTypeAttrs = _npc.GetDataTable(
                    "GetInfoAboutType",
                    new object[] { typeName, 1 }
                );
                var dtAttrValues = _npc.GetDataTable(
                    "GetInfoAboutVersion",
                    new object[] { "", "", "", idVersion, 3 }
                ); // получаем аттрибуты выбранного объекта

                var valueMap = new Dictionary<string, string>();

                foreach (DataRow row in dtAttrValues.Rows)
                {
                    string name = row["_NAME"]?.ToString() ?? "";

                    string value = row["_VALUE"]?.ToString() ?? "";

                    valueMap[name] = value;
                }

                if (dtTypeAttrs == null)
                    return;

                var attributes = new List<AttributeItem>();

                foreach (DataRow row in dtTypeAttrs.Rows)
                {
                    string name = row["_NAME"]?.ToString() ?? "";

                    bool obligatory = Convert.ToInt32(row["_OBLIGATORY"]) == 1;

                    string value = valueMap.ContainsKey(name) ? valueMap[name] : "";

                    attributes.Add(
                        new AttributeItem
                        {
                            Name = obligatory ? $"{name} *" : name,

                            Value = value
                        }
                    );
                }
                dgAttributes.ItemsSource = attributes;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void LoadChildren(TreeViewItem parentItem)
        {
            try
            {
                char separator = (char)1;

                var data = (TreeItemData)parentItem.Tag;
                int idVersion = data.IdVersion;

                string linkTypes =
                    "Документы"
                    + separator
                    + "Состоит из ..."
                    + separator
                    + "Изготавливается из ...";

                /*var dt = _npc.GetDataTable("GetLObjs", new object[] { idVersion, false });*/
                // для получения всех объектах вне зависимости от типа связи
                var dt = _npc.GetDataTable(
                    "GetTree",
                    new object[] { "", "", "", idVersion, linkTypes, false }
                );

                if (dt == null || dt.Rows.Count == 0)
                    return;

                foreach (DataRow row in dt.Rows)
                {
                    int childId = Convert.ToInt32(row["_ID_VERSION"]);

                    string product = row["_PRODUCT"]?.ToString() ?? "";

                    var child = CreateTreeItem(row, childId);

                    child.Header = $"{product}";

                    child.Items.Add(null);

                    parentItem.Items.Add(child);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}
