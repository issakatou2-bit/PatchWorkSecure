#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Linq;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        [TestCase(0,"G")][TestCase(9,"G")][TestCase(10,"F")][TestCase(19,"F")]
        [TestCase(20,"E")][TestCase(34,"E")][TestCase(35,"D")][TestCase(49,"D")]
        [TestCase(50,"C")][TestCase(64,"C")][TestCase(65,"B")][TestCase(79,"B")]
        [TestCase(80,"A")][TestCase(94,"A")][TestCase(95,"S")][TestCase(100,"S")]
        public void 計画画面のランク境界は承認仕様どおり(int value,string rank) => Assert.AreEqual(rank,OpsGame.PlanningRank(value));

        [UnityTest] public IEnumerator 承認モックの配置と実数値と詳細導線を確認する()
        {
            SceneManager.LoadScene("CompanyYear");yield return null;yield return new WaitForSeconds(.5f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);yield return null;
            CheckRect("OfficeStage",352,108,900,792);CheckRect("CompanyGrowth",24,108,312,612);
            CheckRect("ConsultationCard",1268,108,308,237.2f);CheckRect("PlanningCharacter",260,450,470,470,10);
            CheckRect("Navigator",600,580,420,129.2f);CheckRect("Action_audit",660,740,165.9f,144);
            CheckRect("AdvanceMonth",1268,660,308,60);
            Assert.AreEqual(game.State.budget.ToString(),Find<TextMeshProUGUI>("予算Value").text);
            int[] values={game.State.stability,game.State.Organization,game.State.trust,game.State.culture,game.State.Preparedness,game.State.Resilience};
            string[] labels={"業務の安定","チームの力","経営の信頼","相談文化","備え","立て直す力"};
            for(int i=0;i<6;i++) Assert.AreEqual(values[i].ToString(),Find<TextMeshProUGUI>(labels[i]+"Value").text);
            Assert.AreEqual("総合 "+OpsGame.PlanningRank(values.Sum()/6),Find<TextMeshProUGUI>("CompanyRank").text);
            Assert.AreEqual("達成で 信頼+3",Find<TextMeshProUGUI>("RewardText").text);
            CheckCapacityLabel();
            Assert.AreEqual(game.State.capacity,Object.FindObjectsByType<Image>().Count(i=>i.name.StartsWith("WorkToken")&&i.color==new Color(63/255f,169/255f,245/255f)));
            Assert.AreEqual(game.State.Current.title.Replace("、","、\n"),Find<TextMeshProUGUI>("CaseTitle").text);
            foreach(string marker in new[]{"OfficeConsultation","OfficeTicket"})
            {
                var label=Find<Button>(marker).GetComponentInChildren<TextMeshProUGUI>();label.ForceMeshUpdate();
                Assert.IsFalse(label.isTextOverflowing,marker);Assert.Greater(label.textInfo.characterCount,0,marker);
            }
            Assert.IsFalse(Object.FindObjectsByType<Transform>().Any(t=>t.name=="OfficeBadge"||t.name=="MissionBoard"||t.name=="DecisionPanel"));
            foreach(string id in new[]{"Action_audit","Action_listen","Action_map","Action_rest","OpenProjects","ConsultationDetails","AdvanceMonth","Menu"})CheckPointer(id);
            CheckText();Capture("46-approved-planning",1600,900);Capture("47-approved-planning-720",1280,720);Capture("48-approved-planning-1080",1920,1080);
            Click("ConsultationDetails");yield return null;
            StringAssert.Contains(game.State.Current.news,Find<TextMeshProUGUI>("DialogBody").text);
            CheckPointer("OpenEventBrief");CheckPointer("EmployeeConsultation");CheckText();Click("CloseDialog");yield return null;
            foreach(string id in new[]{"OpenTeam","OpenTicket","Tab2","Goal1","OpenSituation","OpenGuide"})
            {
                game.OpenTab(0);Click("Menu");yield return null;Click("AdvancedSettings");yield return new WaitForSecondsRealtime(.5f);CheckPointer(id);Click(id);yield return null;
                Assert.IsTrue(Object.FindObjectsByType<Transform>().Any(t=>t.name=="ModalBlocker"));
            }
            game.OpenTab(0);game.State.fatigue=80;game.OpenTab(0);yield return null;
            Assert.AreEqual("要休息",Find<TextMeshProUGUI>("StatHint5").text);
            Assert.AreEqual(50.4f,Find<RectTransform>("SpareFill").rect.width,.01f);
            game.State.month=4;game.OpenTab(0);yield return null;
            Assert.IsNotNull(Find<Image>("SeasonHaze"));Assert.IsFalse(Object.FindObjectsByType<Transform>().Any(t=>t.name.StartsWith("SeasonParticle")));
            game.State.month=8;game.OpenTab(0);yield return null;
            Assert.AreEqual(8,Object.FindObjectsByType<Image>().Count(t=>t.name.StartsWith("SeasonParticle")&&t.sprite==game.PlanningArt.snow));
            game.OpenTab(0);Click("Menu");yield return null;
            if(!game.ReducedMotion)Click("ReduceMotion");Click("CloseDialog");yield return null;
            var motions=Object.FindObjectsByType<OpsPlanningMotion>();yield return null;
            var positions=motions.Select(m=>((RectTransform)m.transform).anchoredPosition).ToArray();
            yield return new WaitForSecondsRealtime(.35f);
            for(int i=0;i<motions.Length;i++)
            {Assert.AreEqual(positions[i],((RectTransform)motions[i].transform).anchoredPosition);Assert.AreEqual(Vector3.one,motions[i].transform.localScale);}
            Assert.IsEmpty(glyphWarnings,string.Join("\n",glyphWarnings));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator 相談見出しの改行と工数コマを一年分確認する()
        {
            SceneManager.LoadScene("CompanyYear");yield return null;yield return new WaitForSeconds(.5f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);yield return null;
            for(int month=0;month<12;month++)
            {
                game.State.month=month;game.OpenTab(0);yield return null;
                var title=Find<TextMeshProUGUI>("CaseTitle");title.ForceMeshUpdate();
                Assert.AreEqual(game.State.Current.title.Replace("、","、\n"),title.text);
                Assert.IsFalse(title.isTextOverflowing,game.State.Current.name+" / "+title.text);
                CheckCapacityLabel();
            }
            game.State.month=0;
            for(int capacity=0;capacity<=8;capacity++)
            {
                game.State.capacity=capacity;game.OpenTab(0);yield return null;CheckCapacityLabel();
                Assert.AreEqual(capacity,Object.FindObjectsByType<Image>().Count(i=>i.name.StartsWith("WorkToken")&&i.color==new Color(63/255f,169/255f,245/255f)));
            }
            Assert.AreEqual(8,Find<Button>("Stat_1").GetComponentsInChildren<Image>().Count(i=>i.name.StartsWith("WorkToken")));
            foreach(string id in new[]{"Stat_0","Stat_1","Menu","Action_audit","AdvanceMonth"})CheckPointer(id);
            CheckText();Capture("49-planning-finish-max-work",1600,900);
            Assert.IsEmpty(glyphWarnings,string.Join("\n",glyphWarnings));LogAssert.NoUnexpectedReceived();
        }
        private static void CheckCapacityLabel()
        {
            var label=Find<TextMeshProUGUI>("CapacityTitle");label.ForceMeshUpdate();
            Assert.AreEqual("工数",label.text);Assert.IsFalse(label.isTextOverflowing);
            var labelRect=label.rectTransform;var token=Find<RectTransform>("WorkToken0");
            float right=labelRect.anchoredPosition.x+(1-labelRect.pivot.x)*labelRect.rect.width;
            float left=token.anchoredPosition.x-token.pivot.x*token.rect.width;
            Assert.GreaterOrEqual(left-right,12,"工数ラベルとコマが重ならない");
        }
        [UnityTest] public IEnumerator 自然な社員成長後の支援対象と画面を照合する()
        {
            SceneManager.LoadScene("CompanyYear");yield return null;yield return new WaitForSeconds(.5f);
            var game=Object.FindAnyObjectByType<OpsGame>();game.StartYear(14);yield return null;
            var memory=new PersonaMemory{role=2,cycle=1};var state=game.State;
            // レベルや設備の値を直接盛らず、既存の合法な行動で支援可能になるまで進める。
            while(state.month<11&&state.ResponsePower("scope").staff==0)
            {
                var turn=new PersonaTurn();PersonaCommand command;
                while((command=CompanyOpsPersonaPolicy.Next(state,memory,turn))!=null)
                {Assert.IsTrue(CompanyOpsPersonaPolicy.ApplyRule(state,command));CompanyOpsPersonaPolicy.Applied(turn,command);}
                Assert.IsTrue(state.BeginIncident());Assert.IsTrue(state.Resolve(CompanyOpsPersonaPolicy.Response(state,memory)));
                if(state.QuarterRewardPending)Assert.IsTrue(state.ClaimQuarterReward("budget"));
                Assert.IsTrue(state.NextMonth());Assert.IsTrue(state.Valid());
            }
            Assert.Greater(state.ResponsePower("scope").staff,0,"自然な育成で実際の事件支援が成立する");
            game.OpenTab(0);yield return null;CheckCapacityLabel();CheckText();
            foreach(string action in new[]{"audit","listen","map","rest"})
                Assert.IsNull(Find<Button>("Action_"+action).transform.Find("StaffSupport"),"計画行動に未実装の加算を示さない");
            Capture("50-supported-month-planning",1600,900);
            Click("OpenTeam");yield return null;CheckText();Capture("51-supported-month-team",1600,900);
            Click("CloseDialog");yield return null;
            Click("AdvanceMonth");yield return null;
            if(state.phase==OpsPhase.Planning){Click("ConfirmAdvance");yield return null;}
            Assert.AreEqual(OpsPhase.Incident,state.phase);Click("Power_scope");yield return new WaitForSecondsRealtime(.8f);
            Assert.AreEqual("+"+state.ResponsePower("scope").staff,Find<TextMeshProUGUI>("PowerStepValue2").text);
            StringAssert.Contains(state.SupportSummary,Find<TextMeshProUGUI>("PowerSupport").text);
            CheckText();Capture("52-actual-incident-staff-support",1600,900);
            Click("CloseDialog");yield return null;game.Resolve("scope");yield return null;
            if(!state.Latest.benign)
            {
                float deadline=Time.realtimeSinceStartup+4;
                while(!Object.FindObjectsByType<TextMeshProUGUI>().Any(t=>t.name=="CutinCaption"&&t.text=="社員が助けてくれた！")&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.AreEqual("社員が助けてくれた！",Find<TextMeshProUGUI>("CutinCaption").text);
                StringAssert.Contains("+"+state.Latest.power.staff,Find<TextMeshProUGUI>("CutinEffect").text);
                while(Mathf.Abs(Find<RectTransform>("ResolutionCutin").anchoredPosition.x-380)>1&&Time.realtimeSinceStartup<deadline)yield return null;
                Capture("66-natural-staff-cutin",1600,900);
            }
            yield return WaitForResolution(game);
            Assert.IsEmpty(glyphWarnings,string.Join("\n",glyphWarnings));LogAssert.NoUnexpectedReceived();
        }
        private static void CheckRect(string name,float x,float y,float w,float h,float tolerance=.1f)
        {
            var r=Find<RectTransform>(name);Assert.AreEqual(x,r.anchoredPosition.x,tolerance,name);Assert.AreEqual(-y,r.anchoredPosition.y,tolerance,name);
            Assert.AreEqual(w,r.rect.width,.1f,name);Assert.AreEqual(h,r.rect.height,.1f,name);
        }
    }
}
#endif
