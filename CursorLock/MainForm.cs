using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.Win32;

namespace CursorLock
{
    public partial class MainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        private const string _regKeyPath = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private globalKeyboardHook _hotkey = new globalKeyboardHook();
        private List<string> _windowList = new List<string>();
        private bool _taskbarNotificationShown;
        private bool _cursorFree = false;

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_DROPSHADOW = 0x20000;
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            if (!killDuplicates())
            {
                MessageBox.Show("There are multiple instances of this app running and I could not kill the others.", 
                    "CursorLock", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }

            loadSettings();

            _hotkey.HookedKeys.Add(Keys.F11);
            _hotkey.KeyUp += _hotkey_KeyUp;
        }

        private void _hotkey_KeyUp(object sender, KeyEventArgs e)
        {
            if (_cursorFree)
            {
                _cursorFree = false;
                lockCursor();
            }
            else
            {
                _cursorFree = true;
                Cursor.Clip = new Rectangle();
            }
        }

        private void panelMenu_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void btnHide_Click(object sender, EventArgs e)
        {
            Hide();

            if (!_taskbarNotificationShown)
            {
                makeBallonPopup("Running minimized.", 1000);
                _taskbarNotificationShown = true;
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            string input = txtInput.Text;

            if (!string.IsNullOrWhiteSpace(input))
                addWindowToList(input);
            else
            {
                MessageBox.Show("Enter a window name into the box or press Find to select from an active window.",
                    "CursorLock",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            var selectedWindowName = (string)listboxWindows.SelectedItem;
            if (!string.IsNullOrWhiteSpace(selectedWindowName))
            {
                _windowList.Remove(selectedWindowName);
                refreshWindowList();
            }
        }

        private void btnFind_Click(object sender, EventArgs e)
        {
            var browseWindowsForm = new FindForm();
            browseWindowsForm.ShowDialog();

            addWindowToList(browseWindowsForm.SelectedWindow);
        }

        private void menuVisibility_Click(object sender, EventArgs e)
        {
            toggleFormVisibility();
        }

        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                toggleFormVisibility();
        }

        private void menuExit_Click(object sender, EventArgs e)
        {
            refreshWindowList();
            Cursor.Clip = new Rectangle();
            Environment.Exit(1);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            refreshWindowList();
            Cursor.Clip = new Rectangle();
        }

        private void lblGithub_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/Ezzpify");
        }

        private void lblGithub_MouseEnter(object sender, EventArgs e)
        {
            lblGithub.ForeColor = Color.FromArgb(79, 93, 115);
        }

        private void lblGithub_MouseLeave(object sender, EventArgs e)
        {
            lblGithub.ForeColor = Color.Silver;
        }

        private void gameTimer_Tick(object sender, EventArgs e)
        {
            lockCursor();
        }

        private void txtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                e.Handled = true;

                addWindowToList(txtInput.Text);
            }
        }

        private void cbAutostart_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                string currentProcessName = Process.GetCurrentProcess().ProcessName;
                var regKey = Registry.CurrentUser.OpenSubKey(_regKeyPath, true);

                if (cbAutostart.Checked)
                    regKey.SetValue(currentProcessName, Application.ExecutablePath);
                else
                    regKey.DeleteValue(currentProcessName);
            }
            catch
            {
                MessageBox.Show("Could not modify the registry. Run me as adminstrator to change the autostart option.", 
                    "CursorLock",
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }
        }

        private void lockCursor()
        {
            if (_cursorFree)
                return;

            var handle = GetForegroundWindow();
            var windowName = getWindowName(handle);
            var screen = Screen.FromHandle(handle);

            if (_windowList.Contains(windowName))
            {
                if (Cursor.Clip != screen.Bounds)
                {
                    Cursor.Clip = screen.Bounds;
                }
            }
            else
            {
                Cursor.Clip = new Rectangle();
            }
        }

        private void addWindowToList(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                if (!_windowList.Contains(name))
                    _windowList.Add(name);

                txtInput.Text = string.Empty;
                refreshWindowList();
            }
        }

        private void refreshWindowList()
        {
            var sc = new StringCollection();
            sc.AddRange(_windowList.ToArray());

            Properties.Settings.Default.windowlist = sc;
            Properties.Settings.Default.Save();

            listboxWindows.Items.Clear();
            _windowList.ForEach(o => listboxWindows.Items.Add(o));
        }

        private void loadSettings()
        {
            if (Properties.Settings.Default.windowlist == null)
                Properties.Settings.Default.windowlist = new StringCollection();

            _windowList = Properties.Settings.Default.windowlist.Cast<string>().ToList();
            refreshWindowList();

            var regKey = Registry.CurrentUser.OpenSubKey(_regKeyPath, true);
            cbAutostart.Checked = regKey.GetValue(Process.GetCurrentProcess().ProcessName) != null;
        }

        private string getWindowName(IntPtr hWnd)
        {
            const int chars = 256;
            var buffer = new StringBuilder(chars);

            if (GetWindowText(hWnd, buffer, chars) > 0)
            {
                return buffer.ToString();
            }

            return string.Empty;
        }

        private bool killDuplicates()
        {
            bool success = true;
            var currentProcess = Process.GetCurrentProcess();
            var duplicates = Process.GetProcessesByName(currentProcess.ProcessName).Where(o => o.Id != currentProcess.Id);

            foreach (var proc in duplicates)
            {
                try { proc.Kill(); }
                catch { success = false; }
            }

            return success;
        }

        private void makeBallonPopup(string text, int duration)
        {
            notifyIcon.BalloonTipText = text;
            notifyIcon.ShowBalloonTip(duration);
        }

        private void toggleFormVisibility()
        {
            if (Visible)
            {
                menuVisibility.Text = "Show";
                Hide();
            }
            else
            {
                menuVisibility.Text = "Hide";
                Show();
            }
        }
    }
}
