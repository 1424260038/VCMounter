# VCMounter - VeraCrypt 取证挂载助手

面向电子取证场景的 VeraCrypt 容器挂载工具（Windows Forms，单文件 C# 实现）。

## 解决的问题

取证人员经常需要把检材里的 VeraCrypt 容器（`.hc` / `.vc`，或 dd/E01/raw 等镜像内的隐藏容器）挂载为只读盘符查看。VeraCrypt 原生界面流程繁琐，且文件选择框默认过滤扩展名，导致无后缀/特殊后缀的检材"看不到"。VCMounter 做了这些优化：

- **文件对话框默认"所有文件"**——任何扩展名的检材直接可见、直接选中，不用再手动切换过滤器
- **记住上次浏览目录和检材文件**——下次启动自动带入，重复工作归零
- **一键只读挂载/强制卸载**——底层调用本机 VeraCrypt 命令行（`/v /l /a /q /m ro`），不改动卷数据
- **自动定位 VeraCrypt**——同目录 → 子目录 → 注册表 → 常见安装路径，找不到可手动指定并记住
- **盘符状态实时显示**——已挂载/空闲一目了然，支持刷新

## 使用方法

1. 下载/编译得到 `VCMounter.exe`，放到任意目录（建议和 VeraCrypt 主程序同目录）
2. 双击运行（需要管理员权限，manifest 已声明 `requireAdministrator`）
3. 程序自动查找 VeraCrypt；找不到点「更改…」手动指定一次，之后自动记住
4. 点「浏览…」选择检材容器文件 → 选盘符 → 输密码 →（建议勾选）只读挂载 → 点「▶ 挂载」
5. 挂载成功后资源管理器里直接访问对应盘符；用完点「■ 卸载」

> 依赖：Windows + .NET Framework 4.x（Win10/11 自带）+ 本机已安装 [VeraCrypt](https://www.veracrypt.fr/)

## 从源码编译

需要 .NET SDK（任意版本，用其自带的 Roslyn）或 Visual Studio：

```bash
# 方式一：dotnet Roslyn 直接编译（本项目使用的构建脚本即此方式）
dotnet "C:/Program Files/dotnet/sdk/<版本>/Roslyn/bincore/csc.dll" \
  -nologo -target:winexe -platform:anycpu \
  -out:VCMounter.exe -win32manifest:app.manifest \
  -r:"C:/Windows/Microsoft.NET/Framework64/v4.0.30319/mscorlib.dll" \
  -r:"C:/Windows/Microsoft.NET/Framework64/v4.0.30319/System.dll" \
  -r:"C:/Windows/Microsoft.NET/Framework64/v4.0.30319/System.Drawing.dll" \
  -r:"C:/Windows/Microsoft.NET/Framework64/v4.0.30319/System.Windows.Forms.dll" \
  VCMounter.cs
```

```powershell
# 方式二：Visual Studio 开发者命令行
csc /nologo /target:winexe /platform:anycpu /out:VCMounter.exe /win32manifest:app.manifest /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll VCMounter.cs
```

## 文件说明

| 文件 | 用途 |
|---|---|
| `VCMounter.cs` | 全部源码（单文件） |
| `app.manifest` | 应用清单（管理员权限声明） |
| `build_vcmounter.sh` | 一键构建脚本（dotnet Roslyn） |
| `VCMounter.exe` | 已编译的可执行文件 |

运行时会在 exe 同目录生成 `VCMounter.ini` 保存配置（VC 路径、上次浏览目录、上次检材），不会写注册表。

## 注意事项

- 仅支持 VeraCrypt 加密卷的挂载/卸载；物理磁盘类检材 VeraCrypt 会拒绝操作，数据不受影响
- 取证操作务必勾选「只读挂载」
- 本工具不实现加密/解密，所有加解密由 VeraCrypt 完成

## License

MIT
