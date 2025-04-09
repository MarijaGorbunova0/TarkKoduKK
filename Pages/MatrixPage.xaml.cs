using MQTTnet;
using Microsoft.Maui.Controls;
using TarkKoduKK.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;

namespace TarkKoduKK
{
    public partial class MatrixPage : ContentPage
    {
        private IMqttClient mqttClient;
        private const int MatrixSize = 16;
        private const int CellSize = 20;
        private bool isDrawing = false;
        private MatrixDrawable drawable;
        private Color currentColor = Colors.Red;

        private readonly ConcurrentQueue<string> messageQueue = new();
        private readonly HashSet<string> sentRecently = new();
        private CancellationTokenSource publishLoopCts;

        public MatrixPage()
        {
            InitializeComponent();
            InitializeMqttClient();
            _ = ConnectToBroker();

            drawable = new MatrixDrawable();
            MatrixCanvas.Drawable = drawable;
            MatrixCanvas.Invalidate();

            StartPublishLoop();
        }

        private void InitializeMqttClient()
        {
            var mqttFactory = new MqttClientFactory();
            mqttClient = mqttFactory.CreateMqttClient();
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
                ConnectionStatus.Text = "Подключено ✅";
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось подключиться: {ex.Message}", "OK");
            }
        }

        private void OnCanvasTapped(object sender, EventArgs e)
        {
            var position = e as TappedEventArgs;
            var touchPoint = position?.GetPosition(MatrixCanvas); 

            if (touchPoint.HasValue) 
            {
                double xPos = touchPoint.Value.X;  
                double yPos = touchPoint.Value.Y;  
                DrawCanvas(xPos, yPos);
            }
        }

        private void OnCanvasPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            if (e.StatusType == GestureStatus.Started)
            {
                isDrawing = true;
            }
            if (e.StatusType == GestureStatus.Running && isDrawing)
            {
                DrawCanvas(e.TotalX, e.TotalY);
            }
            if (e.StatusType == GestureStatus.Completed || e.StatusType == GestureStatus.Canceled)
            {
                isDrawing = false;
            }
        }

        private void DrawCanvas(double xPos, double yPos)
        {
            int x = (int)Math.Floor(xPos / CellSize);
            int y = (int)Math.Floor(yPos / CellSize);

            if (x >= 0 && x < MatrixSize && y >= 0 && y < MatrixSize)
            {
                drawable.DrawPixel(x, y, currentColor);
                MatrixCanvas.Invalidate();

                int r = (int)(currentColor.Red * 255);
                int g = (int)(currentColor.Green * 255);
                int b = (int)(currentColor.Blue * 255);

                string message = $"{x},{y},{r},{g},{b}";
                EnqueueMessage(message);
            }
        }

        private void EnqueueMessage(string message)
        {
            if (sentRecently.Contains(message)) return;

            messageQueue.Enqueue(message);
            sentRecently.Add(message);

            _ = Task.Delay(500).ContinueWith(_ => sentRecently.Remove(message));
        }

        private void StartPublishLoop()
        {
            publishLoopCts = new CancellationTokenSource();
            var token = publishLoopCts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    int maxPerCycle = 10;
                    int sentCount = 0;

                    while (sentCount < maxPerCycle && messageQueue.TryDequeue(out string message))
                    {
                        if (mqttClient != null && mqttClient.IsConnected)
                        {
                            try
                            {
                                var msg = new MqttApplicationMessageBuilder()
                                    .WithTopic(MqttBroker.Topic)
                                    .WithPayload(message)
                                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                                    .Build();

                                await mqttClient.PublishAsync(msg, CancellationToken.None);
                                sentCount++;
                            }
                            catch
                            {
                            }
                        }
                    }

                    await Task.Delay(2);
                }
            }, token);
        }

        private void OnColorButtonClicked(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                currentColor = button.BackgroundColor;
                ConnectionStatus.Text = $"Выбран цвет";
            }
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            publishLoopCts?.Cancel();

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
