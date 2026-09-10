# NDToolsBox-3dsMax

3ds Max 插件盒子（NDToolsBox / 天晴盒子）客户端源码。

从 `Git_CSharp` 单体仓库拆分为独立项目。

## 环境

- Visual Studio 2017+（建议 2022）
- .NET Framework（各 Max 版本工程见对应 `Max20xx` 目录）
- 需安装对应版本的 Autodesk 3ds Max SDK / 程序集引用

## 打开工程

打开根目录 `NDToolsBox.sln`。

还原 NuGet 包后编译目标 Max 版本工程。

## 批量编译

各 Max 年版需分别输出同名 `NDToolsBox.dll`（依赖该年 `Autodesk.Max`）。可用根目录脚本按本机已安装版本一键编译：

```powershell
.\build-all.ps1
.\build-all.ps1 -Configuration Release
.\build-all.ps1 -Years 2022,2023,2024
```

- 默认 `Debug` + `x64`；探测 `%ProgramFiles%\Autodesk\3ds Max {年}\` 与 `D:\Program Files\Autodesk\3ds Max {年}\` 下是否存在 `Autodesk.Max.dll`。
- 未安装的年份会 **Skip**，不算失败；仅编译失败才返回非 0。
- `-Force`：即使未检测到 Max 也尝试编译（CI / 自定义 SDK 路径）。
- `-MsBuild`：可指定 `MSBuild.exe` 路径。
- `-NoPause`：结束后不暂停（CI）；默认会 `Press Enter`，避免窗口一闪关闭。
- 不编译杂糅工程 `TreeViewWithViewModelDemo\NDToolsBox.csproj` 及测试辅助工程。

输出复制到 `dist\{年份}\assemblies\NDToolsBox.dll`（若存在则一并复制 `.pdb`）。工程目录下的 `bin\` 仍会保留中间产物。各 `Max20xx` 工程的 PostBuild 拷贝到 Max 安装目录已清空，避免 Max 占用 DLL 或旧路径导致 MSB3073。

**重要（调试时必看）：** 3ds Max 加载的是安装目录里的程序集，例如：

`D:\Program Files\Autodesk\3ds Max 2022\bin\assemblies\NDToolsBox.dll`

VS 编到 `Max2022\bin\Debug\` 或 `bin\x64\Debug\` **不会自动覆盖**上述路径。Max 开着时该文件会被锁定。请：

1. **完全退出** `3dsmax.exe`  
2. 复制最新 DLL，例如：

```powershell
Copy-Item -Force "G:\ND_openSource\NDToolsBox-3dsMax\Max2022\bin\Debug\NDToolsBox.dll" `
  "D:\Program Files\Autodesk\3ds Max 2022\bin\assemblies\NDToolsBox.dll"
```

3. 再启动 Max 验证（自定义 Tab 右键应出现「新建列表组」）

编译日志：

- 总日志：`dist\logs\build-all-yyyyMMdd-HHmmss.log`
- 各年版：`dist\{年份}\build.log`、`dist\{年份}\build.msbuild.log`（含完整错误信息）

## ToolbarsV 用户配置

侧边工具栏（`ToolbarsV`）的用户数据写在插件安装目录，代码里对应 `WebAddress.apppath`：

`C:\ProgramData\Autodesk\ApplicationPlugins\NDToolsBox`

| 文件 | 内容 |
|------|------|
| `ToolBar.xml` | 「动画」页按钮列表（常用工具、时间段等） |
| `ToolBarTabs.xml` | 用户自定义 Tab：`Id`、标题、`ListId`（该 Tab 唯一列表配置名） |
| `{控件名}.xml` | 「绑定」页三列列表，对应 XAML 中 `NDListBox` 的 `Name`：`MyListBox_Rig.xml`、`MyListBox_Rig_2.xml`、`MyListBox_Rig_3.xml` |
| `CustomTab_{Id}.xml` | 自定义 Tab 整页配置：`CustomTabListsViewModle`，`Items` 中每一项是一组 Expander+ListBox |

说明：

- 点击左侧 `+` 会新建 Tab，并写入 `ToolBarTabs.xml`；该 Tab 的列表首次保存或改动后生成对应 `CustomTab_*.xml`（默认一段 Expander）。
- 自定义 Tab 右键「重命名」只改 `ToolBarTabs.xml` 中的标题；「删除」会从该文件移除条目，并删除该 Tab 的列表 xml。
- 列表内按钮改名、增删、复制粘贴、设置上下间距、刷新，都作用在整份 `CustomTab_*.xml` 上。
- 旧版单列表 `NDListBoxViewModle` xml 加载时自动包成一段；旧版三列 `ListIds` 仍迁到单个 `ListId`。
- 备份或迁移用户工具条时，复制上述目录中的这些 xml 即可。

外部程序若要批量创建 / 更新自定义 Tab，见：[docs/external-tool-custom-tabs.md](docs/external-tool-custom-tabs.md)（多段 `Items`、合并写回与刷新生效方式）。

## 相关

- 资源打包工具：`NDToolsResourcesPack`
- 安装/下载入口：https://sundaybox.cc/ndtooldata/

## 开源组织

本项目由以下组织开源：

- 网龙网络公司
- 福建天晴数码有限公司

## 开源协议

本项目采用 [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0) 开源协议。

你可以自由使用、修改和分发本软件，包括用于商业用途，但须遵守该协议的要求，例如保留版权与许可声明，并在修改时注明变更。软件按「原样」提供，不附带任何明示或暗示的担保。

## Bug 修复记录（2026-09）

以下缺陷已修复（**未改** `ResetMaxCUI`，该工具仍仅覆盖 2015–2018 且假定 ENU/CHS 路径）。

### 高优先级

| 问题 | 修复说明 |
|------|----------|
| Max 2021+ `InstallMenus` 访问 `actionTable[1]` 越界 | 按版本分别绑定：2015–2020 为 Float + SelectSet；2021+ 仅 SelectSet 在 `[0]` |
| GUP `Stop()` 未注销 `SystemPostNew` | `Stop()` 中对称 `UnRegisterNotification` |
| 选择集浮动条 DPI 坐标重复换算 | WPF 统一使用 DIP；移除 `Graphics.FromHwnd` 错误除法，消除 GDI 泄漏 |
| 侧边工具栏 `TimerangeChange` 未注销 | `ToolbarsV` 在 `Unloaded` 中反注册 |
| `GetRootPathTree` XML 缺失时空引用 | `rootnode == null` 时直接返回空树 |

### 中优先级

| 问题 | 修复说明 |
|------|----------|
| 空命名选择集无法添加节点 | `AddNodeToSeleSet` 仅在移除且列表为空时提前返回 |
| XML 节点缺字段崩溃 | `NewXmlNodePerson` / `SetPath` 安全取值与空判断 |
| `Path.Combine` 参数为 null | `CfgHelpPersonXml` / `CfgHelpPerson` 使用 `?? ""` |
| 版本号 `Parse` 无防护 | 改为 `TryParse`；异步拉取远程版本后再比较更新提示 |
| 搜索「下一项」逻辑错误 / 空集合越界 | 使用 enumerator `Current` 并选中；访问前检查 `Count` |
| `PersonViewModel.StartupFolder` / `SubPath` 错误递归 | 改为读取 `_person` 字段 |
| `NDBoxToolbarItem` 图标路径为 null | 创建 `Uri` 前判空 |
| `TcpClient` 失败路径泄漏 | `using` + `EndConnect` |
| `Ini.endWithCRLF` 空串越界 | `Length < 2` 直接返回 false |
| `GetSeteNames` 吞异常 | 输出 Listener 错误信息 |
| TreeView 取消选中 NRE | `as` 判空后再更新 |
| MaxScript 路径含引号 | 导入/导出选择集前校验路径 |
| `GetDir(20)` 魔法数字 | 提取为命名常量 `MaxSysRootDirIndex` 并注释 |
| 主题色字符串尾随空格 | 去掉 `colorTheme_light` 中多余空格 |
| 打开帮助失败无反馈 | `help_Click` 捕获异常后提示用户 |

涉及主要文件：`GlobalUtility.cs`、`Database.cs`、`SelectSetCuiDock.cs`、`SelectSetToolBar.xaml.cs`、`ToolbarsV.xaml.cs`、`ScriptsUtilities.cs`（含 Max2022 副本）、`FamilyTreeViewModel.cs`、`TextSearchDemoControl.xaml.cs`、`PersonViewModel.cs`、`WebAddress.cs`、`Ini.cs`、`NDBoxToolbarItem.cs`、`CfgHelpPersonXml.cs` 等。
