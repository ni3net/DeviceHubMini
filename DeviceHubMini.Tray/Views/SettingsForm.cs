using System;
using System.IO;
using System.Windows.Forms;
using System.Text.Json;

namespace DeviceHubMini.Tray.Views
{
    public partial class SettingsForm : Form
    {
        private string _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "DeviceHubMini", "config.json");

        public SettingsForm()
        {
            InitializeComponent();
            LoadConfig();
        }

        private void LoadConfig()
        {
            if (!File.Exists(_configPath)) return;

            var json = File.ReadAllText(_configPath);
            var cfg = JsonSerializer.Deserialize<ConfigModel>(json);
            txtGraphQlUrl.Text = cfg?.GraphQLUrl;
            txtApiKey.Text = cfg?.ApiKey;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            var cfg = new ConfigModel
            {
                GraphQLUrl = txtGraphQlUrl.Text,
                ApiKey = txtApiKey.Text
            };

            Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
            File.WriteAllText(_configPath, JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true }));
            MessageBox.Show("Configuration saved successfully.", "DeviceHubMini", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    public class ConfigModel
    {
        public string GraphQLUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }
}
