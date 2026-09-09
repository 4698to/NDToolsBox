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
