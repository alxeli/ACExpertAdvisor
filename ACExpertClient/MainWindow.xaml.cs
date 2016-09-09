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
using NamedPipeWrapper;

namespace ACExpertClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        NamedPipeClient<string> client = new NamedPipeClient<string>("Demo123");
        bool isTrading = true;

        public MainWindow()
        {
            InitializeComponent();

            //Set event handler for recieving ACExpert server messages
            client.ServerMessage += delegate (NamedPipeConnection<string, string> conn, string message)
            {
                DisplayToConsole(message + "\n");
            };

            client.Start();
        }

        /// <summary>
        ///     displays a message to the ACExpert client console
        /// </summary>
        private void DisplayToConsole(string text)
        {
            Dispatcher.Invoke(() =>
            {
                uxTextbox.AppendText(text);
                uxTextbox.ScrollToEnd();
            });
        }

        /// <summary>
        ///     event handler for start/stop trading button
        /// </summary>
        private void uxStartTrading_Click(object sender, RoutedEventArgs e)
        {
            if (!isTrading)
            {
                client.PushMessage("!start");
                uxStartTrading.Content = "Stop Trading";
                DisplayToConsole("Trading Started.\n");
            }
            else
            {
                client.PushMessage("!stop");
                uxStartTrading.Content = "Start Trading";
                DisplayToConsole("Trading Stopped.\n");
            }

            isTrading = !isTrading;
        }
    }
}
