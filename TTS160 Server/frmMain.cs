using System.Windows.Forms;

namespace ASCOM.LocalServer
{
    /// <summary>
    /// Hidden WinForms host window that provides the message pump required by the COM local server.
    /// Not visible in the taskbar or on screen — exists solely to keep the application's message loop alive.
    /// </summary>
    public partial class FrmMain : Form
    {
        private delegate void SetTextCallback(string text);

        public FrmMain()
        {
            InitializeComponent();
            this.ShowInTaskbar = false;
            this.Visible = false;
        }
    }
}