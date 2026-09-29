using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        public OpsMinigame Minigame {get;private set;}
        public bool MinigameActive=>Minigame!=null;
        private RectTransform minigameCanvas,minigameModal;
        private Action<OpsMinigame> minigameDone;
        private string minigameResponse;
        private int minigameStartVoice,minigameComboVoice,minigameMissVoice;
        private float minigameResultAt;
        private bool minigameResultDrawn;
        private AudioSource minigameAudio;
        public bool MinigameCounting {get;private set;}
        public void ChooseResponse(string response)
        {
            if(State==null||State.phase!=OpsPhase.Incident||!ResponseIds.Contains(response)||MinigameActive)return;
            var session=State.CreateContainment();
            if(session==null){Resolve(response);return;}
            OpenMinigame(session,response,s=>Resolve(response,s.Score,s.Delegated));
        }
        public bool OpenMinigame(OpsMinigame session,string response,Action<OpsMinigame> complete)
        {
            if(session==null||session.Phase!=OpsMinigamePhase.Brief||State==null||State.phase!=OpsPhase.Incident||MinigameActive||ResolutionActive)return false;
            Minigame=session;minigameResponse=response;minigameDone=complete;minigameResultDrawn=false;MinigameCounting=false;
            StopVoice();homeVisible=false;NewScreen();
            var shared=screen.Find("SharedBackground");if(shared!=null)shared.gameObject.SetActive(false);
            KitGradient(screen.GetComponent<Image>(),Hex("2a1830"),Hex("1d2a44"));
            minigameCanvas=Box(screen,"MinigameCanvas",42.105f,0,1280,760,Hex("1d2a44"));minigameCanvas.localScale=Vector3.one*(900f/760f);
            KitGradient(minigameCanvas.GetComponent<Image>(),Hex("2a1830"),Hex("1d2a44"));
            DrawMinigameBackground(minigameCanvas);
            IncidentShape(minigameCanvas,"MinigameHazard","tape",0,0,1280,14,Hex("ffc02e"));
            var top=PCard(minigameCanvas,"MinigameTop",0,24,1280,70,Color.white,20);
            MinigameGloss(top,1280,70);
            var alert=PCard(top,"MinigameAlert",20,12,140,46,Hex("e0405f"),16,false);PText(alert,"MinigameAlertText","緊急対応",0,0,140,46,24,Color.white,true,true);
            KitGradient(alert.GetComponent<Image>(),Hex("ff7a93"),Hex("e0405f"));
            var topic=PCard(top,"MinigameTopic",172,18,328,34,PlanInk,12,false);
            PText(topic,"MinigameEvent",State.CurrentEvent?.title??State.Current.@event,10,0,308,34,17,Color.white,true,true);
            var timer=PCard(top,"MinigameTimer",520,26,310,18,Hex("e3e8f1"),12,false);
            var fill=PCard(timer,"MinigameTimerFill",0,0,310,18,Hex("e0405f"),12,false);KitGradient(fill.GetComponent<Image>(),Hex("ffc02e"),Hex("e0405f"),true);
            PText(top,"MinigameTime","20.0",842,13,66,44,20,PlanInk,true,true);
            PText(top,"MinigameVisible","見えている感染 0",918,13,208,44,16,Hex("e0405f"),true,true);
            PText(top,"MinigameStopped","停止 0",1134,13,130,44,18,PlanInk,true,true);
            DrawMinigameBoard(minigameCanvas);
            var side=PCard(minigameCanvas,"MinigameSide",916,110,364,560,Color.white,20);
            MinigameGloss(side,364,560);
            PText(side,"MinigameToolsHeading","道具",18,14,328,32,18);
            DrawMinigameTools(side);
            var speech=PCard(side,"MinigameSpeech",18,268,328,86,Color.white,16);
            var outline=speech.gameObject.AddComponent<Outline>();outline.effectColor=Hex("ffd3de");outline.effectDistance=new Vector2(2,-2);
            PText(speech,"NavigatorSpeech","備えと判断を活かして対応しよう。",12,8,304,70,15,null,false);
            Portrait(side,"NavigatorPortrait",156,344,198,210,"pose_startled");
            MinigameBrief();return true;
        }
        private RectTransform MinigameModal(string title)
        {
            if(minigameModal!=null){minigameModal.gameObject.SetActive(false);Destroy(minigameModal.gameObject);}
            minigameModal=Box(minigameCanvas,"MinigameModal",0,0,1280,760,new Color(.04f,.06f,.12f,.55f));
            minigameModal.GetComponent<Image>().raycastTarget=true;
            var card=PCard(minigameModal,"MinigameModalCard",300,156,680,440,Color.white,20);
            MinigameGloss(card,680,440);
            PText(card,"MinigameModalTitle",title,26,20,628,46,28);return card;
        }
        private void MinigameBrief()
        {
            var card=MinigameModal(Minigame.Title);
            PText(card,"MinigameInstructions","20秒で広がりを止めよう。\n端末を押す：1台ずつ切り離す\n部屋を選んで調べる：隠れた感染が3秒見える\n部屋ごと止める：速いが、正常な端末も止まる",26,78,628,108,17,null,false);
            PText(card,"MinigameEquipment",MinigameEquipment(),26,204,628,112,16,null,false);
            PButton(card,"MinigameStart","対応を始める",26,332,390,58,StartMinigame,PlanPink,Color.white,20);
            PButton(card,"MinigameDelegate","社員に任せる / 50点",432,332,222,58,DelegateMinigame,Color.white,PlanInk,17);
        }
        private string MinigameEquipment()=>"事件の前にそろえた備え\n"+
            (Minigame.Monitor?"導入済み":"未導入")+"：監視と通知 / 感染がすぐ見える\n"+
            (Minigame.Segment?"導入済み":"未導入")+"：ネットワーク分離 / 部屋をまたがない\n"+
            (Minigame.Backup?"導入済み":"未導入")+"：分離バックアップ / 結果の復旧に反映済み";
        public void StartMinigame()
        {
            if(Minigame==null||!Minigame.Start())return;
            minigameModal.gameObject.SetActive(false);Destroy(minigameModal.gameObject);minigameModal=null;
            SpeakSceneLine("mg_start_0"+(1+minigameStartVoice++%2),0);RefreshMinigameBoard();
        }
        public void DelegateMinigame()
        {
            if(Minigame==null||!Minigame.Delegate())return;
            MinigameResult();SpeakSceneLine("mg_delegate",0);
        }
        public void TickMinigame(float delta)
        {
            if(Minigame==null)return;
            var phase=Minigame.Phase;Minigame.Tick(delta);
            if(phase==OpsMinigamePhase.Playing)
            {
                FindMinigameText("MinigameTime").text=Minigame.Remaining.ToString("F1");
                var fill=minigameCanvas.Find("MinigameTop/MinigameTimer/MinigameTimerFill") as RectTransform;
                if(fill!=null)fill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,310*Minigame.Remaining/OpsCatalog.MinigameSeconds);
                RefreshMinigameBoard();
                if(Minigame.Phase==OpsMinigamePhase.Result){minigameResultAt=Time.unscaledTime+1.1f;ShowMinigameFinish();}
            }
            if(Minigame.Phase==OpsMinigamePhase.Result&&!minigameResultDrawn&&Time.unscaledTime>=minigameResultAt)MinigameResult();
        }
        private TextMeshProUGUI FindMinigameText(string name)=>minigameCanvas.GetComponentsInChildren<TextMeshProUGUI>(true).First(t=>t.name==name);
        private void ShowMinigameFinish()
        {
            var containment=Minigame as OpsContainmentMinigame;
            string title=containment!=null&&containment.TotalInfected==0?"確認完了":containment!=null&&containment.Uncontained>0?"広がってしまった…":"封じ込め成功！";
            bool good=Minigame.Score>=OpsCatalog.MinigameGood;
            MinigameBanner(title,good?PlanMint:Hex("e0405f"));
            if(good)StartCoroutine(MinigameChord());else MinigameTone(150,"saw");
            SpeakSceneLine(Minigame.EndVoice,0);
        }
        private void MinigameResult()
        {
            minigameResultDrawn=true;var card=MinigameModal(Minigame.Delegated?"社員が対応しました":"感染の封じ込め / 結果");
            var stamp=PCard(card,"MinigameRankStamp",26,88,170,116,Minigame.Grade=="S"?Hex("e0a100"):PlanPink,22,false);
            KitGradient(stamp.GetComponent<Image>(),Minigame.Grade=="S"?Hex("ffe38a"):Hex("ffb3c6"),Minigame.Grade=="S"?Hex("e0a100"):PlanPink);
            var depth=stamp.gameObject.AddComponent<Shadow>();depth.effectDistance=new Vector2(0,-8);depth.effectColor=Minigame.Grade=="S"?Hex("a87400"):Hex("d94a70");
            PText(stamp,"MinigameGrade",Minigame.Grade,0,0,170,116,92,Color.white,true,true);Shine(stamp,170,116);
            PText(card,"MinigameScore",Minigame.Score+"点",222,94,432,76,48,PlanInk,true,true);
            PText(card,"MinigameResultDetail",Minigame.Delegated?"社員に任せたため、現在と同じ50点の対応です。":MinigameResultDetail(),26,224,628,112,16,null,false);
            PButton(card,"MinigameContinue","結果を反映する",26,352,628,58,ConfirmMinigame,PlanPink,Color.white,20);
            if(!Minigame.Delegated)StartCoroutine(CountMinigameResult(stamp,Minigame.Score));
        }
        public void ConfirmMinigame()
        {
            if(Minigame==null||Minigame.Phase!=OpsMinigamePhase.Result)return;
            var session=Minigame;var complete=minigameDone;CancelMinigame();complete?.Invoke(session);
        }
        private void CancelMinigame()
        {
            Minigame=null;minigameDone=null;minigameResultDrawn=false;MinigameCounting=false;
            if(minigameAudio!=null)minigameAudio.Stop();
        }
        private string MinigameResultDetail(){string text="";DescribeMinigameResult(ref text);return text;}
        partial void DrawMinigameBoard(RectTransform parent);
        partial void DrawMinigameBackground(RectTransform parent);
        partial void DrawMinigameTools(RectTransform parent);
        partial void RefreshMinigameBoard();
        partial void DescribeMinigameResult(ref string text);
        private void MinigameBanner(string text,Color color)
        {
            var banner=PCard(minigameCanvas,"MinigameBanner",200,272,680,80,color,24,false);PText(banner,"MinigameBannerText",text,0,0,680,80,38,Color.white,true,true);
            Reveal(banner,0,true);StartCoroutine(RemoveMinigameEffect(banner,1.2f));
        }
        private System.Collections.IEnumerator RemoveMinigameEffect(RectTransform effect,float seconds)
        {yield return new WaitForSecondsRealtime(seconds);if(effect!=null)Destroy(effect.gameObject);}
        private void MinigameSound(OpsCue cue,float pitch)
        {
            if(!Application.isPlaying||muted||soundVolume<=0)return;
            PlayCue(cue);if(eventAudio==null||eventAudio.clip==null)return;
            if(minigameAudio==null)minigameAudio=NewAudioSource();minigameAudio.Stop();minigameAudio.clip=eventAudio.clip;
            minigameAudio.pitch=pitch;minigameAudio.volume=soundVolume*.65f;minigameAudio.Play();eventAudio.Stop();
        }
        private System.Collections.IEnumerator CountMinigameResult(RectTransform stamp,int score)
        {
            var session=Minigame;var label=FindMinigameText("MinigameScore");stamp.gameObject.SetActive(false);MinigameCounting=true;
            int value=0,step=Mathf.Max(1,Mathf.RoundToInt(score/25f));label.text="0点";
            if(!ReducedMotion)while(value<score&&Minigame==session)
            {
                value=Mathf.Min(score,value+step);label.text=value+"点";MinigameTone(700+value*6,"triangle");
                yield return new WaitForSecondsRealtime(.035f);
            }
            if(Minigame!=session||stamp==null)yield break;
            label.text=score+"点";MinigameCounting=false;stamp.gameObject.SetActive(true);
            MinigameVisual(stamp,"stamp",.5f);MinigameTone(180,"square");StartCoroutine(MinigameChord(.12f));
        }
    }
}
