using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AutoNetSwitcher
{
    public partial class MainForm
    {
        /// <summary>
        /// 고정 IP 설정 다이얼로그 (실시간 유효성 검증 및 시큐어 코딩 적용)
        /// </summary>
        public void ShowSetIpDialog()
        {
            if (string.IsNullOrEmpty(this.currentSsid) ||
                this.currentSsid == "확인 중..." ||
                this.currentSsid == "연결된 Wi-Fi 없음")
            {
                ShowCustomAlert(
                    "먼저 Wi-Fi에 연결되어 있어야 해당 AP에 맞는 고정 IP/게이트웨이/DNS를 등록할 수 있습니다.",
                    "AutoNet 안내");
                return;
            }

            string initialIp = (this.currentIp != "0.0.0.0" && !string.IsNullOrEmpty(this.currentIp)) ? this.currentIp : "192.168.1.150";
            string initialSubnet = "255.255.255.0";
            string initialGw = (!string.IsNullOrEmpty(this.currentGateway) && this.currentGateway != "-") ? this.currentGateway : "192.168.1.1";
            string initialDns = "8.8.8.8";

            if (!string.IsNullOrEmpty(this.currentDns) && this.currentDns != "-")
            {
                int commaIdx = this.currentDns.IndexOf(',');
                initialDns = (commaIdx >= 0) ? this.currentDns.Substring(0, commaIdx).Trim() : this.currentDns.Trim();
            }

            ApProfile existing = FindSavedProfile(this.currentSsid);
            if (existing != null)
            {
                if (!string.IsNullOrEmpty(existing.Ip) && existing.Ip != "0.0.0.0") initialIp = existing.Ip;
                if (!string.IsNullOrEmpty(existing.Subnet)) initialSubnet = existing.Subnet;
                if (!string.IsNullOrEmpty(existing.Gateway)) initialGw = existing.Gateway;
                if (!string.IsNullOrEmpty(existing.Dns)) initialDns = existing.Dns;
            }

            using (Form dlg = new Form())
            {
                dlg.FormBorderStyle = FormBorderStyle.None;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.Size = new Size(320, 360);
                dlg.BackColor = Color.FromArgb(15, 23, 42);
                dlg.ForeColor = Color.FromArgb(248, 250, 252);
                dlg.TopMost = true;

                dlg.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (Pen p = new Pen(Color.FromArgb(51, 65, 85), 1.5f))
                    {
                        e.Graphics.DrawRectangle(p, 1, 1, dlg.Width - 2, dlg.Height - 2);
                    }
                };

                Panel pnlTop = new Panel { Location = new Point(0, 0), Size = new Size(320, 36), BackColor = Color.Transparent };
                pnlTop.MouseDown += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        NativeMethods.ReleaseCapture();
                        NativeMethods.SendMessage(dlg.Handle, NativeMethods.WM_NCLBUTTONDOWN, NativeMethods.HTCAPTION, 0);
                    }
                };

                Label lblDlgTitle = new Label
                {
                    Text = " 고정 IP 등록 및 설정",
                    Image = UiIcons.Get("sliders", 14, Color.FromArgb(56, 189, 248)),
                    ImageAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(56, 189, 248),
                    Location = new Point(14, 8),
                    Padding = new Padding(18, 0, 0, 0),
                    AutoSize = true
                };

                Button btnCloseDlg = new Button
                {
                    Image = UiIcons.Get("x", 12, Color.FromArgb(148, 163, 184)),
                    ImageAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent,
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(24, 24),
                    Location = new Point(288, 6),
                    Cursor = Cursors.Hand
                };
                btnCloseDlg.FlatAppearance.BorderSize = 0;
                btnCloseDlg.MouseEnter += (s, e) => { btnCloseDlg.Image = UiIcons.Get("x", 12, Color.FromArgb(244, 63, 94)); };
                btnCloseDlg.MouseLeave += (s, e) => { btnCloseDlg.Image = UiIcons.Get("x", 12, Color.FromArgb(148, 163, 184)); };
                btnCloseDlg.Click += (s, e) => { dlg.DialogResult = DialogResult.Cancel; dlg.Close(); };

                pnlTop.Controls.Add(lblDlgTitle);
                pnlTop.Controls.Add(btnCloseDlg);

                Label lblSub = new Label
                {
                    Text = "AP 대상: " + this.currentSsid,
                    Font = new Font("Segoe UI", 8f),
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Location = new Point(16, 42),
                    Size = new Size(288, 18),
                    AutoEllipsis = true
                };

                Label lblL1 = new Label { Text = "IPv4 주소", Font = new Font("Segoe UI", 8f), ForeColor = Color.FromArgb(203, 213, 225), Location = new Point(16, 68), AutoSize = true };
                TextBox txtIp = new TextBox { Text = initialIp, Font = new Font("Consolas", 9.5f), BackColor = Color.FromArgb(2, 6, 23), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Location = new Point(16, 88), Size = new Size(288, 22) };

                Label lblL2 = new Label { Text = "서브넷 마스크", Font = new Font("Segoe UI", 8f), ForeColor = Color.FromArgb(203, 213, 225), Location = new Point(16, 118), AutoSize = true };
                TextBox txtSub = new TextBox { Text = initialSubnet, Font = new Font("Consolas", 9.5f), BackColor = Color.FromArgb(2, 6, 23), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Location = new Point(16, 138), Size = new Size(288, 22) };

                Label lblL3 = new Label { Text = "기본 게이트웨이 (선택 사항)", Font = new Font("Segoe UI", 8f), ForeColor = Color.FromArgb(203, 213, 225), Location = new Point(16, 168), AutoSize = true };
                TextBox txtGw = new TextBox { Text = initialGw, Font = new Font("Consolas", 9.5f), BackColor = Color.FromArgb(2, 6, 23), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Location = new Point(16, 188), Size = new Size(288, 22) };

                Label lblL4 = new Label { Text = "기본 DNS 주소", Font = new Font("Segoe UI", 8f), ForeColor = Color.FromArgb(203, 213, 225), Location = new Point(16, 218), AutoSize = true };
                TextBox txtDns = new TextBox { Text = initialDns, Font = new Font("Consolas", 9.5f), BackColor = Color.FromArgb(2, 6, 23), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Location = new Point(16, 238), Size = new Size(288, 22) };

                Label lblHint = new Label
                {
                    Text = "올바른 IPv4 형식(예: 192.168.1.100)을 입력하십시오.",
                    Font = new Font("Segoe UI", 7.5f),
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Location = new Point(16, 268),
                    Size = new Size(288, 18)
                };

                Button btnOk = new Button
                {
                    Text = "적용 및 프로필 저장",
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(15, 23, 42),
                    BackColor = Color.FromArgb(6, 182, 212),
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(180, 32),
                    Location = new Point(16, 306),
                    Cursor = Cursors.Hand
                };
                btnOk.FlatAppearance.BorderSize = 0;

                Button btnCancel = new Button
                {
                    Text = "취소",
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = Color.FromArgb(203, 213, 225),
                    BackColor = Color.FromArgb(30, 41, 59),
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(100, 32),
                    Location = new Point(204, 306),
                    Cursor = Cursors.Hand
                };
                btnCancel.FlatAppearance.BorderSize = 0;
                btnCancel.Click += (s, e) => { dlg.DialogResult = DialogResult.Cancel; dlg.Close(); };

                Action updateValidation = () =>
                {
                    string ipText = txtIp.Text.Trim();
                    string gwText = txtGw.Text.Trim();
                    string subText = txtSub.Text.Trim();
                    string dnsText = txtDns.Text.Trim();

                    bool ipValid = SecurityHelper.IsValidIpv4(ipText);
                    bool subValid = SecurityHelper.IsValidSubnetMask(subText);
                    bool gwValid = string.IsNullOrEmpty(gwText) || gwText == "-" || SecurityHelper.IsValidIpv4(gwText);
                    bool dnsValid = string.IsNullOrEmpty(dnsText) || dnsText == "-" || SecurityHelper.IsValidIpv4(dnsText);
                    bool subnetMatch = SecurityHelper.IsSubnetMatch(ipText, gwText, subText);

                    if (!ipValid)
                    {
                        lblHint.Text = "⚠ IPv4 주소 형식이 올바르지 않습니다.";
                        lblHint.ForeColor = Color.FromArgb(244, 63, 94);
                        btnOk.Enabled = false;
                    }
                    else if (!subValid)
                    {
                        lblHint.Text = "⚠ 서브넷 마스크 형식이 올바르지 않습니다.";
                        lblHint.ForeColor = Color.FromArgb(244, 63, 94);
                        btnOk.Enabled = false;
                    }
                    else if (!gwValid)
                    {
                        lblHint.Text = "⚠ 게이트웨이 주소 형식이 올바르지 않습니다.";
                        lblHint.ForeColor = Color.FromArgb(244, 63, 94);
                        btnOk.Enabled = false;
                    }
                    else if (!dnsValid)
                    {
                        lblHint.Text = "⚠ DNS 주소 형식이 올바르지 않습니다.";
                        lblHint.ForeColor = Color.FromArgb(244, 63, 94);
                        btnOk.Enabled = false;
                    }
                    else if (!subnetMatch)
                    {
                        lblHint.Text = "⚠ IP와 게이트웨이가 서로 다른 서브넷 대역입니다.";
                        lblHint.ForeColor = Color.FromArgb(251, 191, 36);
                        btnOk.Enabled = true;
                    }
                    else
                    {
                        lblHint.Text = "✔ 유효한 네트워크 주소입니다.";
                        lblHint.ForeColor = Color.FromArgb(52, 211, 153);
                        btnOk.Enabled = true;
                    }
                };

                txtIp.TextChanged += (s, e) => { updateValidation(); };
                txtSub.TextChanged += (s, e) => { updateValidation(); };
                txtGw.TextChanged += (s, e) => { updateValidation(); };
                txtDns.TextChanged += (s, e) => { updateValidation(); };
                updateValidation();

                btnOk.Click += (s, e) =>
                {
                    string ipVal = txtIp.Text.Trim();
                    string subVal = txtSub.Text.Trim();
                    string gwVal = txtGw.Text.Trim();
                    string dnsVal = txtDns.Text.Trim();

                    if (!SecurityHelper.IsValidIpv4(ipVal))
                    {
                        MessageBox.Show("올바른 IPv4 주소를 입력해 주십시오.", "입력 검증 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    lock (this.profileLock)
                    {
                        string keyName = existing != null ? existing.Ssid : this.currentSsid;
                        string origSsid = existing != null ? existing.OriginalSsid : this.currentSsid;
                        ApProfile profile = new ApProfile(keyName, "static", ipVal, subVal, gwVal, dnsVal, origSsid);
                        this.savedProfiles[keyName] = profile;
                    }

                    SaveProfiles();
                    UpdateSavedApCount();
                    ApplyStaticIp(ipVal, gwVal, dnsVal, subVal);

                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                };

                dlg.Controls.Add(pnlTop);
                dlg.Controls.Add(lblSub);
                dlg.Controls.Add(lblL1);
                dlg.Controls.Add(txtIp);
                dlg.Controls.Add(lblL2);
                dlg.Controls.Add(txtSub);
                dlg.Controls.Add(lblL3);
                dlg.Controls.Add(txtGw);
                dlg.Controls.Add(lblL4);
                dlg.Controls.Add(txtDns);
                dlg.Controls.Add(lblHint);
                dlg.Controls.Add(btnOk);
                dlg.Controls.Add(btnCancel);

                dlg.ShowDialog(this);
            }
        }

        /// <summary>
        /// 기억된 Wi-Fi AP 프로필 목록 다이얼로그
        /// </summary>
        public void ShowSavedProfilesDialog()
        {
            using (Form dlg = new Form())
            {
                dlg.FormBorderStyle = FormBorderStyle.None;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.Size = new Size(340, 395);
                dlg.BackColor = Color.FromArgb(15, 23, 42);
                dlg.ForeColor = Color.FromArgb(248, 250, 252);
                dlg.TopMost = true;

                dlg.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (Pen p = new Pen(Color.FromArgb(51, 65, 85), 1.5f))
                    {
                        e.Graphics.DrawRectangle(p, 1, 1, dlg.Width - 2, dlg.Height - 2);
                    }
                };

                Panel pnlTop = new Panel { Location = new Point(0, 0), Size = new Size(340, 36), BackColor = Color.Transparent };
                pnlTop.MouseDown += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        NativeMethods.ReleaseCapture();
                        NativeMethods.SendMessage(dlg.Handle, NativeMethods.WM_NCLBUTTONDOWN, NativeMethods.HTCAPTION, 0);
                    }
                };

                Label lblDlgTitle = new Label
                {
                    Text = " 저장된 AP 네트워크 프로필",
                    Image = UiIcons.Get("list", 14, Color.FromArgb(56, 189, 248)),
                    ImageAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(56, 189, 248),
                    Location = new Point(14, 8),
                    Padding = new Padding(18, 0, 0, 0),
                    AutoSize = true
                };

                Button btnCloseDlg = new Button
                {
                    Image = UiIcons.Get("x", 12, Color.FromArgb(148, 163, 184)),
                    ImageAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent,
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(24, 24),
                    Location = new Point(308, 6),
                    Cursor = Cursors.Hand
                };
                btnCloseDlg.FlatAppearance.BorderSize = 0;
                btnCloseDlg.MouseEnter += (s, e) => { btnCloseDlg.Image = UiIcons.Get("x", 12, Color.FromArgb(244, 63, 94)); };
                btnCloseDlg.MouseLeave += (s, e) => { btnCloseDlg.Image = UiIcons.Get("x", 12, Color.FromArgb(148, 163, 184)); };
                btnCloseDlg.Click += (s, e) => { dlg.Close(); };

                pnlTop.Controls.Add(lblDlgTitle);
                pnlTop.Controls.Add(btnCloseDlg);

                Panel pnlList = new Panel
                {
                    Location = new Point(14, 42),
                    Size = new Size(312, 300),
                    AutoScroll = true,
                    BackColor = Color.FromArgb(2, 6, 23)
                };

                ToolTip cardToolTip = new ToolTip();
                dlg.Disposed += (s, e) => { cardToolTip.Dispose(); };

                Action renderCards = null;
                renderCards = () =>
                {
                    pnlList.Controls.Clear();

                    List<ApProfile> list = new List<ApProfile>();
                    lock (this.profileLock)
                    {
                        foreach (KeyValuePair<string, ApProfile> kvp in this.savedProfiles)
                        {
                            list.Add(kvp.Value.Clone());
                        }
                    }

                    if (list.Count == 0)
                    {
                        Label lblEmpty = new Label
                        {
                            Text = "저장된 Wi-Fi AP 프로필이 없습니다.\nWi-Fi 연결 후 [고정 IP 설정]으로 등록하십시오.",
                            Font = new Font("Segoe UI", 8.5f),
                            ForeColor = Color.FromArgb(100, 116, 139),
                            Location = new Point(16, 100),
                            Size = new Size(280, 60),
                            TextAlign = ContentAlignment.MiddleCenter
                        };
                        pnlList.Controls.Add(lblEmpty);
                        return;
                    }

                    int y = 6;
                    foreach (ApProfile p in list)
                    {
                        string ssidKey = p.Ssid;
                        Panel card = new Panel
                        {
                            Location = new Point(6, y),
                            Size = new Size(282, 60),
                            BackColor = Color.FromArgb(15, 23, 42)
                        };

                        card.Paint += (cs, ce) =>
                        {
                            ce.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                            using (Pen cp = new Pen(Color.FromArgb(30, 41, 59), 1f))
                            {
                                ce.Graphics.DrawRectangle(cp, 0, 0, card.Width - 1, card.Height - 1);
                            }
                        };

                        Label lblSsidName = new Label
                        {
                            Text = p.Ssid,
                            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                            ForeColor = Color.FromArgb(248, 250, 252),
                            Location = new Point(8, 6),
                            Size = new Size(155, 18),
                            AutoEllipsis = true,
                            Cursor = Cursors.Hand
                        };

                        Label lblModeBadge = new Label
                        {
                            Text = (p.Mode == "static") ? "고정 IP" : "DHCP",
                            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                            ForeColor = (p.Mode == "static") ? Color.FromArgb(56, 189, 248) : Color.FromArgb(52, 211, 153),
                            Location = new Point(166, 6),
                            Size = new Size(52, 16),
                            TextAlign = ContentAlignment.MiddleRight
                        };

                        string detailText = (p.Mode == "static")
                            ? string.Format("IP: {0} | GW: {1}", p.Ip, string.IsNullOrEmpty(p.Gateway) ? "-" : p.Gateway)
                            : "공유기 자동 할당 (DHCP)";

                        Label lblDetails = new Label
                        {
                            Text = detailText,
                            Font = new Font("Consolas", 7.5f),
                            ForeColor = Color.FromArgb(148, 163, 184),
                            Location = new Point(8, 25),
                            Size = new Size(240, 16),
                            AutoEllipsis = true
                        };

                        string dnsText = (p.Mode == "static") ? ("DNS: " + (p.Dns ?? "-")) : "";
                        Label lblDnsLine = new Label
                        {
                            Text = dnsText,
                            Font = new Font("Consolas", 7.5f),
                            ForeColor = Color.FromArgb(100, 116, 139),
                            Location = new Point(8, 40),
                            Size = new Size(240, 16),
                            AutoEllipsis = true
                        };

                        Button btnEdit = new Button
                        {
                            Image = UiIcons.Get("pencil", 12, Color.FromArgb(148, 163, 184)),
                            ImageAlign = ContentAlignment.MiddleCenter,
                            BackColor = Color.Transparent,
                            FlatStyle = FlatStyle.Flat,
                            Size = new Size(22, 22),
                            Location = new Point(224, 4),
                            Cursor = Cursors.Hand
                        };
                        btnEdit.FlatAppearance.BorderSize = 0;
                        btnEdit.MouseEnter += (es, ee) => { btnEdit.Image = UiIcons.Get("pencil", 12, Color.FromArgb(56, 189, 248)); };
                        btnEdit.MouseLeave += (es, ee) => { btnEdit.Image = UiIcons.Get("pencil", 12, Color.FromArgb(148, 163, 184)); };
                        btnEdit.Click += (es, ee) =>
                        {
                            ShowRenameProfileDialog(dlg, ssidKey, () => { renderCards(); });
                        };

                        lblSsidName.DoubleClick += (ls, le) =>
                        {
                            ShowRenameProfileDialog(dlg, ssidKey, () => { renderCards(); });
                        };

                        Button btnDel = new Button
                        {
                            Image = UiIcons.Get("x", 12, Color.FromArgb(148, 163, 184)),
                            ImageAlign = ContentAlignment.MiddleCenter,
                            BackColor = Color.Transparent,
                            FlatStyle = FlatStyle.Flat,
                            Size = new Size(22, 22),
                            Location = new Point(252, 4),
                            Cursor = Cursors.Hand
                        };
                        btnDel.FlatAppearance.BorderSize = 0;
                        btnDel.MouseEnter += (ds, de) => { btnDel.Image = UiIcons.Get("x", 12, Color.FromArgb(244, 63, 94)); };
                        btnDel.MouseLeave += (ds, de) => { btnDel.Image = UiIcons.Get("x", 12, Color.FromArgb(148, 163, 184)); };
                        btnDel.Click += (bs, be) =>
                        {
                            lock (this.profileLock)
                            {
                                this.savedProfiles.Remove(ssidKey);
                            }
                            SaveProfiles();
                            UpdateSavedApCount();
                            renderCards();
                        };

                        cardToolTip.SetToolTip(lblSsidName, "더블 클릭하여 이름 변경");
                        cardToolTip.SetToolTip(btnEdit, "이름 변경");
                        cardToolTip.SetToolTip(btnDel, "삭제");

                        card.Controls.Add(lblSsidName);
                        card.Controls.Add(lblModeBadge);
                        card.Controls.Add(lblDetails);
                        card.Controls.Add(lblDnsLine);
                        card.Controls.Add(btnEdit);
                        card.Controls.Add(btnDel);

                        pnlList.Controls.Add(card);
                        y += 66;
                    }
                };

                renderCards();

                Button btnClearAll = new Button
                {
                    Text = " 전체 삭제",
                    Image = UiIcons.Get("trash-2", 12, Color.FromArgb(253, 164, 175)),
                    ImageAlign = ContentAlignment.MiddleLeft,
                    TextImageRelation = TextImageRelation.ImageBeforeText,
                    Font = new Font("Segoe UI", 8f),
                    ForeColor = Color.FromArgb(253, 164, 175),
                    BackColor = Color.FromArgb(30, 41, 59),
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(100, 30),
                    Location = new Point(14, 352),
                    Cursor = Cursors.Hand
                };
                btnClearAll.FlatAppearance.BorderSize = 0;
                btnClearAll.Click += (s, e) =>
                {
                    if (MessageBox.Show("저장된 모든 Wi-Fi AP 프로필을 삭제하시겠습니까?", "확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        lock (this.profileLock)
                        {
                            this.savedProfiles.Clear();
                        }
                        SaveProfiles();
                        UpdateSavedApCount();
                        renderCards();
                    }
                };

                Button btnClose = new Button
                {
                    Text = "닫기",
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = Color.FromArgb(203, 213, 225),
                    BackColor = Color.FromArgb(30, 41, 59),
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(80, 30),
                    Location = new Point(246, 352),
                    Cursor = Cursors.Hand
                };
                btnClose.FlatAppearance.BorderSize = 0;
                btnClose.Click += (s, e) => { dlg.Close(); };

                dlg.Controls.Add(pnlTop);
                dlg.Controls.Add(pnlList);
                dlg.Controls.Add(btnClearAll);
                dlg.Controls.Add(btnClose);

                dlg.ShowDialog(this);
            }
        }

        /// <summary>
        /// 저장된 Wi-Fi AP 프로필 이름(SSID) 변경 다이얼로그
        /// </summary>
        public void ShowRenameProfileDialog(IWin32Window parentOwner, string oldSsid, Action onSuccess = null)
        {
            if (string.IsNullOrWhiteSpace(oldSsid))
                return;

            using (Form dlg = new Form())
            {
                dlg.FormBorderStyle = FormBorderStyle.None;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.Size = new Size(330, 195);
                dlg.BackColor = Color.FromArgb(15, 23, 42);
                dlg.ForeColor = Color.FromArgb(248, 250, 252);
                dlg.TopMost = true;

                dlg.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (Pen p = new Pen(Color.FromArgb(51, 65, 85), 1.5f))
                    {
                        e.Graphics.DrawRectangle(p, 1, 1, dlg.Width - 2, dlg.Height - 2);
                    }
                };

                Panel pnlTop = new Panel { Location = new Point(0, 0), Size = new Size(330, 36), BackColor = Color.Transparent };
                pnlTop.MouseDown += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        NativeMethods.ReleaseCapture();
                        NativeMethods.SendMessage(dlg.Handle, NativeMethods.WM_NCLBUTTONDOWN, NativeMethods.HTCAPTION, 0);
                    }
                };

                Label lblDlgTitle = new Label
                {
                    Text = " AP 프로필 이름 수정",
                    Image = UiIcons.Get("pencil", 14, Color.FromArgb(56, 189, 248)),
                    ImageAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(56, 189, 248),
                    Location = new Point(14, 8),
                    Padding = new Padding(18, 0, 0, 0),
                    AutoSize = true
                };

                Button btnCloseDlg = new Button
                {
                    Image = UiIcons.Get("x", 12, Color.FromArgb(148, 163, 184)),
                    ImageAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent,
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(24, 24),
                    Location = new Point(298, 6),
                    Cursor = Cursors.Hand
                };
                btnCloseDlg.FlatAppearance.BorderSize = 0;
                btnCloseDlg.MouseEnter += (s, e) => { btnCloseDlg.Image = UiIcons.Get("x", 12, Color.FromArgb(244, 63, 94)); };
                btnCloseDlg.MouseLeave += (s, e) => { btnCloseDlg.Image = UiIcons.Get("x", 12, Color.FromArgb(148, 163, 184)); };
                btnCloseDlg.Click += (s, e) => { dlg.Close(); };

                pnlTop.Controls.Add(lblDlgTitle);
                pnlTop.Controls.Add(btnCloseDlg);

                Label lblInputPrompt = new Label
                {
                    Text = "새 이름 (현재: " + oldSsid + ")",
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Location = new Point(16, 46),
                    Size = new Size(298, 18),
                    AutoEllipsis = true
                };

                TextBox txtNewName = new TextBox
                {
                    Font = new Font("Segoe UI", 9.5f),
                    ForeColor = Color.FromArgb(248, 250, 252),
                    BackColor = Color.FromArgb(2, 6, 23),
                    BorderStyle = BorderStyle.FixedSingle,
                    Location = new Point(16, 68),
                    Size = new Size(298, 24),
                    MaxLength = 32,
                    Text = oldSsid
                };

                Label lblHint = new Label
                {
                    Font = new Font("Segoe UI", 7.5f),
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Location = new Point(16, 98),
                    Size = new Size(298, 34)
                };

                Button btnSave = new Button
                {
                    Text = " 저장",
                    Image = UiIcons.Get("pencil", 11, Color.White),
                    ImageAlign = ContentAlignment.MiddleLeft,
                    TextImageRelation = TextImageRelation.ImageBeforeText,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(2, 132, 199),
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(100, 32),
                    Location = new Point(16, 145),
                    Cursor = Cursors.Hand
                };
                btnSave.FlatAppearance.BorderSize = 0;

                Button btnCancel = new Button
                {
                    Text = "취소",
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = Color.FromArgb(203, 213, 225),
                    BackColor = Color.FromArgb(30, 41, 59),
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(80, 32),
                    Location = new Point(234, 145),
                    Cursor = Cursors.Hand
                };
                btnCancel.FlatAppearance.BorderSize = 0;
                btnCancel.Click += (s, e) => { dlg.DialogResult = DialogResult.Cancel; dlg.Close(); };

                Action validateInput = () =>
                {
                    string newName = txtNewName.Text.Trim();
                    if (string.IsNullOrEmpty(newName) || newName.Length > 32 || !SecurityHelper.IsSafeSsid(newName))
                    {
                        lblHint.Text = "⚠ 1~32자 이내, 특수문자(\", &, |, ;, `, $, >, <, \\) 제외";
                        lblHint.ForeColor = Color.FromArgb(244, 63, 94);
                        btnSave.Enabled = false;
                        return;
                    }

                    bool existsOther = false;
                    lock (this.profileLock)
                    {
                        if (!newName.Equals(oldSsid, StringComparison.OrdinalIgnoreCase) && this.savedProfiles.ContainsKey(newName))
                            existsOther = true;
                    }

                    if (existsOther)
                    {
                        lblHint.Text = "⚠ 이미 등록된 다른 AP 프로필 이름입니다.";
                        lblHint.ForeColor = Color.FromArgb(244, 63, 94);
                        btnSave.Enabled = false;
                        return;
                    }

                    lblHint.Text = "✔ 사용 가능한 이름입니다.";
                    lblHint.ForeColor = Color.FromArgb(52, 211, 153);
                    btnSave.Enabled = true;
                };

                txtNewName.TextChanged += (s, e) => { validateInput(); };
                validateInput();

                btnSave.Click += (s, e) =>
                {
                    string finalName = txtNewName.Text.Trim();
                    if (!SecurityHelper.IsSafeSsid(finalName)) return;

                    if (!finalName.Equals(oldSsid, StringComparison.Ordinal))
                    {
                        lock (this.profileLock)
                        {
                            if (this.savedProfiles.ContainsKey(oldSsid))
                            {
                                ApProfile profile = this.savedProfiles[oldSsid];
                                this.savedProfiles.Remove(oldSsid);
                                profile.Ssid = finalName;
                                this.savedProfiles[finalName] = profile;
                            }
                        }

                        SaveProfiles();
                        UpdateSavedApCount();
                        Log(string.Format("기록관리: AP 프로필 이름 수정 완료 [{0}] -> [{1}]", oldSsid, finalName));
                        if (onSuccess != null) onSuccess();
                    }

                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                };

                dlg.AcceptButton = btnSave;
                dlg.CancelButton = btnCancel;
                dlg.Controls.Add(pnlTop);
                dlg.Controls.Add(lblInputPrompt);
                dlg.Controls.Add(txtNewName);
                dlg.Controls.Add(lblHint);
                dlg.Controls.Add(btnSave);
                dlg.Controls.Add(btnCancel);

                dlg.Shown += (s, e) =>
                {
                    txtNewName.Focus();
                    txtNewName.SelectAll();
                };

                dlg.ShowDialog(parentOwner ?? this);
            }
        }

        /// <summary>
        /// Wi-Fi AP 선택 팝업 컨텍스트 메뉴 표시
        /// </summary>
        public void ShowApPopupMenu(Control anchor)
        {
            if (anchor == null) return;

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Renderer = new DarkMenuRenderer();
            menu.ShowImageMargin = false;
            menu.BackColor = Color.FromArgb(15, 23, 42);
            menu.ForeColor = Color.FromArgb(241, 245, 249);
            menu.Font = new Font("Segoe UI", 9f);

            ToolStripLabel titleItem = new ToolStripLabel("── 주변 Wi-Fi AP 검색 목록 ──");
            titleItem.ForeColor = Color.FromArgb(100, 116, 139);
            titleItem.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            menu.Items.Add(titleItem);

            List<string> aps = ScanNearbyWifiAps();
            if (aps.Count == 0)
            {
                ToolStripLabel noneItem = new ToolStripLabel("  (검색된 Wi-Fi AP 없음)");
                noneItem.ForeColor = Color.FromArgb(148, 163, 184);
                menu.Items.Add(noneItem);
            }
            else
            {
                foreach (string ap in aps)
                {
                    string targetSsid = ap;
                    bool isCurrent = targetSsid.Equals(this.currentSsid, StringComparison.OrdinalIgnoreCase);
                    string label = isCurrent ? ("✔ " + targetSsid + " (현재 연결)") : ("📶 " + targetSsid);

                    menu.Items.Add(label, null, (s, e) =>
                    {
                        if (!isCurrent)
                        {
                            ConnectToSsid(targetSsid);
                        }
                    });
                }
            }

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("🔄 Wi-Fi 상태 새로고침", null, (s, e) => { this.RefreshNetworkStatus(); });

            menu.Show(anchor, new Point(0, anchor.Height));
        }

        /// <summary>
        /// 커스텀 알림 모달 다이얼로그
        /// </summary>
        public void ShowCustomAlert(string message, string title)
        {
            using (Form dlg = new Form())
            {
                dlg.FormBorderStyle = FormBorderStyle.None;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.Size = new Size(310, 160);
                dlg.BackColor = Color.FromArgb(15, 23, 42);
                dlg.ForeColor = Color.FromArgb(248, 250, 252);
                dlg.TopMost = true;

                dlg.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (Pen p = new Pen(Color.FromArgb(51, 65, 85), 1.5f))
                    {
                        e.Graphics.DrawRectangle(p, 1, 1, dlg.Width - 2, dlg.Height - 2);
                    }
                };

                Label lblT = new Label
                {
                    Text = string.IsNullOrEmpty(title) ? "AutoNet 안내" : title,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(34, 211, 238),
                    Location = new Point(16, 14),
                    Size = new Size(278, 20)
                };

                Label lblM = new Label
                {
                    Text = message ?? string.Empty,
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = Color.FromArgb(203, 213, 225),
                    Location = new Point(16, 42),
                    Size = new Size(278, 64)
                };

                Button btnOk = new Button
                {
                    Text = "확인",
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(241, 245, 249),
                    BackColor = Color.FromArgb(30, 41, 59),
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(278, 32),
                    Location = new Point(16, 114),
                    Cursor = Cursors.Hand
                };
                btnOk.FlatAppearance.BorderSize = 0;
                btnOk.Click += (s, e) => { dlg.Close(); };

                dlg.Controls.Add(lblT);
                dlg.Controls.Add(lblM);
                dlg.Controls.Add(btnOk);

                dlg.ShowDialog(this);
            }
        }
    }
}
