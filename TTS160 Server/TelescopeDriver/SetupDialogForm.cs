using ASCOM.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ASCOM.TTS160.Telescope;

namespace ASCOM.TTS160
{
    [ComVisible(false)]					// Form not registered for COM!
    public partial class SetupDialogForm : Form
    {
        TraceLogger tl; // Holder for a reference to the driver's trace logger

        private Util utilities;

        public SetupDialogForm(TraceLogger tlDriver)
        {
            InitializeComponent();

            // Save the provided trace logger for use within the setup dialogue
            tl = tlDriver;

            // Initialise current values of user settings from the ASCOM Profile
            InitUI();
        }

        private void cmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            // Place any validation constraint checks here
            // Update the state variables with results from the dialogue
            try
            {
                TelescopeHardware.profileProperties.ComPort = (string)comboBoxComPort.SelectedItem;
            }
            catch
            {
                // Ignore any errors here in case the PC does not have any COM ports that can be selected
            }
            tl.Enabled = chkTrace.Checked;
        }

        private void cmdCancel_Click(object sender, EventArgs e) // Cancel button event handler
        {
            Close();
        }

        private void BrowseToAscom(object sender, EventArgs e) // Click on ASCOM logo event handler
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
        public ProfileProperties GetProfile(ProfileProperties CurProfile)
        {

            int CompatMode = 0;
            int GuideComp = 0;
            int DefaultTracking = 0;
            bool CanSetTrackingOverride = false;
            bool CanSetGuideRatesOverride = false;
            int HCGuideRate = 2;

            utilities = new Util();

            if (mpmBtn.Checked)
            {
                CompatMode = 1;
                CanSetTrackingOverride = true;
                CanSetGuideRatesOverride = true;
            }

            if (radioButtonGuidingAlt.Checked)
            {
                GuideComp = 1;
            }

            if (radioSidereal.Checked) { DefaultTracking = 0; }
            else
            if (radioLunar.Checked) { DefaultTracking = 1; }
            else
            if (radioSolar.Checked) { DefaultTracking = 2; }

            if (radioButtonGR0.Checked) { HCGuideRate = 0; }
            else
            if (radioButtonGR1.Checked) { HCGuideRate = 1; }
            else
            if (radioButtonGR2.Checked) { HCGuideRate = 2; }
            else
            if (radioButtonGR3.Checked) { HCGuideRate = 3; }
            else
            if (radioButtonGR4.Checked) { HCGuideRate = 4; }

            var profileProperties = new ProfileProperties
            {

                TraceLogger = chkTrace.Checked,
                ComPort = comboBoxComPort.SelectedItem.ToString(),
                SiteElevation = Double.Parse(SiteAltTxt.Text),
                SlewSettleTime = Int16.Parse(SlewSetTimeTxt.Text),
                SiteLatitude = CurProfile.SiteLatitude,
                SiteLongitude = CurProfile.SiteLongitude,
                CompatMode = CompatMode,
                CanSetTrackingOverride = CanSetTrackingOverride,
                CanSetGuideRatesOverride = CanSetGuideRatesOverride,
                SyncTimeOnConnect = TimeSyncChk.Checked,
                GuideComp = GuideComp,
                GuideCompMaxDelta = Int32.Parse(textMaxDelta.Text),
                GuideCompBuffer = Int32.Parse(textBuffer.Text),
                TrackingRateOnConnect = DefaultTracking,
                PulseGuideEquFrame = checkBoxPulseGuideTopoEqu.Checked,
                DriverSiteOverride = checkBoxDriverSiteOverride.Checked,
                DriverSiteLatitude = utilities.DMSToDegrees(textBoxDriverSiteLat.Text),
                DriverSiteLongitude = utilities.DMSToDegrees(textBoxDriverSiteLong.Text),
                HCGuideRate = HCGuideRate
            };

            return profileProperties;
        }

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
                SiteLatlbl.Text = profileProperties.SiteLatitude.ToString();
            }

            if (profileProperties.SiteLongitude == 200)
            {
                SiteLonglbl.Text = "Not Yet Read";
            }
            else
            {
                SiteLonglbl.Text = profileProperties.SiteLongitude.ToString();
            }

            switch (profileProperties.CompatMode)
            {
                case 0:
                    noneBtn.Checked = true;
                    mpmBtn.Checked = false;
                    break;
                case 1:
                    noneBtn.Checked = false;
                    mpmBtn.Checked = true;
                    break;
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

            switch (profileProperties.TrackingRateOnConnect)
            {
                case 0:
                    radioSidereal.Checked = true;
                    radioLunar.Checked = false;
                    radioSolar.Checked = false;
                    break;
                case 1:
                    radioSidereal.Checked = false;
                    radioLunar.Checked = true;
                    radioSolar.Checked = false;
                    break;
                case 2:
                    radioSidereal.Checked = false;
                    radioLunar.Checked = false;
                    radioSolar.Checked = true;
                    break;

            }

            switch (profileProperties.HCGuideRate)
            {
                case 0:
                    radioButtonGR0.Checked = true;
                    radioButtonGR1.Checked = false;
                    radioButtonGR2.Checked = false;
                    radioButtonGR3.Checked = false;
                    radioButtonGR4.Checked = false;
                    break;

                case 1:
                    radioButtonGR0.Checked = false;
                    radioButtonGR1.Checked = true;
                    radioButtonGR2.Checked = false;
                    radioButtonGR3.Checked = false;
                    radioButtonGR4.Checked = false;
                    break;

                case 2:
                    radioButtonGR0.Checked = false;
                    radioButtonGR1.Checked = false;
                    radioButtonGR2.Checked = true;
                    radioButtonGR3.Checked = false;
                    radioButtonGR4.Checked = false;
                    break;

                case 3:
                    radioButtonGR0.Checked = false;
                    radioButtonGR1.Checked = false;
                    radioButtonGR2.Checked = false;
                    radioButtonGR3.Checked = true;
                    radioButtonGR4.Checked = false;
                    break;

                case 4:
                    radioButtonGR0.Checked = false;
                    radioButtonGR1.Checked = false;
                    radioButtonGR2.Checked = false;
                    radioButtonGR3.Checked = false;
                    radioButtonGR4.Checked = true;
                    break;
            }

            TimeSyncChk.Checked = profileProperties.SyncTimeOnConnect;

            textBoxDriverSiteLat.Text = utilities.DegreesToDMS(profileProperties.DriverSiteLatitude, ":", ":");
            textBoxDriverSiteLong.Text = utilities.DegreesToDMS(profileProperties.DriverSiteLongitude, ":", ":");
            checkBoxDriverSiteOverride.Checked = profileProperties.DriverSiteOverride;

            checkBoxPulseGuideTopoEqu.Checked = profileProperties.PulseGuideEquFrame;

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
    }

}