using Newtonsoft.Json;
using System.Text;
using TarkKoduKK.Data;
namespace TarkKoduKK
{
    public partial class RegistrPage : ContentPage
    {
        private const string FirebaseBaseUrl = "https://tarkkodu-b9f41-default-rtdb.europe-west1.firebasedatabase.app/users";

        public RegistrPage()
        {
            InitializeComponent();
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            string kasutajanimi = UsernameEntry.Text?.Trim();
            string parool = PasswordEntry.Text;

            if (string.IsNullOrWhiteSpace(kasutajanimi) || string.IsNullOrWhiteSpace(parool))
            {
                await DisplayAlert("Viga", "Palun sisesta kasutajanimi ja parool.", "OK");
                return;
            }

            string kasutajaId = await GenereeriUnikaalneId();

            var uusKasutaja = new User
            {
                Id = kasutajaId,
                UserName = kasutajanimi,
                UserPassword = parool
            };

            try
            {
                string json = JsonConvert.SerializeObject(uusKasutaja);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var client = new HttpClient();
                var response = await client.PutAsync($"{FirebaseBaseUrl}/{kasutajaId}.json", content);
                response.EnsureSuccessStatusCode();

                await DisplayAlert("Edukas", $"Registreerimine õnnestus!\nSinu ID: {kasutajaId}", "OK");

                UsernameEntry.Text = string.Empty;
                PasswordEntry.Text = string.Empty;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Viga", $"Registreerimine ebaõnnestus: {ex.Message}", "OK");
            }
        }

        private async Task<string> GenereeriUnikaalneId()
        {
            const string tähed = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var juhuslik = new Random();

            using var client = new HttpClient();

            while (true)
            {
                var id = new string(Enumerable.Repeat(tähed, 7)
                    .Select(s => s[juhuslik.Next(s.Length)]).ToArray());

                var response = await client.GetAsync($"{FirebaseBaseUrl}/{id}.json");
                var content = await response.Content.ReadAsStringAsync();

                if (content == "null")
                    return id;
            }
        }
        private async void LoginClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new LoginPage());
        }
    }
}
