using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private string roomFilter="";
        public static string ProjectRoom(string id)=>id=="mfa"||id=="inventory"?"office":id=="education"||id=="drill"||id=="runbook"||id=="automation"?"meeting":"server";
        public static string EventRoom(string kind,string profile)=>profile=="bec"||kind=="supply"?"reception":kind=="leak"||kind=="identity"?"office":kind=="social"?"meeting":"server";
        private string CurrentRoom=>EventRoom(State.Current.kind,State.CurrentProfile?.id);
        private static string RoomName(string room)=>room=="office"?"執務室":room=="meeting"?"会議室":room=="reception"?"受付":"サーバー室";
        private static Rect RoomBounds(string room)=>room=="office"?new Rect(86,96,246,207):room=="meeting"?new Rect(570,96,244,200):room=="reception"?new Rect(330,340,227,269):new Rect(342,96,214,226);
        private void PlanningRooms(RectTransform stage)
        {
            foreach(string id in new[]{"office","server","meeting","reception"})
            {
                var area=RoomBounds(id);var room=PButton(stage,"Room_"+id,"",area.x,area.y,area.width,area.height,()=>OpenRoomProjects(id),Color.clear,Color.clear,16);
                Hover(room,RoomName(id)+" / "+(id=="reception"?"社外との連絡・日常業務":"この部屋の設備・運用を整える"));
                var label=PCard(room.transform,"RoomLabel",8,6,Mathf.Min(area.width-16,132),24,new Color(1,1,1,.92f),12,false);PText(label,"RoomLabelText",RoomName(id),0,0,label.rect.width,24,13,PlanInk,true,true);
                var ids=OpsCatalog.Projects.Where(p=>ProjectRoom(p.id)==id).Select(p=>p.id).ToArray();
                int placeholders=0,slot=0;
                for(int i=0;i<ids.Length;i++)
                {
                    int level=State.Level(ids[i]);if(level==0&&placeholders++>=3)continue;float x=16+slot++*34;
                    if(level==0)
                    {
                        var device=Rect(room.transform,"RoomDevice_"+ids[i],area.width-40-(placeholders-1)*34,area.height-44,26,30);
                        var frame=IncidentShape(device,"UninstalledFrame","round-dashed",0,0,26,30,new Color(1,1,1,.5f));
                        frame.GetComponent<OpsIncidentGraphic>().StrokeWidth=1;
                        PText(device,"UninstalledPlus","+",0,0,26,30,20,new Color(1,1,1,.5f),true,true);
                    }
                    else
                    {
                        var device=Box(room.transform,"RoomDevice_"+ids[i],x,area.height-44,24,30,level==1?Hex("2774a8"):Hex("1a7c63"));
                        for(int n=0;n<2;n++)PCard(device,"DeviceLight"+n,5,8+n*9,14,4,n<level?n==1?Hex("ffd23f"):Hex("5dff9c"):Hex("6b7894"),12,false);
                    }
                }
            }
            PButton(stage,"Room_officeLower","",86,308,220,222,()=>OpenRoomProjects("office"),Color.clear,Color.clear,16);
        }
        private void OpenRoomProjects(string room)
        {
            if(room=="reception")
            {
                var d=Dialog("受付 / 社外とのやりとり","外部委託先や訪問者、請求・送金の依頼を確認する窓口。侵害が起きた場所を確定する表示ではありません。",440);
                Button(d,"ReceptionBrief","今月の社外連絡と根拠",32,220,748,48,EventBriefDialog,Accent);
                Button(d,"ReceptionTicket","日常の問い合わせ",32,280,748,48,TicketDialog,Edge,State.Ticket!=null);return;
            }
            roomFilter=room;filter="all";tab=1;Render();
        }
        private void RoomSupportMarker(string member)
        {
            var old=screen.Find("SupportRoom");if(old!=null){old.gameObject.SetActive(false);Destroy(old.gameObject);}
            var area=RoomBounds(CurrentRoom);var card=PCard(screen,"SupportRoom",1140,112,400,164,PlanInk,20,false);card.gameObject.AddComponent<Mask>().showMaskGraphic=true;
            float scale=Mathf.Max(360/area.width,120/area.height);
            PImage(card,"SupportRoomArt",OfficeArt,20-area.x*scale,32-area.y*scale,900*scale,900*scale,new Color(.55f,.55f,.55f,1));
            var label=PCard(card,"SupportRoomTag",12,8,180,26,Color.white,12,false);PText(label,"SupportRoomName",RoomName(CurrentRoom)+"へ応援",0,0,180,26,14,PlanInk,true,true);
            var face=PCard(card,"RoomHelper",20,70,44,44,PlanBlue,20,false);PText(face,"RoomHelperInitial",member.Substring(0,1),0,0,44,44,22,Color.white,true,true);
            PText(card,"RoomHelperName",member+" / 実際に支援",212,122,176,26,14,Color.white,true,true);
            var motion=face.gameObject.AddComponent<OpsRoomHelperMotion>();motion.Owner=this;motion.TargetRoom=CurrentRoom;
        }
        private void ShowInstallation(int index)
        {
            if (!Application.isPlaying) return;
            var project = OpsCatalog.Projects[index];
            var map = screen.Find("OfficeStage") as RectTransform;
            if (map == null) return;
            string target = "Room_"+ProjectRoom(project.id);
            var pin = map.Find(target) as RectTransform;
            if (pin != null) StartCoroutine(InstallationPulse(pin));
            var marker = Box(map, "InstallationNotice", 18, 86, 628, 44, Ink);
            marker.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            marker.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            Text(marker, "InstallationLabel", project.name + "  Lv." + State.levels[index] + "  導入完了", 14, 7, 600, 30, 22, Mint);
            StartCoroutine(FadeOfficeNotice(marker));
        }

        private IEnumerator InstallationPulse(RectTransform pin)
        {
            float elapsed = 0;
            while (pin != null && elapsed < .7f)
            {
                pin.localScale = Vector3.one * (ReducedMotion ? 1 : 1 + .06f * Mathf.Sin(elapsed / .7f * Mathf.PI));
                elapsed += Time.unscaledDeltaTime; yield return null;
            }
            if (pin != null) pin.localScale = Vector3.one;
        }

        private IEnumerator FadeOfficeNotice(RectTransform notice)
        {
            var group = notice.GetComponent<CanvasGroup>();
            yield return new WaitForSecondsRealtime(1.1f);
            float elapsed = 0;
            while (notice != null && elapsed < .35f)
            {
                group.alpha = 1 - elapsed / .35f;
                elapsed += Time.unscaledDeltaTime; yield return null;
            }
            if (notice != null) Destroy(notice.gameObject);
        }

        private void OfficeOutcome(RectTransform map, OpsOutcome result)
        {
            if (result == null) return;
            // 停止時間は今月の確定記録。現在も停止中、侵害地点が特定済みとは描写しない。
            var record = Box(map, "OfficeOutcome", 18, 86, 230, 104, Ink);
            record.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            Color tone = result.loss > 0 || result.downtime > 4 ? Coral : Mint;
            Box(record, "OutcomeRail", 0, 0, 4, 105, tone).GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            Text(record, "OfficeOutcomeTitle", "今月の対応完了", 13, 9, 204, 26, 20, tone);
            Text(record, "OfficeOutcomeNumbers", "被害 " + result.loss + "万円 / 停止 " + result.downtime + "h", 13, 40, 204, 28, 17);
            Text(record, "OfficeOutcomeNote", "今月の記録 / 現在の障害ではない", 13, 77, 204, 21, 12, Muted);
            if (result.power == null) return;
            var support = Box(map, "OutcomeSupport", 18, 209, 404, 74, Ink);
            support.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            Text(support, "OutcomeSupportTitle", result.power.staff > 0 ? "社員の助力  +" + result.power.staff + " 抑制力" : "社員の対応加算 0 / 担当・習熟に応じる", 14, 8, 376, 28, 20, result.power.staff > 0 ? StaffColor : Muted);
            Text(support, "OutcomeSupportDetail", result.power.support, 14, 39, 376, 27, 15, Paper);
        }
    }
    public sealed class OpsRoomHelperMotion : MonoBehaviour
    {
        public OpsGame Owner;public string TargetRoom;private float elapsed;
        private void Update(){elapsed+=Time.unscaledDeltaTime;float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.45f));
            ((RectTransform)transform).anchoredPosition=new Vector2(Owner.ReducedMotion?290:Mathf.Lerp(20,290,t),-70+(Owner.ReducedMotion?0:22*Mathf.Sin(t*Mathf.PI)));if(t>=1)enabled=false;}
    }
    public sealed class OpsStatSpark : MonoBehaviour
    {
        public OpsGame Owner;public Vector2 From,To;public float Delay;public int Stat;private float elapsed;
        private void Update(){elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01((elapsed-Delay)/.6f);var rect=(RectTransform)transform;
            rect.anchoredPosition=Owner.ReducedMotion?To:Vector2.Lerp(From,To,Mathf.SmoothStep(0,1,t))+Vector2.up*120*Mathf.Sin(t*Mathf.PI);
            GetComponent<Image>().color=new Color(GetComponent<Image>().color.r,GetComponent<Image>().color.g,GetComponent<Image>().color.b,elapsed<Delay?0:1-t*.7f);if(t>=1)Destroy(gameObject);}
    }
    public sealed class OpsStatGaugeGrow : MonoBehaviour
    {
        public OpsGame Owner;public float From,To;private float elapsed;
        private void Update(){elapsed+=Time.unscaledDeltaTime;((RectTransform)transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,Owner.ReducedMotion?To:Mathf.Lerp(From,To,Mathf.SmoothStep(0,1,elapsed/.45f)));if(elapsed>=.45f)Destroy(this);}
    }
}
