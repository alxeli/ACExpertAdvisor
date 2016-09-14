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
        bool isServerRunning = false;

        public MainWindow()
        {
            InitializeComponent();
            uxStartTrading.IsEnabled = false;

            //Set event handler for recieving ACExpert server messages
            client.ServerMessage += delegate (NamedPipeConnection<string, string> conn, string message)
            {
                InterpretServerMessage(conn, message);
            };

            client.Start();
        }
        void InterpretServerMessage(NamedPipeConnection<string, string> conn, string message)
        {
            switch (message)
            {
                case "!s_start":
                    {
                        isServerRunning = true;
                        uxStartTrading_Toggle(true);
                    }
                    break;
                case "!s_stop":
                    {
                        isServerRunning = false;
                        uxStartTrading_Toggle(false);
                    }
                    break;
                default:
                    {
                        DisplayToConsole(message + "\n");
                    }
                    break;
            }
        }

        private void uxStartTrading_Toggle(bool status)
        {
            Dispatcher.Invoke(() =>
            {
                uxStartTrading.IsEnabled = status;
            });
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
                client.PushMessage("!c_start");
                uxStartTrading.Content = "Stop Trading";
                DisplayToConsole("Trading Started.\n");
            }
            else
            {
                client.PushMessage("!c_stop");
                uxStartTrading.Content = "Start Trading";
                DisplayToConsole("Trading Stopped.\n");
            }
            
            isTrading = !isTrading;
        }
    }
}
