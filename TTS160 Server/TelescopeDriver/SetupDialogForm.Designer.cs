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
            this.compatBox = new System.Windows.Forms.GroupBox();
            this.mpmBtn = new System.Windows.Forms.RadioButton();
            this.noneBtn = new System.Windows.Forms.RadioButton();
            this.TimeSyncChk = new System.Windows.Forms.CheckBox();
            this.groupGuideComp = new System.Windows.Forms.GroupBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.textBuffer = new System.Windows.Forms.TextBox();
            this.textMaxDelta = new System.Windows.Forms.TextBox();
            this.radioButtonGuidingAlt = new System.Windows.Forms.RadioButton();
            this.radioButtonGuidingNone = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.radioSolar = new System.Windows.Forms.RadioButton();
            this.radioLunar = new System.Windows.Forms.RadioButton();
            this.radioSidereal = new System.Windows.Forms.RadioButton();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.textBoxDriverSiteLat = new System.Windows.Forms.TextBox();
            this.textBoxDriverSiteLong = new System.Windows.Forms.TextBox();
            this.checkBoxDriverSiteOverride = new System.Windows.Forms.CheckBox();
            this.checkBoxPulseGuideTopoEqu = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.radioButtonGR4 = new System.Windows.Forms.RadioButton();
            this.radioButtonGR3 = new System.Windows.Forms.RadioButton();
            this.radioButtonGR2 = new System.Windows.Forms.RadioButton();
            this.radioButtonGR1 = new System.Windows.Forms.RadioButton();
            this.radioButtonGR0 = new System.Windows.Forms.RadioButton();
            ((System.ComponentModel.ISupportInitialize)(this.picASCOM)).BeginInit();
            this.compatBox.SuspendLayout();
            this.groupGuideComp.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // cmdOK
            // 
            this.cmdOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.cmdOK.Location = new System.Drawing.Point(612, 361);
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
            this.cmdCancel.Location = new System.Drawing.Point(612, 391);
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
            this.picASCOM.Location = new System.Drawing.Point(623, 9);
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
            this.label2.Location = new System.Drawing.Point(13, 32);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(58, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Comm Port";
            // 
            // chkTrace
            // 
            this.chkTrace.AutoSize = true;
            this.chkTrace.Location = new System.Drawing.Point(77, 60);
            this.chkTrace.Name = "chkTrace";
            this.chkTrace.Size = new System.Drawing.Size(69, 17);
            this.chkTrace.TabIndex = 6;
            this.chkTrace.Text = "Trace on";
            this.chkTrace.UseVisualStyleBackColor = true;
            // 
            // comboBoxComPort
            // 
            this.comboBoxComPort.FormattingEnabled = true;
            this.comboBoxComPort.Location = new System.Drawing.Point(77, 29);
            this.comboBoxComPort.Name = "comboBoxComPort";
            this.comboBoxComPort.Size = new System.Drawing.Size(90, 21);
            this.comboBoxComPort.TabIndex = 7;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(197, 29);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(80, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Site Altitude (m)";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(197, 61);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(120, 13);
            this.label4.TabIndex = 9;
            this.label4.Text = "Slew Settling Time (sec)";
            this.label4.Click += new System.EventHandler(this.label4_Click);
            // 
            // SiteAltTxt
            // 
            this.SiteAltTxt.CharacterCasing = System.Windows.Forms.CharacterCasing.Lower;
            this.SiteAltTxt.Location = new System.Drawing.Point(327, 26);
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
            this.SlewSetTimeTxt.Location = new System.Drawing.Point(327, 58);
            this.SlewSetTimeTxt.Margin = new System.Windows.Forms.Padding(1);
            this.SlewSetTimeTxt.Name = "SlewSetTimeTxt";
            this.SlewSetTimeTxt.Size = new System.Drawing.Size(103, 20);
            this.SlewSetTimeTxt.TabIndex = 11;
            this.SlewSetTimeTxt.Text = "2";
            this.SlewSetTimeTxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.SlewSetTimeTxt.TextChanged += new System.EventHandler(this.SlewSetTimeTxt_TextChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(13, 117);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(99, 13);
            this.label5.TabIndex = 12;
            this.label5.Text = "Mount Site Latitude";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(13, 139);
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
            this.SiteLatlbl.Location = new System.Drawing.Point(155, 117);
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
            this.SiteLonglbl.Location = new System.Drawing.Point(155, 139);
            this.SiteLonglbl.MinimumSize = new System.Drawing.Size(114, 13);
            this.SiteLonglbl.Name = "SiteLonglbl";
            this.SiteLonglbl.Size = new System.Drawing.Size(114, 15);
            this.SiteLonglbl.TabIndex = 15;
            // 
            // compatBox
            // 
            this.compatBox.Controls.Add(this.mpmBtn);
            this.compatBox.Controls.Add(this.noneBtn);
            this.compatBox.Location = new System.Drawing.Point(16, 248);
            this.compatBox.Margin = new System.Windows.Forms.Padding(1);
            this.compatBox.Name = "compatBox";
            this.compatBox.Padding = new System.Windows.Forms.Padding(1);
            this.compatBox.Size = new System.Drawing.Size(157, 84);
            this.compatBox.TabIndex = 16;
            this.compatBox.TabStop = false;
            this.compatBox.Text = "App Compatibility Mode";
            // 
            // mpmBtn
            // 
            this.mpmBtn.AutoSize = true;
            this.mpmBtn.Location = new System.Drawing.Point(14, 52);
            this.mpmBtn.Margin = new System.Windows.Forms.Padding(1);
            this.mpmBtn.Name = "mpmBtn";
            this.mpmBtn.Size = new System.Drawing.Size(136, 17);
            this.mpmBtn.TabIndex = 1;
            this.mpmBtn.Text = "Moon Panorama Maker";
            this.mpmBtn.UseVisualStyleBackColor = true;
            // 
            // noneBtn
            // 
            this.noneBtn.AutoSize = true;
            this.noneBtn.Checked = true;
            this.noneBtn.Location = new System.Drawing.Point(14, 28);
            this.noneBtn.Margin = new System.Windows.Forms.Padding(1);
            this.noneBtn.Name = "noneBtn";
            this.noneBtn.Size = new System.Drawing.Size(51, 17);
            this.noneBtn.TabIndex = 0;
            this.noneBtn.TabStop = true;
            this.noneBtn.Text = "None";
            this.noneBtn.UseVisualStyleBackColor = true;
            // 
            // TimeSyncChk
            // 
            this.TimeSyncChk.AutoSize = true;
            this.TimeSyncChk.Checked = true;
            this.TimeSyncChk.CheckState = System.Windows.Forms.CheckState.Checked;
            this.TimeSyncChk.Location = new System.Drawing.Point(15, 85);
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
            this.groupGuideComp.Location = new System.Drawing.Point(390, 93);
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
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.radioSolar);
            this.groupBox1.Controls.Add(this.radioLunar);
            this.groupBox1.Controls.Add(this.radioSidereal);
            this.groupBox1.Location = new System.Drawing.Point(200, 248);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(1);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(1);
            this.groupBox1.Size = new System.Drawing.Size(157, 108);
            this.groupBox1.TabIndex = 19;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Default Tracking Rate";
            // 
            // radioSolar
            // 
            this.radioSolar.AutoSize = true;
            this.radioSolar.Location = new System.Drawing.Point(14, 74);
            this.radioSolar.Margin = new System.Windows.Forms.Padding(1);
            this.radioSolar.Name = "radioSolar";
            this.radioSolar.Size = new System.Drawing.Size(49, 17);
            this.radioSolar.TabIndex = 2;
            this.radioSolar.Text = "Solar";
            this.radioSolar.UseVisualStyleBackColor = true;
            // 
            // radioLunar
            // 
            this.radioLunar.AutoSize = true;
            this.radioLunar.Location = new System.Drawing.Point(14, 52);
            this.radioLunar.Margin = new System.Windows.Forms.Padding(1);
            this.radioLunar.Name = "radioLunar";
            this.radioLunar.Size = new System.Drawing.Size(52, 17);
            this.radioLunar.TabIndex = 1;
            this.radioLunar.Text = "Lunar";
            this.radioLunar.UseVisualStyleBackColor = true;
            // 
            // radioSidereal
            // 
            this.radioSidereal.AutoSize = true;
            this.radioSidereal.Checked = true;
            this.radioSidereal.Location = new System.Drawing.Point(14, 28);
            this.radioSidereal.Margin = new System.Windows.Forms.Padding(1);
            this.radioSidereal.Name = "radioSidereal";
            this.radioSidereal.Size = new System.Drawing.Size(63, 17);
            this.radioSidereal.TabIndex = 0;
            this.radioSidereal.TabStop = true;
            this.radioSidereal.Text = "Sidereal";
            this.radioSidereal.UseVisualStyleBackColor = true;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(13, 197);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(106, 13);
            this.label10.TabIndex = 21;
            this.label10.Text = "Driver Site Longitude";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(13, 175);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(97, 13);
            this.label11.TabIndex = 20;
            this.label11.Text = "Driver Site Latitude";
            // 
            // textBoxDriverSiteLat
            // 
            this.textBoxDriverSiteLat.Location = new System.Drawing.Point(155, 168);
            this.textBoxDriverSiteLat.Name = "textBoxDriverSiteLat";
            this.textBoxDriverSiteLat.Size = new System.Drawing.Size(112, 20);
            this.textBoxDriverSiteLat.TabIndex = 22;
            // 
            // textBoxDriverSiteLong
            // 
            this.textBoxDriverSiteLong.Location = new System.Drawing.Point(155, 192);
            this.textBoxDriverSiteLong.Name = "textBoxDriverSiteLong";
            this.textBoxDriverSiteLong.Size = new System.Drawing.Size(112, 20);
            this.textBoxDriverSiteLong.TabIndex = 23;
            // 
            // checkBoxDriverSiteOverride
            // 
            this.checkBoxDriverSiteOverride.AutoSize = true;
            this.checkBoxDriverSiteOverride.Location = new System.Drawing.Point(273, 178);
            this.checkBoxDriverSiteOverride.Name = "checkBoxDriverSiteOverride";
            this.checkBoxDriverSiteOverride.Size = new System.Drawing.Size(111, 17);
            this.checkBoxDriverSiteOverride.TabIndex = 24;
            this.checkBoxDriverSiteOverride.Text = "Driver Site Enable";
            this.checkBoxDriverSiteOverride.UseVisualStyleBackColor = true;
            // 
            // checkBoxPulseGuideTopoEqu
            // 
            this.checkBoxPulseGuideTopoEqu.AutoSize = true;
            this.checkBoxPulseGuideTopoEqu.Location = new System.Drawing.Point(390, 226);
            this.checkBoxPulseGuideTopoEqu.Name = "checkBoxPulseGuideTopoEqu";
            this.checkBoxPulseGuideTopoEqu.Size = new System.Drawing.Size(176, 17);
            this.checkBoxPulseGuideTopoEqu.TabIndex = 25;
            this.checkBoxPulseGuideTopoEqu.Text = "Pulse Guide in Equatorial Frame";
            this.checkBoxPulseGuideTopoEqu.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.radioButtonGR4);
            this.groupBox2.Controls.Add(this.radioButtonGR3);
            this.groupBox2.Controls.Add(this.radioButtonGR2);
            this.groupBox2.Controls.Add(this.radioButtonGR1);
            this.groupBox2.Controls.Add(this.radioButtonGR0);
            this.groupBox2.Location = new System.Drawing.Point(390, 260);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(212, 152);
            this.groupBox2.TabIndex = 26;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "HC Guide Rate";
            // 
            // radioButtonGR4
            // 
            this.radioButtonGR4.AutoSize = true;
            this.radioButtonGR4.Location = new System.Drawing.Point(20, 121);
            this.radioButtonGR4.Name = "radioButtonGR4";
            this.radioButtonGR4.Size = new System.Drawing.Size(97, 17);
            this.radioButtonGR4.TabIndex = 4;
            this.radioButtonGR4.Text = "20 arc-sec/sec";
            this.radioButtonGR4.UseVisualStyleBackColor = true;
            // 
            // radioButtonGR3
            // 
            this.radioButtonGR3.AutoSize = true;
            this.radioButtonGR3.Location = new System.Drawing.Point(20, 98);
            this.radioButtonGR3.Name = "radioButtonGR3";
            this.radioButtonGR3.Size = new System.Drawing.Size(97, 17);
            this.radioButtonGR3.TabIndex = 3;
            this.radioButtonGR3.Text = "10 arc-sec/sec";
            this.radioButtonGR3.UseVisualStyleBackColor = true;
            // 
            // radioButtonGR2
            // 
            this.radioButtonGR2.AutoSize = true;
            this.radioButtonGR2.Checked = true;
            this.radioButtonGR2.Location = new System.Drawing.Point(20, 75);
            this.radioButtonGR2.Name = "radioButtonGR2";
            this.radioButtonGR2.Size = new System.Drawing.Size(91, 17);
            this.radioButtonGR2.TabIndex = 2;
            this.radioButtonGR2.TabStop = true;
            this.radioButtonGR2.Text = "5 arc-sec/sec";
            this.radioButtonGR2.UseVisualStyleBackColor = true;
            // 
            // radioButtonGR1
            // 
            this.radioButtonGR1.AutoSize = true;
            this.radioButtonGR1.Location = new System.Drawing.Point(20, 52);
            this.radioButtonGR1.Name = "radioButtonGR1";
            this.radioButtonGR1.Size = new System.Drawing.Size(91, 17);
            this.radioButtonGR1.TabIndex = 1;
            this.radioButtonGR1.Text = "3 arc-sec/sec";
            this.radioButtonGR1.UseVisualStyleBackColor = true;
            // 
            // radioButtonGR0
            // 
            this.radioButtonGR0.AutoSize = true;
            this.radioButtonGR0.Location = new System.Drawing.Point(20, 29);
            this.radioButtonGR0.Name = "radioButtonGR0";
            this.radioButtonGR0.Size = new System.Drawing.Size(91, 17);
            this.radioButtonGR0.TabIndex = 0;
            this.radioButtonGR0.Text = "1 arc-sec/sec";
            this.radioButtonGR0.UseVisualStyleBackColor = true;
            // 
            // SetupDialogForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(681, 424);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.checkBoxPulseGuideTopoEqu);
            this.Controls.Add(this.checkBoxDriverSiteOverride);
            this.Controls.Add(this.textBoxDriverSiteLong);
            this.Controls.Add(this.textBoxDriverSiteLat);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.groupGuideComp);
            this.Controls.Add(this.TimeSyncChk);
            this.Controls.Add(this.compatBox);
            this.Controls.Add(this.SiteLonglbl);
            this.Controls.Add(this.SiteLatlbl);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.SlewSetTimeTxt);
            this.Controls.Add(this.SiteAltTxt);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.comboBoxComPort);
            this.Controls.Add(this.chkTrace);
            this.Controls.Add(this.label2);
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
            this.compatBox.ResumeLayout(false);
            this.compatBox.PerformLayout();
            this.groupGuideComp.ResumeLayout(false);
            this.groupGuideComp.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
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
        private System.Windows.Forms.GroupBox compatBox;
        private System.Windows.Forms.RadioButton mpmBtn;
        private System.Windows.Forms.RadioButton noneBtn;
        private System.Windows.Forms.CheckBox TimeSyncChk;
        private System.Windows.Forms.GroupBox groupGuideComp;
        private System.Windows.Forms.RadioButton radioButtonGuidingAlt;
        private System.Windows.Forms.RadioButton radioButtonGuidingNone;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBuffer;
        private System.Windows.Forms.TextBox textMaxDelta;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton radioSolar;
        private System.Windows.Forms.RadioButton radioLunar;
        private System.Windows.Forms.RadioButton radioSidereal;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox textBoxDriverSiteLat;
        private System.Windows.Forms.TextBox textBoxDriverSiteLong;
        private System.Windows.Forms.CheckBox checkBoxDriverSiteOverride;
        private System.Windows.Forms.CheckBox checkBoxPulseGuideTopoEqu;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton radioButtonGR4;
        private System.Windows.Forms.RadioButton radioButtonGR3;
        private System.Windows.Forms.RadioButton radioButtonGR2;
        private System.Windows.Forms.RadioButton radioButtonGR1;
        private System.Windows.Forms.RadioButton radioButtonGR0;
    }
}