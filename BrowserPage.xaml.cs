using Ascon.Plm.Loodsman.PluginSDK;
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

namespace LoodsmanBrowser2000
{
    /// <summary>
    /// Логика взаимодействия для BrowserPage.xaml
    /// </summary>
    public partial class BrowserPage : Page
    {
        private readonly INetPluginCall _npc;
        public BrowserPage(INetPluginCall npc)
        {
            InitializeComponent();
            _npc = npc;
        }
        
    }
}
