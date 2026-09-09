using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UiViewModels.ActionContainer;
using UiViewModels.Actions;

namespace NDToolsBox
{
    class NDBoxToolbarItem: ToolbarItem
    {
        public NDBoxToolbarItem()
        {
            ActionPropertyName = "NDBox Action";
            IsInFilterCondition = true;
            Action = new NDBoxCuiAction();
            Title = "Test NDBox";
            IsSeparator = false;
            string icon = ScriptsUtilities.iconUri();
            if (!string.IsNullOrEmpty(icon))
            {
                Image = BitmapFrame.Create(new Uri(icon));
            }
        }
       
    }
}
