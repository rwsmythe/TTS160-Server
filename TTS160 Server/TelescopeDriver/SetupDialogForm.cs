using ASCOM.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ASCOM.TTS160.Telescope;
using Microsoft.VisualBasic;
using System.Management;

namespace ASCOM.TTS160
{
    /// <summary>
    /// ASCOM setup dialog for the TTS-160 driver configuration.
    /// Shown when the user selects "Properties" in the ASCOM Chooser or clicks Setup in a client application.
    /// </summary>
    /// <remarks>
    /// <para>Reads/writes driver settings via <see cref="ProfileProperties"/>. Settings include COM port,
    /// site coordinates, slew settle time, guide compensation parameters, Align-on-Sync, park location,
    /// and developer/troubleshooting options.</para>
    /// <para>Not registered for COM — only used internally by the driver.</para>
    /// </remarks>
    [ComVisible(false)]					// Form not registered for COM!
    public partial class SetupDialogForm : Form
    {
        TraceLogger tl; // Holder for a reference to the driver's trace logger

        private Util utilities;

        /// <summary>
        /// Initializes the setup dialog with the driver's trace logger and populates the UI
        /// with current settings from the ASCOM Profile.
        /// </summary>
        /// <param name="tlDriver">The driver's trace logger instance for diagnostic output.</param>
        public SetupDialogForm(TraceLogger tlDriver)
        {
            InitializeComponent();

            // Save the provided trace logger for use within the setup dialogue
            tl = tlDriver;
            // Set the title of the form to include the driver name
            this.Text = TelescopeHardware.Description + " Setup";

            // Initialise current values of user settings from the ASCOM Profile
            InitUI();
        }

        /// <summary>
        /// OK button handler. Validates driver site coordinate format (DMS) and saves the selected COM port.
        /// </summary>
        private void cmdOK_Click(object sender, EventArgs e)
        {
            try
            {
                TelescopeHardware.profileProperties.ComPort = (string)comboBoxComPort.SelectedItem;
            }
            catch
            {
                // Ignore any errors here in case the PC does not have any COM ports that can be selected
            }
            try
            {
                double driversitelatbuff = utilities.DMSToDegrees(textBoxDriverSiteLat.Text);
                double driversitelongbuff = utilities.DMSToDegrees(textBoxDriverSiteLong.Text);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error: Ensure Driver Latitude and Longitude are of the form: DD:MM:SS or sDDD:MM:SS" + Environment.NewLine + ex.Message);
                throw new InvalidValueException(ex.Message);
            }
            tl.Enabled = chkTrace.Checked;
        }

        /// <summary>Cancel button handler. Closes the dialog without saving changes.</summary>
        private void cmdCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>Opens the ASCOM Standards website when the ASCOM logo is clicked.</summary>
        private void BrowseToAscom(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("https://ascom-standards.org/");
            }
            catch (System.ComponentModel.Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (System.Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }

        /// <summary>
        /// Populates the COM port combo box with available ports and selects the currently configured one.
        /// </summary>
        private void InitUI()
        {
            chkTrace.Checked = tl.Enabled;
            // set the list of com ports to those that are currently available
            comboBoxComPort.Items.Clear();
            comboBoxComPort.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());      // use System.IO because it's static
            // select the current port if possible
            if (comboBoxComPort.Items.Contains(TelescopeHardware.profileProperties.ComPort))
            {
                comboBoxComPort.SelectedItem = TelescopeHardware.profileProperties.ComPort;
            }
        }

        private void SetupDialogForm_Load(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void SiteAltTxt_TextChanged(object sender, EventArgs e)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(SiteAltTxt.Text, "[^0-9]"))
            {
                MessageBox.Show("Please enter only numbers");
                SiteAltTxt.Text = SiteAltTxt.Text.Remove(SiteAltTxt.Text.Length - 1);
            }
        }

        private void SlewSetTimeTxt_TextChanged(object sender, EventArgs e)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(SlewSetTimeTxt.Text, "[^0-9]"))
            {
                MessageBox.Show("Please enter only numbers");
                SlewSetTimeTxt.Text = SlewSetTimeTxt.Text.Remove(SlewSetTimeTxt.Text.Length - 1);
            }
        }
        /// <summary>
        /// Reads all UI control values and builds a new <see cref="ProfileProperties"/> instance.
        /// </summary>
        /// <param name="CurProfile">The current profile, used to preserve site lat/long values
        /// (which are read from the mount, not from UI controls).</param>
        /// <returns>A new <see cref="ProfileProperties"/> populated from the dialog's UI state.</returns>
        public ProfileProperties GetProfile(ProfileProperties CurProfile)
        {

            int GuideComp = 0;
            int AlignOnSyncPoints = 0;
            bool AlignOnSyncEnabled = false;
            bool ParkLoc = false;
            bool SetParkLoc = false;

            utilities = new Util();

            if (radioButtonGuidingAlt.Checked)
            {
                GuideComp = 1;
            }

            if (checkBoxAlignonSync.Checked)
            {
                AlignOnSyncEnabled = true;
                if (radioButtonAlignonSync1.Checked)
                    AlignOnSyncPoints = 1;
                else if (radioButtonAlignonSync2.Checked)
                    AlignOnSyncPoints = 2;
                else
                    AlignOnSyncPoints = 3;
            }
            else
            {
                AlignOnSyncEnabled = false;
                AlignOnSyncPoints = 0;
            }

            if (radioButtonParkCustom.Checked) { ParkLoc = true; }
            if (checkBoxParkUpdate.Checked) { SetParkLoc = true; }

            try
            {
                double driversitelatbuff = utilities.DMSToDegrees(textBoxDriverSiteLat.Text);
                double driversitelongbuff = utilities.DMSToDegrees(textBoxDriverSiteLong.Text);

                var profileProperties = new ProfileProperties
                {

                    TraceLogger = chkTrace.Checked,
                    ComPort = comboBoxComPort.SelectedItem.ToString(),
                    SiteElevation = Double.Parse(SiteAltTxt.Text),
                    SlewSettleTime = Int16.Parse(SlewSetTimeTxt.Text),
                    SiteLatitude = CurProfile.SiteLatitude,
                    SiteLongitude = CurProfile.SiteLongitude,
                    SyncTimeOnConnect = TimeSyncChk.Checked,
                    GuideComp = GuideComp,
                    GuideCompMaxDelta = Int32.Parse(textMaxDelta.Text),
                    GuideCompBuffer = Int32.Parse(textBuffer.Text),
                    PulseGuideEquFrame = checkBoxPulseGuideTopoEqu.Checked,
                    DriverSiteOverride = checkBoxDriverSiteOverride.Checked,
                    DriverSiteLatitude = driversitelatbuff,
                    DriverSiteLongitude = driversitelongbuff,
                    PulseGuideDurationSynchronous = checkBoxPulseGuideDuration.Checked,
                    AlignOnSyncEnabled = AlignOnSyncEnabled,
                    AlignOnSyncPoints = AlignOnSyncPoints,
                    SetParkLoc = SetParkLoc,
                    ParkLoc = ParkLoc,
                    ParkLocAlt = Double.Parse(textBoxParkLocAlt.Text),
                    ParkLocAz = Double.Parse(textBoxParkLocAz.Text)
                };

                return profileProperties;
            }
            catch
            {
                throw;
            }



        }

        /// <summary>
        /// Populates all UI controls from the given <see cref="ProfileProperties"/> settings.
        /// </summary>
        /// <param name="profileProperties">The settings to display in the dialog.</param>
        /// <remarks>
        /// Site latitude/longitude display "Not Yet Read" when they hold sentinel values
        /// (100 for latitude, 200 for longitude), indicating the mount hasn't been queried yet.
        /// </remarks>
        public void SetProfile(ProfileProperties profileProperties)
        {

            utilities = new Util();

            SiteAltTxt.Text = profileProperties.SiteElevation.ToString();

            SlewSetTimeTxt.Text = profileProperties.SlewSettleTime.ToString();

            textMaxDelta.Text = profileProperties.GuideCompMaxDelta.ToString();

            textBuffer.Text = profileProperties.GuideCompBuffer.ToString();

            if (profileProperties.SiteLatitude == 100)
            {
                SiteLatlbl.Text = "Not Yet Read";
            }
            else
            {
                SiteLatlbl.Text = utilities.DegreesToDMS(profileProperties.SiteLatitude, ":", ":", "");
            }

            if (profileProperties.SiteLongitude == 200)
            {
                SiteLonglbl.Text = "Not Yet Read";
            }
            else
            {
                SiteLonglbl.Text = utilities.DegreesToDMS(profileProperties.SiteLongitude,":",":","");
            }

            switch (profileProperties.GuideComp)
            {
                case 0:
                    radioButtonGuidingNone.Checked = true;
                    radioButtonGuidingAlt.Checked = false;
                    break;
                case 1:
                    radioButtonGuidingNone.Checked = false;
                    radioButtonGuidingAlt.Checked = true;
                    break;
            }

            TimeSyncChk.Checked = profileProperties.SyncTimeOnConnect;

            textBoxDriverSiteLat.Text = utilities.DegreesToDMS(profileProperties.DriverSiteLatitude, ":", ":","", 1);
            textBoxDriverSiteLong.Text = utilities.DegreesToDMS(profileProperties.DriverSiteLongitude, ":", ":","",1);
            checkBoxDriverSiteOverride.Checked = profileProperties.DriverSiteOverride;

            checkBoxPulseGuideTopoEqu.Checked = profileProperties.PulseGuideEquFrame;
            checkBoxPulseGuideDuration.Checked = profileProperties.PulseGuideDurationSynchronous;

            radioButtonGuidingNone.Enabled = !checkBoxPulseGuideTopoEqu.Checked;
            radioButtonGuidingAlt.Enabled = !checkBoxPulseGuideTopoEqu.Checked;
            textMaxDelta.Enabled = !checkBoxPulseGuideTopoEqu.Checked;
            textBuffer.Enabled = !checkBoxPulseGuideTopoEqu.Checked;

            checkBoxAlignonSync.Checked = profileProperties.AlignOnSyncEnabled;
            radioButtonAlignonSync1.Enabled = radioButtonAlignonSync2.Enabled = radioButtonAlignonSync3.Enabled = profileProperties.AlignOnSyncEnabled;
            switch(profileProperties.AlignOnSyncPoints)
            {
                case 1:
                    radioButtonAlignonSync1.Checked = true;
                    break;
                case 0:
                case 2:
                    radioButtonAlignonSync2.Checked = true;
                    break;
                case 3:
                    radioButtonAlignonSync3.Checked = true;
                    break;
            }

            radioButtonParkinPlace.Checked = !profileProperties.ParkLoc;
            radioButtonParkCustom.Checked = profileProperties.ParkLoc;
            checkBoxParkUpdate.Checked = profileProperties.SetParkLoc;
            textBoxParkLocAlt.Text = profileProperties.ParkLocAlt.ToString();
            textBoxParkLocAz.Text = profileProperties.ParkLocAz.ToString();
        }

        private void radioButtonGuidingEl_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textMaxDelta_TextChanged(object sender, EventArgs e)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(textMaxDelta.Text, "[^0-9]"))
            {
                MessageBox.Show("Please enter only numbers");
                textMaxDelta.Text = textMaxDelta.Text.Remove(textMaxDelta.Text.Length - 1);
            }
        }

        private void textBuffer_TextChanged(object sender, EventArgs e)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(textBuffer.Text, "[^0-9]"))
            {
                MessageBox.Show("Please enter only numbers");
                textBuffer.Text = textBuffer.Text.Remove(textBuffer.Text.Length - 1);
            }
        }

        /// <summary>
        /// When equatorial frame pulse guiding is enabled, disables the altitude compensation
        /// controls (they are mutually exclusive — equatorial frame handles compensation internally).
        /// </summary>
        private void checkBoxPulseGuideTopoEqu_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxPulseGuideTopoEqu.Checked)
            {
                radioButtonGuidingNone.Checked = checkBoxPulseGuideTopoEqu.Checked;
                radioButtonGuidingAlt.Checked = !checkBoxPulseGuideTopoEqu.Checked;
            }

            radioButtonGuidingNone.Enabled = !checkBoxPulseGuideTopoEqu.Checked;
            radioButtonGuidingAlt.Enabled = !checkBoxPulseGuideTopoEqu.Checked;
            textMaxDelta.Enabled = !checkBoxPulseGuideTopoEqu.Checked;
            textBuffer.Enabled = !checkBoxPulseGuideTopoEqu.Checked;
        }

        /// <summary>
        /// Guards access to the developer/troubleshooting tab with a password prompt.
        /// These options can cause erratic behavior and are intended for debugging only.
        /// </summary>
        private void tabControl1_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (e.TabPage == tabPage2)
            {
                string nl = Environment.NewLine;
                string input = Interaction.InputBox($"CAUTION: these options may result{nl}in erratic driver behavior and should{nl}be left alone.{nl}{nl}Tal, ven og gå ind.",
                    "Developer Features for Troubleshooting");
                if (!input.ToUpper().Equals("VEN"))
                {
                    e.Cancel = true;
                }
            }
              
        }

        private void checkBoxAlignonSync_CheckedChanged(object sender, EventArgs e)
        {
            radioButtonAlignonSync1.Enabled = radioButtonAlignonSync2.Enabled = radioButtonAlignonSync3.Enabled = checkBoxAlignonSync.Checked;

        }

        private void radioButtonAlignonSync1_CheckedChanged(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Queries WMI for COM port device information and displays it to help the user identify
        /// which COM port corresponds to the TTS-160 mount.
        /// </summary>
        private void buttonFindMount_Click(object sender, EventArgs e)
        {

            /*Serial serial = new Serial();

            serial.Speed = SerialSpeed.ps9600;
            serial.Parity = SerialParity.None;
            serial.DataBits = 8;
            serial.StopBits = SerialStopBits.One;
            serial.ReceiveTimeoutMs = 200;

            foreach (var port in comboBoxComPort.Items )
            {

                try
                {
                    serial.PortName = port.ToString();
                    serial.Connected = true;
                    serial.ClearBuffers();
                    serial.Transmit(":GVP#");
                    string resp = serial.ReceiveTerminated("#").TrimEnd('#');
                    if( resp.Equals("TTS-160 Panther"))
                    {
                        serial.Connected = false;
                        comboBoxComPort.SelectedItem = port;
                        labelMountDetect.Text = port.ToString();
                        return;
                    }

                }
                catch
                {
                    serial.Connected = false;
                    continue;
                }

            }
            labelMountDetect.Text = "Not Detected";*/

            StringBuilder portInfo = new StringBuilder();
            portInfo.AppendLine("COM Port Device Information:\n");

            try
            {
                var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PnPEntity WHERE Caption LIKE '%(COM%'");

                foreach (ManagementObject device in searcher.Get())
                {
                    portInfo.AppendLine($"Name: {device["Caption"]}");
                    portInfo.AppendLine($"Device ID: {device["DeviceID"]}");
                    portInfo.AppendLine($"Manufacturer: {device["Manufacturer"]}");
                    portInfo.AppendLine($"Service: {device["Service"]}");
                    portInfo.AppendLine($"Hardware ID: {((string[])device["HardwareID"])?[0]}");
                    portInfo.AppendLine("--------------------------------");
                }

                if (portInfo.Length > 50) // Has content beyond header
                {
                    MessageBox.Show(portInfo.ToString(), "COM Port Information",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("No COM ports found.", "COM Port Information",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error querying COM ports: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void groupBox4_Enter(object sender, EventArgs e)
        {

        }
    }

}