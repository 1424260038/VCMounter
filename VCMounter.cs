using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

class VCMounter : Form
{
    private TextBox txtVCPath;
    private TextBox txtFile;
    private ComboBox cboDrive;
    private TextBox txtPassword;
    private TextBox txtStatus;
    private Label lblDriveState;
    private Button btnMount;
    private Button btnUnmount;

    private static readonly string[] VC_NAMES_64 = { "VeraCrypt-x64.exe", "VeraCrypt.exe" };
    private static readonly string[] VC_NAMES_32 = { "VeraCrypt.exe", "VeraCrypt-x64.exe" };

    public VCMounter()
    {
        this.Text = "VeraCrypt 挂载工具";
        this.Size = new Size(580, 430);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.Font = new Font("Microsoft YaHei UI", 9F);

        int y = 18;
        // VC 路径
        var lblVC = new Label { Text = "VC程序:", Location = new Point(15, y + 3), AutoSize = true };
        txtVCPath = new TextBox { Location = new Point(100, y), Size = new Size(340, 25), ReadOnly = true, BackColor = SystemColors.Window };
        var btnChangeVC = new Button { Text = "更改…", Location = new Point(450, y - 2), Size = new Size(80, 28) };
        btnChangeVC.Click += BtnChangeVC_Click;
        y += 38;

        // 容器文件
        var lblFile = new Label { Text = "容器文件:", Location = new Point(15, y + 3), AutoSize = true };
        txtFile = new TextBox { Location = new Point(100, y), Size = new Size(340, 25), ReadOnly = true, BackColor = SystemColors.Window };
        var btnBrowse = new Button { Text = "浏览…", Location = new Point(450, y - 2), Size = new Size(80, 28) };
        btnBrowse.Click += BtnBrowse_Click;
        y += 38;

        // 盘符
        var lblDrive = new Label { Text = "盘    符:", Location = new Point(15, y + 3), AutoSize = true };
        cboDrive = new ComboBox { Location = new Point(100, y), Size = new Size(60, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        LoadAllDrives();
        cboDrive.SelectedIndexChanged += (s, e) => UpdateDriveState();
        lblDriveState = new Label { Text = "", Location = new Point(170, y + 3), AutoSize = true, ForeColor = Color.Gray };
        var btnRefresh = new Button { Text = "刷新", Location = new Point(450, y - 2), Size = new Size(80, 28) };
        btnRefresh.Click += (s, e) => { LoadAllDrives(); UpdateDriveState(); };
        y += 38;

        // 密码
        var lblPass = new Label { Text = "密    码:", Location = new Point(15, y + 3), AutoSize = true };
        txtPassword = new TextBox { Location = new Point(100, y), Size = new Size(340, 25), PasswordChar = '*' };
        var chkShow = new CheckBox { Text = "显示", Location = new Point(450, y + 2), AutoSize = true };
        chkShow.CheckedChanged += (s, e) => { txtPassword.PasswordChar = chkShow.Checked ? '\0' : '*'; };
        y += 33;

        // 只读
        var chkRO = new CheckBox { Text = "只读挂载 (取证推荐)", Location = new Point(100, y), AutoSize = true };
        y += 33;

        // 按钮
        btnMount = new Button { Text = "▶ 挂载", Location = new Point(100, y), Size = new Size(150, 40), BackColor = Color.FromArgb(220, 255, 220), FlatStyle = FlatStyle.Flat };
        btnMount.Click += (s, e) => BtnMount_Click(chkRO.Checked);
        btnUnmount = new Button { Text = "■ 卸载", Location = new Point(270, y), Size = new Size(150, 40), BackColor = Color.FromArgb(255, 220, 220), FlatStyle = FlatStyle.Flat };
        btnUnmount.Click += BtnUnmount_Click;
        y += 50;

        // 状态
        var lblStatus = new Label { Text = "状态:", Location = new Point(15, y + 3), AutoSize = true };
        txtStatus = new TextBox { Location = new Point(100, y), Size = new Size(455, 80), Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 9F) };
        y += 88;

        var lblHint = new Label { Text = "首次运行自动查找 VeraCrypt; 找不到点「更改」手动指定, 之后自动记住。", Location = new Point(15, y), AutoSize = true, ForeColor = Color.Gray };

        this.Controls.AddRange(new Control[] { lblVC, txtVCPath, btnChangeVC, lblFile, txtFile, btnBrowse, lblDrive, cboDrive, lblDriveState, btnRefresh, lblPass, txtPassword, chkShow, chkRO, btnMount, btnUnmount, lblStatus, txtStatus, lblHint });

        // 初始化: 查找 VC
        string vc = FindVC();
        txtVCPath.Text = vc ?? "(未找到, 请点「更改」手动选择)";
        txtVCPath.ForeColor = vc != null ? Color.Black : Color.Red;

        // 启动时自动带入上次选择的检材文件
        string lastFile = LoadLastFile();
        if (!string.IsNullOrEmpty(lastFile) && File.Exists(lastFile)) txtFile.Text = lastFile;

        UpdateDriveState();
    }

    // ====== VC 查找逻辑 ======
    private string GetIniPath() { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VCMounter.ini"); }

    private string[] GetVCNames() { return Environment.Is64BitOperatingSystem ? VC_NAMES_64 : VC_NAMES_32; }

    private string CheckVCInDir(string dir)
    {
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return null;
        foreach (var n in GetVCNames())
        {
            string p = Path.Combine(dir, n);
            if (File.Exists(p)) return p;
        }
        return null;
    }

    private string FindVC()
    {
        // 1. 配置文件记住的路径 (VcPath= 键; 兼容旧版整文件裸路径格式)
        try
        {
            string saved = GetIniValue("VcPath");
            if (saved == null)
            {
                string ini = GetIniPath();
                if (File.Exists(ini))
                {
                    string raw = File.ReadAllText(ini).Trim();
                    // 旧格式: 整个文件就是一个路径 (不含 = 号)
                    if (raw.Length > 0 && !raw.Contains("=")) saved = raw;
                }
            }
            if (!string.IsNullOrEmpty(saved) && File.Exists(saved)) return saved;
        }
        catch { }

        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        // 2. 同目录
        string p1 = CheckVCInDir(baseDir);
        if (p1 != null) return p1;
        // 3. 同目录的 VeraCrypt 子目录
        string p1b = CheckVCInDir(Path.Combine(baseDir, "VeraCrypt"));
        if (p1b != null) return p1b;

        // 4. 注册表 Uninstall 项
        string p2 = FindVCInRegistry();
        if (p2 != null) return p2;

        // 5. 常见安装路径
        string[] common = {
            @"C:\Program Files\VeraCrypt",
            @"C:\Program Files (x86)\VeraCrypt",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "VeraCrypt")
        };
        foreach (var d in common)
        {
            string p = CheckVCInDir(d);
            if (p != null) return p;
        }
        return null;
    }

    private string FindVCInRegistry()
    {
        var roots = new[] {
            Tuple.Create(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
            Tuple.Create(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"),
            Tuple.Create(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall")
        };
        foreach (var r in roots)
        {
            try
            {
                using (var k = r.Item1.OpenSubKey(r.Item2))
                {
                    if (k == null) continue;
                    foreach (var sub in k.GetSubKeyNames())
                    {
                        using (var sk = k.OpenSubKey(sub))
                        {
                            if (sk == null) continue;
                            string name = sk.GetValue("DisplayName") as string;
                            if (name == null || name.IndexOf("VeraCrypt", StringComparison.OrdinalIgnoreCase) < 0) continue;
                            // InstallLocation
                            string loc = sk.GetValue("InstallLocation") as string;
                            if (!string.IsNullOrEmpty(loc)) { string p = CheckVCInDir(loc); if (p != null) return p; }
                            // UninstallString 推断目录
                            string unins = sk.GetValue("UninstallString") as string;
                            if (!string.IsNullOrEmpty(unins))
                            {
                                string trimmed = unins.Trim().Trim('"');
                                try
                                {
                                    string dir = Path.GetDirectoryName(trimmed);
                                    if (!string.IsNullOrEmpty(dir)) { string p = CheckVCInDir(dir); if (p != null) return p; }
                                }
                                catch { }
                            }
                            // DisplayIcon 有时也指向 exe
                            string icon = sk.GetValue("DisplayIcon") as string;
                            if (!string.IsNullOrEmpty(icon))
                            {
                                string trimmed = icon.Split(',')[0].Trim().Trim('"');
                                if (File.Exists(trimmed) && Path.GetFileName(trimmed).StartsWith("VeraCrypt", StringComparison.OrdinalIgnoreCase))
                                    return trimmed;
                            }
                        }
                    }
                }
            }
            catch { }
        }
        return null;
    }

    private void SaveVCPath(string path)
    {
        try
        {
            string ini = GetIniPath();
            var lines = ReadIniLines(ini);
            SetIniValue(lines, "VcPath", path ?? "");
            File.WriteAllLines(ini, lines);
        }
        catch { }
    }

    private string CurrentVC()
    {
        string t = txtVCPath.Text;
        return File.Exists(t) ? t : null;
    }

    private void BtnChangeVC_Click(object s, EventArgs e)
    {
        using (var ofd = new OpenFileDialog())
        {
            ofd.Title = "选择 VeraCrypt 主程序";
            ofd.Filter = "VeraCrypt (VeraCrypt*.exe)|VeraCrypt*.exe|可执行文件 (*.exe)|*.exe";
            ofd.CheckFileExists = true;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtVCPath.Text = ofd.FileName;
                txtVCPath.ForeColor = Color.Black;
                SaveVCPath(ofd.FileName);
                Status("已设置 VeraCrypt: " + ofd.FileName);
            }
        }
    }

    // ====== 盘符 ======
    private void LoadAllDrives()
    {
        object sel = cboDrive.SelectedItem;
        cboDrive.Items.Clear();
        for (char c = 'E'; c <= 'Z'; c++) cboDrive.Items.Add(c.ToString());
        string mounted = FindFirstMountedVC();
        if (mounted != null && cboDrive.Items.Contains(mounted)) cboDrive.SelectedItem = mounted;
        else if (sel != null && cboDrive.Items.Contains(sel)) cboDrive.SelectedItem = sel;
        else if (cboDrive.Items.Count > 0) cboDrive.SelectedIndex = 0;
    }

    private string FindFirstMountedVC()
    {
        for (char c = 'E'; c <= 'Z'; c++)
        {
            if (Directory.Exists(c + @":\"))
            {
                try { var di = new DriveInfo(c.ToString()); if (di.DriveType == DriveType.Removable || di.DriveType == DriveType.Fixed) return c.ToString(); }
                catch { }
            }
        }
        return null;
    }

    private void UpdateDriveState()
    {
        if (cboDrive.SelectedItem == null) return;
        string drive = cboDrive.SelectedItem.ToString();
        bool mounted = Directory.Exists(drive + @":\");
        if (mounted)
        {
            try
            {
                var di = new DriveInfo(drive);
                long sizeGB = di.TotalSize > 0 ? di.TotalSize / 1024 / 1024 / 1024 : 0;
                lblDriveState.Text = "● " + drive + ": 已挂载 (" + sizeGB + " GB)";
                lblDriveState.ForeColor = Color.Green;
                btnMount.Enabled = false; btnUnmount.Enabled = true;
            }
            catch
            {
                lblDriveState.Text = "● " + drive + ": 已挂载"; lblDriveState.ForeColor = Color.Green;
                btnMount.Enabled = false; btnUnmount.Enabled = true;
            }
        }
        else
        {
            lblDriveState.Text = "○ " + drive + ": 空闲"; lblDriveState.ForeColor = Color.Gray;
            btnMount.Enabled = true; btnUnmount.Enabled = false;
        }
    }

    // ====== 浏览容器 ======
    private void BtnBrowse_Click(object s, EventArgs e)
    {
        using (var ofd = new OpenFileDialog())
        {
            ofd.Title = "选择检材 / VeraCrypt 容器文件";
            // 默认选中"所有文件", 任何扩展名的检材 (dd/E01/raw/镜像/无后缀) 都能直接看到、直接选中
            ofd.Filter = "所有文件 (*.*)|*.*|VeraCrypt 容器 (*.hc;*.vc)|*.hc;*.vc|磁盘镜像 (*.dd;*.raw;*.img;*.e01;*.e01;*.aff;*.vhd;*.vhdx;*.vmdk)|*.dd;*.raw;*.img;*.e01;*.aff;*.vhd;*.vhdx;*.vmdk";
            ofd.FilterIndex = 1;                 // 默认"所有文件"
            ofd.CheckFileExists = true;
            ofd.DereferenceLinks = true;
            ofd.AddExtension = false;            // 不自动补扩展名
            ofd.ValidateNames = true;
            // 记住上次成功选择的目录, 下次打开直接定位
            string lastDir = LoadLastBrowseDir();
            if (!string.IsNullOrEmpty(lastDir) && Directory.Exists(lastDir))
                ofd.InitialDirectory = lastDir;
            else
            {
                // 兜底: 优先常见检材盘 (U盘/移动盘), 不再写死 F 盘
                foreach (var d in new[] { "F", "E", "G", "H", "D" })
                {
                    string p = d + @":\";
                    try { if (Directory.Exists(p)) { ofd.InitialDirectory = p; break; } } catch { }
                }
            }
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtFile.Text = ofd.FileName;
                SaveLastBrowse(Path.GetDirectoryName(ofd.FileName), ofd.FileName);
            }
        }
    }

    // ====== 上次浏览目录 / 上次文件记忆 (一次写盘) ======
    private string GetBrowseIniPath() { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VCMounter.ini"); }

    private void SaveLastBrowse(string dir, string file)
    {
        try
        {
            string ini = GetBrowseIniPath();
            var lines = ReadIniLines(ini);
            SetIniValue(lines, "LastBrowseDir", dir ?? "");
            SetIniValue(lines, "LastFile", file ?? "");
            File.WriteAllLines(ini, lines);
        }
        catch { }
    }

    private List<string> ReadIniLines(string ini)
    {
        if (File.Exists(ini))
        {
            return new List<string>(File.ReadAllLines(ini));
        }
        return new List<string>();
    }

    private void SetIniValue(List<string> lines, string key, string value)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].StartsWith(key + "=", StringComparison.Ordinal))
            {
                lines[i] = key + "=" + value;
                return;
            }
        }
        lines.Add(key + "=" + value);
    }

    private string GetIniValue(string key)
    {
        try
        {
            string ini = GetBrowseIniPath();
            if (!File.Exists(ini)) return null;
            foreach (var line in File.ReadAllLines(ini))
            {
                if (line.StartsWith(key + "=", StringComparison.Ordinal))
                    return line.Substring(key.Length + 1);
            }
        }
        catch { }
        return null;
    }

    private string LoadLastBrowseDir()
    {
        return GetIniValue("LastBrowseDir");
    }

    private string LoadLastFile()
    {
        return GetIniValue("LastFile");
    }

    // ====== 挂载 ======
    private void BtnMount_Click(bool readOnly)
    {
        string vc = CurrentVC();
        if (vc == null) { Status("错误: 未设置 VeraCrypt 程序路径。\n请点「更改」按钮选择 VeraCrypt 主程序 (VeraCrypt-x64.exe 或 VeraCrypt.exe)。"); return; }
        if (string.IsNullOrWhiteSpace(txtFile.Text) || !File.Exists(txtFile.Text)) { Status("错误: 请选择有效的容器文件。"); return; }
        if (cboDrive.SelectedItem == null) { Status("错误: 请选择盘符。"); return; }

        string drive = cboDrive.SelectedItem.ToString();
        if (Directory.Exists(drive + @":\")) { Status("错误: " + drive + ": 已被占用, 请选空闲盘符。"); return; }

        string pass = txtPassword.Text;
        string args = "/v \"" + txtFile.Text + "\" /l " + drive + " /a /q";
        if (readOnly) args += " /m ro";
        if (pass.Length > 0) args += " /p \"" + pass + "\"";

        Status("正在挂载 " + drive + ": ...\n" + (pass.Length == 0 ? "(未输入密码, 将弹出 VeraCrypt 密码框)" : ""));
        Application.DoEvents();

        var psi = new ProcessStartInfo { FileName = vc, Arguments = args, UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(vc) };
        try
        {
            var p = Process.Start(psi);
            if (!p.WaitForExit(120000)) { try { p.Kill(); } catch { } Status("挂载超时 (120 秒无响应)。"); return; }
            if (p.ExitCode == 0 && Directory.Exists(drive + @":\"))
            {
                Status("✓ 挂载成功!  " + drive + ": 已就绪。\n文件: " + txtFile.Text);
                UpdateDriveState();
            }
            else Status("✗ 挂载失败。ExitCode=" + p.ExitCode + "\n可能原因: 密码错误 / 盘符被占用 / 容器格式不支持。");
        }
        catch (Exception ex) { Status("异常: " + ex.Message); }
    }

    // ====== 卸载 ======
    private void BtnUnmount_Click(object s, EventArgs e)
    {
        string vc = CurrentVC();
        if (vc == null) { Status("错误: 未设置 VeraCrypt 程序路径。"); return; }
        if (cboDrive.SelectedItem == null) { Status("请选择要卸载的盘符。"); return; }
        string drive = cboDrive.SelectedItem.ToString();
        if (!Directory.Exists(drive + @":\")) { Status(drive + ": 未挂载, 无需卸载。"); return; }

        Status("正在卸载 " + drive + ": ...");
        Application.DoEvents();
        var psi = new ProcessStartInfo { FileName = vc, Arguments = "/d " + drive + " /q /f", UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(vc) };
        try
        {
            var p = Process.Start(psi);
            p.WaitForExit(15000);
            if (!Directory.Exists(drive + @":\"))
            {
                Status("✓ 卸载成功。  " + drive + ": 已释放。");
                UpdateDriveState();
            }
            else Status("✗ 卸载失败。ExitCode=" + p.ExitCode + "\n(VC 卷才可卸载; 若是物理磁盘 VC 会拒绝操作, 数据不受影响)");
        }
        catch (Exception ex) { Status("异常: " + ex.Message); }
    }

    private void Status(string s) { txtStatus.Text = s; txtStatus.SelectionStart = 0; txtStatus.ScrollToCaret(); }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new VCMounter());
    }
}
