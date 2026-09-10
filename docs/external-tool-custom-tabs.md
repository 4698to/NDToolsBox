# 外部工具：创建 / 更新自定义 Tab 配置

侧边工具栏（`ToolbarsV`）支持由外部程序直接写 xml，在不打开 Max UI「+」的情况下注册自定义 Tab 并填充按钮列表。

配置目录（代码中为 `WebAddress.apppath`）：

```text
C:\ProgramData\Autodesk\ApplicationPlugins\NDToolsBox
```

每个自定义 Tab 需要 **两份文件**：

1. 在 `ToolBarTabs.xml` 中登记 Tab 元数据  
2. 单独的列表配置 `{ListId}.xml`：根为 `CustomTabListsViewModle`，其中 **`Items` 的每一项 = 一组 Expander + ListBox**

---

## 1. 总览

```text
NDToolsBox/
├── ToolBarTabs.xml          ← Tab 目录（可有多个 Tab）
├── MyRigTools.xml           ← 某 Tab 的多段列表（ListId = MyRigTools）
└── CustomTab_abc123....xml  ← UI 新建时常见命名
```

| 文件 | 作用 | 何时读取 |
|------|------|----------|
| `ToolBarTabs.xml` | Tab 的 `Id` / `Header` / `ListId` | 工具条启动时 |
| `{ListId}.xml` | 该 Tab 下多段 Expander+ListBox | 启动时；任一段右键「刷新」可再读整份 |

---

## 2. ListId 规则

**不是强制固定格式。**

- UI 点「+」时默认：`CustomTab_` + 32 位 Guid（无横线），例如 `CustomTab_a1b2c3d4e5f64789a0b1c2d3e4f50607`
- 外部工具可使用任意合法文件名字符串，例如 `MyRigTools`、`team_anim_v1`
- 实际列表路径：`{apppath}\{ListId}.xml`

约束：

- `ListId` 必须是合法文件名：不要包含 `\ / : * ? " < > |` 以及首尾空格
- 多个 Tab 的 `ListId` 建议全局唯一，避免互相覆盖
- 若 `ListId` 为空：加载时会尝试旧字段 `ListIds` 的第一项，再否则使用 `CustomTab_{Id}`

`Id` 字段：

- **必填**；为空则该 Tab 被跳过
- 用于 Tab 身份（重命名 / 删除）；与 `ListId` 可以不同
- 建议全局唯一（Guid 或业务稳定 id 均可）

---

## 3. `ToolBarTabs.xml` 格式

根类型名必须为 **`ToolBarTabsConfig`**（与 `XmlSerializer` 一致）。

编码：插件读取使用 **UTF-8（无 BOM）**。外部写入请用 UTF-8；声明可写 `encoding="utf-8"`。

### 最小示例（新建一个 Tab）

```xml
<?xml version="1.0" encoding="utf-8"?>
<ToolBarTabsConfig xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <Tabs>
    <Tab>
      <Id>my_rig_tools</Id>
      <Header>绑定工具</Header>
      <ListId>MyRigTools</ListId>
    </Tab>
  </Tabs>
</ToolBarTabsConfig>
```

### 字段说明

| 元素 | 必填 | 说明 |
|------|------|------|
| `Id` | 是 | Tab 唯一标识；空则忽略该条 |
| `Header` | 否 | 侧栏标题；空则显示「自定义」 |
| `ListId` | 否* | 列表文件名（不含 `.xml`）；空则按上文规则回退 |

\* 建议外部工具始终显式写 `ListId`，避免依赖回退规则。

### 合并已有配置（重要）

若用户目录里已有 `ToolBarTabs.xml`，外部工具应：

1. 读取现有文件  
2. 在 `<Tabs>` 中追加 / 更新对应 `<Tab>`（按 `Id` 匹配）  
3. 写回整文件  

**不要**用只含自己一个 Tab 的文件直接覆盖，否则会丢掉用户其它自定义 Tab。

旧版三列 `ListIds` 仍可被读入（取第一列迁到 `ListId`）；新保存不再写出 `ListIds`。外部工具请只写单个 `ListId`。

---

## 4. `{ListId}.xml` 格式（多段 Expander + ListBox）

根类型名必须为 **`CustomTabListsViewModle`**（注意拼写：`Modle`）。

- 顶层 **`Items`**：每一项是一段 UI = **一个 Expander + 一个 ListBox**
- 每一段类型为 **`NDListBoxViewModle`**，其内部再有 `Items` 存放按钮

同样建议 **UTF-8（无 BOM）**。

### 示例（两段列表）

```xml
<?xml version="1.0" encoding="utf-8"?>
<CustomTabListsViewModle xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <Items>
    <NDListBoxViewModle>
      <Header>...</Header>
      <ItemMarginTop>1</ItemMarginTop>
      <ItemMarginBottom>1</ItemMarginBottom>
      <Items>
        <toolbarItemViewModle>
          <Space>false</Space>
          <Path></Path>
          <Commit>print "section 1"</Commit>
          <Name>第一组按钮</Name>
          <ToolTip></ToolTip>
        </toolbarItemViewModle>
      </Items>
    </NDListBoxViewModle>
    <NDListBoxViewModle>
      <Header>...</Header>
      <ItemMarginTop>1</ItemMarginTop>
      <ItemMarginBottom>1</ItemMarginBottom>
      <Items>
        <toolbarItemViewModle>
          <Space>false</Space>
          <Path>C:\Scripts\my_tool.ms</Path>
          <Commit></Commit>
          <Name>第二组按钮</Name>
          <ToolTip></ToolTip>
        </toolbarItemViewModle>
      </Items>
    </NDListBoxViewModle>
  </Items>
</CustomTabListsViewModle>
```

### 段（`NDListBoxViewModle`）字段

| 元素 | 说明 |
|------|------|
| `Header` | Expander 标题；可空，界面默认 `...` |
| `ItemMarginTop` | 该段按钮行上间距（整数，≥0） |
| `ItemMarginBottom` | 该段按钮行下间距 |
| `Items` | 该段内的按钮集合 |

### 按钮字段（`toolbarItemViewModle`）

| 元素 | 说明 |
|------|------|
| `Name` | 按钮显示文字 |
| `Path` | `.ms` / `.mse` 等脚本文件路径；有值时点击 `FileIn` |
| `Commit` | MaxScript 字符串；`Path` 为空时点击执行该脚本 |
| `ToolTip` | 悬停提示；可空（空时界面可能回退为 `Name`） |
| `Space` | `true` 时在该按钮上方多加一段间隔 |

执行优先级（插件内逻辑）：`Path` 非空则跑文件，否则跑 `Commit`。

### 兼容旧单列表文件

若 `{ListId}.xml` 根仍是旧的 `NDListBoxViewModle`（整文件只有一段按钮列表），加载时会自动包成「只有一项」的 `CustomTabListsViewModle`。再次「保存配置」后会写成新的多段根格式。

若仍保留旧版三列文件 `CustomTab_{Id}_1.xml` / `_2.xml` / `_3.xml`，启动时会合并成多段 Expander，并另存为统一的 `CustomTab_{Id}.xml`，同时更新 `ToolBarTabs.xml` 里的 `ListId`。

**不要**在同一个 `NDListBoxViewModle` 根下并列写多个 `<Items>` 指望出多段（`XmlSerializer` 只会保留一段）。多段请用上面的 `CustomTabListsViewModle` 结构；插件对「并列多个 Items」会尽量兼容解析，但外部工具应写标准多段格式。

---

## 5. 右键「复制 / 黏贴」按钮（剪贴板格式）

列表右键菜单的 **复制 / 黏贴** 作用于当前选中按钮，剪贴板为 **Unicode 文本**。完整按钮快照会覆盖目标按钮的全部字段（不是只改 `Commit`）。

### 完整按钮快照（插件「复制」写出）

整段文本结构：

```text
NDToolsBox.ToolbarItem.v1
<?xml version="1.0" encoding="utf-16"?>
<ToolbarItem xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <Name>显示名</Name>
  <Path></Path>
  <Commit>print "hello"</Commit>
  <ToolTip>悬停提示</ToolTip>
  <Space>false</Space>
</ToolbarItem>
```

规则：

| 部分 | 说明 |
|------|------|
| 第 1 行 | 固定前缀 **`NDToolsBox.ToolbarItem.v1`**，后接换行 `\n` |
| 其后 | `ToolbarItemClipboardData` 的 XML（根元素 **`ToolbarItem`**） |

XML 字段与配置文件中的按钮字段一致：

| 元素 | 说明 |
|------|------|
| `Name` | 显示名称 |
| `Path` | 脚本文件路径；可空 |
| `Commit` | MaxScript；可空（可含多行） |
| `ToolTip` | 存储的悬停提示（不含 UI 回退到 `Name` 的逻辑） |
| `Space` | 是否为间隔项 |

**黏贴**行为：

1. 若剪贴板以 `NDToolsBox.ToolbarItem.v1\n` 开头：反序列化 XML，写入当前选中按钮的 `Name` / `Path` / `Commit` / `ToolTip` / `Space`
2. 否则按兼容逻辑：
   - 文本是已存在的文件路径 → 只当作 `Path`（清空 `Commit`），保留原 `Name` / `ToolTip` / `Space`
   - 其它文本 → 当作 `Commit`，并尽量从脚本注释推导 `Name`（失败则为 `Mxs`），清空 `Path`

外部工具若要程序化「粘贴式」写入按钮，可向系统剪贴板放入上述带前缀的文本，再在 Max 内对该按钮执行「黏贴」；更常见做法仍是直接改 `{ListId}.xml`。

> 说明：`XmlSerializer` + `StringWriter` 写出的 XML 声明常为 `encoding="utf-16"`，剪贴板内容本身是 Unicode 文本，解析时不依赖该声明。

---

## 6. 列表接受的拖拽格式

自定义 Tab / 绑定页的 `NDListBox`，以及动画页列表，均通过 `AllowDrop` 接收拖入，在 `DragEnter` / `Drop` 中识别以下 **`DataFormats`**（与右键「黏贴」的完整按钮快照 **不是同一套**）。

### 可接受的数据格式

| `DataFormats` | 含义 | 拖入后行为 |
|---------------|------|------------|
| `FileDrop` | 资源管理器等拖入的文件路径数组 | 仅保留扩展名为 **`.ms`** / **`.mse`** 的文件；每个文件 **新建** 一个按钮，`Path` = 完整路径，`Name` = 无扩展名文件名 |
| `UnicodeText` | 单段 Unicode 字符串 | 若该字符串是已存在文件路径 → 新建 `Path` 按钮；否则 → 新建 `Commit` 按钮，`Name` 尽量从脚本注释推导（`GetNDBoxMxsCommitScriptName`） |

`DragEnter`：仅当存在 `FileDrop` 或 `UnicodeText` 时显示 `Copy` 光标，其它格式为 `None`。

### `FileDrop` 细节

- 过滤在 `GetFiles`：`File.Exists` 且扩展名 **等于** `.ms` 或 `.mse`（大小写敏感比较；建议路径使用小写扩展名）
- 支持一次拖入多个文件；按数组顺序依次插入
- 插入位置：落点命中某按钮时，插在该按钮索引处；未命中时按纵向位置估算索引，估失败则 **追加到末尾**（`index = -1`）
- 若列表中已有相同 `Path` 或相同 `Commit` 文本，则 **跳过**（不重复添加）

新建的文件型按钮字段大致为：

| 字段 | 值 |
|------|-----|
| `Path` | 拖入的绝对路径 |
| `Commit` | 空 |
| `Name` | `Path.GetFileNameWithoutExtension` |
| `ToolTip` | 默认与 `Name` 相同（构造时） |
| `Space` | `false` |

### `UnicodeText` 细节

适用于从其它控件 / 工具拖出一段文本（例如 NDBox 脚本树拖出路径或脚本正文）：

```text
# 情况 A：磁盘上存在的文件路径
C:\Scripts\my_tool.ms

# 情况 B：MaxScript 正文（非已存在路径）
print "hello"
```

| 判定 | 结果 |
|------|------|
| `File.Exists(text)` 为真 | `AddNewFileItem`：`Path = text` |
| 否则 | `AddNewCommitItem`：`Commit = text`，`Name` 来自脚本名解析（可空时用默认名） |

注意：

- 拖入的 `UnicodeText` **不会**解析第 5 节的 `NDToolsBox.ToolbarItem.v1` 完整按钮快照；该格式仅用于右键 **复制 / 黏贴**
- 若同时携带 `FileDrop` 与 `UnicodeText`，两者都会依次处理（先文件，后文本）

### 外部工具如何拖入

1. **推荐**：用资源管理器或自建拖源提供 `DataFormats.FileDrop`（`.ms` / `.mse`）
2. **文本**：提供 `DataFormats.UnicodeText`，内容为单文件绝对路径，或一段要作为 `Commit` 的 MaxScript
3. **完整 Name/Commit/ToolTip**：请写 `{ListId}.xml`，或复制完整快照后在列表内「黏贴」，不要依赖拖拽

相关实现：`Max2022/NDListBox.xaml.cs`、`ToolbarsV.xaml.cs` 中的 `MyListBox_DragEnter` / `MyListBox_Drop`。

---

## 7. 推荐写入流程（外部工具）

1. 选定稳定的 `Id`、`ListId`、`Header`  
2. 写入 / 更新 `{apppath}\{ListId}.xml`（`CustomTabListsViewModle`，按需多个 `NDListBoxViewModle` 段）  
3. 读取 `{apppath}\ToolBarTabs.xml`（若不存在则按上文最小结构新建）  
4. 按 `Id` 更新或追加 `<Tab>`，写回 `ToolBarTabs.xml`  
5. 生效方式：  
   - **新 Tab / 改 Header**：关闭并重新打开侧边工具条（或重启 Max）——`ToolBarTabs.xml` 在启动时加载  
   - **只改列表内容**：对任一段列表右键「刷新」，或重开工具条  

删除 Tab（外部）：

1. 从 `ToolBarTabs.xml` 去掉对应 `<Tab>`  
2. 删除 `{ListId}.xml`（以及若曾用旧三列命名的遗留文件）  
3. 重开工具条  

---

## 8. 伪代码示例

```python
# 示意：合并登记一个 Tab，并写出两段 Expander 列表
import os
import xml.etree.ElementTree as ET

APP = r"C:\ProgramData\Autodesk\ApplicationPlugins\NDToolsBox"
tabs_path = os.path.join(APP, "ToolBarTabs.xml")
list_id = "MyRigTools"
tab_id = "my_rig_tools"
header = "绑定工具"

# 1) 写多段列表
list_xml = f"""<?xml version="1.0" encoding="utf-8"?>
<CustomTabListsViewModle xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <Items>
    <NDListBoxViewModle>
      <Header>...</Header>
      <ItemMarginTop>1</ItemMarginTop>
      <ItemMarginBottom>1</ItemMarginBottom>
      <Items>
        <toolbarItemViewModle>
          <Space>false</Space>
          <Path></Path>
          <Commit>print "ok"</Commit>
          <Name>示例</Name>
          <ToolTip></ToolTip>
        </toolbarItemViewModle>
      </Items>
    </NDListBoxViewModle>
    <NDListBoxViewModle>
      <Header>...</Header>
      <ItemMarginTop>1</ItemMarginTop>
      <ItemMarginBottom>1</ItemMarginBottom>
      <Items>
        <toolbarItemViewModle>
          <Space>false</Space>
          <Path></Path>
          <Commit>print "section2"</Commit>
          <Name>第二段</Name>
          <ToolTip></ToolTip>
        </toolbarItemViewModle>
      </Items>
    </NDListBoxViewModle>
  </Items>
</CustomTabListsViewModle>
"""
with open(os.path.join(APP, list_id + ".xml"), "w", encoding="utf-8") as f:
    f.write(list_xml)

# 2) 合并 ToolBarTabs.xml
if os.path.isfile(tabs_path):
    tree = ET.parse(tabs_path)
    root = tree.getroot()
else:
    root = ET.Element("ToolBarTabsConfig")
    ET.SubElement(root, "Tabs")
    tree = ET.ElementTree(root)

tabs = root.find("Tabs")
if tabs is None:
    tabs = ET.SubElement(root, "Tabs")

existing = None
for tab in tabs.findall("Tab"):
    id_el = tab.find("Id")
    if id_el is not None and id_el.text == tab_id:
        existing = tab
        break

if existing is None:
    existing = ET.SubElement(tabs, "Tab")
    ET.SubElement(existing, "Id").text = tab_id
    ET.SubElement(existing, "Header").text = header
    ET.SubElement(existing, "ListId").text = list_id
else:
    for tag, val in (("Header", header), ("ListId", list_id)):
        el = existing.find(tag)
        if el is None:
            el = ET.SubElement(existing, tag)
        el.text = val

tree.write(tabs_path, encoding="utf-8", xml_declaration=True)
```

> 若用 .NET `XmlSerializer` 写出，根类型请使用 `CustomTabListsViewModle`，段类型为 `NDListBoxViewModle`。

---

## 9. 常见问题

**Q: 写完文件界面没有新 Tab？**  
A: `ToolBarTabs.xml` 只在工具条初始化时读。请关掉侧边栏再开，或重启 Max。

**Q: Tab 在，但按钮还是旧的？**  
A: 对该列表右键「刷新」，或确认 `ListId` 与 `{ListId}.xml` 文件名一致。

**Q: 如何增加第三段 Expander？**  
A: 在 `CustomTabListsViewModle/Items` 下再追加一个 `NDListBoxViewModle` 节点即可。

**Q: 可以用中文 ListId 吗？**  
A: 可以，但需保证文件系统与编码正常；更推荐英文 / 数字 / 下划线。

**Q: 和「绑定」页三列 `MyListBox_Rig*.xml` 是一回事吗？**  
A: 不是。「绑定」是内置 Tab，固定三个独立 `NDListBox` 文件；自定义 Tab 是一份 `CustomTabListsViewModle`，内含多段。

**Q: 编码要用 utf-16 吗？**  
A: 插件侧读取按 UTF-8。外部工具请写 UTF-8。若看到插件自己保存的文件声明为 `utf-16`，那是 `StringWriter` + `XmlSerializer` 的声明习惯；外部工具以 UTF-8 内容为准即可。

**Q: 右键复制后剪贴板里只有脚本 / 路径？**  
A: 新版本复制的是完整按钮快照（见第 5 节）。若仍只有纯文本，说明 Max 加载的仍是旧 DLL，请更新 `assemblies\NDToolsBox.dll` 后重试。

**Q: 拖入 `.ms` 没反应？**  
A: 确认格式为 `FileDrop`（不是只含路径的普通文本）、扩展名为 `.ms`/`.mse`，且列表中尚无相同 `Path`。完整按钮字段请用配置文件或第 5 节黏贴，拖拽只建 Path/Commit 按钮（见第 6 节）。

---

## 10. 相关代码

- `TreeViewWithViewModelDemo/TextSearch/ViewModel/CustomToolbarTab.cs` — Tab 元数据与 `Normalize`
- `TreeViewWithViewModelDemo/TextSearch/ToolbarsV.xaml.cs` — 加载 / 新建 / 删除 Tab；列表拖拽 `Drop`
- `TreeViewWithViewModelDemo/TextSearch/ViewModel/toolbarsViewModle.cs` — `ToolbarItemClipboard`（复制 / 黏贴完整按钮）
- `Max2022/NDCustomTabLists.xaml(.cs)` — 自定义 Tab 多段渲染
- `Max2022/NDListBox.xaml.cs` — `CustomTabListsViewModle`、单段列表、保存 / 刷新 / 拖拽
- `TreeViewWithViewModelDemo/DataAccess/WebAddress.cs` — `apppath`、`ToolBarTabsConfig`
