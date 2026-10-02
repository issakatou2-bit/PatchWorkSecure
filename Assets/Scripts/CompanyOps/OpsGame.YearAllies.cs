using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        public void RequestEngineer()
        {
            if(State==null||MinigameActive||!State.RequestEngineerResearch())return;
            Save();Render();Toast("りりぃの調査 / 状況の把握 +"+OpsCatalog.EngineerKnowledge,true,OpsCue.Action);
            QueueCompanionScene("investigate");
        }
        private void SelectTeamMember(int member)
        {if(!State.SelectSupportMember(member))return;Save();Render();TeamDialog();}
    }
}
