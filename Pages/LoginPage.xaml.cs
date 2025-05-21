using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace TarkKoduKK
{
    public partial class LoginPage : ContentPage
    {
        private const string FirebaseUrl = "https://tarkkodu-b9f41-default-rtdb.europe-west1.firebasedatabase.app/users.json";

        public LoginPage()
        {
            InitializeComponent();
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UsernameEntry.Text) || string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                await DisplayAlert("Ошибка", "Введите логин и пароль", "OK");
                return;
            }

            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync(FirebaseUrl);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var users = JsonConvert.DeserializeObject<Dictionary<string, User>>(json);

                string enteredLogin = UsernameEntry.Text.Trim();
                string enteredPassword = PasswordEntry.Text;

                var userExists = users?.Any(u =>
                    u.Value.UserName == enteredLogin &&
                    u.Value.UserPassword == enteredPassword) ?? false;

                if (userExists)
                {
                    await DisplayAlert("Успех", "Авторизация прошла успешно!", "OK");
                }
                else
                {
                    await DisplayAlert("Ошибка", "Неверный логин или пароль", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка подключения: {ex.Message}", "OK");
            }
        }

        private class User
        {
            public string UserName { get; set; }
            public string UserPassword { get; set; }
        }
    }
}
