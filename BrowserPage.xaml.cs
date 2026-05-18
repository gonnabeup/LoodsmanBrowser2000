using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Ascon.Plm.Loodsman.PluginSDK;


namespace LoodsmanBrowser2000
{
    public partial class BrowserPage : Page
    {
        private readonly MainWindow _mainWindow;

        private readonly INetPluginCall _npc;

        // нажатые кнопки
        private readonly HashSet<Key> _pressedKeys = new HashSet<Key>();

        // кеш иконок типов
        private readonly Dictionary<string, ImageSource> _typeIcons =
            new Dictionary<string, ImageSource>();

        // кеш иконок состояний
        private readonly Dictionary<string, ImageSource> _stateIcons =
            new Dictionary<string, ImageSource>();

        // модель аттрибута
        public class AttributeItem
        {
            public string Name { get; set; }

            public string Value { get; set; }

            public bool IsObligatory { get; set; }
        }

        // модель данных элемента дерева
        public class TreeItemData
        {
            public int IdVersion { get; set; }

            public string TypeName { get; set; }
        }

        // модель файла
        public class FileItem
        {
            public string Name { get; set; }
            public string LocalPath { get; set; }
            public int IdVersion { get; set; }
            public string Size { get; set; }
        }
        
        // конструктор
        public BrowserPage(INetPluginCall npc, MainWindow mainWindow)
        {
            InitializeComponent();

            _npc = npc;
            _mainWindow = mainWindow;

            Loaded += BrowserPage_Loaded;
            tvObjects.SelectedItemChanged += TvObjects_SelectedItemChanged;
            dgFiles.MouseDoubleClick += DgFiles_MouseDoubleClick;
            HideFilesArea();
            PreviewKeyDown += Page_PreviewKeyDown;
            PreviewKeyUp += Page_PreviewKeyUp;
            Loaded += (_, __) => Keyboard.Focus(this);
        }

        // открытие файлов двойным нажатием
        private void DgFiles_MouseDoubleClick(
            object sender,
            System.Windows.Input.MouseButtonEventArgs e
        )
        {
            try
            {
                var file = dgFiles.SelectedItem as FileItem;

                if (file == null)
                    return;

                string extractedFile = _npc.RunMethod(
                        "ExtractFile",
                        new object[] { "", "", "", file.IdVersion, file.Name, file.LocalPath, 0 }
                    )
                    ?.ToString();

                if (string.IsNullOrWhiteSpace(extractedFile))
                {
                    MessageBox.Show("Не удалось извлечь файл");

                    return;
                }

                Process.Start(
                    new ProcessStartInfo { FileName = extractedFile, UseShellExecute = true }
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        /*private void LoadRootObjects()
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
        // получение корневых объектов через GetProjectList2

        // создание элементов дерева
        private TreeViewItem CreateTreeItem(DataRow row, int idVersion)
        {
            string product = row["_PRODUCT"]?.ToString().Trim() ?? "";
            string version = row["_VERSION"]?.ToString().Trim() ?? "";
            string type = row["_TYPE"]?.ToString().Trim() ?? "";
            string name = GetObjectName(idVersion);
            string state = row["_STATE"]?.ToString().Trim() ?? "";
            int accessLevel =
                row["_ACCESSLEVEL"] != DBNull.Value ? Convert.ToInt32(row["_ACCESSLEVEL"]) : 0;
            string colorName = GetAccessColor(accessLevel);

            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            // круг - индикатор прав доступа
            if (0 < accessLevel && accessLevel <= 3)
            {
                panel.Children.Add(
                    new Ellipse
                    {
                        Width = 12,
                        Height = 12,
                        Fill = (System.Windows.Media.Brush)
                            new BrushConverter().ConvertFromString(colorName),
                        Margin = new Thickness(0, 0, 4, 0),
                    }
                );
            }
            /*var iconName = GetAccessIcon(accessLevel);
            var assemblyPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var path = System.IO.Path.Combine(
                assemblyPath,
                "Resources",
                iconName);
            var accessIcon = new Image
            {
                Source = new BitmapImage(new Uri(path)),
                Width = 16,
                Height = 16,
                Margin = new Thickness(0, 0, 5, 0)
            };
            */
            // сторонние иконки из папки

            // иконка типа
            if (_typeIcons.ContainsKey(type))
            {
                panel.Children.Add(
                    new Image
                    {
                        Source = _typeIcons[type],
                        Width = 16,
                        Height = 16,
                        Margin = new Thickness(0, 0, 4, 0),
                    }
                );
            }

            // иконка состояния
            if (_stateIcons.ContainsKey(state))
            {
                panel.Children.Add(
                    new Image
                    {
                        Source = _stateIcons[state],
                        Width = 16,
                        Height = 16,
                        Margin = new Thickness(0, 0, 6, 0),
                    }
                );
            }
            var text = new TextBlock
            {
                Text =
                    $"{product}"
                    + $"{(string.IsNullOrEmpty(name) ? "" : $" - {name}")}"
                    + $"{(string.IsNullOrEmpty(version) ? "" : $", версия {version}")}",
            };
            panel.Children.Add(text);

            var tvi = new TreeViewItem
            {
                Header = panel,
                Tag = new TreeItemData { IdVersion = idVersion, TypeName = type },
            };
            // делаем расширяемым

            tvi.Expanded += TreeItem_Expanded;

            return tvi;
        }

        // получаем наименование объекта
        private string GetObjectName(int idVersion)
        {
            try
            {
                var dt = _npc.GetDataTable(
                    "GetInfoAboutVersion",
                    new object[] { "", "", "", idVersion, 2 }
                );

                if (dt == null)
                    return "";

                foreach (DataRow row in dt.Rows)
                {
                    string attrName = row["_NAME"]?.ToString() ?? "";

                    if (attrName == "Наименование")
                    {
                        return row["_VALUE"]?.ToString() ?? "";
                    }
                }
            }
            catch { }

            return "";
        }

        // проверка есть ли у документа вложенные файлы
        private bool hasFiles(int idVersion)
        {
            var dt = _npc.GetDataTable(
                "GetInfoAboutVersion",
                new object[] { "", "", "", idVersion, 7 }
            );

            if (dt == null || dt.Rows.Count == 0)
                return false;
            else
                return true;
        }

        // проверка, документ ли объект
        private bool IsDocument(int idVersion)
        {
            try
            {
                var dt = _npc.GetDataTable(
                    "GetInfoAboutVersion",
                    new object[] { "", "", "", idVersion, 15 }
                );

                if (dt == null || dt.Rows.Count == 0)
                    return false;

                int isDocument = Convert.ToInt32(dt.Rows[0]["_DOCUMENT"]);

                return isDocument == 1;
            }
            catch
            {
                return false;
            }
        }

        /*Загрузчики*/

        // загружаем корневые объекты
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

        // загружаем файлы
        private void LoadFiles(TreeItemData data)
        {
            try
            {
                dgFiles.ItemsSource = null;

                var dt = _npc.GetDataTable(
                    "GetInfoAboutVersion",
                    new object[] { "", "", "", data.IdVersion, 7 }
                );

                if (dt == null)
                    return;

                var files = new List<FileItem>();

                foreach (DataRow row in dt.Rows)
                {
                    string name = row["_NAME"]?.ToString() ?? "";

                    string localPath = row["_LOCALNAME"]?.ToString() ?? "";

                    long sizeBytes =
                        row["_SIZE"] != DBNull.Value ? Convert.ToInt64(row["_SIZE"]) : 0;

                    files.Add(
                        new FileItem
                        {
                            Name = name,

                            LocalPath = localPath,

                            IdVersion = data.IdVersion,

                            Size = $"{sizeBytes / 1024} KB",
                        }
                    );
                }

                dgFiles.ItemsSource = files;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        // загружаем иконки типов из ЛОЦМАН
        private void LoadTypeIcons()
        {
            try
            {
                var dt = _npc.GetDataTable("GetTypeList", new object[] { });

                if (dt == null)
                    return;

                foreach (DataRow row in dt.Rows)
                {
                    string typeName = row["_TYPENAME"]?.ToString() ?? "";

                    // поле _ICON уже содержит картинку
                    if (row["_ICON"] != DBNull.Value)
                    {
                        byte[] blob = (byte[])row["_ICON"];

                        var image = LoadImageFromBytes(blob);

                        if (image != null)
                        {
                            _typeIcons[typeName] = image;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        // загружаем иконки состояний из ЛОЦМАН
        private void LoadStateIcons()
        {
            try
            {
                var dt = _npc.GetDataTable("GetStateList", new object[] { });

                if (dt == null)
                    return;

                foreach (DataRow row in dt.Rows)
                {
                    string stateName = row["_NAME"]?.ToString() ?? "";

                    if (row["_ICON"] != DBNull.Value)
                    {
                        byte[] blob = (byte[])row["_ICON"];

                        var image = LoadImageFromBytes(blob);

                        if (image != null)
                        {
                            _stateIcons[stateName] = image;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        // загружаем изображение из блоб
        private ImageSource LoadImageFromBytes(byte[] bytes)
        {
            try
            {
                using (var ms = new MemoryStream(bytes))
                {
                    // грузим bitmap из blob
                    using (var bitmap = new System.Drawing.Bitmap(ms))
                    {
                        // цвет прозрачности (розовый) делаем прозрачным
                        bitmap.MakeTransparent(System.Drawing.Color.Magenta);

                        using (var pngStream = new MemoryStream())
                        {
                            // сохраняем уже как png
                            bitmap.Save(pngStream, System.Drawing.Imaging.ImageFormat.Png);

                            pngStream.Position = 0;

                            var image = new BitmapImage();

                            image.BeginInit();
                            image.CacheOption = BitmapCacheOption.OnLoad;
                            image.StreamSource = pngStream;
                            image.EndInit();

                            image.Freeze();

                            return image;
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        // загружаем атрибуты
        private void LoadAttributes(int idVersion, string typeName)
        {
            try
            {
                var dtTypeAttrs = _npc.GetDataTable(
                    "GetInfoAboutType",
                    new object[] { typeName, 1 }
                ); // получаем все аттрибуты типа выбранного объекта
                var dtAttrValues = _npc.GetDataTable(
                    "GetInfoAboutVersion",
                    new object[] { "", "", "", idVersion, 2 }
                ); // получаем заполненные аттрибуты выбранного объекта

                if (dtTypeAttrs == null) // проверяем наличие атрибутов у типа
                    return;

                var valueMap = new Dictionary<string, string>(); // словарь для атрибутов со значениями

                foreach (DataRow row in dtAttrValues.Rows)
                {
                    string name = row["_NAME"]?.ToString() ?? ""; // название атрибута

                    string value = row["_VALUE"]?.ToString() ?? ""; // значение

                    valueMap[name] = value;
                } // сначала закидываем названия и значения всех заполненных атрибутов в словарь

                var attributes = new List<AttributeItem>(); // общий список атрибутов для выбранного объекта

                foreach (DataRow row in dtTypeAttrs.Rows)
                {
                    string name = row["_NAME"]?.ToString() ?? ""; // название атрибута

                    bool obligatory = Convert.ToInt32(row["_OBLIGATORY"]) == 1; // обязательность атрибута

                    string value = valueMap.ContainsKey(name) ? valueMap[name] : ""; // если в словаре атрибутов со значениями есть
                    // атрибут с таким названием,
                    // берем его,
                    // если нет его,
                    // то оставляем значение пустым

                    attributes.Add(
                        new AttributeItem
                        {
                            Name = obligatory ? $"{name} *" : name,
                            Value = value,
                            IsObligatory = obligatory,
                        }
                    ); // записываем в список атрибутов пару название значение
                }
                attributes = attributes
                    .OrderByDescending(a => a.IsObligatory)
                    .ThenBy(a => a.Name)
                    .ToList(); // сортируем атрибуты по обязательности и названию в алфавитном
                dgAttributes.ItemsSource = attributes; // выводим атрибуты в датагрид
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        // загружаем детей
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
                    + "Изготавливается из ..."; // список связей, по которым будем выводить детей

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

                    child.Items.Add(null);

                    parentItem.Items.Add(child);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private GridLength _savedFilesRowHeight = new GridLength(1, GridUnitType.Star);

        private void HideFilesArea()
        {
            _savedFilesRowHeight = rowFilesHost.Height;

            borderFiles.Visibility = Visibility.Collapsed;
            splitterFiles.Visibility = Visibility.Collapsed;

            rowFilesHost.Height = new GridLength(0);
            rowFilesHost.MinHeight = 0;

            dgFiles.ItemsSource = null;
        }

        private void ShowFilesArea()
        {
            borderFiles.Visibility = Visibility.Visible;
            splitterFiles.Visibility = Visibility.Visible;

            rowFilesHost.MinHeight = 200;

            if (rowFilesHost.Height.Value == 0)
                rowFilesHost.Height =
                    _savedFilesRowHeight.Value > 0
                        ? _savedFilesRowHeight
                        : new GridLength(1, GridUnitType.Star);

            if (rowFilesHost.Height.Value == 0)
                rowFilesHost.Height = new GridLength(1, GridUnitType.Star);
        }

        /*Обработчики*/

        // Обработчик загрузки страницы
        private void BrowserPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadStateIcons();
            LoadTypeIcons();
            LoadRootObjects();
        }

        // обработчик раскрытия объекта treeview
        private void TreeItem_Expanded(object sender, RoutedEventArgs e)
        {
            var tvi = sender as TreeViewItem;
            if (tvi.Items.Count == 1 && tvi.Items[0] == null) // Загружаем только при первом раскрытии
            {
                tvi.Items.Clear();
                LoadChildren(tvi);
            }
        }

        // обработчик события смены выбранного объекта дерева(выбора другого объекта)
        private void TvObjects_SelectedItemChanged(
            object sender,
            RoutedPropertyChangedEventArgs<object> e
        )
        {
            try
            {
                var selectedItem = tvObjects.SelectedItem as TreeViewItem; // узнаем у тривью какой объект выбран

                if (selectedItem == null) // проверяем что хоть что то выбрано
                    return;

                var data = (TreeItemData)selectedItem.Tag; // дергаем данные о элементе дерева спрятанные в тэг

                int idVersion = data.IdVersion; //
                string typeName = data.TypeName; //
                if (IsDocument(idVersion) && hasFiles(idVersion))
                {
                    ShowFilesArea();
                    LoadAttributes(idVersion, typeName);
                    LoadFiles(data);
                }
                else
                {
                    HideFilesArea();
                    LoadAttributes(idVersion, typeName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        // обработчик события нажатия на клавишу
        private void Page_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            _pressedKeys.Add(e.Key);

            if (IsFiveKeyComboPressed())
            {
                _mainWindow.MainFrame.Navigate(new DbConnectionPage(_mainWindow));
                e.Handled = true;
            }
        }

        // обработчик события отжатия клавишы
        private void Page_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            _pressedKeys.Remove(e.Key);
        }

        /*Прочее*/

        // цвета иконок
        private string GetAccessColor(int accessLevel)
        {
            switch (accessLevel)
            {
                case 1:
                    return "Gray"; // только чтение

                case 2:
                    return "Green"; // чтение и запись

                case 3:
                    return "Blue"; // полный доступ

                default:
                    return "";
            }
        }

        // чит-код на меню выбора баз
        private bool IsFiveKeyComboPressed()
        {
            return _pressedKeys.Contains(Key.A)
                && _pressedKeys.Contains(Key.D)
                && _pressedKeys.Contains(Key.M)
                && _pressedKeys.Contains(Key.I)
                && _pressedKeys.Contains(Key.N);
        }
    }
}
