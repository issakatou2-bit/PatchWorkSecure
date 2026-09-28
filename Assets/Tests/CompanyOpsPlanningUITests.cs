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
            CheckRect("ConsultationCard",1268,108,308,237.2f);CheckRect("PlanningCharacter",230,450,470,470,10);
            CheckRect("Navigator",600,540,420,129.2f);CheckRect("Action_audit",660,740,165.9f,144);
            CheckRect("AdvanceMonth",1268,660,308,60);
            Assert.AreEqual(game.State.budget.ToString(),Find<TextMeshProUGUI>("予算Value").text);
            int[] values={game.State.stability,game.State.Organization,game.State.trust,game.State.culture,game.State.Preparedness,game.State.Resilience};
            string[] labels={"業務の安定","チームの力","経営の信頼","相談文化","備え","立て直す力"};
            for(int i=0;i<6;i++) Assert.AreEqual(values[i].ToString(),Find<TextMeshProUGUI>(labels[i]+"Value").text);
            Assert.AreEqual("総合 "+OpsGame.PlanningRank(values.Sum()/6),Find<TextMeshProUGUI>("CompanyRank").text);
            Assert.AreEqual("達成で 信頼+3",Find<TextMeshProUGUI>("RewardText").text);
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
                game.OpenTab(0);Click("Menu");yield return null;CheckPointer(id);Click(id);yield return null;
                Assert.IsTrue(Object.FindObjectsByType<Transform>().Any(t=>t.name=="ModalBlocker"));
            }
            game.OpenTab(0);game.State.fatigue=80;game.OpenTab(0);yield return null;
            Assert.AreEqual("要休息",Find<TextMeshProUGUI>("StatHint5").text);
            Assert.AreEqual(50.4f,Find<RectTransform>("SpareFill").rect.width,.01f);
            game.State.month=4;game.OpenTab(0);yield return null;
            Assert.IsNotNull(Find<Image>("SummerDaylight"));Assert.IsFalse(Object.FindObjectsByType<Transform>().Any(t=>t.name.StartsWith("SeasonParticle")));
            game.State.month=8;game.OpenTab(0);yield return null;
            Assert.AreEqual(6,Object.FindObjectsByType<Image>().Count(t=>t.name.StartsWith("SeasonParticle")&&t.sprite==game.PlanningArt.snow));
            game.OpenTab(0);Click("Menu");yield return null;
            if(!game.ReducedMotion)Click("ReduceMotion");Click("CloseDialog");yield return null;
            var motions=Object.FindObjectsByType<OpsPlanningMotion>();yield return null;
            var positions=motions.Select(m=>((RectTransform)m.transform).anchoredPosition).ToArray();
            yield return new WaitForSecondsRealtime(.35f);
            for(int i=0;i<motions.Length;i++)
            {Assert.AreEqual(positions[i],((RectTransform)motions[i].transform).anchoredPosition);Assert.AreEqual(Vector3.one,motions[i].transform.localScale);}
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
