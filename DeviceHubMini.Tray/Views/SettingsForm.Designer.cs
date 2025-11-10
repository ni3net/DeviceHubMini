namespace DeviceHubMini.Tray.Views
{
    partial class SettingsForm
    {
        private System.ComponentModel.IContainer components = null;
        private TextBox txtGraphQlUrl;
        private TextBox txtApiKey;
        private Button btnSave;
        private Label lblUrl;
        private Label lblApiKey;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            lblUrl = new Label() { Text = "GraphQL URL", Left = 10, Top = 15, Width = 100 };
            txtGraphQlUrl = new TextBox() { Left = 120, Top = 12, Width = 250 };

            lblApiKey = new Label() { Text = "API Key", Left = 10, Top = 55, Width = 100 };
            txtApiKey = new TextBox() { Left = 120, Top = 52, Width = 250 };

            btnSave = new Button() { Text = "Save", Left = 120, Top = 90, Width = 100 };
            btnSave.Click += btnSave_Click;

            Controls.Add(lblUrl);
            Controls.Add(txtGraphQlUrl);
            Controls.Add(lblApiKey);
            Controls.Add(txtApiKey);
            Controls.Add(btnSave);

            Text = "DeviceHubMini Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new System.Drawing.Size(400, 140);
        }
    }
}
