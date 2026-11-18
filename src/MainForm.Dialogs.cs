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

            const int W = 340;
            const int fieldW = W - 40;
            using (ThemedDialog dlg = new ThemedDialog("고정 IP 등록 및 설정", "sliders", W, 446))
            {
                // 대상 AP 안내 칩
                RoundedPanel pnlTarget = new RoundedPanel
                {
                    Location = new Point(20, 54),
                    Size = new Size(fieldW, 34),
                    Radius = 9,
                    BackColor = Theme.Surface,
                    BorderColor = Theme.Border
                };
                Label lblTargetIcon = new Label
                {
                    Location = new Point(10, 8),
                    Size = new Size(18, 18),
                    Image = UiIcons.Get("wifi", 15, Theme.Accent),
                    ImageAlign = ContentAlignment.MiddleCenter
                };
                Label lblSub = new Label
                {
                    Text = this.currentSsid,
                    Font = Theme.Ui(9f, FontStyle.Bold),
                    ForeColor = Theme.Text,
                    Location = new Point(34, 8),
                    Size = new Size(fieldW - 44, 18),
                    AutoEllipsis = true
                };
                pnlTarget.Controls.Add(lblTargetIcon);
                pnlTarget.Controls.Add(lblSub);
                dlg.Controls.Add(pnlTarget);

                ThemedInput inIp = dlg.AddField("IPv4 주소", initialIp, 20, 100, fieldW, true);
                ThemedInput inSub = dlg.AddField("서브넷 마스크", initialSubnet, 20, 160, fieldW, true);
                ThemedInput inGw = dlg.AddField("기본 게이트웨이 (선택 사항)", initialGw, 20, 220, fieldW, true);
                ThemedInput inDns = dlg.AddField("기본 DNS 주소", initialDns, 20, 280, fieldW, true);
                TextBox txtIp = inIp.TextBox;
                TextBox txtSub = inSub.TextBox;
                TextBox txtGw = inGw.TextBox;
                TextBox txtDns = inDns.TextBox;

                Label lblHint = new Label
                {
                    Text = "올바른 IPv4 형식(예: 192.168.1.100)을 입력하십시오.",
                    Font = Theme.Ui(8f),
                    ForeColor = Theme.TextMuted,
                    Location = new Point(20, 346),
                    Size = new Size(fieldW, 18)
                };

                ModernButton btnOk = ThemedDialog.PrimaryButton("적용 및 프로필 저장");
                btnOk.IconName = "check";
                btnOk.IconSize = 13;
                btnOk.IconColor = Color.White;
                btnOk.Size = new Size(190, 40);
                btnOk.Location = new Point(20, 384);

                ModernButton btnCancel = ThemedDialog.SecondaryButton("취소");
                btnCancel.Size = new Size(fieldW - 190 - 8, 40);
                btnCancel.Location = new Point(20 + 190 + 8, 384);
                btnCancel.Click += (s, e) => { dlg.DialogResult = DialogResult.Cancel; dlg.Close(); };

                Action<ThemedInput, bool> mark = (input, ok) => { input.StateBorder = ok ? Color.Empty : Theme.Danger; };

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

                    mark(inIp, ipValid);
                    mark(inSub, subValid);
                    mark(inGw, gwValid);
                    mark(inDns, dnsValid);

                    if (!ipValid)
                    {
                        lblHint.Text = "⚠ IPv4 주소 형식이 올바르지 않습니다.";
                        lblHint.ForeColor = Theme.Danger;
                        btnOk.Enabled = false;
                    }
                    else if (!subValid)
                    {
                        lblHint.Text = "⚠ 서브넷 마스크 형식이 올바르지 않습니다.";
                        lblHint.ForeColor = Theme.Danger;
                        btnOk.Enabled = false;
                    }
                    else if (!gwValid)
                    {
                        lblHint.Text = "⚠ 게이트웨이 주소 형식이 올바르지 않습니다.";
                        lblHint.ForeColor = Theme.Danger;
                        btnOk.Enabled = false;
                    }
                    else if (!dnsValid)
                    {
                        lblHint.Text = "⚠ DNS 주소 형식이 올바르지 않습니다.";
                        lblHint.ForeColor = Theme.Danger;
                        btnOk.Enabled = false;
                    }
                    else if (!subnetMatch)
                    {
                        lblHint.Text = "⚠ IP와 게이트웨이가 서로 다른 서브넷 대역입니다.";
                        lblHint.ForeColor = Theme.Warning;
                        inGw.StateBorder = Theme.Warning;
                        btnOk.Enabled = true;
                    }
                    else
                    {
                        lblHint.Text = "✔ 유효한 네트워크 주소입니다.";
                        lblHint.ForeColor = Theme.Success;
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

                dlg.Controls.Add(lblHint);
                dlg.Controls.Add(btnOk);
                dlg.Controls.Add(btnCancel);
                dlg.CancelButton = btnCancel;

                dlg.ShowDialog(this);
            }
        }

        /// <summary>
        /// 기억된 Wi-Fi AP 프로필 목록 다이얼로그
        /// </summary>
        public void ShowSavedProfilesDialog()
        {
            const int W = 360;
            using (ThemedDialog dlg = new ThemedDialog("저장된 AP 네트워크 프로필", "list", W, 440))
            {
                Panel pnlList = new Panel
                {
                    Location = new Point(16, 54),
                    Size = new Size(W - 32, 316),
                    AutoScroll = true,
                    BackColor = Theme.Bg
                };

                ToolTip cardToolTip = new ToolTip();
                dlg.Disposed += (s, e) => { cardToolTip.Dispose(); };

                Action renderCards = null;
                renderCards = () =>
                {
                    pnlList.SuspendLayout();
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
                        Label lblEmptyIcon = new Label
                        {
                            Location = new Point(0, 92),
                            Size = new Size(pnlList.Width, 36),
                            Image = UiIcons.Get("wifi", 30, Theme.TextSubtle),
                            ImageAlign = ContentAlignment.MiddleCenter
                        };
                        Label lblEmpty = new Label
                        {
                            Text = "저장된 Wi-Fi AP 프로필이 없습니다.\nWi-Fi 연결 후 [고정 IP 설정]으로 등록하십시오.",
                            Font = Theme.Ui(8.5f),
                            ForeColor = Theme.TextMuted,
                            Location = new Point(0, 134),
                            Size = new Size(pnlList.Width, 44),
                            TextAlign = ContentAlignment.MiddleCenter
                        };
                        pnlList.Controls.Add(lblEmptyIcon);
                        pnlList.Controls.Add(lblEmpty);
                        pnlList.ResumeLayout();
                        return;
                    }

                    // 스크롤바가 필요한 경우 카드 폭을 줄여 가로 스크롤 방지
                    int cardH = 70;
                    int gapY = 8;
                    bool needsScroll = list.Count * (cardH + gapY) > pnlList.Height;
                    int cardW = pnlList.Width - (needsScroll ? SystemInformation.VerticalScrollBarWidth + 4 : 0);

                    int y = 0;
                    foreach (ApProfile p in list)
                    {
                        string ssidKey = p.Ssid;
                        bool isStatic = p.Mode == "static";

                        RoundedPanel card = new RoundedPanel
                        {
                            Location = new Point(0, y),
                            Size = new Size(cardW, cardH),
                            Radius = 11,
                            BackColor = Theme.Surface,
                            BorderColor = Theme.Border
                        };

                        Label lblSsidName = new Label
                        {
                            Text = p.Ssid,
                            Font = Theme.Ui(9.5f, FontStyle.Bold),
                            ForeColor = Theme.Text,
                            Location = new Point(12, 9),
                            Size = new Size(cardW - 150, 20),
                            AutoEllipsis = true,
                            Cursor = Cursors.Hand
                        };

                        PillLabel lblModeBadge = new PillLabel
                        {
                            Text = isStatic ? "고정 IP" : "DHCP",
                            ForeColor = isStatic ? Theme.Accent : Theme.Success,
                            PillColor = isStatic ? Theme.AccentTint : Theme.SuccessTint,
                            Size = new Size(52, 20),
                            Location = new Point(cardW - 138, 10)
                        };

                        string detailText = isStatic
                            ? string.Format("IP {0}  ·  GW {1}", p.Ip, string.IsNullOrEmpty(p.Gateway) ? "-" : p.Gateway)
                            : "공유기 자동 할당 (DHCP)";

                        Label lblDetails = new Label
                        {
                            Text = detailText,
                            Font = Theme.Mono(8f),
                            ForeColor = Theme.TextMuted,
                            Location = new Point(12, 33),
                            Size = new Size(cardW - 24, 15),
                            AutoEllipsis = true
                        };

                        string dnsText = isStatic ? ("DNS " + (p.Dns ?? "-")) : "";
                        Label lblDnsLine = new Label
                        {
                            Text = dnsText,
                            Font = Theme.Mono(8f),
                            ForeColor = Theme.TextSubtle,
                            Location = new Point(12, 49),
                            Size = new Size(cardW - 24, 15),
                            AutoEllipsis = true
                        };

                        ModernButton btnEdit = new ModernButton
                        {
                            IconName = "pencil",
                            IconSize = 12,
                            IconColor = Theme.TextMuted,
                            IconHoverColor = Theme.Accent,
                            BackColor = Color.Transparent,
                            HoverBackColor = Theme.SurfaceHover,
                            Radius = 7,
                            Size = new Size(28, 26),
                            Location = new Point(cardW - 78, 7),
                            TabStop = false
                        };
                        btnEdit.Click += (es, ee) =>
                        {
                            ShowRenameProfileDialog(dlg, ssidKey, () => { renderCards(); });
                        };

                        // 클릭: 카드 선택 강조 / 더블클릭: 이 프로필 설정으로 즉시 변경
                        ApProfile cardProfile = p;
                        EventHandler selectCard = (cs, ce) =>
                        {
                            foreach (Control other in pnlList.Controls)
                            {
                                RoundedPanel rp = other as RoundedPanel;
                                if (rp != null) rp.BorderColor = Theme.Border;
                            }
                            card.BorderColor = Theme.AccentStrong;
                        };
                        EventHandler applyCard = (cs, ce) =>
                        {
                            ApplySavedProfile(cardProfile);
                            dlg.Close();
                        };
                        foreach (Control target in new Control[] { card, lblSsidName, lblModeBadge, lblDetails, lblDnsLine })
                        {
                            target.Cursor = Cursors.Hand;
                            target.Click += selectCard;
                            target.DoubleClick += applyCard;
                            cardToolTip.SetToolTip(target, "더블클릭하여 이 설정으로 변경");
                        }

                        ModernButton btnDel = new ModernButton
                        {
                            IconName = "trash-2",
                            IconSize = 12,
                            IconColor = Theme.TextMuted,
                            IconHoverColor = Theme.Danger,
                            BackColor = Color.Transparent,
                            HoverBackColor = Theme.DangerTint,
                            Radius = 7,
                            Size = new Size(28, 26),
                            Location = new Point(cardW - 44, 7),
                            TabStop = false
                        };
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

                        cardToolTip.SetToolTip(btnEdit, "이름 변경");
                        cardToolTip.SetToolTip(btnDel, "삭제");

                        card.Controls.Add(lblSsidName);
                        card.Controls.Add(lblModeBadge);
                        card.Controls.Add(lblDetails);
                        card.Controls.Add(lblDnsLine);
                        card.Controls.Add(btnEdit);
                        card.Controls.Add(btnDel);

                        pnlList.Controls.Add(card);
                        y += cardH + gapY;
                    }
                    pnlList.ResumeLayout();
                };

                renderCards();

                ModernButton btnClearAll = new ModernButton
                {
                    Text = "전체 삭제",
                    IconName = "trash-2",
                    IconSize = 12,
                    IconColor = Theme.DangerSoft,
                    Font = Theme.Ui(8.5f),
                    ForeColor = Theme.DangerSoft,
                    BackColor = Color.Transparent,
                    HoverBackColor = Theme.DangerTint,
                    BorderColor = Theme.Blend(Theme.Bg, Theme.Danger, 0.35f),
                    Radius = 9,
                    Size = new Size(104, 38),
                    Location = new Point(16, 384)
                };
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

                ModernButton btnClose = ThemedDialog.SecondaryButton("닫기");
                btnClose.Size = new Size(92, 38);
                btnClose.Location = new Point(W - 16 - 92, 384);
                btnClose.Click += (s, e) => { dlg.Close(); };

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

            const int W = 340;
            const int fieldW = W - 40;
            using (ThemedDialog dlg = new ThemedDialog("AP 프로필 이름 수정", "pencil", W, 222))
            {
                Label lblInputPrompt = new Label
                {
                    Text = "새 이름 (현재: " + oldSsid + ")",
                    Font = Theme.Ui(8.5f),
                    ForeColor = Theme.TextMuted,
                    Location = new Point(20, 56),
                    Size = new Size(fieldW, 18),
                    AutoEllipsis = true
                };

                ThemedInput inName = new ThemedInput(false)
                {
                    Location = new Point(20, 78),
                    Width = fieldW
                };
                TextBox txtNewName = inName.TextBox;
                txtNewName.MaxLength = 32;
                txtNewName.Text = oldSsid;

                Label lblHint = new Label
                {
                    Font = Theme.Ui(8f),
                    ForeColor = Theme.TextMuted,
                    Location = new Point(20, 118),
                    Size = new Size(fieldW, 34)
                };

                ModernButton btnSave = ThemedDialog.PrimaryButton("저장");
                btnSave.IconName = "check";
                btnSave.IconSize = 13;
                btnSave.IconColor = Color.White;
                btnSave.Size = new Size(190, 40);
                btnSave.Location = new Point(20, 164);

                ModernButton btnCancel = ThemedDialog.SecondaryButton("취소");
                btnCancel.Size = new Size(fieldW - 190 - 8, 40);
                btnCancel.Location = new Point(20 + 190 + 8, 164);
                btnCancel.Click += (s, e) => { dlg.DialogResult = DialogResult.Cancel; dlg.Close(); };

                Action validateInput = () =>
                {
                    string newName = txtNewName.Text.Trim();
                    if (string.IsNullOrEmpty(newName) || newName.Length > 32 || !SecurityHelper.IsSafeSsid(newName))
                    {
                        lblHint.Text = "⚠ 1~32자 이내, 특수문자(\", &, |, ;, `, $, >, <, \\) 제외";
                        lblHint.ForeColor = Theme.Danger;
                        inName.StateBorder = Theme.Danger;
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
                        lblHint.ForeColor = Theme.Danger;
                        inName.StateBorder = Theme.Danger;
                        btnSave.Enabled = false;
                        return;
                    }

                    lblHint.Text = "✔ 사용 가능한 이름입니다.";
                    lblHint.ForeColor = Theme.Success;
                    inName.StateBorder = Color.Empty;
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
                dlg.Controls.Add(lblInputPrompt);
                dlg.Controls.Add(inName);
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
            menu.ShowImageMargin = true;
            menu.BackColor = Theme.Surface;
            menu.ForeColor = Theme.Text;
            menu.Font = Theme.Ui(9f);
            menu.Closed += (s, e) => { menu.BeginInvoke(new Action(() => { menu.Dispose(); })); };

            ToolStripLabel titleItem = new ToolStripLabel("주변 Wi-Fi AP");
            titleItem.ForeColor = Theme.TextSubtle;
            titleItem.Font = Theme.Ui(7.5f, FontStyle.Bold);
            menu.Items.Add(titleItem);

            List<string> aps = ScanNearbyWifiAps();
            if (aps.Count == 0)
            {
                ToolStripLabel noneItem = new ToolStripLabel("검색된 Wi-Fi AP 없음");
                noneItem.ForeColor = Theme.TextMuted;
                menu.Items.Add(noneItem);
            }
            else
            {
                foreach (string ap in aps)
                {
                    string targetSsid = ap;
                    bool isCurrent = targetSsid.Equals(this.currentSsid, StringComparison.OrdinalIgnoreCase);
                    bool isSaved = FindSavedProfile(targetSsid) != null;
                    string label = targetSsid + (isCurrent ? "  (현재 연결)" : (isSaved ? "  · 저장됨" : ""));
                    Image img = isCurrent
                        ? UiIcons.Get("check", 14, Theme.Success)
                        : UiIcons.Get("wifi", 14, isSaved ? Theme.Accent : Theme.TextMuted);

                    ToolStripMenuItem item = new ToolStripMenuItem(label, img, (s, e) =>
                    {
                        if (!isCurrent)
                        {
                            ConnectToSsid(targetSsid);
                        }
                    });
                    if (isCurrent) item.Font = Theme.Ui(9f, FontStyle.Bold);
                    menu.Items.Add(item);
                }
            }

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Wi-Fi 상태 새로고침", UiIcons.Get("refresh-cw", 14, Theme.TextMuted), (s, e) => { this.RefreshNetworkStatus(); });

            menu.Show(anchor, new Point(0, anchor.Height + 4));
        }

        /// <summary>
        /// 커스텀 알림 모달 다이얼로그
        /// </summary>
        public void ShowCustomAlert(string message, string title)
        {
            const int W = 330;
            using (ThemedDialog dlg = new ThemedDialog(string.IsNullOrEmpty(title) ? "AutoNet 안내" : title, "wifi", W, 176))
            {
                Label lblM = new Label
                {
                    Text = message ?? string.Empty,
                    Font = Theme.Ui(9f),
                    ForeColor = Theme.TextSecondary,
                    Location = new Point(20, 56),
                    Size = new Size(W - 40, 54)
                };

                ModernButton btnOk = ThemedDialog.PrimaryButton("확인");
                btnOk.Size = new Size(W - 40, 40);
                btnOk.Location = new Point(20, 118);
                btnOk.Click += (s, e) => { dlg.Close(); };

                dlg.Controls.Add(lblM);
                dlg.Controls.Add(btnOk);
                dlg.AcceptButton = btnOk;

                dlg.ShowDialog(this);
            }
        }
    }
}
