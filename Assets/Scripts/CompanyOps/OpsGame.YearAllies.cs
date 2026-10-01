using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        public void RequestEngineer()
        {
            if(State==null||MinigameActive||!State.RequestEngineerResearch())return;
            Save();Render();Toast("エンジニアさんの調査 / 状況の把握 +"+OpsCatalog.EngineerKnowledge,true,OpsCue.Action);
            // まだ未確認の真相は話さない。専用音声がないので表示だけで進む。
        }
        private void SelectTeamMember(int member)
        {if(!State.SelectSupportMember(member))return;Save();Render();TeamDialog();}
    }
}
