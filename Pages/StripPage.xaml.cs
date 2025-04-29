using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using Microsoft.Maui.Controls;
using TarkKoduKK.Data;

namespace TarkKoduKK
{
    public partial class StripPage : ContentPage
    {
        private IMqttClient mqttClient;


        public StripPage()
        {
            InitializeComponent();
            InitializeMqttClient();
        }

        private void InitializeMqttClient()
        {
            var mqttFactory = new MqttClientFactory();
            mqttClient = mqttFactory.CreateMqttClient();
        }

        private async void OnSendMessageClicked(object sender, EventArgs e)
        {
            if (!mqttClient.IsConnected)
            {
                await ConnectToBroker();
            }

            if (mqttClient.IsConnected)
            {
                await PublishMessage("1,15,255,0,0", MqttBroker.TopicDrow);
            }
            else
            {
                await DisplayAlert("Ошибка", "Не удалось подключиться к брокеру", "OK");
            }
        }

        private async Task ConnectToBroker()
        {
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(MqttBroker.Broker, MqttBroker.Port)
                .WithCredentials(MqttBroker.Username, MqttBroker.Password)
                .WithTlsOptions(new MqttClientTlsOptions
                {
                    UseTls = true,
                    IgnoreCertificateRevocationErrors = true,
                    AllowUntrustedCertificates = true
                })
                .Build();
            try
            {
                await mqttClient.ConnectAsync(options, CancellationToken.None);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось подключиться: {ex.Message}", "OK");
            }
        }

        private async Task PublishMessage(string message, string Topic)
        {
            try
            {
                var msg = new MqttApplicationMessageBuilder()
                    .WithTopic(Topic)
                    .WithPayload(message)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                await mqttClient.PublishAsync(msg, CancellationToken.None);
                await DisplayAlert("Успех", "Сообщение отправлено!", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось отправить сообщение: {ex.Message}", "OK");
            }
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            if (mqttClient.IsConnected)
            {
                await mqttClient.DisconnectAsync();
            }
        }
        private void OnMenuButtonClicked(object sender, EventArgs e)
        {
            DropdownMenu.IsVisible = !DropdownMenu.IsVisible;
        }

        private async void OnMenuItemClicked(object sender, EventArgs e)
        {
            var button = (Button)sender;
            string pageName = button.Text;

            DropdownMenu.IsVisible = false;

            Page targetPage = pageName switch
            {
                "Test Connection" => new MainPage(),
                "Matrix" => new MatrixPage(),
                "LED strip" => new StripPage(),
                _ => null
            };

            if (targetPage != null && Navigation.NavigationStack.LastOrDefault()?.GetType() != targetPage.GetType())
            {
                await Navigation.PushAsync(targetPage);
            }
        }
    }

}
