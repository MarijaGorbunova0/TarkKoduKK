using Newtonsoft.Json;
using System.Text;
using TarkKoduKK.Data; 

namespace TarkKoduKK
{
    public partial class LoginPage : ContentPage
    {
        private const string FirebaseUrl = "https://tarkkodu-b9f41-default-rtdb.europe-west1.firebasedatabase.app/users.json";

        public LoginPage()
        {
            InitializeComponent();
        }

        private async void LoginClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UserIdEntry.Text) || string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                await DisplayAlert("Viga", "sisesta ID ja parool", "OK");
                return;
            }

            string sisestatudId = UserIdEntry.Text.Trim();
            string sisestatudParool = PasswordEntry.Text;

            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync(FirebaseUrl);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var users = JsonConvert.DeserializeObject<Dictionary<string, User>>(json);

                if (users != null && users.TryGetValue(sisestatudId, out var kasutaja))
                {
                    if (kasutaja.UserPassword == sisestatudParool)
                    {
                        await DisplayAlert("Edu", $"Sisse logitud! Tere tulemast, {kasutaja.UserName}", "OK");
                    }
                    else
                    {
                        await DisplayAlert("viga", "Vale parool", "OK");
                    }
                }
                else
                {
                    await DisplayAlert("Viga", "Kasutajat selle ID-ga ei leitud.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Viga", $"Ühenduse viga {ex.Message}", "OK");
            }
        }
        private async void RegisterClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RegistrPage());
        }

    }
}
