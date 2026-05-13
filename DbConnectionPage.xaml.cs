using Ascon.Plm.Loodsman.PluginSDK;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection.Emit;
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

namespace LoodsmanBrowser2000
{
    /// <summary>
    /// Логика взаимодействия для DbConnectionPage.xaml
    /// </summary>
    public partial class DbConnectionPage : Page
    {
        private readonly MainWindow _mainWindow;
        private readonly INetPluginCall _npc;
        public DbConnectionPage(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _npc = mainWindow.NPC;
            Loaded += DbConnectionPage_Loaded; // проверяем что страница со всеми элементами загрузилась(нулреференс превеншен)
        }
        private void LoadDatabases()
        {
            var res = _npc.RunMethod("GetDbList", new object[] { }); //получаем строку, со списком баз 
            if (res is string dbList && !string.IsNullOrWhiteSpace(dbList))
            {
                var dbs = dbList.Split(new[] { ',' }) //делим строку на массив
                                          .Select(db => db.Trim())
                                          .Where(db => !string.IsNullOrEmpty(db))
                                          .ToList(); //переделываем массив в список

                cbDatabases.ItemsSource = dbs; //вставляем список баз в комбобокс

                if (dbs.Count > 0) 
                    cbDatabases.SelectedIndex = 0; //автоматом выбираем первую базу из списка 
            }
            else
            {
                MessageBox.Show("Невозможно получить список баз", "проблм"); 
            }
        }
        private void DbConnectionPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDatabases();
        }
        
        private void ConnectButton_Click(object sender, RoutedEventArgs e) 
        {
            if (cbDatabases.SelectedItem == null) //вряд ли это возможно учитывая то, что я кодом выбрал первый элемент, но кто знает этих ползунов
            
            {
                MessageBox.Show("Выберите базу!", "Внимание!");
            }

            string dbName = cbDatabases.SelectedItem.ToString(); //дергаем имя выбранной базы
            string user = tbUser.Text.Trim(); //введенный юзернейм
            string password = pbPassword.Password; //пароль

            try
            {
                if (slAuthType.Value == 1) //тип аутентификации sql
                {
                    _npc.RunMethod("ConnectToDBEx", dbName, user, password); //подключаемся к базе
                    var res = _npc.RunMethod("CurrentBase"); //узнаем к какой базе мы подключены
                    if (dbName == (string)res)
                    {
                        _mainWindow.MainFrame.Navigate(new BrowserPage(_npc)); //едем дальше
                    }
                }
                else //тип аутентификации windows
                {
                    _npc.RunMethod("ConnectToDB", dbName); //подключаемся к базе
                    var res = _npc.RunMethod("CurrentBase"); //узнаем к какой базе мы подключены
                    if (dbName == (string)res)
                    {
                        _mainWindow.MainFrame.Navigate(new BrowserPage(_npc)); //едем дальше
                    }
                    else
                    {
                        MessageBox.Show("Пользователь с такими данными не зарегистрирован в базе", "Ошибка Windows аутентификации");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка подключения");
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;
            spSQLCredentials.IsEnabled = slAuthType.Value > 0;
        }
    }
}
