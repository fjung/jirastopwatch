﻿/**************************************************************************
Copyright 2015 Carsten Gehling

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
**************************************************************************/
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace StopWatch
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            this.jiraClient = new JiraClient();

            this.issues = new List<IssueControl>();

            LoadSettings();

            ticker = new Timer();
            ticker.Interval = 5000;
            ticker.Tick += ticker_Tick;
            ticker.Start();
        }


        #region private eventhandlers
        void issue_TimerStarted(object sender, EventArgs e)
        {
            IssueControl senderCtrl = (IssueControl)sender;

            this.issues.ForEach(
                issue => {
                    if (issue != senderCtrl)
                        issue.Pause();
                }
            );
        }


        void ticker_Tick(object sender, EventArgs e)
        {
            UpdateIssuesOutput();
        }


        private void pbSettings_Click(object sender, EventArgs e)
        {
            using (var form = new SettingsForm())
            {
                form.JiraBaseUrl = jiraClient.BaseUrl;
                form.IssueCount = this.issueCount;
                form.AlwaysOnTop = this.alwaysOnTop;

                this.TopMost = false;

                if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    jiraClient.BaseUrl = form.JiraBaseUrl;
                    this.issueCount = form.IssueCount;
                    this.alwaysOnTop = form.AlwaysOnTop;

                    InitializeIssueControls();
                }
            }
        }


        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Copy issue keys to settings and save settings
            this.issueKeys.Clear();

            this.issues.ForEach(
                issue => this.issueKeys.Add(issue.IssueKey)
            );

            SaveSettings();
        }


        private void MainForm_Shown(object sender, EventArgs e)
        {
            if (this.username != "" && this.password != "")
                this.jiraClient.Authenticate(this.username, this.password);
            else
                JiraLogin();

            InitializeIssueControls();

            // Add issuekeys from settings to issue controls
            for (int i = 0; i < this.issues.Count; i++)
            {
                var issue = this.issues[i];
                if (i < this.issueKeys.Count)
                    issue.IssueKey = this.issueKeys[i];
            }

            UpdateIssuesOutput();
        }


        private void pbLogin_Click(object sender, EventArgs e)
        {
            JiraLogin();
        }
        #endregion


        #region private methods
        private void InitializeIssueControls()
        {
            this.SuspendLayout();

            // If we have too many issue controls, compared to this.issueCount
            // remove the ones not needed
            while (this.issues.Count > this.issueCount)
            {
                var issue = this.issues.Last();
                this.Controls.Remove(issue);
                this.issues.Remove(issue);
            }

            // Create issue controls needed
            while (this.issues.Count < this.issueCount)
            {
                var issue = new IssueControl(this.jiraClient);
                issue.TimerStarted += issue_TimerStarted;
                this.issues.Add(issue);
                this.Controls.Add(issue);
            }

            // Position all issue controls
            for (int i = 0; i < this.issues.Count; i++)
            {
                var issue = this.issues[i];
                issue.Left = 12;
                issue.Top = i * issue.Height + 12;
            }

            // Resize form and reposition settings button
            this.ClientSize = new Size(issues.Last().Width + 24, (this.issueCount) * issues.Last().Height + 22 + 12);

            pbSettings.Left = this.ClientSize.Width - 30;
            pbSettings.Top = this.ClientSize.Height - 30;

            pbLogin.Left = 8;
            pbLogin.Top = this.ClientSize.Height - 30;

            this.TopMost = this.alwaysOnTop;

            this.ResumeLayout(false);
            this.PerformLayout();
        }


        private void UpdateIssuesOutput()
        {
            this.issues.ForEach(
                issue => issue.UpdateOutput()
            );
        }


        private void LoadSettings()
        {
            jiraClient.BaseUrl = Properties.Settings.Default.JiraBaseUrl ?? "";

            this.alwaysOnTop = Properties.Settings.Default.AlwaysOnTop;
            this.issueCount = Properties.Settings.Default.IssueCount;
            this.issueKeys = Properties.Settings.Default.IssueKeys ?? new System.Collections.Specialized.StringCollection();
            this.username = Properties.Settings.Default.Username;
            if (Properties.Settings.Default.Password != "")
                this.password = DPAPI.Decrypt(Properties.Settings.Default.Password);
            else
                this.password = "";
            this.rememberCredentials = Properties.Settings.Default.RememberCredentials;
        }


        private void SaveSettings()
        {
            Properties.Settings.Default.JiraBaseUrl = jiraClient.BaseUrl;

            Properties.Settings.Default.AlwaysOnTop = this.alwaysOnTop;
            Properties.Settings.Default.IssueCount = this.issueCount;
            Properties.Settings.Default.IssueKeys = this.issueKeys;
            Properties.Settings.Default.Username = this.username;
            if (this.password != "")
                Properties.Settings.Default.Password = DPAPI.Encrypt(this.password);
            else
                Properties.Settings.Default.Password = "";
            Properties.Settings.Default.RememberCredentials = this.rememberCredentials;
            Properties.Settings.Default.Save();
        }


        private void JiraLogin()
        {
            using (var form = new LoginForm())
            {
                this.TopMost = false;

                form.Username = this.username;
                form.Password = this.password;
                form.Remember = this.rememberCredentials;

                if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    this.jiraClient.Authenticate(form.Username, form.Password);

                    this.rememberCredentials = form.Remember;
                    if (this.rememberCredentials)
                    {
                        this.username = form.Username;
                        this.password = form.Password;
                    }
                    else
                    {
                        this.username = "";
                        this.password = "";
                    }
                }
            }

        }
        #endregion


        #region private members

        private List<IssueControl> issues;

        private Timer ticker;

        private JiraClient jiraClient;

        private bool alwaysOnTop;
        private int issueCount;
        private string username;
        private string password;
        private bool rememberCredentials;

        private System.Collections.Specialized.StringCollection issueKeys;
        #endregion
    }
}