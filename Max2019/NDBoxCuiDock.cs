using System;
using Autodesk.Max;
using UiViewModels.Actions;
namespace NDToolsBox
{
    //3dsMax2019
    public class NDBoxCuiDock : CuiDockableContentAdapter
    {
        public NDBoxCuiDock()
        {
            //this.LoadingConfiguration += new EventHandler<CuiDockableContentConfigEventArgs>(TextEvent);
            //this.SavingConfiguration += new EventHandler<CuiDockableContentConfigEventArgs>(TextEvent);
        }
        public override string ActionText 
        {
            //get { return "NDBox-Dock Window"; }
            get { return InternalActionText; }

        }
        public override string Category
        {
            get { return InternalCategory; }
        }

        public override string WindowTitle
        {
            get { return InternalActionText; }
        }
        public override string InternalActionText
        {
            get { 
                return "天晴盒子";

                //return "NDBox-Dock";
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
        public override string ObjectName
        {
            get
            {
                return "ndbox";
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
                return false;
            }
        }
        protected void TextEvent(object sender, EventArgs e)
        {
            
            //CuiDockableContentConfigEventArgs ne = (CuiDockableContentConfigEventArgs)e;
            //ScriptsUtilities.print(ne.Filename);

        }
       
    }
}
