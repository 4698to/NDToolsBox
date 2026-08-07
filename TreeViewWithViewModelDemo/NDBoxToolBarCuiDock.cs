using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Max;
using NDToolsBox.TextSearch;
using UiViewModels.Actions;
namespace NDToolsBox
{
    public class NDBoxToolBarCuiDock: CuiDockableContentAdapter
    {
        public override string ActionText
        {
            get { return "ToolsBar Window"; }
        }
        public override string Category
        {
            get { return InternalCategory; }
        }
        public override string ButtonText
        {
            get
            {
                return "侧边工具栏";
                //return "ToolsBar-Dock";

            }
        }
        public override string WindowTitle
        {
            get { return InternalActionText; }
        }
        public override string InternalActionText
        {
            get { 
                return "侧边工具栏";

                //return "ToolsBar-Dock";
            }
        }
        public override string MenuText
        {
            get { return InternalActionText; }
        }
        public override string InternalCategory
        {
            get { return "A-NDTools-Dock"; }
        }


        public override Type ContentType
        {
            get { return typeof(ToolbarsV); }
        }

        public override object CreateDockableContent()
        {

            return new ToolbarsV();
        }


        public override DockStates.Dock DockingModes
        {
            get
            {
                return DockStates.Dock.Left | DockStates.Dock.Right | DockStates.Dock.Floating ;
                //return DockStates.Dock.Floating;
            }

        }

    }
}
