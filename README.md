# VCMounter — VeraCrypt 取证挂载助手

一个轻量的 VeraCrypt 容器挂载辅助工具（Windows Forms，单文件 C# 实现），**为解决 VeraCrypt 原生 GUI 闪退问题而生**，并为电子取证场景做了易用性优化。

## 为什么做这个工具

**直接起因：VeraCrypt 原生图形界面在部分取证环境机上点击就闪退**——双击 VeraCrypt.exe 窗口一闪就没，或一点「选择文件」就崩溃（常见原因：第三方 Shell 扩展注入文件对话框后崩溃、VC 为非官方编译版、驱动服务异常等）。

排查发现：绕开原生 GUI，直接调用 VeraCrypt 的**命令行模式**可以稳定工作。于是有了这个壳工具——VCMounter 负责界面交互（用自己的 Windows 文件对话框选文件），挂载/卸载动作全部通过命令行参数（`/v /l /a /q /m ro`）交给 VeraCrypt 后台完成，彻底绕开出问题的原生 GUI。

在解决闪退问题的同时，也顺手补齐了取证场景的易用性短板。

## 功能

| 功能 | 说明 |
|---|---|
| 绕过 VC GUI 闪退 | 命令行内核稳定挂载，不依赖原生界面 |
| 选择容器 | Windows 原生文件对话框，**默认"所有文件"**——dd/E01/raw/无后缀检材直接可见可选，不用手动切过滤器 |
| 记忆功能 | 记住上次浏览目录和检材文件，下次启动自动带入 |
| 选择盘符 | 下拉列出 E–Z 所有盘符，实时显示「已挂载 / 空闲」状态 |
| 输入密码 | 密码框可切换显示/隐藏；留空则由 VeraCrypt 弹框输入（更安全） |
| 挂载 | 调用 `VeraCrypt-x64.exe /v /l /a /q /p` 完成挂载 |
| 卸载 | 调用 `/d /q /f` 卸载指定盘符 |
| 只读挂载 | 勾选后以只读模式挂载（取证场景推荐） |
| 状态反馈 | 挂载/卸载结果实时显示在状态栏 |
| 自动查找 VC | 配置 → 同目录 → 子目录 → 注册表 → 常见路径，零配置 |

## 使用方法

1. 双击 `VCMounter.exe`（弹出 UAC，确认后打开界面）
2. 确认顶部「VC程序」路径正确（自动查找，找不到点「更改」手动选，之后自动记住）
3. 点「浏览…」选择检材/容器文件
4. 在「盘符」下拉选择目标盘符（看右侧状态，选「空闲」的）
5. 输入密码（建议勾选「只读挂载」）
6. 点「▶ 挂载」，状态栏显示结果
7. 用完点「■ 卸载」释放盘符

> 依赖：Windows 7+ / .NET Framework 4.x（系统自带）+ 本机已安装 [VeraCrypt](https://www.veracrypt.fr/)（官方版或 portable 版均可）

> **给别人的最简方案**：把 `VCMounter.exe` 直接放进 VeraCrypt 目录（和 `VeraCrypt-x64.exe` 同级），同目录命中，零配置即用。

## VeraCrypt 主程序查找顺序

1. **配置文件** `VCMounter.ini`（记住上次手动选的路径）
2. **同目录**（exe 所在目录）
3. **同目录的 `VeraCrypt\` 子目录**
4. **注册表** Uninstall 项（扫描 DisplayName 含 "VeraCrypt" 的安装记录）
5. **常见安装路径**（`C:\Program Files\VeraCrypt`、`C:\Program Files (x86)\VeraCrypt`）

全部找不到时，路径框显示红色提示，点「更改」手动选择一次即可，路径会自动记住。

## 从源码编译

```bash
# 方式一：dotnet Roslyn 编译（仓库自带 build_vcmounter.sh 即此方式，按本机 SDK 路径微调）
dotnet "C:/Program Files/dotnet/sdk/<版本>/Roslyn/bincore/csc.dll" \
  -nologo -target:winexe -platform:anycpu \
  -out:VCMounter.exe -win32manifest:app.manifest \
  -r:"C:/Windows/Microsoft.NET/Framework64/v4.0.30319/mscorlib.dll" \
  -r:"C:/Windows/Microsoft.NET/Framework64/v4.0.30319/System.dll" \
  -r:"C:/Windows/Microsoft.NET/Framework64/v4.0.30319/System.Drawing.dll" \
  -r:"C:/Windows/Microsoft.NET/Framework64/v4.0.30319/System.Windows.Forms.dll" \
  VCMounter.cs
```

```bat
rem 方式二：Windows 自带的 .NET Framework 编译器（开发者命令行）
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /out:VCMounter.exe /win32manifest:app.manifest /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.dll VCMounter.cs
```

## 文件清单

| 文件 | 说明 |
|---|---|
| `VCMounter.exe` | 编译好的可执行文件 |
| `VCMounter.cs` | C# 源码（单文件） |
| `app.manifest` | exe 清单（管理员提权） |
| `build_vcmounter.sh` | 一键构建脚本（dotnet Roslyn） |
| `VCMounter.ini` | 运行后自动生成，记录 VC 路径 / 上次浏览目录 / 上次检材 |

## 常见问题

### Q：不小心对物理磁盘（如 F:、G:）点了「卸载」会丢数据吗？
**不会。** VeraCrypt 的 `/d` 命令只能卸载 VeraCrypt 自己挂载的卷。如果目标盘符是物理磁盘，VC 会发现它不在自己的卷列表里，直接报错返回，**不会对物理磁盘做任何操作**。最多看到一行「卸载失败」的报错，磁盘数据纹丝不动。

### Q：为什么用这个工具而不是直接用 VeraCrypt？
部分环境下 VeraCrypt GUI 点「选择文件」会闪退（常见原因：第三方 Shell 扩展注入文件对话框后崩溃、VC 为非官方编译版、驱动服务异常等）。本工具用独立的 Windows 文件对话框选文件，再用命令行调用 VC 完成挂载，绕开了闪退环节。

### Q：密码安全吗？
密码通过命令行参数 `/p` 传递给 VeraCrypt。如果对安全性有更高要求，**密码框留空**，挂载时 VeraCrypt 会自行弹出密码输入框，密码不会经过命令行。

### Q：挂载失败提示 ExitCode 非零？
多为密码错误或盘符被占用；VeraCrypt 命令行报错时会静默返回非零码，可先用原生 GUI（如果能启动）验证卷和密码是否正常。

## 注意事项

- 仅支持 VeraCrypt 加密卷的挂载/卸载；物理磁盘类检材 VeraCrypt 会拒绝操作，数据不受影响
- 取证操作务必勾选「只读挂载」
- 本工具不实现加密/解密，所有加解密由 VeraCrypt 完成

## License

MIT
