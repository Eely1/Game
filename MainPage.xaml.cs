using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Essentials;
using SQLite;
using System.Linq;
using System;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace Game
{
    public class Credential
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }

        public string Service { get; set; }
        public string Login { get; set; }
        public string EncryptedPassword { get; set; }
    }

    public partial class MainPage : ContentPage
    {
        private DatabaseHelper dbHelper;
        private List<Credential> credentials = new List<Credential>();


        public MainPage()
        {
            InitializeComponent();
            string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "credentials.db3");
            dbHelper = new DatabaseHelper(dbPath);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadCredentials();
        }

        private async Task LoadCredentials()
        {
            credentials = await dbHelper.GetCredentialsAsync();
            UpdateTable();
        }



        private void SaveButton_Clicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(serviceEntry.Text) ||
                string.IsNullOrWhiteSpace(loginEntry.Text) ||
                string.IsNullOrWhiteSpace(passwordEntry.Text))
            {
                DisplayAlert("Ошибка", "Все поля должны быть заполнены", "OK");
                return;
            }

            var credential = new Credential
            {
                Service = serviceEntry.Text,
                Login = loginEntry.Text,
                EncryptedPassword = EncryptPassword(passwordEntry.Text)
            };

            credentials.Add(credential);
            SaveCredential();
            UpdateTable();

            serviceEntry.Text = "";
            loginEntry.Text = "";
            passwordEntry.Text = "";
        }




        private async Task UpdateTable()
        {
            credentialsSection.Clear();

            foreach (var credential in credentials)
            {

                var menuItemCopyPassword = new MenuItem { Text = "Скопировать пароль" };
                menuItemCopyPassword.Clicked += (sender, e) =>
                {
                    Clipboard.SetTextAsync(DecryptPassword(credential.EncryptedPassword));
                };

                var menuItemEditCredentials = new MenuItem { Text = "Изменить" };
                menuItemEditCredentials.Clicked += async (sender, e) =>
                {
                    passwordEntry.Text = DecryptPassword(credential.EncryptedPassword);
                    loginEntry.Text = credential.Login;
                    serviceEntry.Text = credential.Service;
                    await dbHelper.DeleteItemAsync(credential.ID);
                    credentials.Remove(credential);
                    await UpdateTable();
                };

                var menuItemCopyLogin = new MenuItem { Text = "Скопировать логин" };
                menuItemCopyLogin.Clicked += (sender, e) =>
                {
                    Clipboard.SetTextAsync(credential.Login);
                };

                var menuItemDelete = new MenuItem { Text = "Удалить", IsDestructive = true };
                menuItemDelete.Clicked += async (sender, e) =>
                {
                    await dbHelper.DeleteItemAsync(credential.ID);
                    credentials.Remove(credential);
                    await UpdateTable();
                };

                var menuItemShowPassword = new MenuItem { Text = "Показать пароль" };
                menuItemShowPassword.Clicked += (sender, e) =>
                {
                    DisplayAlert("Пароль", DecryptPassword(credential.EncryptedPassword), "OK");
                };

                var cell = new TextCell
                {
                    Text = $"{credential.Service} -- {credential.Login}"
                };

                cell.ContextActions.Add(menuItemShowPassword);
                cell.ContextActions.Add(menuItemCopyPassword);
                cell.ContextActions.Add(menuItemCopyLogin);
                cell.ContextActions.Add(menuItemDelete);
                cell.ContextActions.Add(menuItemEditCredentials);
                credentialsSection.Add(cell);
            }
        }


        private void ShowPassword_Clicked(object sender, EventArgs e)
        {
            passwordEntry.IsPassword = !passwordEntry.IsPassword;
        }


        private void GeneratedButton_Clicked(object sender, EventArgs e)
        {
            const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!#$%^&*()";
            StringBuilder sb = new StringBuilder();
            Random rnd = new Random();

            for (int i = 0; i < 12; i++)
            {
                int index = rnd.Next(validChars.Length);
                sb.Append(validChars[index]);
            }
            passwordEntry.Text = sb.ToString();
        }


        

        private string EncryptPassword(string clearText)
        {
            if (clearText.Length < 6 || !ContainsDigit(clearText))
            {
                DisplayAlert("Внимание!", "Ваш пароль небезопасен, рекомендуем усложнить.", "OK");
            }
            using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.KeySize = 256;


                aesAlg.GenerateIV();


                using (RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider())
                {
                    byte[] generatedkey = new byte[32];
                    rngCsp.GetBytes(generatedkey);

                    aesAlg.Key = generatedkey;
                }

                byte[] iv = aesAlg.IV;
                byte[] key = aesAlg.Key;

                using (var encryptor = aesAlg.CreateEncryptor(key, iv))
                {
                    using (var msEncrypt = new MemoryStream())
                    {
                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (var swEncrypt = new StreamWriter(csEncrypt))
                            {
                                swEncrypt.Write(clearText);
                            }
                        }
                        byte[] encryptedData = msEncrypt.ToArray();
                        byte[] combinedData = iv.Concat(key).Concat(encryptedData).ToArray();
                        return Convert.ToBase64String(combinedData);
                    }
                }
            }
        }

        private string DecryptPassword(string encryptedText)
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);

            using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.KeySize = 128;

                byte[] iv = encryptedBytes.Take(16).ToArray();
                byte[] key = encryptedBytes.Skip(16).Take(32).ToArray();
                byte[] encryptedData = encryptedBytes.Skip(48).ToArray();

                aesAlg.IV = iv;
                aesAlg.Key = key;

                using (var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV))
                {
                    using (var msDecrypt = new MemoryStream(encryptedData))
                    {
                        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (var srDecrypt = new StreamReader(csDecrypt))
                            {
                                return srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }
        }

        private async Task SaveCredential()
        {
            await dbHelper.SaveCredentialAsync(credentials);
        }
        static bool ContainsDigit(string str)
        {
            foreach (char c in str)
            {
                if (char.IsDigit(c))
                {
                    return true;
                }
            }
            return false;
        }
    }
}