using System.Collections.Generic;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_SubMenu_EnableMesh : ViveSR_Experience_ISubMenu
    {
        public Dictionary<EnableMesh_SubBtn, ViveSR_Experience_ISubBtn> EnableMesh_Subbtns = new Dictionary<EnableMesh_SubBtn, ViveSR_Experience_ISubBtn>();

        protected override void AwakeToDo()
        {
            for (int i = 0; i < (int)EnableMesh_SubBtn.MaxNum; i++)
            {
                EnableMesh_Subbtns[(EnableMesh_SubBtn)i] = subBtnScripts[i];
            }
        }
    }
}
