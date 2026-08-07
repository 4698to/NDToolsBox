using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NDToolsBox.TextSearch;
using UiViewModels.Actions;
namespace NDToolsBox
{
    //3dsMax2019

    public class NDBoxToolBarCuiDock: CuiDockableContentAdapter
    {
        public NDBoxToolBarCuiDock()
        {
            this.LoadingConfiguration += new EventHandler<CuiDockableContentConfigEventArgs>(TextEvent);
            this.SavingConfiguration += new EventHandler<CuiDockableContentConfigEventArgs>(TextEvent);
           
        }
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
        public override string ObjectName
        {
            get
            {
                return "ndboxTbar";
            }
        }
        public override bool DocksMaximized
        {
            get
            {
                return true;

            }
        }

        public override bool IsMainContent
        {
            get
            {
                return true;
            }
        }
        protected void TextEvent(object sender, EventArgs e)
        {

            //CuiDockableContentConfigEventArgs ne = (CuiDockableContentConfigEventArgs)e;
            ScriptsUtilities.print("ne.Filename");

        }
    }
}
