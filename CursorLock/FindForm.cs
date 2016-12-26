using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace CursorLock
{
    public partial class FindForm : Form
    {
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

        public string SelectedWindow;

        public FindForm()
        {
            InitializeComponent();
        }

        private void FindForm_Load(object sender, EventArgs e)
        {
            Location = Cursor.Position;

            foreach (var proc in Process.GetProcesses().Where(o => !string.IsNullOrWhiteSpace(o.MainWindowTitle)))
                windowList.Items.Add(proc.MainWindowTitle);

            if (windowList.Items.Count > 0)
                windowList.SelectedIndex = 0;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            SelectedWindow = (string)windowList.SelectedItem;
            Close();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
