using MQTTnet;
using Microsoft.Maui.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;
using TarkKoduKK.Data;
namespace TarkKoduKK
{
    public partial class MatrixPage : ContentPage
    {
        private const int MatrixSize = 16;
        private const int CellSize = 20;

        private bool isDrawing = false;
        private MatrixDrawable drawable;
        private Color currentColor = Colors.Red;

        private Point? panStartAbsolute;
        private (int x, int y, Color color)? lastSentPixel = null;

        private IMqttClient mqttClient;

        public MatrixPage()
        {
            InitializeComponent();
            InitializeMqttClient(); 
            drawable = new MatrixDrawable();
            MatrixCanvas.Drawable = drawable;
            MatrixCanvas.Invalidate();
        }

        private void InitializeMqttClient()
        {
            var mqttFactory = new MqttClientFactory();
            mqttClient = mqttFactory.CreateMqttClient();
            ConnectToBroker(); 
        }

        private async void ConnectToBroker()
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
                await mqttClient.ConnectAsync(options);
                Console.WriteLine("Connected to MQTT Broker");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to broker: {ex.Message}");
            }
        }

        private void OnCanvasTapped(object sender, EventArgs e)
        {
            if (e is TappedEventArgs tap)
            {
                var touchPoint = tap.GetPosition(MatrixCanvas);

                if (touchPoint.HasValue)
                {
                    panStartAbsolute = touchPoint.Value;
                    DrawCanvas(touchPoint.Value.X, touchPoint.Value.Y);
                }
            }
        }

        private void OnCanvasPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    isDrawing = true;
                    break;

                case GestureStatus.Running when isDrawing:
                    if (panStartAbsolute is Point start)
                    {
                        double x = start.X + e.TotalX;
                        double y = start.Y + e.TotalY;
                        DrawCanvas(x, y);
                    }
                    break;

                case GestureStatus.Completed:
                case GestureStatus.Canceled:
                    isDrawing = false;
                    panStartAbsolute = null;
                    break;
            }
        }

        private void DrawCanvas(double xPos, double yPos)
        {
            int x = (int)Math.Floor(xPos / CellSize);
            int y = (int)Math.Floor(yPos / CellSize);

            if (x >= 0 && x < MatrixSize && y >= 0 && y < MatrixSize)
            {
                var current = (x, y, currentColor);

                if (lastSentPixel != current)
                {
                    drawable.DrawPixel(x, y, currentColor);
                    MatrixCanvas.Invalidate();
                    lastSentPixel = current;

                    string selectedTopic = MqttBroker.TopicDraw;

                    SendToBroker(x, y, currentColor, selectedTopic);
                }
            }
        }

        private async void SendToBroker(int x, int y, Color color, string topic)
        {
            var message = $"{x},{y},{(int)(color.Red * 255)},{(int)(color.Green * 255)},{(int)(color.Blue * 255)}";
            await PublishMessage(message, topic);
        }

        private async Task PublishMessage(string message, string topic)
        {
            try
            {
                var mqttMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topic) 
                    .WithPayload(message)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                await mqttClient.PublishAsync(mqttMessage);
                Console.WriteLine($"Message sent to topic {topic}: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
            }
        }

        private void OnColorButtonClicked(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                currentColor = button.BackgroundColor;
            }
        }

        private void ClearCanvas(object sender, EventArgs e)
        {
            drawable = new MatrixDrawable();
            MatrixCanvas.Drawable = drawable;
            MatrixCanvas.Invalidate();
        }
        private void FillCanvas(object sender, EventArgs e)
        {
            for (int x = 0; x < MatrixSize; x++)
            {
                for (int y = 0; y < MatrixSize; y++)
                {
                    drawable.DrawPixel(x, y, currentColor);
                }
            }        
            MatrixCanvas.Invalidate();
            PublishMessage("Fill" + currentColor, MqttBroker.TopicCommands);
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
                "Test connection" => new MainPage(),
                "Matrix" => new MatrixPage(),
                "Strip" => new StripPage(),
                _ => null
            };

            if (targetPage != null && Navigation.NavigationStack.LastOrDefault()?.GetType() != targetPage.GetType())
            {
                await Navigation.PushAsync(targetPage);
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
