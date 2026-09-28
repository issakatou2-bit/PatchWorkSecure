#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using PatchWorkSecure.CompanyOps;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace PatchWorkSecure.Tests
{
    public partial class CompanyOpsTests
    {
        private static void SetEvent(OpsState state, string id)
        {
            int previous = Array.IndexOf(state.eventSchedule, id);
            if (previous >= 0) state.eventSchedule[previous] = state.eventSchedule[state.month];
            state.eventSchedule[state.month] = id;
        }
        [Test] public void 出来事と日常仕事の参照が揃い十脅威全てに題材がある()
        {
            Assert.AreEqual(40, OpsEventCatalog.Events.Length); Assert.AreEqual(18, OpsEventCatalog.Tickets.Length);
            Assert.AreEqual(40, OpsEventCatalog.Events.Select(e=>e.id).Distinct().Count());
            foreach (int rank in Enumerable.Range(1,10)) Assert.IsTrue(OpsEventCatalog.Events.Any(e=>e.threats.Contains(rank)), "題材がない脅威: "+rank);
            foreach (var e in OpsEventCatalog.Events)
            {
                var p = OpsEventCatalog.Profile(e.profile); Assert.IsNotNull(p);
                Assert.AreEqual(11,p.prevention.Length); Assert.IsTrue(p.prevention.All(n=>n>=0));
                Assert.IsNotNull(OpsCatalog.Term(p.lesson)); Assert.GreaterOrEqual(OpsCatalog.Index(p.projectA),0); Assert.GreaterOrEqual(OpsCatalog.Index(p.projectB),0);
                Assert.IsTrue(e.threats.All(n=>n>=1&&n<=10)); Assert.IsFalse(string.IsNullOrEmpty(e.finding));
            }
            foreach (var t in OpsEventCatalog.Tickets) { Assert.IsNotNull(OpsCatalog.Term(t.lesson)); Assert.That(t.member,Is.InRange(0,2)); }
        }
        [Test] public void 年度の抽選は重複せず保存と行動で引き直さない()
        {
            var all = new System.Collections.Generic.HashSet<string>();
            for (int seed=0;seed<200;seed++)
            {
                var state=new OpsState(seed,true); Assert.IsTrue(state.Valid());
                CollectionAssert.AreEqual(state.eventSchedule,new OpsState(seed,true).eventSchedule);
                Assert.AreEqual(12,state.eventSchedule.Distinct().Count()); Assert.AreEqual(12,state.ticketSchedule.Distinct().Count());
                Assert.AreEqual(4,state.eventSchedule.Count(id=>OpsEventCatalog.Event(id).operational));
                foreach (string id in state.eventSchedule) all.Add(id);
                string snapshot=string.Join(",",state.eventSchedule); state.Act("audit"); state.Act("listen");
                var saved=JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(state)); Assert.IsTrue(saved.Valid());
                Assert.AreEqual(snapshot,string.Join(",",saved.eventSchedule));
            }
            Assert.AreEqual(40,all.Count,"年度を変えても抽選されない題材がある");
        }
        [Test] public void 復元はデータ障害に効きDDoSと情報流出は取り消さない()
        {
            foreach (string id in new[]{"ops-storage","ddos-web","ai-upload","insider-export"})
            {
                var plain=new OpsState(14,true); SetEvent(plain,id);
                var equipped=JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(plain)); equipped.levels[OpsCatalog.Index("backup")]=2;
                Assert.AreEqual(plain.Preview("scope").prevention,equipped.Preview("scope").prevention);
                if (id=="ops-storage") Assert.Less(equipped.Preview("scope").loss,plain.Preview("scope").loss);
                else Assert.AreEqual(plain.Preview("scope").loss,equipped.Preview("scope").loss,id);
            }
            var state=new OpsState(14,true); SetEvent(state,"ops-update"); state.levels[OpsCatalog.Index("runbook")]=1;
            Assert.Less(state.Preview("recover").downtime,state.Preview("contain").downtime);
            Assert.Less(state.Preview("recover").loss,state.Preview("scope").loss);
        }
        [Test] public void セッション悪用と通常の入口ではMFAの効き方が異なる()
        {
            var entry=new OpsState(14,true); SetEvent(entry,"remote-vpn");
            var session=new OpsState(14,true); SetEvent(session,"targeted-session");
            int a=entry.Preview("scope").prevention,b=session.Preview("scope").prevention;
            entry.levels[OpsCatalog.Index("mfa")]=session.levels[OpsCatalog.Index("mfa")]=1;
            Assert.Greater(entry.Preview("scope").prevention-a,session.Preview("scope").prevention-b);
            var payment=new OpsState(14,true); SetEvent(payment,"bec-voice"); int power=payment.Preview("scope").containment;
            payment.levels[OpsCatalog.Index("segment")]=2; Assert.AreEqual(power,payment.Preview("scope").containment);
            var device=new OpsState(14,true); SetEvent(device,"remote-device"); int before=device.Preview("scope").prevention;
            device.levels[OpsCatalog.Index("mfa")]=2; Assert.AreEqual(before,device.Preview("scope").prevention,"MFAは紛失した端末の保存データを保護しない");
            SetEvent(device,"geo-claim"); device.culture=80; device.levels[OpsCatalog.Index("education")]=2;
            Assert.AreEqual(device.Current.staff,device.StaffVoice,"侵害情報の確認を経理の振込確認の台詞へ置き換えない");
        }
        [Test] public void 日常チケットの工数委任と二重報酬と保存を守る()
        {
            var state=new OpsState(14,true); Assert.IsFalse(state.ResolveTicket(true));
            int member=state.Ticket.member; state.staffExperience[member]=3; state.levels[OpsCatalog.Index("runbook")]=1;
            int capacity=state.capacity; Assert.IsTrue(state.ResolveTicket(true)); Assert.AreEqual(capacity,state.capacity);
            string snapshot=JsonUtility.ToJson(state); Assert.IsFalse(state.ResolveTicket(false)); Assert.AreEqual(snapshot,JsonUtility.ToJson(state));
            var saved=JsonUtility.FromJson<OpsState>(snapshot); Assert.IsTrue(saved.Valid()); Assert.AreEqual("delegate",saved.ticketResolution);
            saved.BeginIncident(); saved.Resolve("scope"); Assert.AreEqual("delegate",saved.Latest.ticketMode);
            Assert.AreEqual(saved.Ticket.id,saved.Latest.ticketId); saved.NextMonth(); Assert.AreEqual("",saved.ticketResolution);
            capacity=saved.capacity; Assert.IsTrue(saved.ResolveTicket(false)); Assert.AreEqual(capacity-1,saved.capacity);
            saved.BeginIncident(); Assert.IsFalse(saved.ResolveTicket(false));
        }
        [Test] public void 新しい月報は出来事と知識とチケットを残し旧年度は固定のまま()
        {
            var state=new OpsState(14,true); SetEvent(state,"ai-upload"); state.ResolveTicket(false); state.BeginIncident(); state.Resolve("scope");
            Assert.AreEqual("ai-upload",state.Latest.eventId); Assert.AreEqual("ai",state.Latest.lessonId); Assert.IsTrue(state.Valid());
            var saved=JsonUtility.FromJson<OpsState>(JsonUtility.ToJson(state)); Assert.AreEqual(state.Latest.eventTitle,saved.Latest.eventTitle);
            saved.eventSchedule[1]="unknown"; Assert.IsFalse(saved.Valid());
            var old=new OpsState(14); Assert.AreEqual(0,old.eventRules); Assert.IsTrue(old.Valid()); Assert.AreEqual(OpsCatalog.Months[0].title,old.Current.title);
            Assert.IsNull(old.Ticket); Assert.IsFalse(old.ResolveTicket(false)); old.BeginIncident(); old.Resolve("scope"); Assert.IsTrue(old.Valid());
        }
        [UnityTest] public IEnumerator 四十種の相談と対応を表示しチケットを社員へ任せられる()
        {
            SceneManager.LoadScene("CompanyYear"); yield return null; yield return new WaitForSeconds(.5f);
            var game=Object.FindAnyObjectByType<OpsGame>();
            foreach (var e in OpsEventCatalog.Events)
            {
                game.StartYear(14); SetEvent(game.State,e.id); game.OpenTab(0); yield return null;
                CheckText();CheckPointer("ConsultationDetails");Click("ConsultationDetails");yield return null;CheckPointer("OpenEventBrief");
                if (e.id=="ai-upload") { Capture("35-ai-brief",1280,720); Click("OpenEventBrief"); yield return null; CheckPointer("EventKnowledge"); Capture("36-event-details"); }
                Click("CloseDialog");yield return null;Click("Menu");yield return null;Click("AdvancedSettings");yield return new WaitForSecondsRealtime(.5f);CheckPointer("OpenTicket");Click("CloseDialog");yield return null;
                game.BeginIncident(); yield return null; CheckText(); CheckPointer("Respond_scope");
                if (e.id=="ops-update") Capture("37-update-incident",1280,720);
                game.Resolve("scope"); yield return null; CheckText();
            }
            game.StartYear(14); yield return null; var state=game.State;
            Click("OpenTicket"); yield return null; CheckPointer("ResolveTicket"); Capture("38-daily-ticket",1280,720);
            Click("ResolveTicket"); yield return null; Assert.AreEqual("self",state.ticketResolution);
            Assert.AreEqual(3,state.capacity); game.BeginIncident(); game.Resolve("scope"); game.Next(); yield return null;
            state.staffExperience[state.Ticket.member]=3; state.levels[OpsCatalog.Index("runbook")]=1; game.OpenTab(0); yield return null;
            Click("OpenTicket"); yield return null; CheckPointer("DelegateTicket"); Click("DelegateTicket"); yield return null;
            Assert.AreEqual("delegate",state.ticketResolution); Assert.IsEmpty(glyphWarnings,string.Join("\n",glyphWarnings)); LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
