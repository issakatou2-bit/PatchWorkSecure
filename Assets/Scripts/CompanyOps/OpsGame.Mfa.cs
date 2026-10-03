using TMPro;
using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private RectTransform mfaStaff,mfaPhone,mfaRequest,mfaSide,mfaLog,mfaDanger;
        private UnityEngine.UI.Button mfaDeny,mfaAllow;
        private int renderedMfaCount=-1,renderedMfaLogs=-1;
        private static readonly string[] MfaColors={"3fa9f5","2bb673","ff8a3d","6c63ff"};
        private void DrawMfaPresentation()
        {
            renderedMfaCount=-1;renderedMfaLogs=-1;minigameHeartbeatAt=-1;
            DrawDecisionMinigameTop("多要素認証の関所",224,670,"侵入","足止め",Hex("6c63ff"));
            mfaStaff=PCard(minigameCanvas,"MfaStaff",0,92,400,600,Color.white,22);DecisionPanel(mfaStaff);MinigameGloss(mfaStaff,400,600);
            PText(mfaStaff,"MfaStaffHeading","社員の今の様子",18,18,364,30,17);
            PText(mfaStaff,"MfaHint","本人がちょうどログインしようとしているなら許可。そうでなければ誰かがパスワードを使っている。",18,360,364,64,12,PlanGray,false);
            var phone=PCard(minigameCanvas,"MfaPhone",440,92,400,640,Hex("0f172a"),28);
            phone.GetComponent<UnityEngine.UI.Image>().pixelsPerUnitMultiplier=28f/44f;
            mfaPhone=PCard(phone,"MfaScreen",18,18,364,604,Color.white,28,false);
            foreach(var edge in mfaPhone.GetComponents<UnityEngine.UI.Outline>())DestroyImmediate(edge);
            KitGradient(mfaPhone.GetComponent<UnityEngine.UI.Image>(),Hex("eef3ff"),Color.white);
            PCard(mfaPhone,"MfaNotch",127,10,110,22,Hex("0f172a"),12,false);
            PText(mfaPhone,"MfaSpam","",18,370,328,60,14,PlanGray,false,true);
            mfaDeny=PButton(mfaPhone,"MfaDeny","拒否",18,508,157,70,()=>AnswerMfa(false),Hex("e0405f"),Color.white,22,Hex("a82643"));
            mfaAllow=PButton(mfaPhone,"MfaAllow","許可",189,508,157,70,()=>AnswerMfa(true),Hex("2bb673"),Color.white,22,Hex("1d8a55"));
            mfaSide=PCard(minigameCanvas,"MinigameSide",870,92,410,600,Color.white,22);DecisionPanel(mfaSide);MinigameGloss(mfaSide,410,600);
            var speech=PCard(mfaSide,"MinigameSpeech",18,18,374,64,Color.white,16,false);
            var outline=speech.gameObject.AddComponent<UnityEngine.UI.Outline>();outline.effectColor=Hex("ffd3de");outline.effectDistance=new Vector2(3,-3);
            PText(speech,"NavigatorSpeech",CaptionsEnabled?"…":"",12,8,350,48,15,PlanInk,false);
            mfaLog=DecisionScroll(mfaSide,"MfaLog",18,100,374,230);
            Portrait(mfaSide,"NavigatorPortrait",184,326,220,270,"pose_laptop");
            mfaDanger=MinigameShape(minigameCanvas,"MinigameDanger","danger-edge",0,0,1280,760,new Color(.88f,.25f,.37f,.45f));MinigameVisual(mfaDanger,"danger",.8f);mfaDanger.gameObject.SetActive(false);
            RefreshMfaPresentation();
        }
        private void RefreshMfaPresentation()
        {
            var game=Minigame as OpsMfaMinigame;if(game==null)return;var q=game.Current;
            if(q!=null&&renderedMfaCount!=game.Count)
            {
                renderedMfaCount=game.Count;
                RemoveDecisionCard(ref mfaRequest);
                mfaRequest=PCard(mfaPhone,"MfaRequest",18,60,328,game.NumberMatch?246:210,Color.white,22);DecisionPanel(mfaRequest);MinigameGloss(mfaRequest,328,210);
                PText(mfaRequest,"MfaRequestTitle","サインインを承認しますか？",18,18,292,32,20);
                string[] names={"アカウント","場所","アプリ","時刻"},values={game.People[q.Who].Name+"さん",q.Place,q.App,"いま"};
                for(int i=0;i<4;i++)
                {
                    PText(mfaRequest,"MfaRequestLabel"+i,names[i],18,58+i*34,92,32,15,PlanGray,false);
                    var value=PText(mfaRequest,"MfaRequestValue"+i,values[i],112,58+i*34,198,32,15,PlanInk,false);value.alignment=TextAlignmentOptions.MidlineRight;
                    // 手本の細い破線。枠用の太い破線Graphicを使わない。
                    for(int dash=0;dash<49;dash++)PCard(mfaRequest,"MfaSeparator"+i+"_"+dash,18+dash*6,90+i*34,3,1,Hex("e3e8f1"),12,false);
                }
                if(game.NumberMatch)PText(mfaRequest,"MfaNumber",q.Legitimate?"画面の番号 <b>"+q.Number+"</b> と一致":"入力された番号がない",18,208,292,28,16,q.Legitimate?PlanInk:Hex("e0405f"),false,true);
                FindMinigameText("MfaSpam").text=q.Spam?"同じアカウントへの承認依頼が続いている…":"";
                MinigameVisual(mfaRequest,"enter",.35f);
                for(int i=0;i<4;i++)
                {
                    var old=mfaStaff.Find("MfaPerson"+i);if(old!=null){old.gameObject.SetActive(false);Destroy(old.gameObject);}
                    var person=PCard(mfaStaff,"MfaPerson"+i,18,56+i*74,364,64,Hex(i==q.Who?"e4f6ff":"f3f6fb"),16,false);
                    var face=PCard(person,"MfaFace",12,10,44,44,Hex(MfaColors[i]),28,false);PText(face,"MfaInitial",game.People[i].Name.Substring(0,1),0,0,44,44,18,Color.white,true,true);
                    PText(person,"MfaName",game.People[i].Name+"さん",66,8,286,24,16);
                    PText(person,"MfaActivity",game.People[i].Activity,66,32,286,24,13,PlanGray,false);
                }
            }
            FindMinigameText("DecisionGood").text="正解 <color=#2bb673>"+game.Correct+"</color>";
            FindMinigameText("MfaHint").rectTransform.anchoredPosition=new Vector2(18,-(game.Phase==OpsMinigamePhase.Brief?56:340));
            FindMinigameText("DecisionMiss").text="侵入 <color=#e0405f>"+game.Breaches+"</color>";
            FindMinigameText("DecisionFalse").text="足止め <color=#ff8a3d>"+game.Blocks+"</color>";
            mfaDeny.interactable=mfaAllow.interactable=game.CanAnswer;
            if(renderedMfaLogs!=game.Logs.Count)
            {
                renderedMfaLogs=game.Logs.Count;
                foreach(Transform child in mfaLog){child.gameObject.SetActive(false);Destroy(child.gameObject);}
                for(int i=game.Logs.Count-1;i>=0;i--)
                {
                    var log=game.Logs[i];float height=log.Text.Length>28?52:32;
                    var row=PCard(mfaLog,"MfaLogRow"+i,0,0,374,height,Hex(log.Answer==OpsMfaAnswer.Correct?"e3f7ea":log.Answer==OpsMfaAnswer.Breach?"ffe1e6":"fff0dd"),12,false);
                    row.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight=height;
                    PText(row,"MfaLogText",log.Text,8,4,358,height-8,13,PlanInk,false);
                }
            }
            RefreshDecisionDanger(mfaDanger);
        }
        public void AnswerMfa(bool allow)
        {
            var game=Minigame as OpsMfaMinigame;if(game==null)return;
            var result=game.Answer(allow);if(result==OpsMfaAnswer.None)return;
            StopVoice();
            if(result==OpsMfaAnswer.Correct)
            {
                MinigameTone((allow?620:760)*Mathf.Pow(1.05946f,Mathf.Min(game.Streak-1,8)));
                MinigamePop(new Vector2(640,300),(allow?"通した！":"弾いた！")+(game.Streak>=2?" ×"+game.Streak:""),Hex(allow?"2bb673":"6c63ff"));
                if(!allow){MinigameCutBurst(new Vector2(640,300));MinigameVisual(mfaRequest,"hit",OpsCatalog.ContainmentHitStop);}
                PresentMinigameSuccess();
            }
            else
            {
                MinigameTone(result==OpsMfaAnswer.Breach?160:260,result==OpsMfaAnswer.Breach?"saw":"square");MinigameVisual(minigameCanvas,"shake",.35f);
                ResetMinigameSuccess();SpeakSceneLine(result==OpsMfaAnswer.Breach?"mg_mfa_breach":"mg_miss_02",0);
            }
            RefreshMfaPresentation();
        }
    }
}
