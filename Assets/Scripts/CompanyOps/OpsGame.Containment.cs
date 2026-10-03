using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private readonly Button[] containmentPCs=new Button[OpsCatalog.ContainmentRooms*OpsCatalog.ContainmentPCsPerRoom];
        private readonly Button[] containmentRooms=new Button[OpsCatalog.ContainmentRooms];
        private Button containmentScan,containmentWide,containmentEdr;
        private RectTransform containmentDanger;
        private float minigameHeartbeatAt;
        private readonly Dictionary<string,AudioClip> minigameTones=new Dictionary<string,AudioClip>();
        partial void DrawMinigameBackground(RectTransform parent)
        {
            var grid=MinigameShape(parent,"MinigameNetworkGrid","grid",0,0,1280,760,new Color(.25f,.66f,.96f,.06f));MinigameVisual(grid,"grid",30);
            for(int i=0;i<18;i++)
            {
                var mote=PCard(parent,"MinigameMote_"+i,(i*193)%1280,310+(i*71)%430,4,4,new Color(.5f,.81f,1,.5f),12,false);
                MinigameVisual(mote,"mote",6+i%8,new Vector2(0,i*.13f));
            }
        }
        partial void DrawMinigameBoard(RectTransform parent)
        {
            var game=Minigame as OpsContainmentMinigame;if(game==null)return;
            minigameHeartbeatAt=-1;
            var office=PCard(parent,"MinigameOffice",0,110,900,560,Color.white,20);
            MinigameGloss(office,900,560);
            for(int r=0;r<OpsCatalog.ContainmentRooms;r++)
            {
                int room=r;float x=18+r*219.5f;
                var button=PButton(office,"MinigameRoom_"+r,"",x,18,205.5f,524,()=>SelectMinigameRoom(room),Hex("eef3fa"),PlanInk,16);
                containmentRooms[r]=button;button.GetComponent<OpsButtonFeedback>().PressDepth=0;
                PText(button.transform,"MinigameRoomName_"+r,OpsCatalog.ContainmentRoomNames[r],10,7,68,24,15);
                PText(button.transform,"MinigameSelected_"+r,r==0?"選択中":"部屋を選ぶ",78,7,117.5f,24,12,PlanGray,true,true);
                var selected=button.gameObject.AddComponent<Outline>();selected.effectColor=Hex("3fa9f5");selected.effectDistance=new Vector2(3,-3);selected.enabled=r==0;
                for(int row=0;row<OpsCatalog.ContainmentPCsPerRoom;row++)
                {
                    int index=r*OpsCatalog.ContainmentPCsPerRoom+row;
                    var pc=PButton(button.transform,"MinigamePC_"+index,"",10,40+row*93.4f,185.5f,85.4f,()=>CutMinigamePC(index),Color.white,PlanInk,16,Hex("c7d0e0"));
                    pc.GetComponent<Shadow>().effectDistance=new Vector2(0,-3);containmentPCs[index]=pc;
                    var colors=pc.colors;colors.disabledColor=Color.white;pc.colors=colors;
                    MinigameShape(pc.transform,"MinigamePCIcon","pc",70.75f,20.7f,44,44,PlanInk);
                    var cut=MinigameShape(pc.transform,"MinigameCutSlash","slash",28,37,129.5f,12,Hex("8892a6"));cut.gameObject.SetActive(false);
                    var suspect=PText(pc.transform,"MinigameSuspect","?",153,4,24,28,20,Hex("ff8a3d"),true,true);Motion(suspect.rectTransform,"blink",1);
                    suspect.gameObject.SetActive(false);
                    var glow=pc.gameObject.AddComponent<Outline>();glow.effectColor=Hex("e0405f");glow.effectDistance=new Vector2(4,-4);glow.enabled=false;
                }
            }
            containmentDanger=MinigameShape(office,"MinigameDanger","danger-edge",0,0,900,560,new Color(.88f,.25f,.37f,.45f));
            MinigameVisual(containmentDanger,"danger",.8f);containmentDanger.gameObject.SetActive(false);
            game.Spread+=(from,to)=>MinigameSpreadLine(from,to);
        }
        partial void DrawMinigameTools(RectTransform parent)
        {
            containmentScan=PButton(parent,"MinigameScan","",18,56,328,76,ScanMinigameRoom,Color.white,PlanInk,16,Hex("c7d0e0"));
            PText(containmentScan.transform,"MinigameScanLabel","部屋を調べる（あと2回）",12,4,304,32,17);
            PText(containmentScan.transform,"MinigameScanHint","部屋を選んでから押す。感染が3秒見える",12,36,304,32,12,PlanGray,false);
            containmentWide=PButton(parent,"MinigameWide","",18,142,328,76,StopMinigameRoom,Color.white,PlanInk,16,Hex("c7d0e0"));
            PText(containmentWide.transform,"MinigameWideLabel","部屋ごと止める",12,4,304,32,17);
            PText(containmentWide.transform,"MinigameWideHint","選んだ部屋を全部切り離す。業務も止まる",12,36,304,32,12,PlanGray,false);
            containmentEdr=null;
            if(Minigame is OpsContainmentMinigame c&&c.Edr)
            {
                containmentEdr=PButton(parent,"MinigameEdr","EDR 即時隔離 / あと1回\n<size=11>最初の感染端末 / 時間消費なし</size>",18,220,328,44,EdrIsolateMinigamePC,Hex("dff1ff"),PlanInk,14,Hex("8ac9ef"));
                var label=containmentEdr.GetComponentInChildren<TextMeshProUGUI>();label.fontSize=14;label.enableAutoSizing=false;
                label.overflowMode=TextOverflowModes.Overflow;label.alignment=TextAlignmentOptions.Center;
            }
            else PText(parent,"MinigameToolHint","端末を押すと1台ずつ切り離す。\n赤は確認できた感染、?は様子がおかしい端末。",18,228,328,36,12,PlanGray,false);
        }
        partial void RefreshMinigameBoard()
        {
            var game=Minigame as OpsContainmentMinigame;if(game==null)return;
            bool playing=game.Phase==OpsMinigamePhase.Playing;
            for(int r=0;r<OpsCatalog.ContainmentRooms;r++)
            {
                bool selected=r==game.SelectedRoom;
                int detected=game.EdrRoomAlerts(r);
                containmentRooms[r].GetComponent<Image>().color=selected||game.Scanning(r)?Hex("dff1ff"):Hex("eef3fa");
                containmentRooms[r].interactable=playing;containmentRooms[r].GetComponent<Outline>().enabled=selected||game.Scanning(r)||detected>0;
                containmentRooms[r].GetComponent<Outline>().effectColor=detected>0?Hex("e0405f"):Hex("3fa9f5");
                var roomNote=FindMinigameText("MinigameSelected_"+r);roomNote.text=detected>0?"EDR 検知 "+detected:selected?"選択中":"部屋を選ぶ";roomNote.color=detected>0?Hex("c23a60"):PlanGray;
            }
            for(int i=0;i<game.PCCount;i++)
            {
                var pc=containmentPCs[i];bool seen=game.Visible(i),cut=game.IsStopped(i);
                pc.interactable=playing&&!cut;pc.GetComponent<Image>().color=cut?game.StoppedInfection(i)?Hex("cfe9d8"):Hex("d7dce6"):seen?Hex("ffe1e6"):Color.white;
                pc.GetComponent<Shadow>().enabled=!cut;pc.GetComponent<Outline>().enabled=seen;
                pc.GetComponent<Outline>().effectColor=new Color(.88f,.25f,.37f,ReducedMotion?.55f:.3f+.25f*Mathf.Sin(Time.unscaledTime*Mathf.PI*2/.6f));
                pc.transform.Find("MinigameCutSlash").gameObject.SetActive(cut);pc.transform.Find("MinigameSuspect").gameObject.SetActive(game.Suspect(i));
            }
            FindMinigameText("MinigameVisible").text="見えている感染 "+game.VisibleCount;
            FindMinigameText("MinigameStopped").text="停止 "+game.StoppedCount;
            FindMinigameText("MinigameScanLabel").text="部屋を調べる（あと"+game.ScansLeft+"回）";
            containmentScan.interactable=playing&&game.ScansLeft>0;containmentWide.interactable=playing;
            if(containmentEdr!=null)
            {
                containmentEdr.interactable=game.CanEdrIsolate;
                containmentEdr.GetComponentInChildren<TextMeshProUGUI>().text=(game.EdrIsolationsLeft==0?"EDR 即時隔離 / 使用済み":game.CanEdrIsolate?"EDR 即時隔離 / あと1回":"EDR 即時隔離 / 検知対象なし")+"\n<size=11>最初の感染端末 / 時間消費なし</size>";
            }
            bool low=playing&&game.Remaining<OpsCatalog.MinigameDangerSeconds;containmentDanger.gameObject.SetActive(low);
            if(low&&game.Elapsed-minigameHeartbeatAt>(game.Remaining<OpsCatalog.MinigameUrgentSeconds?.45f:.75f))
            {minigameHeartbeatAt=game.Elapsed;MinigameTone(90);StartCoroutine(DelayedMinigameTone(70,.09f));}
        }
        private void SelectMinigameRoom(int room)
        {if((Minigame as OpsContainmentMinigame)?.SelectRoom(room)==true)RefreshMinigameBoard();}
        private void ScanMinigameRoom()
        {
            var game=Minigame as OpsContainmentMinigame;if(game==null||!game.Scan())return;
            MinigameTone(880);StartCoroutine(DelayedMinigameTone(1175,.08f));
            FindMinigameText("NavigatorSpeech").text=CaptionsEnabled?OpsCatalog.ContainmentRoomNames[game.SelectedRoom]+"を調べたよ。3秒だけ感染が見える！":"";RefreshMinigameBoard();
        }
        private void StopMinigameRoom()
        {
            var game=Minigame as OpsContainmentMinigame;if(game==null||game.StopRoom()==0)return;
            MinigameTone(160,"saw");MinigameBanner(OpsCatalog.ContainmentRoomNames[game.SelectedRoom]+"を止めた",PlanBlue);
            SpeakSceneLine("mg_miss_0"+(1+minigameMissVoice++%2),0);RefreshMinigameBoard();
        }
        private void CutMinigamePC(int index)
        {
            var game=Minigame as OpsContainmentMinigame;if(game==null)return;
            var result=game.Cut(index);if(result==OpsTerminalCut.None)return;
            PresentMinigameCut(index,result);
        }
        private void EdrIsolateMinigamePC()
        {
            var game=Minigame as OpsContainmentMinigame;if(game==null)return;
            int index=game.EdrIsolate();if(index<0)return;
            PresentMinigameCut(index,OpsTerminalCut.Infected);
        }
        private void PresentMinigameCut(int index,OpsTerminalCut result)
        {
            var game=Minigame as OpsContainmentMinigame;if(game==null)return;
            var center=MinigamePCCenter(index);
            if(result==OpsTerminalCut.Infected)
            {
                MinigameVisual((RectTransform)containmentPCs[index].transform,"hit",.09f);MinigameCutBurst(center);
                MinigameTone(520*Mathf.Pow(1.12f,Mathf.Min(game.Streak,8)));
                MinigamePop(center,game.Streak>=2?"ナイス遮断 ×"+game.Streak:"ナイス遮断！",Hex("2bb673"));
                if(game.Streak==3)SpeakSceneLine("mg_combo_0"+(1+minigameComboVoice++%2),0);
                else if(LastReactionId!="mg_contain_cut"&&game.Streak==1)SpeakSceneLine("mg_contain_cut",0);
            }
            else
            {
                MinigameTone(200,"square");MinigamePop(center,"正常な端末…",Hex("9aa6bd"));MinigameVisual(minigameCanvas,"shake",.35f);
                int miss=minigameMissVoice++;SpeakSceneLine(miss%2==0?"mg_contain_false":"mg_miss_0"+(1+(miss/2)%2),0);
            }
            RefreshMinigameBoard();
        }
        partial void DescribeMinigameResult(ref string text)
        {
            var game=Minigame as OpsContainmentMinigame;if(game==null)return;
            double f=1-OpsCatalog.MinigameResultInfluence*(game.Score-50)/50;
            bool final=!DailyPracticeActive&&State!=null&&State.SupportsFinalRecovery;
            text=game.Finding+"\n感染 "+game.TotalInfected+"台（止めきれず "+game.Uncontained+"台） / 正常なのに停止 "+game.NormalStopped+"台\n"+(final?"被害に ×":"被害・停止に ×")+f.ToString("F2")+"。現行結果を含む目安の幅で抑えます。"+
                (final?"\n停止への影響は、次の復旧の点数で決まります。":"")+
                (game.Backup?"\n分離バックアップの効果は反映済み（二重に減らしません）。":"");
        }
        private Vector2 MinigamePCCenter(int index)=>new Vector2(18+index/5*219.5f+10+185.5f/2,110+18+40+index%5*93.4f+85.4f/2);
        private void MinigamePop(Vector2 center,string text,Color color)
        {
            var pop=PText(minigameCanvas,"MinigameCutFeedback",text,center.x-150,center.y-30,300,40,20,Color.white,true,true);
            var outline=pop.gameObject.AddComponent<Outline>();outline.effectColor=color;outline.effectDistance=new Vector2(2,-2);
            MinigameVisual(pop.rectTransform,"pop",1);
        }
        private void MinigameCutBurst(Vector2 center)
        {
            var ring=MinigameShape(minigameCanvas,"MinigameCutRing","ring",center.x-10,center.y-10,20,20,Color.white);
            ring.pivot=new Vector2(.5f,.5f);ring.anchoredPosition=new Vector2(center.x,-center.y);MinigameVisual(ring,"ring",.45f);
            for(int i=0;i<10;i++)
            {
                float angle=i*Mathf.PI*2/10;
                var shard=Box(minigameCanvas,"MinigameCutShard",center.x,center.y,9,9,i%2==0?Color.white:Hex("ff7a93"));shard.GetComponent<Image>().raycastTarget=false;
                MinigameVisual(shard,"shard",.55f,new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*(50+i*5));
            }
        }
        private void MinigameSpreadLine(int from,int to)
        {
            var a=MinigamePCCenter(from);var b=MinigamePCCenter(to);
            var line=MinigameShape(minigameCanvas,"MinigameSpreadLine","line",0,0,1280,760,Hex("ff4d6d"));
            var graphic=line.GetComponent<OpsMinigameGraphic>();graphic.From=new Vector2(a.x,-a.y);graphic.To=new Vector2(b.x,-b.y);
            var glow=line.gameObject.AddComponent<Shadow>();glow.effectColor=new Color(1,.3f,.43f,.3f);glow.effectDistance=new Vector2(0,-3);
            MinigameVisual(line,"line",.7f);MinigameTone(1400,"triangle");
        }
        private RectTransform MinigameShape(Transform parent,string name,string kind,float x,float y,float w,float h,Color color)
        {var rect=Rect(parent,name,x,y,w,h);var shape=rect.gameObject.AddComponent<OpsMinigameGraphic>();shape.Kind=kind;shape.color=color;shape.raycastTarget=false;return rect;}
        private void MinigameGloss(RectTransform panel,float width,float height)
        {
            var gloss=Box(panel,"MinigameTopGloss",14,3,width-28,height*.4f,Color.white);
            var image=gloss.GetComponent<Image>();image.raycastTarget=false;image.sprite=PlanningArt.round16;image.type=Image.Type.Sliced;
            KitGradient(image,new Color(1,1,1,.6f),new Color(1,1,1,0));
        }
        private void MinigameVisual(RectTransform target,string kind,float duration,Vector2 direction=default)
        {
            var old=target.GetComponent<OpsMinigameVisual>();if(old!=null){old.Stop();Destroy(old);}
            var motion=target.gameObject.AddComponent<OpsMinigameVisual>();motion.Owner=this;motion.Kind=kind;motion.Duration=kind=="stamp"?duration/PresentationRate:duration;motion.Direction=direction;
        }
        private void MinigameTone(float frequency,string wave="sine")
        {
            if(!Application.isPlaying||Minigame==null||muted||soundVolume<=0)return;
            string key=Mathf.RoundToInt(frequency)+"/"+wave;
            if(!minigameTones.TryGetValue(key,out var clip))
            {
                const int rate=24000;var data=new float[Mathf.RoundToInt(rate*.21f)];
                for(int i=0;i<data.Length;i++)
                {
                    float t=i/(float)rate,p=t*frequency,v=wave=="square"?Mathf.Sign(Mathf.Sin(p*Mathf.PI*2)):wave=="saw"?2*Mathf.Repeat(p,1)-1:wave=="triangle"?1-4*Mathf.Abs(Mathf.Repeat(p+.25f,1)-.5f):Mathf.Sin(p*Mathf.PI*2);
                    data[i]=v*.14f*Mathf.Min(1,t/.006f)*Mathf.Exp(-t*25)*(1-t/.21f);
                }
                clip=AudioClip.Create("封じ込めSE_"+key,data.Length,1,rate,false);clip.SetData(data,0);minigameTones.Add(key,clip);
            }
            if(minigameAudio==null)minigameAudio=NewAudioSource();minigameAudio.pitch=1;minigameAudio.volume=soundVolume*.65f;minigameAudio.PlayOneShot(clip);ObservePromoAudio(minigameAudio,clip,"sfx-generated",key);
        }
        private IEnumerator DelayedMinigameTone(float frequency,float delay)
        {var session=Minigame;yield return new WaitForSecondsRealtime(delay);if(Minigame==session)MinigameTone(frequency);}
        private IEnumerator MinigameChord(float delay=0)
        {
            var session=Minigame;if(delay>0)yield return new WaitForSecondsRealtime(delay);
            foreach(float note in new[]{523f,659f,784f,1046f}){if(Minigame!=session)yield break;MinigameTone(note);yield return new WaitForSecondsRealtime(.07f);}
        }
    }
}
