using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Configuration;
using System.Security.Cryptography;

namespace Classroom_Killer
{
    public partial class CPD : Form
    {

        public CPD()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(textBox1.Text == "")
            {
                MessageBox.Show("The new PassWord can not be empty!", "ERROR!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else
            {
                string filePath = Paths.ConfigFile;
                if (File.Exists(Paths.ConfigFile))
                {
                    ///
                    ExeConfigurationFileMap exeConfigurationFileMap;
                    Configuration configuration;
                    exeConfigurationFileMap = new ExeConfigurationFileMap();
                    exeConfigurationFileMap.ExeConfigFilename = filePath;
                    configuration = ConfigurationManager.OpenMappedExeConfiguration(exeConfigurationFileMap, ConfigurationUserLevel.None);
                    configuration.AppSettings.Settings["PassWord"].Value = PasswordHasher.Hash(this.textBox1.Text.Trim());
                    configuration.Save();
                    MessageBox.Show("The new password was changed successfully!", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Unable to find important software files!\r\nThe software does not continue to run!\r\nThe following files are missing:\r\nSettings.config\r\n\r\nPress OK and the app will be closed!", "ERROR!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(0);
                }
            }
        }
    }
}
