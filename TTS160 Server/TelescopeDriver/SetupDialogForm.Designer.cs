namespace ASCOM.TTS160
{
    partial class SetupDialogForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SetupDialogForm));
            this.cmdOK = new System.Windows.Forms.Button();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.picASCOM = new System.Windows.Forms.PictureBox();
            this.label2 = new System.Windows.Forms.Label();
            this.chkTrace = new System.Windows.Forms.CheckBox();
            this.comboBoxComPort = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.SiteAltTxt = new System.Windows.Forms.TextBox();
            this.SlewSetTimeTxt = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.SiteLatlbl = new System.Windows.Forms.Label();
            this.SiteLonglbl = new System.Windows.Forms.Label();
            this.TimeSyncChk = new System.Windows.Forms.CheckBox();
            this.groupGuideComp = new System.Windows.Forms.GroupBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.textBuffer = new System.Windows.Forms.TextBox();
            this.textMaxDelta = new System.Windows.Forms.TextBox();
            this.radioButtonGuidingAlt = new System.Windows.Forms.RadioButton();
            this.radioButtonGuidingNone = new System.Windows.Forms.RadioButton();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.textBoxDriverSiteLat = new System.Windows.Forms.TextBox();
            this.textBoxDriverSiteLong = new System.Windows.Forms.TextBox();
            this.checkBoxDriverSiteOverride = new System.Windows.Forms.CheckBox();
            this.checkBoxPulseGuideTopoEqu = new System.Windows.Forms.CheckBox();
            this.checkBoxPulseGuideDuration = new System.Windows.Forms.CheckBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.checkBoxParkUpdate = new System.Windows.Forms.CheckBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.textBoxParkLocAz = new System.Windows.Forms.TextBox();
            this.textBoxParkLocAlt = new System.Windows.Forms.TextBox();
            this.radioButtonParkCustom = new System.Windows.Forms.RadioButton();
            this.radioButtonParkinPlace = new System.Windows.Forms.RadioButton();
            this.labelMountDetect = new System.Windows.Forms.Label();
            this.buttonFindMount = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.radioButtonAlignonSync3 = new System.Windows.Forms.RadioButton();
            this.radioButtonAlignonSync2 = new System.Windows.Forms.RadioButton();
            this.radioButtonAlignonSync1 = new System.Windows.Forms.RadioButton();
            this.checkBoxAlignonSync = new System.Windows.Forms.CheckBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            ((System.ComponentModel.ISupportInitialize)(this.picASCOM)).BeginInit();
            this.groupGuideComp.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // cmdOK
            // 
            this.cmdOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.cmdOK.Location = new System.Drawing.Point(696, 386);
            this.cmdOK.Name = "cmdOK";
            this.cmdOK.Size = new System.Drawing.Size(59, 24);
            this.cmdOK.TabIndex = 0;
            this.cmdOK.Text = "OK";
            this.cmdOK.UseVisualStyleBackColor = true;
            this.cmdOK.Click += new System.EventHandler(this.cmdOK_Click);
            // 
            // cmdCancel
            // 
            this.cmdCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cmdCancel.Location = new System.Drawing.Point(696, 416);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(59, 25);
            this.cmdCancel.TabIndex = 1;
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.UseVisualStyleBackColor = true;
            this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
            // 
            // picASCOM
            // 
            this.picASCOM.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.picASCOM.Cursor = System.Windows.Forms.Cursors.Hand;
            this.picASCOM.Image = ((System.Drawing.Image)(resources.GetObject("picASCOM.Image")));
            this.picASCOM.Location = new System.Drawing.Point(707, 9);
            this.picASCOM.Name = "picASCOM";
            this.picASCOM.Size = new System.Drawing.Size(48, 56);
            this.picASCOM.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.picASCOM.TabIndex = 3;
            this.picASCOM.TabStop = false;
            this.picASCOM.Click += new System.EventHandler(this.BrowseToAscom);
            this.picASCOM.DoubleClick += new System.EventHandler(this.BrowseToAscom);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(21, 25);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(58, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Comm Port";
            // 
            // chkTrace
            // 
            this.chkTrace.AutoSize = true;
            this.chkTrace.Location = new System.Drawing.Point(20, 81);
            this.chkTrace.Name = "chkTrace";
            this.chkTrace.Size = new System.Drawing.Size(69, 17);
            this.chkTrace.TabIndex = 6;
            this.chkTrace.Text = "Trace on";
            this.chkTrace.UseVisualStyleBackColor = true;
            // 
            // comboBoxComPort
            // 
            this.comboBoxComPort.FormattingEnabled = true;
            this.comboBoxComPort.Location = new System.Drawing.Point(85, 22);
            this.comboBoxComPort.Name = "comboBoxComPort";
            this.comboBoxComPort.Size = new System.Drawing.Size(90, 21);
            this.comboBoxComPort.TabIndex = 7;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(205, 22);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(80, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Site Altitude (m)";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(205, 54);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(120, 13);
            this.label4.TabIndex = 9;
            this.label4.Text = "Slew Settling Time (sec)";
            this.label4.Click += new System.EventHandler(this.label4_Click);
            // 
            // SiteAltTxt
            // 
            this.SiteAltTxt.CharacterCasing = System.Windows.Forms.CharacterCasing.Lower;
            this.SiteAltTxt.Location = new System.Drawing.Point(335, 19);
            this.SiteAltTxt.Margin = new System.Windows.Forms.Padding(1);
            this.SiteAltTxt.Name = "SiteAltTxt";
            this.SiteAltTxt.Size = new System.Drawing.Size(103, 20);
            this.SiteAltTxt.TabIndex = 10;
            this.SiteAltTxt.Text = "0";
            this.SiteAltTxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.SiteAltTxt.TextChanged += new System.EventHandler(this.SiteAltTxt_TextChanged);
            // 
            // SlewSetTimeTxt
            // 
            this.SlewSetTimeTxt.CharacterCasing = System.Windows.Forms.CharacterCasing.Lower;
            this.SlewSetTimeTxt.Location = new System.Drawing.Point(335, 51);
            this.SlewSetTimeTxt.Margin = new System.Windows.Forms.Padding(1);
            this.SlewSetTimeTxt.Name = "SlewSetTimeTxt";
            this.SlewSetTimeTxt.Size = new System.Drawing.Size(103, 20);
            this.SlewSetTimeTxt.TabIndex = 11;
            this.SlewSetTimeTxt.Text = "1";
            this.SlewSetTimeTxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.SlewSetTimeTxt.TextChanged += new System.EventHandler(this.SlewSetTimeTxt_TextChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(21, 110);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(99, 13);
            this.label5.TabIndex = 12;
            this.label5.Text = "Mount Site Latitude";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(21, 132);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(108, 13);
            this.label6.TabIndex = 13;
            this.label6.Text = "Mount Site Longitude";
            // 
            // SiteLatlbl
            // 
            this.SiteLatlbl.AutoSize = true;
            this.SiteLatlbl.BackColor = System.Drawing.Color.White;
            this.SiteLatlbl.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.SiteLatlbl.Location = new System.Drawing.Point(163, 110);
            this.SiteLatlbl.MinimumSize = new System.Drawing.Size(114, 13);
            this.SiteLatlbl.Name = "SiteLatlbl";
            this.SiteLatlbl.Size = new System.Drawing.Size(114, 15);
            this.SiteLatlbl.TabIndex = 14;
            // 
            // SiteLonglbl
            // 
            this.SiteLonglbl.AutoSize = true;
            this.SiteLonglbl.BackColor = System.Drawing.Color.White;
            this.SiteLonglbl.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.SiteLonglbl.Location = new System.Drawing.Point(163, 132);
            this.SiteLonglbl.MinimumSize = new System.Drawing.Size(114, 13);
            this.SiteLonglbl.Name = "SiteLonglbl";
            this.SiteLonglbl.Size = new System.Drawing.Size(114, 15);
            this.SiteLonglbl.TabIndex = 15;
            // 
            // TimeSyncChk
            // 
            this.TimeSyncChk.AutoSize = true;
            this.TimeSyncChk.Checked = true;
            this.TimeSyncChk.CheckState = System.Windows.Forms.CheckState.Checked;
            this.TimeSyncChk.Location = new System.Drawing.Point(28, 326);
            this.TimeSyncChk.Margin = new System.Windows.Forms.Padding(1);
            this.TimeSyncChk.Name = "TimeSyncChk";
            this.TimeSyncChk.Size = new System.Drawing.Size(227, 17);
            this.TimeSyncChk.TabIndex = 17;
            this.TimeSyncChk.Text = "Sync Mount Time to Computer on Connect";
            this.TimeSyncChk.UseVisualStyleBackColor = true;
            // 
            // groupGuideComp
            // 
            this.groupGuideComp.Controls.Add(this.label7);
            this.groupGuideComp.Controls.Add(this.label1);
            this.groupGuideComp.Controls.Add(this.textBuffer);
            this.groupGuideComp.Controls.Add(this.textMaxDelta);
            this.groupGuideComp.Controls.Add(this.radioButtonGuidingAlt);
            this.groupGuideComp.Controls.Add(this.radioButtonGuidingNone);
            this.groupGuideComp.Location = new System.Drawing.Point(314, 69);
            this.groupGuideComp.Margin = new System.Windows.Forms.Padding(1);
            this.groupGuideComp.Name = "groupGuideComp";
            this.groupGuideComp.Padding = new System.Windows.Forms.Padding(1);
            this.groupGuideComp.Size = new System.Drawing.Size(213, 129);
            this.groupGuideComp.TabIndex = 18;
            this.groupGuideComp.TabStop = false;
            this.groupGuideComp.Text = "Guiding Compensation";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(12, 106);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(69, 13);
            this.label7.TabIndex = 14;
            this.label7.Text = "Buffer (msec)";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 82);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(87, 13);
            this.label1.TabIndex = 13;
            this.label1.Text = "Max delta (msec)";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // textBuffer
            // 
            this.textBuffer.CharacterCasing = System.Windows.Forms.CharacterCasing.Lower;
            this.textBuffer.Location = new System.Drawing.Point(98, 106);
            this.textBuffer.Margin = new System.Windows.Forms.Padding(1);
            this.textBuffer.Name = "textBuffer";
            this.textBuffer.Size = new System.Drawing.Size(103, 20);
            this.textBuffer.TabIndex = 12;
            this.textBuffer.Text = "20";
            this.textBuffer.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBuffer.TextChanged += new System.EventHandler(this.textBuffer_TextChanged);
            // 
            // textMaxDelta
            // 
            this.textMaxDelta.CharacterCasing = System.Windows.Forms.CharacterCasing.Lower;
            this.textMaxDelta.Location = new System.Drawing.Point(98, 82);
            this.textMaxDelta.Margin = new System.Windows.Forms.Padding(1);
            this.textMaxDelta.Name = "textMaxDelta";
            this.textMaxDelta.Size = new System.Drawing.Size(103, 20);
            this.textMaxDelta.TabIndex = 11;
            this.textMaxDelta.Text = "1000";
            this.textMaxDelta.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textMaxDelta.TextChanged += new System.EventHandler(this.textMaxDelta_TextChanged);
            // 
            // radioButtonGuidingAlt
            // 
            this.radioButtonGuidingAlt.AutoSize = true;
            this.radioButtonGuidingAlt.Location = new System.Drawing.Point(14, 52);
            this.radioButtonGuidingAlt.Margin = new System.Windows.Forms.Padding(1);
            this.radioButtonGuidingAlt.Name = "radioButtonGuidingAlt";
            this.radioButtonGuidingAlt.Size = new System.Drawing.Size(130, 17);
            this.radioButtonGuidingAlt.TabIndex = 1;
            this.radioButtonGuidingAlt.Text = "Altitude Compensation";
            this.radioButtonGuidingAlt.UseVisualStyleBackColor = true;
            this.radioButtonGuidingAlt.CheckedChanged += new System.EventHandler(this.radioButtonGuidingEl_CheckedChanged);
            // 
            // radioButtonGuidingNone
            // 
            this.radioButtonGuidingNone.AutoSize = true;
            this.radioButtonGuidingNone.Checked = true;
            this.radioButtonGuidingNone.Location = new System.Drawing.Point(14, 28);
            this.radioButtonGuidingNone.Margin = new System.Windows.Forms.Padding(1);
            this.radioButtonGuidingNone.Name = "radioButtonGuidingNone";
            this.radioButtonGuidingNone.Size = new System.Drawing.Size(51, 17);
            this.radioButtonGuidingNone.TabIndex = 0;
            this.radioButtonGuidingNone.TabStop = true;
            this.radioButtonGuidingNone.Text = "None";
            this.radioButtonGuidingNone.UseVisualStyleBackColor = true;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(21, 69);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(106, 13);
            this.label10.TabIndex = 21;
            this.label10.Text = "Driver Site Longitude";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(21, 47);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(97, 13);
            this.label11.TabIndex = 20;
            this.label11.Text = "Driver Site Latitude";
            // 
            // textBoxDriverSiteLat
            // 
            this.textBoxDriverSiteLat.Location = new System.Drawing.Point(127, 40);
            this.textBoxDriverSiteLat.Name = "textBoxDriverSiteLat";
            this.textBoxDriverSiteLat.Size = new System.Drawing.Size(112, 20);
            this.textBoxDriverSiteLat.TabIndex = 22;
            // 
            // textBoxDriverSiteLong
            // 
            this.textBoxDriverSiteLong.Location = new System.Drawing.Point(127, 64);
            this.textBoxDriverSiteLong.Name = "textBoxDriverSiteLong";
            this.textBoxDriverSiteLong.Size = new System.Drawing.Size(112, 20);
            this.textBoxDriverSiteLong.TabIndex = 23;
            // 
            // checkBoxDriverSiteOverride
            // 
            this.checkBoxDriverSiteOverride.AutoSize = true;
            this.checkBoxDriverSiteOverride.Location = new System.Drawing.Point(77, 17);
            this.checkBoxDriverSiteOverride.Name = "checkBoxDriverSiteOverride";
            this.checkBoxDriverSiteOverride.Size = new System.Drawing.Size(111, 17);
            this.checkBoxDriverSiteOverride.TabIndex = 24;
            this.checkBoxDriverSiteOverride.Text = "Driver Site Enable";
            this.checkBoxDriverSiteOverride.UseVisualStyleBackColor = true;
            // 
            // checkBoxPulseGuideTopoEqu
            // 
            this.checkBoxPulseGuideTopoEqu.AutoSize = true;
            this.checkBoxPulseGuideTopoEqu.Location = new System.Drawing.Point(314, 40);
            this.checkBoxPulseGuideTopoEqu.Name = "checkBoxPulseGuideTopoEqu";
            this.checkBoxPulseGuideTopoEqu.Size = new System.Drawing.Size(176, 17);
            this.checkBoxPulseGuideTopoEqu.TabIndex = 25;
            this.checkBoxPulseGuideTopoEqu.Text = "Pulse Guide in Equatorial Frame";
            this.checkBoxPulseGuideTopoEqu.UseVisualStyleBackColor = true;
            this.checkBoxPulseGuideTopoEqu.CheckedChanged += new System.EventHandler(this.checkBoxPulseGuideTopoEqu_CheckedChanged);
            // 
            // checkBoxPulseGuideDuration
            // 
            this.checkBoxPulseGuideDuration.AutoSize = true;
            this.checkBoxPulseGuideDuration.Location = new System.Drawing.Point(314, 17);
            this.checkBoxPulseGuideDuration.Name = "checkBoxPulseGuideDuration";
            this.checkBoxPulseGuideDuration.Size = new System.Drawing.Size(193, 17);
            this.checkBoxPulseGuideDuration.TabIndex = 27;
            this.checkBoxPulseGuideDuration.Text = "Synchronous PulseGuide Durations";
            this.checkBoxPulseGuideDuration.UseVisualStyleBackColor = true;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(12, 17);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(678, 393);
            this.tabControl1.TabIndex = 28;
            this.tabControl1.Selecting += new System.Windows.Forms.TabControlCancelEventHandler(this.tabControl1_Selecting);
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.groupBox4);
            this.tabPage1.Controls.Add(this.labelMountDetect);
            this.tabPage1.Controls.Add(this.buttonFindMount);
            this.tabPage1.Controls.Add(this.groupBox3);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.chkTrace);
            this.tabPage1.Controls.Add(this.comboBoxComPort);
            this.tabPage1.Controls.Add(this.label3);
            this.tabPage1.Controls.Add(this.label4);
            this.tabPage1.Controls.Add(this.SiteAltTxt);
            this.tabPage1.Controls.Add(this.SlewSetTimeTxt);
            this.tabPage1.Controls.Add(this.label5);
            this.tabPage1.Controls.Add(this.label6);
            this.tabPage1.Controls.Add(this.SiteLatlbl);
            this.tabPage1.Controls.Add(this.SiteLonglbl);
            this.tabPage1.Controls.Add(this.TimeSyncChk);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(670, 367);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Basic";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.checkBoxParkUpdate);
            this.groupBox4.Controls.Add(this.label9);
            this.groupBox4.Controls.Add(this.label8);
            this.groupBox4.Controls.Add(this.textBoxParkLocAz);
            this.groupBox4.Controls.Add(this.textBoxParkLocAlt);
            this.groupBox4.Controls.Add(this.radioButtonParkCustom);
            this.groupBox4.Controls.Add(this.radioButtonParkinPlace);
            this.groupBox4.Location = new System.Drawing.Point(16, 159);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(240, 151);
            this.groupBox4.TabIndex = 31;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Park Location";
            this.groupBox4.Enter += new System.EventHandler(this.groupBox4_Enter);
            // 
            // checkBoxParkUpdate
            // 
            this.checkBoxParkUpdate.AutoSize = true;
            this.checkBoxParkUpdate.Location = new System.Drawing.Point(140, 19);
            this.checkBoxParkUpdate.Name = "checkBoxParkUpdate";
            this.checkBoxParkUpdate.Size = new System.Drawing.Size(94, 17);
            this.checkBoxParkUpdate.TabIndex = 6;
            this.checkBoxParkUpdate.Text = "Update Mount";
            this.checkBoxParkUpdate.UseVisualStyleBackColor = true;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(11, 120);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(44, 13);
            this.label9.TabIndex = 5;
            this.label9.Text = "Azimuth";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(11, 96);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(42, 13);
            this.label8.TabIndex = 4;
            this.label8.Text = "Altitude";
            // 
            // textBoxParkLocAz
            // 
            this.textBoxParkLocAz.Location = new System.Drawing.Point(56, 117);
            this.textBoxParkLocAz.Name = "textBoxParkLocAz";
            this.textBoxParkLocAz.Size = new System.Drawing.Size(49, 20);
            this.textBoxParkLocAz.TabIndex = 3;
            this.textBoxParkLocAz.Text = "180";
            // 
            // textBoxParkLocAlt
            // 
            this.textBoxParkLocAlt.Location = new System.Drawing.Point(56, 92);
            this.textBoxParkLocAlt.Name = "textBoxParkLocAlt";
            this.textBoxParkLocAlt.Size = new System.Drawing.Size(49, 20);
            this.textBoxParkLocAlt.TabIndex = 2;
            this.textBoxParkLocAlt.Text = "0";
            // 
            // radioButtonParkCustom
            // 
            this.radioButtonParkCustom.AutoSize = true;
            this.radioButtonParkCustom.Location = new System.Drawing.Point(18, 61);
            this.radioButtonParkCustom.Name = "radioButtonParkCustom";
            this.radioButtonParkCustom.Size = new System.Drawing.Size(129, 17);
            this.radioButtonParkCustom.TabIndex = 1;
            this.radioButtonParkCustom.TabStop = true;
            this.radioButtonParkCustom.Text = "Custom Park Location";
            this.radioButtonParkCustom.UseVisualStyleBackColor = true;
            // 
            // radioButtonParkinPlace
            // 
            this.radioButtonParkinPlace.AutoSize = true;
            this.radioButtonParkinPlace.Location = new System.Drawing.Point(18, 29);
            this.radioButtonParkinPlace.Name = "radioButtonParkinPlace";
            this.radioButtonParkinPlace.Size = new System.Drawing.Size(88, 17);
            this.radioButtonParkinPlace.TabIndex = 0;
            this.radioButtonParkinPlace.TabStop = true;
            this.radioButtonParkinPlace.Text = "Park in Place";
            this.radioButtonParkinPlace.UseVisualStyleBackColor = true;
            // 
            // labelMountDetect
            // 
            this.labelMountDetect.AutoSize = true;
            this.labelMountDetect.BackColor = System.Drawing.Color.White;
            this.labelMountDetect.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.labelMountDetect.Location = new System.Drawing.Point(97, 51);
            this.labelMountDetect.Name = "labelMountDetect";
            this.labelMountDetect.Size = new System.Drawing.Size(2, 15);
            this.labelMountDetect.TabIndex = 30;
            // 
            // buttonFindMount
            // 
            this.buttonFindMount.Location = new System.Drawing.Point(16, 49);
            this.buttonFindMount.Name = "buttonFindMount";
            this.buttonFindMount.Size = new System.Drawing.Size(75, 21);
            this.buttonFindMount.TabIndex = 29;
            this.buttonFindMount.Text = "Find Mount";
            this.buttonFindMount.UseVisualStyleBackColor = true;
            this.buttonFindMount.Click += new System.EventHandler(this.buttonFindMount_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.radioButtonAlignonSync3);
            this.groupBox3.Controls.Add(this.radioButtonAlignonSync2);
            this.groupBox3.Controls.Add(this.radioButtonAlignonSync1);
            this.groupBox3.Controls.Add(this.checkBoxAlignonSync);
            this.groupBox3.Location = new System.Drawing.Point(335, 178);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(190, 126);
            this.groupBox3.TabIndex = 28;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Align on Sync Mode";
            // 
            // radioButtonAlignonSync3
            // 
            this.radioButtonAlignonSync3.AutoSize = true;
            this.radioButtonAlignonSync3.Location = new System.Drawing.Point(16, 98);
            this.radioButtonAlignonSync3.Name = "radioButtonAlignonSync3";
            this.radioButtonAlignonSync3.Size = new System.Drawing.Size(112, 17);
            this.radioButtonAlignonSync3.TabIndex = 3;
            this.radioButtonAlignonSync3.Text = "3 Alignment Points";
            this.radioButtonAlignonSync3.UseVisualStyleBackColor = true;
            // 
            // radioButtonAlignonSync2
            // 
            this.radioButtonAlignonSync2.AutoSize = true;
            this.radioButtonAlignonSync2.Checked = true;
            this.radioButtonAlignonSync2.Location = new System.Drawing.Point(16, 75);
            this.radioButtonAlignonSync2.Name = "radioButtonAlignonSync2";
            this.radioButtonAlignonSync2.Size = new System.Drawing.Size(112, 17);
            this.radioButtonAlignonSync2.TabIndex = 2;
            this.radioButtonAlignonSync2.TabStop = true;
            this.radioButtonAlignonSync2.Text = "2 Alignment Points";
            this.radioButtonAlignonSync2.UseVisualStyleBackColor = true;
            // 
            // radioButtonAlignonSync1
            // 
            this.radioButtonAlignonSync1.AutoSize = true;
            this.radioButtonAlignonSync1.Location = new System.Drawing.Point(16, 52);
            this.radioButtonAlignonSync1.Name = "radioButtonAlignonSync1";
            this.radioButtonAlignonSync1.Size = new System.Drawing.Size(107, 17);
            this.radioButtonAlignonSync1.TabIndex = 1;
            this.radioButtonAlignonSync1.Text = "1 Alignment Point";
            this.radioButtonAlignonSync1.UseVisualStyleBackColor = true;
            this.radioButtonAlignonSync1.CheckedChanged += new System.EventHandler(this.radioButtonAlignonSync1_CheckedChanged);
            // 
            // checkBoxAlignonSync
            // 
            this.checkBoxAlignonSync.AutoSize = true;
            this.checkBoxAlignonSync.Location = new System.Drawing.Point(16, 19);
            this.checkBoxAlignonSync.Name = "checkBoxAlignonSync";
            this.checkBoxAlignonSync.Size = new System.Drawing.Size(59, 17);
            this.checkBoxAlignonSync.TabIndex = 0;
            this.checkBoxAlignonSync.Text = "Enable";
            this.checkBoxAlignonSync.UseVisualStyleBackColor = true;
            this.checkBoxAlignonSync.CheckedChanged += new System.EventHandler(this.checkBoxAlignonSync_CheckedChanged);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.label11);
            this.tabPage2.Controls.Add(this.checkBoxPulseGuideDuration);
            this.tabPage2.Controls.Add(this.checkBoxPulseGuideTopoEqu);
            this.tabPage2.Controls.Add(this.label10);
            this.tabPage2.Controls.Add(this.groupGuideComp);
            this.tabPage2.Controls.Add(this.checkBoxDriverSiteOverride);
            this.tabPage2.Controls.Add(this.textBoxDriverSiteLat);
            this.tabPage2.Controls.Add(this.textBoxDriverSiteLong);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(670, 367);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Developer";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // SetupDialogForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(765, 449);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.picASCOM);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.cmdOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SetupDialogForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "TTS160 Setup";
            this.Load += new System.EventHandler(this.SetupDialogForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.picASCOM)).EndInit();
            this.groupGuideComp.ResumeLayout(false);
            this.groupGuideComp.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button cmdOK;
        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.PictureBox picASCOM;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox chkTrace;
        private System.Windows.Forms.ComboBox comboBoxComPort;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox SiteAltTxt;
        private System.Windows.Forms.TextBox SlewSetTimeTxt;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label SiteLatlbl;
        private System.Windows.Forms.Label SiteLonglbl;
        private System.Windows.Forms.CheckBox TimeSyncChk;
        private System.Windows.Forms.GroupBox groupGuideComp;
        private System.Windows.Forms.RadioButton radioButtonGuidingAlt;
        private System.Windows.Forms.RadioButton radioButtonGuidingNone;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBuffer;
        private System.Windows.Forms.TextBox textMaxDelta;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox textBoxDriverSiteLat;
        private System.Windows.Forms.TextBox textBoxDriverSiteLong;
        private System.Windows.Forms.CheckBox checkBoxDriverSiteOverride;
        private System.Windows.Forms.CheckBox checkBoxPulseGuideTopoEqu;
        private System.Windows.Forms.CheckBox checkBoxPulseGuideDuration;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.CheckBox checkBoxAlignonSync;
        private System.Windows.Forms.RadioButton radioButtonAlignonSync3;
        private System.Windows.Forms.RadioButton radioButtonAlignonSync2;
        private System.Windows.Forms.RadioButton radioButtonAlignonSync1;
        private System.Windows.Forms.Button buttonFindMount;
        private System.Windows.Forms.Label labelMountDetect;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.CheckBox checkBoxParkUpdate;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox textBoxParkLocAz;
        private System.Windows.Forms.TextBox textBoxParkLocAlt;
        private System.Windows.Forms.RadioButton radioButtonParkCustom;
        private System.Windows.Forms.RadioButton radioButtonParkinPlace;
    }
}