using MQTTnet;
using Microsoft.Maui.Controls;
using TarkKoduKK.Data;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TarkKoduKK
{
    public partial class MatrixPage : ContentPage
    {
        private IMqttClient mqttClient;

        private const int MatrixSize = 16;
        private const int CellSize = 20;
        private bool isDrawing = false;
        private MatrixDrawable drawable;

        public MatrixPage()
        {
            InitializeComponent();
            InitializeMqttClient();
            _ = ConnectToBroker();

            drawable = new MatrixDrawable();
            MatrixCanvas.Drawable = drawable;

            var touchGesture = new TouchGestureRecognizer();
            touchGesture.TouchPressed += CanvasPressed;
            touchGesture.TouchReleased += CanvasReleased;
            touchGesture.TouchMoved += CanvasDragged;

            MatrixCanvas.GestureRecognizers.Add(touchGesture);
        }

        private void InitializeMqttClient()
        {
            var mqttFactory = new MqttClientFactory();
            mqttClient = mqttFactory.CreateMqttClient(); ;
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

        private async Task PublishMessage(string message)
        {
            if (mqttClient == null || !mqttClient.IsConnected) return;

            try
            {
                ConnectionStatus.Text = $"Сообщение: {message}";
                var msg = new MqttApplicationMessageBuilder()
                    .WithTopic(MqttBroker.Topic)
                    .WithPayload(message)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                await mqttClient.PublishAsync(msg, CancellationToken.None);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось отправить сообщение: {ex.Message}", "OK");
            }
        }

        private void CanvasPressed(object sender, PointerEventArgs e)
        {
            isDrawing = true;
            DrawCanvas(e);
        }

        private void CanvasReleased(object sender, PointerEventArgs e)
        {
            isDrawing = false;
        }

        private async void CanvasDragged(object sender, PointerEventArgs e)
        {
            if (!isDrawing) return;
            DrawCanvas(e);
        }
        private async void DrawCanvas(PointerEventArgs e)
        {
            var position = e.GetPosition(MatrixCanvas);
            if (position == null) return;

            int x = (int)Math.Round(position.Value.X / CellSize);
            int y = (int)Math.Round(position.Value.Y / CellSize);

            if (x >= 0 && x < MatrixSize && y >= 0 && y < MatrixSize)
            {
                var color = Colors.Red;
                drawable.DrawPixel(x, y, color);
                MatrixCanvas.Invalidate();

                int r = (int)(color.Red * 255);
                int g = (int)(color.Green * 255);
                int b = (int)(color.Blue * 255);

                string message = $"{x},{y},{r},{g},{b}";
                await PublishMessage(message);
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
    }

    public class MatrixDrawable : IDrawable
    {
        private readonly Dictionary<(int, int), Color> pixels = new();

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.FillColor = Colors.Black;
            canvas.FillRectangle(dirtyRect);
            canvas.StrokeColor = Colors.Gray;
            canvas.StrokeSize = 1;

            for (int i = 0; i <= 16; i++)
            {
                float pos = i * 20;
                canvas.DrawLine(pos, 0, pos, 320);
                canvas.DrawLine(0, pos, 320, pos);
            }

            foreach (var pixel in pixels)
            {
                canvas.FillColor = pixel.Value;
                canvas.FillRectangle(pixel.Key.Item1 * 20, pixel.Key.Item2 * 20, 20, 20);
            }
        }

        public void DrawPixel(int x, int y, Color color)
        {
            pixels[(x, y)] = color;
        }
    }
}
