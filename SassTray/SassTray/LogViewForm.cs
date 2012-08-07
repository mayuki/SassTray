using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SassTray
{
    public partial class LogViewForm : Form
    {
        public LogViewForm()
        {
            InitializeComponent();
        }

        public void AppendLine(String line)
        {
            if (InvokeRequired)
            {
                Invoke((Action)(() => AppendLine(line)));
                return;
            }
            
            textBoxLog.Text += line + "\r\n";
        }
    }
}
