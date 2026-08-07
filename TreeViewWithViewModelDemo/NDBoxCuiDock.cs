using System;
using Autodesk.Max;
using UiViewModels.Actions;
namespace NDToolsBox
{
    //3dsMax 2015 ，可以停靠
    public class NDBoxCuiDock : CuiDockableContentAdapter
    {

        public NDBoxCuiDock()
        {
            this.LoadingConfiguration += new EventHandler<CuiDockableContentConfigEventArgs>(TextEvent);
            this.SavingConfiguration += new EventHandler<CuiDockableContentConfigEventArgs>(TextEvent);
        }
        public override string ActionText 
        { 
            get { return "NDBox-Dock Window"; } 
        }
        public override string Category 
        { 
            get { return InternalCategory; } 
        }
        public override string ButtonText {
            get {
                //return "NDBox-Dock";
                return "天晴盒子-Dock";

            }
        }
        public override string WindowTitle
        {
            get { return InternalActionText; }
        }
        public override string InternalActionText
        {
            get { 
                return "天晴盒子-Dock";
                //return "NDBox-Dock";
            }

        }
        public override string MenuText
        {
            get { return InternalActionText;}
        }
        public override string InternalCategory
        {
            get { return "A-NDTools"; }
        }
        

        public override Type ContentType
        {
            get { return typeof(TextSearchDemoControl); }
        }

        public override object CreateDockableContent()
        {
            
            return new TextSearchDemoControl();
        }
        
        
        public override DockStates.Dock DockingModes
        {
            get
            {
                return DockStates.Dock.Left | DockStates.Dock.Right | DockStates.Dock.Floating;
                //return DockStates.Dock.Floating;
            }

        }
        protected void TextEvent(object sender, EventArgs e)
        {
            CuiDockableContentConfigEventArgs ne = (CuiDockableContentConfigEventArgs)e;
            ScriptsUtilities.print(ne.Filename);
        }

    }
}
