--https://forums.cgsociety.org/t/max-net-getting-sub-anim-animatable-values/1594593/7

/* dll_file = @"G:\Git_Csharp\NDToolsBox-3dsMax\Max2016\bin\Debug\NDToolsBox.dll"
assembly = (dotnetClass "System.Reflection.Assembly").Load ((dotnetClass "System.IO.File").ReadAllBytes dll_file)
atest = assembly.createInstance "NDToolsBox.ScriptsUtilities" */

ScriptsUtilities = dotNetClass "NDToolsBox.ScriptsUtilities"

a = ScriptsUtilities.GetSeleSetNames()
--b = a.ToArray()
for i in a do 
(
    format "%\n" i.name 
)



--场景中 选择集 的数量
        sel_count = getNumNamedSelSets()

        for i=1 to sel_count do 
        (
            --选择集名称
            set_Name = getNamedSelSetName i
            format "%\n" set_Name
        )

--ScriptsUtilities.test()
/* test_ = @"G:\Git_NDTools\Dev_Channel\Selection_sets\3.xml"
ScriptsUtilities.SerializeListTToXml test_  */

--ScriptsUtilities.DeserializeXml test_
/* a = ScriptsUtilities.DeserializeXmlDemo test_
b = a.ToArray()
print(b) */
-- a = ScriptsUtilities.GetFilterNames()
-- b = a.ToArray()
-- for i in b do 
-- (
--     print(i)
-- )

-- ScriptsUtilities.GetSeleSetNames()

/* target = ScriptsUtilities.ColorToString 186 186 186
print(target)
for i = 0 to 131 do 
(
    cloro_ = ScriptsUtilities.GetUIColor i
    if target == cloro_ do (
        print "find:"
    )
    format "% -> %\n" i cloro_
)  */
/* <SolidColorBrush x:Key="MaxTextColor" Color="WhiteSmoke"/>
<SolidColorBrush x:Key="MaxUiBackgroundColor" Color="Black"/>
<!--按钮的背景颜色-->
<SolidColorBrush x:Key="ButtonColor_A" Color="Red"/>
<SolidColorBrush x:Key="ButtonColor_B" Color="DimGray"/>
<SolidColorBrush x:Key="ButtonColor_C" Color="DeepSkyBlue"/>
<SolidColorBrush x:Key="ButtonColor_D" Color="Fuchsia"/> */