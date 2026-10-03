using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private RectTransform mailStack,mailSide,mailCard,mailDanger;
        private UnityEngine.UI.Button mailSafe,mailReport;
        private int renderedMailNumber=-1,renderedClues=-1;
        private void DrawMailPresentation()
        {
            renderedMailNumber=-1;renderedClues=-1;minigameHeartbeatAt=-1;
            DrawDecisionMinigameTop("メールの仕分け",198,696,"見逃し","止めすぎ",PlanPink);
            mailStack=Rect(minigameCanvas,"MailStack",60,100,760,520);
            mailSafe=PButton(minigameCanvas,"MailSafe","問題なし\n<size=13>← キー／開いて仕事を進める</size>",60,640,371,88,()=>AnswerMail(false),Hex("2bb673"),Color.white,22,Hex("1d8a55"));
            mailReport=PButton(minigameCanvas,"MailReport","怪しい・報告\n<size=13>→ キー／開かずに情シスへ報告</size>",449,640,371,88,()=>AnswerMail(true),Hex("e0405f"),Color.white,22,Hex("a82643"));
            mailSide=PCard(minigameCanvas,"MinigameSide",850,100,430,600,Color.white,22);DecisionPanel(mailSide);MinigameGloss(mailSide,430,600);
            PText(mailSide,"MailCluesHeading","見つけた手がかり",18,18,394,30,17);
            DecisionScroll(mailSide,"MailClues",18,54,394,250);
            var speech=PCard(mailSide,"MinigameSpeech",18,94,250,64,Color.white,16,false);
            var outline=speech.gameObject.AddComponent<UnityEngine.UI.Outline>();outline.effectColor=Hex("ffd3de");outline.effectDistance=new Vector2(2,-2);
            PText(speech,"NavigatorSpeech",CaptionsEnabled?"…":"",12,8,226,48,15,null,false);
            Portrait(mailSide,"NavigatorPortrait",204,326,220,270,"pose_magnifier");
            mailDanger=MinigameShape(minigameCanvas,"MinigameDanger","danger-edge",0,0,1280,760,new Color(.88f,.25f,.37f,.45f));MinigameVisual(mailDanger,"danger",.8f);mailDanger.gameObject.SetActive(false);
            RefreshMailPresentation();
        }
        private void DrawDecisionMinigameTop(string title,float badgeWidth,float timerWidth,string miss,string falseAlarm,Color color)
        {
            var top=PCard(minigameCanvas,"MinigameTop",0,0,1280,72,Color.white,22);DecisionPanel(top);MinigameGloss(top,1280,72);
            var badge=PCard(top,"MinigameAlert",20,12,badgeWidth,46,color,16,false);KitGradient(badge.GetComponent<UnityEngine.UI.Image>(),title.StartsWith("メール")?Hex("ff9ab3"):Hex("8f86ff"),color);
            PText(badge,"MinigameAlertText",title,0,0,badgeWidth,46,24,Color.white,true,true);
            var depth=badge.gameObject.AddComponent<UnityEngine.UI.Shadow>();depth.effectColor=title.StartsWith("メール")?Hex("d94a70"):Hex("4a42c9");depth.effectDistance=new Vector2(0,-5);
            float timerX=20+badgeWidth+16;
            var timer=PCard(top,"MinigameTimer",timerX,28,timerWidth,16,Hex("e3e8f1"),12,false);
            var fill=PCard(timer,"MinigameTimerFill",0,0,timerWidth,16,PlanPink,12,false);KitGradient(fill.GetComponent<UnityEngine.UI.Image>(),PlanPink,title.StartsWith("メール")?PlanBlue:Hex("8f86ff"),true);
            float x=timerX+timerWidth+16;
            PText(top,"MinigameTime",Minigame.Duration.ToString("0"),x,12,42,46,20,PlanInk,true,true);
            PText(top,"DecisionGood","正解 <color=#2bb673>0</color>",x+42,12,78,46,20);
            PText(top,"DecisionMiss",miss+" <color=#e0405f>0</color>",x+128,12,96,46,20);
            PText(top,"DecisionFalse",falseAlarm+" <color=#ff8a3d>0</color>",x+232,12,102,46,20);
        }
        private void DecisionPanel(RectTransform panel)
        {
            foreach(var edge in panel.GetComponents<UnityEngine.UI.Outline>())DestroyImmediate(edge);
            KitGradient(panel.GetComponent<UnityEngine.UI.Image>(),Color.white,Color.white);
            var outline=panel.gameObject.AddComponent<UnityEngine.UI.Outline>();outline.effectColor=Color.white;outline.effectDistance=new Vector2(3,-3);
        }
        private void RemoveDecisionCard(ref RectTransform card)
        {
            if(card==null)return;
            // PCardの影は兄弟。カードだけ消すと影が通数分重なって黒くなる。
            var shadow=card.parent.Find(card.name+"Shadow");if(shadow!=null){shadow.gameObject.SetActive(false);Destroy(shadow.gameObject);}
            card.gameObject.SetActive(false);Destroy(card.gameObject);card=null;
        }
        // 手本の手がかり／ログ欄。すべての記録をスクロールで読める。
        private RectTransform DecisionScroll(Transform parent,string name,float x,float y,float w,float h)
        {
            var root=Rect(parent,name,x,y,w,h);var scroll=root.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();scroll.horizontal=false;
            var viewport=Rect(root,"Viewport",0,0,w,h);var image=viewport.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=Color.white;
            var mask=viewport.gameObject.AddComponent<UnityEngine.UI.Mask>();mask.showMaskGraphic=false;
            var content=Rect(viewport,"Content",0,0,w,h);
            var layout=content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();layout.spacing=6;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;
            var fit=content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();fit.verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport=viewport;scroll.content=content;scroll.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped;return content;
        }
        public void OpenMailTraining()
        {
            var session=State?.CreateMailPractice();if(session==null||MinigameActive)return;
            if(modal!=null)CloseDialog();
            OpenMinigame(session,"",s=>
            {
                var before=ReadStats();State.CompleteMailTraining((OpsMailMinigame)s);RecordStatChanges(before);Save();Render();
                if(!s.Delegated)Toast("メール研修 完了 / 小川の経験 +"+OpsCatalog.MailPracticeXp,true,OpsCue.Growth);
            });
        }
        private void RefreshMailPresentation()
        {
            var game=Minigame as OpsMailMinigame;if(game==null)return;
            var q=game.Phase==OpsMinigamePhase.Brief?null:game.Current;
            if(q!=null&&renderedMailNumber!=game.Number)
            {
                RemoveDecisionCard(ref mailCard);
                renderedMailNumber=game.Number;
                mailCard=PCard(mailStack,"MailCard",0,0,760,520,Color.white,22);DecisionPanel(mailCard);MinigameGloss(mailCard,760,520);mailCard.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
                var swipe=mailCard.gameObject.AddComponent<OpsMailPointer>();swipe.Owner=this;swipe.Swipe=true;
                PText(mailCard,"MailSenderLabel","差出人",30,26,70,28,15,PlanGray);
                string addr=game.Education&&q.MarkAddress?"<b><link=\"mail-hint\">"+q.Address+"</link></b>":q.Address;
                OpsPhraseMarker.Attach(PText(mailCard,"MailSender",q.Sender+"  <"+addr+">",110,26,620,28,15,null,false));
                PText(mailCard,"MailToLabel","宛先",30,56,70,28,15,PlanGray);
                PText(mailCard,"MailTo","あなた <you@nw-shoji.co.jp>",110,56,620,28,15,null,false);
                PText(mailCard,"MailSubject",q.Subject,30,94,700,48,26);
                var body=PCard(mailCard,"MailBody",30,142,700,170,Hex("f6f8fc"),14,false);
                foreach(var edge in body.GetComponents<UnityEngine.UI.Outline>())DestroyImmediate(edge);KitGradient(body.GetComponent<UnityEngine.UI.Image>(),Hex("f6f8fc"),Hex("f6f8fc"));
                string text=game.Education?q.Body.Replace("<mark=#ffe06666>","<b><link=\"mail-hint\">").Replace("</mark>","</link></b>"):q.Body.Replace("<mark=#ffe06666>","").Replace("</mark>","");
                var bodyText=PText(body,"MailBodyText",text,18,14,664,100,17,PlanInk,false);bodyText.alignment=TextAlignmentOptions.TopLeft;bodyText.lineSpacing=14;
                OpsPhraseMarker.Attach(bodyText);
                if(q.Link!="")
                {
                    var link=PButton(body,"MailLink","<u>"+q.LinkLabel+"</u>",18,112,664,32,()=>{},Color.clear,Hex("1a6fd6"),17);
                    foreach(var shadow in link.GetComponents<UnityEngine.UI.Shadow>())shadow.enabled=false;
                    var label=link.GetComponentInChildren<TextMeshProUGUI>();label.alignment=TextAlignmentOptions.Left;label.margin=Vector4.zero;
                    var pointer=link.gameObject.AddComponent<OpsMailPointer>();pointer.Owner=this;pointer.Url=q.Link;
                }
                if(q.Attachment!="")OpsPhraseMarker.Attach(PText(body,"MailAttachment","添付  "+(game.Education&&q.Suspicious?"<b><link=\"mail-hint\">"+q.Attachment+"</link></b>":q.Attachment),18,124,664,30,14,PlanInk));
                PText(mailCard,"MailStatus",game.Number+" / "+OpsCatalog.MailCount+" 通目",30,484,700,22,13,PlanGray);
                MinigameVisual(mailCard,"enter",.4f);
            }
            FindMinigameText("DecisionGood").text="正解 <color=#2bb673>"+game.Correct+"</color>";
            FindMinigameText("DecisionMiss").text="見逃し <color=#e0405f>"+game.Misses+"</color>";
            FindMinigameText("DecisionFalse").text="止めすぎ <color=#ff8a3d>"+game.FalseAlarms+"</color>";
            mailSafe.interactable=mailReport.interactable=game.CanAnswer;
            if(renderedClues!=game.Learned.Count)
            {
                renderedClues=game.Learned.Count;var root=mailSide.Find("MailClues/Viewport/Content");
                foreach(Transform child in root){child.gameObject.SetActive(false);Destroy(child.gameObject);}
                if(game.Learned.Count==0)PText(root,"MailNoClue","まだない",10,0,374,30,13,Hex("9aa6bd"));
                else for(int i=0;i<game.Learned.Count;i++)
                {
                    var row=PCard(root,"MailClue_"+i,0,i*58,394,52,Hex("fff4c4"),10,false);
                    row.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight=52;
                    PText(row,"MailClueText",game.Learned[game.Learned.Count-1-i],10,4,374,44,13,PlanInk,false);
                }
                var speech=mailSide.Find("MinigameSpeech") as RectTransform;speech.anchoredPosition=new Vector2(18,-(64+(game.Learned.Count==0?30:Mathf.Min(250,game.Learned.Count*58))));
            }
            if(game.Feedback!=""&&!minigameCanvas.GetComponentsInChildren<TextMeshProUGUI>().Any(t=>t.name=="MailFeedback"))
            {
                var tip=PCard(minigameCanvas,"MailFeedbackPanel",60,565,760,65,PlanInk,14,false);PText(tip,"MailFeedback",game.Feedback,16,8,728,49,15,Color.white,false);StartCoroutine(RemoveMinigameEffect(tip,OpsCatalog.MailFeedbackSeconds));
            }
            RefreshDecisionDanger(mailDanger);
        }
        private void RefreshDecisionDanger(RectTransform danger)
        {
            bool low=Minigame.Phase==OpsMinigamePhase.Playing&&Minigame.Remaining<OpsCatalog.MinigameDangerSeconds;danger.gameObject.SetActive(low);
            if(low&&Minigame.Elapsed-minigameHeartbeatAt>(Minigame.Remaining<OpsCatalog.MinigameUrgentSeconds?.45f:.75f))
            {minigameHeartbeatAt=Minigame.Elapsed;MinigameTone(90);StartCoroutine(DelayedMinigameTone(70,.09f));}
        }
        public void ShowMailLink(string url)
        {
            if(!(Minigame is OpsMailMinigame game)||game.Phase!=OpsMinigamePhase.Playing||mailCard==null)return;
            FindMinigameText("MailStatus").text=url==""?game.Number+" / "+OpsCatalog.MailCount+" 通目":"リンク先："+url;
        }
        public void AnswerMail(bool report)
        {
            var game=Minigame as OpsMailMinigame;if(game==null)return;
            var result=game.Answer(report);if(result==OpsMailAnswer.None)return;
            StopVoice();MinigameVisual(mailCard,"mail-out",.35f,new Vector2(report?1:-1,0));
            if(result==OpsMailAnswer.Correct)
            {
                MinigameTone(560*Mathf.Pow(1.1f,Mathf.Min(game.Streak,8)));MinigamePop(new Vector2(440,430),(report?"見破った！":"OK！")+(game.Streak>=2?" ×"+game.Streak:""),report?Hex("e0405f"):PlanMint);
                // 次のメールが入る間も粒の上端が題名へ届かない、本文下側の空間で光らせる。
                if(report)MinigameCutBurst(new Vector2(440,430));
                PresentMinigameSuccess();
            }
            else
            {
                MinigameTone(result==OpsMailAnswer.Miss?170:260,result==OpsMailAnswer.Miss?"saw":"square");MinigameVisual(minigameCanvas,"shake",.35f);
                ResetMinigameSuccess();SpeakSceneLine(result==OpsMailAnswer.Miss?"mg_mail_miss":"mg_miss_01",0);
            }
            RefreshMailPresentation();
        }
        private void TickDecisionKeys()
        {
            if(!(Minigame is OpsMailMinigame)&&!(Minigame is OpsMfaMinigame)||Minigame.Phase!=OpsMinigamePhase.Playing)return;
            var key=Keyboard.current;if(key==null)return;
            if(key.leftArrowKey.wasPressedThisFrame){if(Minigame is OpsMailMinigame)AnswerMail(false);else AnswerMfa(false);}
            else if(key.rightArrowKey.wasPressedThisFrame){if(Minigame is OpsMailMinigame)AnswerMail(true);else AnswerMfa(true);}
        }
    }
    // スワイプはカードだけ。リンクはタッチ長押しで照会し、実際のURLは開かない。
    public sealed class OpsMailPointer:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler,IBeginDragHandler,IDragHandler,IEndDragHandler,ISelectHandler,IDeselectHandler
    {
        public OpsGame Owner;public string Url="";public bool Swipe;
        private Vector2 down;private float pressedAt;private bool pressed;
        public void OnPointerEnter(PointerEventData e){if(!Swipe&&e.pointerId<0)Owner.ShowMailLink(Url);}
        public void OnPointerExit(PointerEventData e){if(!Swipe)Owner.ShowMailLink("");pressed=false;}
        public void OnSelect(BaseEventData e){if(!Swipe)Owner.ShowMailLink(Url);}
        public void OnDeselect(BaseEventData e){if(!Swipe)Owner.ShowMailLink("");}
        public void OnPointerDown(PointerEventData e){down=e.position;pressedAt=Time.unscaledTime;pressed=true;}
        public void OnPointerUp(PointerEventData e){pressed=false;}
        public void OnBeginDrag(PointerEventData e){down=e.pressPosition;}
        public void OnDrag(PointerEventData e){}
        public void OnEndDrag(PointerEventData e){if(Swipe&&Mathf.Abs(e.position.x-down.x)>70)Owner.AnswerMail(e.position.x>down.x);}
        private void Update(){if(pressed&&!Swipe&&Time.unscaledTime-pressedAt>.45f){Owner.ShowMailLink(Url);pressed=false;}}
    }
}
