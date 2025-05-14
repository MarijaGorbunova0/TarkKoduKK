using Microsoft.Maui.Controls.Shapes;
using MQTTnet;
using TarkKoduKK.Data;
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;
using System.Threading;

namespace TarkKoduKK
{
    public partial class UserPage : ContentPage
    {
        private IMqttClient mqttClient;
        private Ellipse colorCircle;

        public UserPage()
        {
            InitializeComponent();
            InitializeColorCircle();
            InitializeMqttClient();
            _ = ConnectAndSubscribe(); 
        }

        private void InitializeColorCircle()
        {

            colorCircle = this.FindByName<Ellipse>("ColorCircle");

            if (colorCircle == null)
            {
                // Создаем круг программно, если он не определен в XAML
                colorCircle = new Ellipse
                {
                    WidthRequest = 150,
                    HeightRequest = 150,
                    Fill = Colors.White,
                    Stroke = Color.FromArgb("#474747"),
                    StrokeThickness = 30,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };

                if (Content is StackLayout layout)
                {
                    layout.Children.Insert(1, colorCircle);
                }
            }
        }

        private void InitializeMqttClient()
        {
            var mqttFactory = new MqttClientFactory();
            mqttClient = mqttFactory.CreateMqttClient();
        }

        private async Task ConnectAndSubscribe()
        {
            try
            {
                if (!mqttClient.IsConnected)
                {
                    await ConnectToBroker();
                }

                if (mqttClient.IsConnected)
                {
                    await SubscribeToTopic(MqttBroker.TopicDraw);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private async Task ProcessColorMessage(string message)
        {
            var parts = message.Split(',');

            if (parts.Length >= 3 &&
                byte.TryParse(parts[0], out var r) &&
                byte.TryParse(parts[1], out var g) &&
                byte.TryParse(parts[2], out var b))
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    colorCircle.Fill = new SolidColorBrush(Color.FromRgb(r, g, b));
                });
            }
        }

        private async Task SubscribeToTopic(string topic)
        {
            try
            {
                var options = new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter(topic)
                    .Build();

                await mqttClient.SubscribeAsync(options, CancellationToken.None);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка подписки", ex.Message, "OK");
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
                throw new Exception($"Ошибка подключения: {ex.Message}");
            }
        }

        private async Task PublishMessage(string message, string topic)
        {
            try
            {
                var msg = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(message)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                await mqttClient.PublishAsync(msg, CancellationToken.None);
                await DisplayAlert("Успех", "Сообщение отправлено!", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка отправки", ex.Message, "OK");
            }
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            try
            {
                if (mqttClient.IsConnected)
                {
                    await mqttClient.DisconnectAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отключения: {ex.Message}");
            }
        }
    }
}