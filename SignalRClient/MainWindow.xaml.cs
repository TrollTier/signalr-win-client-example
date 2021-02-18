using Microsoft.AspNetCore.SignalR.Client;
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

namespace SignalRClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        HubConnection connection;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async Task EstablishConnection()
        {
            connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:5001/chatHub")
                .WithAutomaticReconnect()
                .Build();

            connection.On<string, string>("ReceiveMessage", (user, message) =>
            {
                Dispatcher.Invoke(() =>
                {
                    lstMessages.Items.Add($"{user}: {message}");
                });
            });

            connection.Reconnecting += error =>
            {
                Dispatcher.Invoke(() =>
                {
                    lstStates.Items.Add("Connection lost, trying to reconnect");
                });

                return Task.CompletedTask;
            };

            connection.Reconnected += message =>
            {
                Dispatcher.Invoke(() =>
                {
                    lstStates.Items.Add("Connection reestablished");
                });

                return Task.CompletedTask;
            };

            connection.Closed += message =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    lstStates.Items.Add("Connection lost");
                });

                return Task.CompletedTask;
            };

            bool connectionEstablished = false;
            int retrySeconds = 5;

            do
            {
                try
                {
                    Dispatcher.Invoke(() => lstStates.Items.Add("Trying to establish connection..."));
                    await connection.StartAsync();

                    Dispatcher.Invoke(() => lstStates.Items.Add("Connection established..."));
                    connectionEstablished = true;
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => lstStates.Items.Add($"Could not establish connection: {Environment.NewLine}{ex.Message}"));
                    Dispatcher.Invoke(() => lstStates.Items.Add($"Will try again in {retrySeconds} Seconds"));
                }

                if (!connectionEstablished)
                    await Task.Delay(retrySeconds * 1000);

            } while (!connectionEstablished);
        }

        private async void cmdSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await connection.InvokeAsync("SendMessage", "Becky Backend", txtInput.Text ?? String.Empty, new { Id = Guid.NewGuid() });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await EstablishConnection();
        }
    }
}
