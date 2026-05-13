using Ascon.Plm.Loodsman.PluginSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace LoodsmanBrowser2000
{
    [LoodsmanPlugin]
    internal class Plugin : ILoodsmanNetPlugin
    {
        public void BindMenu(IMenuDefinition menu)
        {
            menu.AddMenuItem("LoodsmanBrowser2000#OpenBrowser", OpenBrowser, CanOpenBrowser);
        }
        private void OpenBrowser(INetPluginCall npc)
        {
            var newWindow = new MainWindow(npc);
            newWindow.ShowDialog();
        } 
        private bool CanOpenBrowser(INetPluginCall npc)
        {
            return true;
        }

        public void OnCloseDb()
        {

        }

        public void OnConnectToDb(INetPluginCall npc)
        {

        }

        public void PluginLoad()
        {

        }

        public void PluginUnload()
        {

        }
    }
}
