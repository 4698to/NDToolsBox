using Autodesk.Max;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Max.Plugins;

namespace NDToolsBox
{
    public class Descriptor: ClassDesc2
    {
        public override object Create(bool loading)
        {
            return new GlobalUtility();
        }

        public override bool IsPublic
        {
            get
            {
                return true;
            }
        }

        public override string ClassName
        {
            get
            {
                return "NDBox-Flaot";
            }
        }

        public override SClass_ID SuperClassID
        {
            get
            {
                return SClass_ID.Gup;
            }
        }

        public override IClass_ID ClassID
        {
            get
            {
                return ScriptsUtilities.Class_ID;
            }
        }

        public override string Category
        {
            get
            {
                return "NDBox";
            }
        }
#if M2022 || M2023 || M2024

        public override string NonLocalizedClassName => "NDBox";
#endif

    }
}
