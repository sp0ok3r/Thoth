﻿using ChatLogger.UserSettings;
using Newtonsoft.Json;
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
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using ChatLogger.Helpers;
using ChatLogger.User2Json;
using System.Threading;
using System.Diagnostics;
using System.Net;
using System.Media;
using MetroFramework;
using Win32Interop.Methods;
using MetroFramework.Controls;
using Microsoft.Win32;
using System.Timers;
using static SteamKit2.Internal.CMsgRemoteClientBroadcastStatus;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using MetroFramework.Forms;
using MistClient;
using static SteamKit2.GC.Dota.Internal.CMsgDOTABotDebugInfo;
using SteamKit2;
using Awesomium.Core;

namespace ChatLogger
{
    public partial class Main : MetroFramework.Forms.MetroForm
    {
        public static Random rnd = new Random();
        public static string usernameJSON;
        public static string passwordJSON;
        public static string SelectedUser = "";
        private static List<SteamLoginUsers> _users;

        HandleLogin handleLogin = new HandleLogin();


        private void UpdateChecker()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    string updateCheck = client.DownloadString(Program.spkDomain + "update.php");

                    if (updateCheck != Program.Version)
                    {
                        this.Hide();
                        this.Enabled = false;

                        Console.WriteLine("New update: " + updateCheck);
                        Form Update = new Update(updateCheck);
                        Update.Show();
                    }
                    else
                    {
                        this.Enabled = true;
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("sp0ok3r down :c");
                InfoForm.InfoHelper.CustomMessageBox.Show("Alert", "Check for new updates. https://github.com/sp0ok3r/ChatLogger");
                // Process.Start("https://github.com/sp0ok3r/ChatLogger/releases");
                // Application.Exit();
            }
        }

        public void OnPowerChange(object s, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    // AccountLogin.steamClient.Disconnect();
                    break;
                case PowerModes.Suspend:
                    //AccountLogin.steamClient.Disconnect();
                    break;
            }
        }


        public Main()
        {
            InitializeComponent();

            SystemEvents.PowerModeChanged += OnPowerChange;
            lbl_infoversion.Text = Program.Version;
            ChatLoggerTabControl.SelectedTab = metroTab_AddAcc;
            this.components.SetStyle(this);
            Region = Region.FromHrgn(Gdi32.CreateRoundRectRgn(0, 0, Width, Height, 5, 5));

            IntPtr ptrLogout = Gdi32.CreateRoundRectRgn(1, 1, btn_logout.Width, btn_logout.Height, 5, 5);
            btn_logout.Region = Region.FromHrgn(ptrLogout);
            Gdi32.DeleteObject(ptrLogout);
            foreach (Control tab in ChatLoggerTabControl.Controls)
            {
                TabPage tabPage = (TabPage)tab;
                foreach (Control control in tabPage.Controls.OfType<MetroButton>())
                {
                    IntPtr ptr = Gdi32.CreateRoundRectRgn(1, 1, control.Width, control.Height, 5, 5);
                    control.Region = Region.FromHrgn(ptr);
                    Gdi32.DeleteObject(ptr);
                }
            }


            lbl_connecting.Visible = false;
            lbl_currentUsername.Visible = false;
            btnLabel_PersonaAndFlag.Visible = false;
            btn_logout.Visible = false;
            panel_steamStates.Visible = false;
            picBox_SteamAvatar.Visible = false;

        }

        private void Main_Shown(object sender, EventArgs e)
        {
            var Settingslist = JsonConvert.DeserializeObject<ChatLoggerSettings>(File.ReadAllText(Program.SettingsJsonFile));

            DateTime now = DateTime.Now;

            if (Settingslist.LastTimeCheckedUpdate == null || Settingslist.LastTimeCheckedUpdate.Length == 0)
            {
                Settingslist.LastTimeCheckedUpdate = now.ToString();
            }

            DateTime old = DateTime.Parse(Settingslist.LastTimeCheckedUpdate);
            if (Settingslist.LastTimeCheckedUpdate.Length > 0 && (now - old).TotalDays > 30) //check for update 30 days later
            {
                UpdateChecker();
            }
            File.WriteAllText(Program.SettingsJsonFile, JsonConvert.SerializeObject(Settingslist, Formatting.Indented));


            if (Settingslist.startupAcc != 0)
            {
                var list = JsonConvert.DeserializeObject<RootObject>(File.ReadAllText(Program.AccountsJsonFile));

                btn_login2selected.Enabled = false;
                foreach (var a in list.Accounts)
                {
                    if (a.SteamID == Settingslist.startupAcc)
                    {
                        usernameJSON = a.username;
                        passwordJSON = a.password;
                    }
                }

                _ = handleLogin.StartLogin(usernameJSON, passwordJSON);
            }


            System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
            t.Tick += new EventHandler(Trolha_Tick);
            t.Interval = 2000;
            t.Start();

            combox_Colors.SelectedIndex = Settingslist.startupColor;
            //
            if (Settingslist.Separator.Length > 0)
            {
                txtBox_saveSeparator.Text = Settingslist.Separator;
            }

            if (Settingslist.PathLogs.Length > 0)
            {
                // txtBox_logDir.Text = Settingslist.PathLogs.Replace(@"\\", @"\");
                txtBox_logDir.Text = Settingslist.PathLogs;
            }
            else
            {
                Settingslist.PathLogs = Program.ChatLogsFolder;
                txtBox_logDir.Text = Program.ChatLogsFolder;
                var convertedJson = JsonConvert.SerializeObject(Settingslist, new JsonSerializerSettings { Formatting = Formatting.Indented });
                File.WriteAllText(Program.SettingsJsonFile, convertedJson);
            }

            //Settings LOAD to Main Form
            toggle_startWindows.Checked = Settingslist.startup;
            combox_historysettings.SelectedIndex = Settingslist.HistorySettings;

            if (Settingslist.hideInTaskBar)
            {
                toggle_hideInTask.Checked = true;
                this.ShowInTaskbar = false;
            }
            else
            {
                this.ShowInTaskbar = true;
                toggle_hideInTask.Checked = false;
            }


            if (Settingslist.startMinimized)
            {
                chck_Minimized.Checked = true;
                this.WindowState = FormWindowState.Minimized;
            }
            else
            {
                chck_Minimized.Checked = false;
                this.WindowState = FormWindowState.Normal;
            }

            if (Settingslist.playsound)
            {
                toggle_playSound.Checked = true;
                Stream str = Properties.Resources.ChatLogger_Success;
                SoundPlayer snd = new SoundPlayer(str);
                snd.Play();
            }
            else
            {
                toggle_playSound.Checked = false;
            }
        }

        public void RefreshAccountList()
        {
            int i = 0;
            AccountsList_Grid.Rows.Clear();
            var list = JsonConvert.DeserializeObject<RootObject>(File.ReadAllText(Program.AccountsJsonFile)).Accounts;
            foreach (var a in list.OrderByDescending(x => x.LastLoginTime))
            {
                bool LoginK = true;
                if (string.IsNullOrEmpty(a.LoginKey) || a.LoginKey == "0")
                {
                    LoginK = false;
                }

                string[] row = { a.username, (a.SteamID).ToString(), (LoginK).ToString() };
                AccountsList_Grid.Rows.Add(row);

                if (File.Exists(Program.SentryFolder + a.username + "_tkn.data"))
                {
                    AccountsList_Grid.Rows[i].Cells[0].Style.ForeColor = Color.White;
                }

                if (a.password.Length != 0)
                {
                    AccountsList_Grid.Rows[i].Cells[0].Style.ForeColor = Color.White;
                }
                i++;
            }
            Acc_ScrollBar.Maximum = AccountsList_Grid.Rows.Count;
            AccountsList_Grid.ClearSelection();
        }

        public static void LoginusersVDF_ToFile()
        {
            _users = GetLoginUsers().ToList();

            if (_users.Count == 0)
            {
                throw new IOException(@"Cannot find saved users!");
            }
            foreach (var user in _users)
            {
                var list = JsonConvert.DeserializeObject<UserSettings.RootObject>(File.ReadAllText(Program.AccountsJsonFile));
                foreach (var a in list.Accounts)
                {
                    if (a.username == user.AccountName)
                    {
                        return;
                    }
                }

                list.Accounts.Add(new UserAccounts
                {
                    LastLoginTime = user.LastLoginTime.ToString(),
                    LoginState = 1,
                    username = user.AccountName,
                    password = "",
                    SteamID = user.SteamId64,
                    LoginKey = "0"
                });
                var convertedJson = JsonConvert.SerializeObject(list, new JsonSerializerSettings { Formatting = Formatting.Indented });
                File.WriteAllText(Program.AccountsJsonFile, convertedJson);
            }
        }


        public static IEnumerable<User2Json.SteamLoginUsers> GetLoginUsers()
        {
            if (SteamPath.SteamLocation == null)
            {
                SteamPath.Init();
            }

            dynamic volvo = VdfConvert.Deserialize(File.ReadAllText(SteamPath.SteamLocation + @"\config\loginusers.vdf"));
            VToken v2 = volvo.Value;
            return v2.Children().Select(child => new SteamLoginUsers((VProperty)child)).OrderByDescending(user => user.LastLoginTime).ToList();
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            if (!File.Exists(Program.AccountsJsonFile))
            {
                var DefaultJson = "{Accounts: []}";
                File.WriteAllText(Program.AccountsJsonFile, DefaultJson);
            }

            try // Saved some gamers
            {
                LoginusersVDF_ToFile();
            }
            catch (Exception x)
            {
                Console.WriteLine("Directory not found, but starting anyway..." + x);
            }

            if (!File.Exists(Program.SettingsJsonFile))
            {
                var DefaultJson = "{}";
                File.WriteAllText(Program.SettingsJsonFile, DefaultJson);
            }

            if (!File.Exists(Program.SentryFolder))
            {
                Directory.CreateDirectory(Program.SentryFolder);
            }

            if (!File.Exists(Program.ChatLogsFolder))
            {
                Directory.CreateDirectory(Program.ChatLogsFolder);
            }
            RefreshAccountList();
        }
        private void HandleFormAddAccClosed(Object sender, FormClosedEventArgs e)
        {
            RefreshAccountList();
            btn_addAcc.Enabled = true;
        }
        private void HandleFormEditAccClosed(Object sender, FormClosedEventArgs e)
        {
            RefreshAccountList();
            btn_editAcc.Enabled = true;
        }
        private void btn_addAcc_Click(object sender, EventArgs e)
        {
            btn_addAcc.Enabled = false;
            Form AddAcc = new AddAcc();
            AddAcc.FormClosed += HandleFormAddAccClosed;
            AddAcc.Show();
        }

        private void AccountsList_Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (AccountsList_Grid.SelectedRows.Count > 0)
            {
                SelectedUser = AccountsList_Grid.SelectedRows[0].Cells[0].Value.ToString();
            }
        }

        private void btn_editAcc_Click(object sender, EventArgs e)
        {
            if (AccountsList_Grid.SelectedRows.Count > 0)
            {
                SelectedUser = AccountsList_Grid.SelectedRows[0].Cells[0].Value.ToString();
                btn_editAcc.Enabled = false;
                Form EditAcc = new EditAcc();
                EditAcc.FormClosed += HandleFormEditAccClosed;
                EditAcc.Show();
            }
            else
            {
                InfoForm.InfoHelper.CustomMessageBox.Show("Error", "Please select an account!");
            }
        }


        public void btn_login2selected_Click(object sender, EventArgs e)
        {

            if (string.IsNullOrEmpty(SelectedUser))
            {
                InfoForm.InfoHelper.CustomMessageBox.Show("Info", "Please select an account!");
                return;
            }

            var accounts = JsonConvert.DeserializeObject<RootObject>(File.ReadAllText(Program.AccountsJsonFile)).Accounts;
            var matchedAccount = accounts.Find(a => a.username == SelectedUser);


            if (matchedAccount != null)
            {
                btn_login2selected.Enabled = false;

                if (File.Exists(Program.SentryFolder + matchedAccount.username + "_tkn.data"))
                {
                    // Handle the case where the token file exists
                }
                else if (string.IsNullOrEmpty(matchedAccount.password))
                {
                    InfoForm.InfoHelper.CustomMessageBox.Show("Info", "Please add password to: " + matchedAccount.username);
                    return; // Exit the function if the password is missing
                }

                usernameJSON = matchedAccount.username;
                passwordJSON = matchedAccount.password;
                // Execution will continue here without the need for `break`

                // Start Login
                _ = handleLogin.StartLogin(usernameJSON, passwordJSON);

            }



            //var list = JsonConvert.DeserializeObject<RootObject>(File.ReadAllText(Program.AccountsJsonFile));

            //btn_login2selected.Enabled = false;
            //foreach (var a in list.Accounts)
            //{
            //    if (a.username == SelectedUser)
            //    {
            //        if (File.Exists(Program.SentryFolder + SelectedUser + "_tkn.data"))
            //        {

            //        }
            //        else if (string.IsNullOrEmpty(a.password))
            //        {
            //            InfoForm.InfoHelper.CustomMessageBox.Show("Info", "Please add password to: " + a.username);
            //            return;
            //        }
            //        usernameJSON = a.username;
            //        passwordJSON = a.password;
            //    }
            //}
        }

        //private void btn_login2selected_Click(object sender, EventArgs e)
        //{
        //    if (string.IsNullOrEmpty(SelectedUser))
        //    {
        //        InfoForm.InfoHelper.CustomMessageBox.Show("Info", "Please select an account!");
        //        return;
        //    }
        //    var list = JsonConvert.DeserializeObject<RootObject>(File.ReadAllText(Program.AccountsJsonFile));
        //    foreach (var a in list.Accounts)
        //    {
        //        if (a.username == SelectedUser)
        //        {
        //            if (string.IsNullOrEmpty(a.password))
        //            {
        //                InfoForm.InfoHelper.CustomMessageBox.Show("Info", "Please add password to: " + a.username);
        //                return;
        //            }
        //            usernameJSON = a.username;
        //            passwordJSON = a.password;
        //        }
        //    }
        //    // Start Login
        //    Thread doLogin = new Thread(() => AccountLogin.UserSettingsGather(usernameJSON, passwordJSON));
        //    doLogin.Start();
        //    btn_login2selected.Enabled = false;
        //}

        private void Acc_ScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            if (e.NewValue >= AccountsList_Grid.Rows.Count)
            {
                return;
            }
            AccountsList_Grid.FirstDisplayedScrollingRowIndex = e.NewValue;
        }

        private void metroLink_spk_Click(object sender, EventArgs e)
        {
            Process.Start("http://steamcommunity.com/profiles/76561198041931474");
        }

        private void metroLink_spkMusic_Click(object sender, EventArgs e)
        {
            //Process.Start("");
        }

        private void Trolha_Tick(object sender, EventArgs e)
        {
            try
            {
                if (HandleLogin.IsLoggedIn == true)//AccountLogin.IsLoggedIn
                {
                    btn_login2selected.Enabled = false;

                    // se timer estiver desligado e combo estiver fora do 0 iniciar
                    if ((!TrolhaHistory.Enabled) && combox_historysettings.SelectedIndex != 0)
                    {
                        TrolhaHistory.Start();

                        richtxtbox_HistoryLogs.Clear();
                        richtxtbox_HistoryLogs.Text = "Waiting for new messages...";
                        richtxtbox_HistoryLogs.ForeColor = SystemColors.GrayText;
                    }



                    lbl_connecting.Visible = true;
                    lbl_currentUsername.Visible = true;
                    btnLabel_PersonaAndFlag.Visible = true;
                    btn_logout.Visible = true;
                    panel_steamStates.Visible = true;
                    picBox_SteamAvatar.Visible = true;

                    if (picBox_SteamAvatar.Image == null && btnLabel_PersonaAndFlag.Image == null)
                    {
                        picBox_SteamAvatar.ImageLocation = HandleLogin.GetAvatarLink(HandleLogin.CurrentSteamID);

                        byte[] data = new WebClient().DownloadData("https://flagcdn.com/w20/" + HandleLogin.UserCountry.ToLower() + ".png");//https://flagcdn.com/16x12/
                        btnLabel_PersonaAndFlag.Image = Image.FromStream(new MemoryStream(data));
                    }

                    btnLabel_PersonaAndFlag.Invoke(new Action(() => btnLabel_PersonaAndFlag.Text = " " + HandleLogin.UserPersonaName));

                    panel_steamStates.BackColor = Color.LightSkyBlue;
                    lbl_currentUsername.Invoke(new Action(() => lbl_currentUsername.Text = HandleLogin.CurrentUsername));

                    progressRecord.Visible = true;
                    lbl_recording.Visible = true;
                    lbl_recording.Text = "Recording...";

                }
                else
                {
                    TrolhaHistory.Stop();

                    richtxtbox_HistoryLogs.Clear();
                    richtxtbox_HistoryLogs.Text = "Waiting for loggin...";
                    richtxtbox_HistoryLogs.ForeColor = SystemColors.GrayText;

                    lbl_recording.Visible = false;
                    progressRecord.Visible = false;

                    lbl_connecting.Visible = false;
                    lbl_currentUsername.Visible = false;
                    btnLabel_PersonaAndFlag.Visible = false;
                    btn_logout.Visible = false;
                    panel_steamStates.Visible = false;
                    picBox_SteamAvatar.Visible = false;


                    btn_login2selected.Enabled = true;
                    btnLabel_PersonaAndFlag.Invoke(new Action(() => btnLabel_PersonaAndFlag.Text = "None"));


                    panel_steamStates.BackColor = Color.Gray;
                    picBox_SteamAvatar.BackColor = Color.FromArgb(255, 25, 25, 25);
                    lbl_currentUsername.Invoke(new Action(() => lbl_currentUsername.Text = "None"));
                }
            }
            catch (Exception ewe)
            {
                InfoForm.InfoHelper.CustomMessageBox.Show("Error", e.ToString());
                Console.WriteLine(ewe);
            }
        }

        public void AppendText(RichTextBox box, string text, bool AddNewLine = false) //Thanks Nathan Baulch
        {

            if (box.Lines[0] == "Waiting for new messages...")
            {
                box.Clear();
            }

            box.SelectionStart = 0;
            box.SelectionLength = 0;

            if (AddNewLine)
            {
                text += Environment.NewLine;
            }

            box.Select(0, 0);


            box.SelectionColor = Color.FromArgb(rnd.Next(20, 255), rnd.Next(20, 255), rnd.Next(20, 255));
            // box.AppendText(text);
            box.SelectedText = text;
            box.SelectionColor = box.ForeColor;
        }

        private void TrolhaHistory_Tick(object sender, EventArgs e)
        {
            try
            {
                if ((HandleLogin.LastMessageSent != null) || (HandleLogin.LastMessageReceived != null))
                {
                    scrollbar_history.Maximum = richtxtbox_HistoryLogs.Lines.Count();

                    switch (combox_historysettings.SelectedIndex)
                    {
                        case 0:
                            //Disabled
                            break;
                        case 1:
                            //Only My Messages

                            if ((HandleLogin.LastMessageSent != null) && DateTime.Parse(DateTime.Now.ToString("HH:mm:ss")) <= DateTime.Parse(HandleLogin.LastMessageSent.ToString().Split('[', ']')[1]))
                            {
                                AppendText(richtxtbox_HistoryLogs, HandleLogin.LastMessageSent, true);

                            }
                            break;
                        case 2:
                            //Only Sender Messages
                            if ((HandleLogin.LastMessageReceived != null) && DateTime.Parse(DateTime.Now.ToString("HH:mm:ss")) <= DateTime.Parse(HandleLogin.LastMessageReceived.ToString().Split('[', ']')[1]))
                            {
                                AppendText(richtxtbox_HistoryLogs, HandleLogin.LastMessageReceived, true);

                            }
                            break;
                        case 3:
                            //(Sender + My) Messages

                            if ((HandleLogin.LastMessageSent != null) && DateTime.Parse(DateTime.Now.ToString("HH:mm:ss")) <= DateTime.Parse(HandleLogin.LastMessageSent.ToString().Split('[', ']')[1]))
                            {
                                AppendText(richtxtbox_HistoryLogs, HandleLogin.LastMessageSent, true);

                            }
                            if ((HandleLogin.LastMessageReceived != null) && DateTime.Parse(DateTime.Now.ToString("HH:mm:ss")) <= DateTime.Parse(HandleLogin.LastMessageReceived.ToString().Split('[', ']')[1]))
                            {
                                AppendText(richtxtbox_HistoryLogs, HandleLogin.LastMessageReceived, true);

                            }
                            break;
                    }
                    HandleLogin.LastMessageReceived = null;
                    HandleLogin.LastMessageSent = null;

                }
                return;
            }
            catch (Exception err)
            {
                Console.WriteLine("[TrolhaHistory] - err: " + err);
            }
        }

        private void metrolink_historySettings_Click(object sender, EventArgs e)
        {
            ChatLoggerTabControl.SelectedIndex = 3;
        }

        public void btn_logout_Click(object sender, EventArgs e)
        {
            if (HandleLogin.IsLoggedIn == true)
            {
                handleLogin.Logout();

                richtxtbox_HistoryLogs.Clear();
                richtxtbox_HistoryLogs.Text = "Waiting for loggin...";
                richtxtbox_HistoryLogs.ForeColor = SystemColors.GrayText;
            }
            else
            {
                InfoForm.InfoHelper.CustomMessageBox.Show("Error", "Not logged!");
            }
        }

        private void toggle_startWindows_CheckedChanged(object sender, EventArgs e)
        {
            var Settingslist = JsonConvert.DeserializeObject<ChatLoggerSettings>(File.ReadAllText(Program.SettingsJsonFile));

            if (toggle_startWindows.Checked)
            {
                Settingslist.startup = true;

                Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                key.SetValue("ChatLogger", Program.ExecutablePath + @"\ChatLogger.exe");
            }
            else
            {
                Settingslist.startup = false;

                Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                key.DeleteValue("ChatLogger", false);
            }
            File.WriteAllText(Program.SettingsJsonFile, JsonConvert.SerializeObject(Settingslist, Formatting.Indented));
        }

        private void chck_Minimized_CheckedChanged(object sender, EventArgs e)
        {
            var Settingslist = JsonConvert.DeserializeObject<ChatLoggerSettings>(File.ReadAllText(Program.SettingsJsonFile));

            if (chck_Minimized.Checked)
            {
                Settingslist.startMinimized = true;
            }
            else
            {
                Settingslist.startMinimized = false;
            }
            File.WriteAllText(Program.SettingsJsonFile, JsonConvert.SerializeObject(Settingslist, Formatting.Indented));
        }

        private void toggle_playSound_CheckedChanged(object sender, EventArgs e)
        {
            var Settingslist = JsonConvert.DeserializeObject<ChatLoggerSettings>(File.ReadAllText(Program.SettingsJsonFile));
            if (toggle_playSound.Checked)
            {
                Settingslist.playsound = true;
            }
            else
            {
                Settingslist.playsound = false;
            }
            File.WriteAllText(Program.SettingsJsonFile, JsonConvert.SerializeObject(Settingslist, Formatting.Indented));
        }

        private void btn_separationSave_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(txtBox_saveSeparator.Text))
            {
                var Settingslist = JsonConvert.DeserializeObject<ChatLoggerSettings>(File.ReadAllText(Program.SettingsJsonFile));

                Settingslist.Separator = txtBox_saveSeparator.Text;

                File.WriteAllText(Program.SettingsJsonFile, JsonConvert.SerializeObject(Settingslist, Formatting.Indented));
            }
            else
            {
                InfoForm.InfoHelper.CustomMessageBox.Show("Error", "Please write something :/");
            }
        }

        private void metroLink_ChatLogsPath_Click(object sender, EventArgs e)
        {
            if (HandleLogin.IsLoggedIn == true)
            {
                var Settingslist = JsonConvert.DeserializeObject<ChatLoggerSettings>(File.ReadAllText(Program.SettingsJsonFile));
                string file = Settingslist.PathLogs + @"\" + HandleLogin.steamID;
                if (Directory.Exists(file))
                {
                    Process.Start(file);
                }
                else
                {
                    InfoForm.InfoHelper.CustomMessageBox.Show("Info", "No messages recorded.");
                }
            }
            else
            {
                Process.Start(Program.ChatLogsFolder);
            }
        }

        private void combox_Colors_SelectedIndexChanged(object sender, EventArgs e)
        {
            var Settingslist = JsonConvert.DeserializeObject<ChatLoggerSettings>(File.ReadAllText(Program.SettingsJsonFile));

            Settingslist.startupColor = combox_Colors.SelectedIndex;

            File.WriteAllText(Program.SettingsJsonFile, JsonConvert.SerializeObject(Settingslist, new JsonSerializerSettings { Formatting = Formatting.Indented }));

            this.components.SetStyle(this);
            this.Refresh();
        }

        private void btn_setpathLogs_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    var Settingslist = JsonConvert.DeserializeObject<ChatLoggerSettings>(File.ReadAllText(Program.SettingsJsonFile));

                    Settingslist.PathLogs = fbd.SelectedPath;
                    txtBox_logDir.Text = fbd.SelectedPath;
                    File.WriteAllText(Program.SettingsJsonFile, JsonConvert.SerializeObject(Settingslist, new JsonSerializerSettings { Formatting = Formatting.Indented }));
                }
            }
        }

        private void link_usedefaultPath_Click(object sender, EventArgs e)
        {
            var Settingslist = JsonConvert.DeserializeObject<ChatLoggerSettings>(File.ReadAllText(Program.SettingsJsonFile));
            Settingslist.PathLogs = Program.ChatLogsFolder;
            txtBox_logDir.Text = Program.ChatLogsFolder;
            File.WriteAllText(Program.SettingsJsonFile, JsonConvert.SerializeObject(Settingslist, new JsonSerializerSettings { Formatting = Formatting.Indented }));
        }

        private void Main_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (HandleLogin.IsLoggedIn == true)
            {
                handleLogin.Logout();
            }

            notifyIcon_ChatLogger.Icon = null;
            Environment.Exit(1);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/sp0ok3r/ChatLogger");
        }

        private void lbl_infoversion_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/sp0ok3r/ChatLogger");
        }

        private void toggle_hideInTask_CheckedChanged(object sender, EventArgs e)
        {
            var Settingslist = JsonConvert.DeserializeObject<ChatLoggerSettings>(File.ReadAllText(Program.SettingsJsonFile));
            if (toggle_hideInTask.Checked)
            {
                Settingslist.hideInTaskBar = true;
                this.ShowInTaskbar = false;
            }
            else
            {
                Settingslist.hideInTaskBar = false;
                this.ShowInTaskbar = true;
            }
            File.WriteAllText(Program.SettingsJsonFile, JsonConvert.SerializeObject(Settingslist, Formatting.Indented));
        }

        private void Main_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == WindowState && !this.ShowInTaskbar) // 254iq
            {
                Hide();
            }
        }

        private void notifyIcon_ChatLogger_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Activate();
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void link_github_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/sp0ok3r/ChatLogger");
        }

        private void link_reportBugFeature_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/sp0ok3r/ChatLogger/issues");
        }

        private void picBox_SteamAvatar_Click(object sender, EventArgs e)
        {
            Process.Start("http://steamcommunity.com/profiles/" + HandleLogin.CurrentSteamID);
        }

        private void combox_historysettings_SelectedIndexChanged(object sender, EventArgs e)
        {

            var Settingslist = JsonConvert.DeserializeObject<ChatLoggerSettings>(File.ReadAllText(Program.SettingsJsonFile));
            Settingslist.HistorySettings = combox_historysettings.SelectedIndex;
            File.WriteAllText(Program.SettingsJsonFile, JsonConvert.SerializeObject(Settingslist, Formatting.Indented));

        }

        private void richtxtbox_HistoryLogs_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            Process.Start(e.LinkText);
        }

        private void metroScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            if (e.NewValue >= richtxtbox_HistoryLogs.Lines.Count())
            {
                return;
            }

            richtxtbox_HistoryLogs.SelectionStart = richtxtbox_HistoryLogs.Find(richtxtbox_HistoryLogs.Lines[e.NewValue]);
            richtxtbox_HistoryLogs.ScrollToCaret();
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {

        }

        private void metroButton2_Click(object sender, EventArgs e)
        {
            Form EditAcc = new ViewChats.ChatsVisualizer();
            // EditAcc.FormClosed += HandleFormEditAccClosed;
            EditAcc.Show();
        }

        private void editAccToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (AccountsList_Grid.SelectedRows.Count > 0)
            {
                SelectedUser = AccountsList_Grid.SelectedRows[0].Cells[0].Value.ToString();
                btn_editAcc.Enabled = false;
                Form EditAcc = new EditAcc();
                EditAcc.FormClosed += HandleFormEditAccClosed;
                EditAcc.Show();
            }
            else
            {
                InfoForm.InfoHelper.CustomMessageBox.Show("Error", "Please select an account!");
            }
        }

        private void AddAccToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form AddAcc = new AddAcc();
            AddAcc.FormClosed += HandleFormAddAccClosed;
            AddAcc.Show();

        }
    }
}