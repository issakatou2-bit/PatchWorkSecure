using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private RectTransform logRows,logClues,logSide,logDanger;
        private int logRevision=-1;
        private void DrawLogPresentation()
        {
            logRevision=-1;minigameHeartbeatAt=-1;
            KitGradient(screen.GetComponent<Image>(),Hex("cfe9ff"),Hex("e8e3ff"));KitGradient(minigameCanvas.GetComponent<Image>(),Hex("cfe9ff"),Hex("e8e3ff"));
            DrawDecisionMinigameTop("ログを調べる",182,722,"発見","空振り",PlanBlue);
            var count=FindMinigameText("DecisionGood");count.rectTransform.sizeDelta=new Vector2(172,46);count.enableAutoSizing=false;
            FindMinigameText("DecisionMiss").gameObject.SetActive(false);
            KitGradient(minigameCanvas.Find("MinigameTop/MinigameAlert").GetComponent<Image>(),Hex("7fd0ff"),PlanBlue);
            KitGradient(minigameCanvas.Find("MinigameTop/MinigameTimer/MinigameTimerFill").GetComponent<Image>(),Hex("8f86ff"),PlanBlue,true);
            var terminal=PCard(minigameCanvas,"LogTerminal",0,92,860,650,Hex("0e1628"),24);
            var header=PCard(terminal,"LogHeader",0,0,860,44,Hex("16213a"),24,false);
            string[] colors={"ff6f91","ffc02e","2bb673"};
            for(int i=0;i<3;i++)PCard(header,"LogLamp"+i,16+i*20,16,12,12,Hex(colors[i]),12,false);
            PText(header,"LogTitle","auth.log — ねっとわーく商事 認証サーバー",86,0,740,44,13,Hex("9fb3d6"),false);
            logRows=Rect(terminal,"LogRows",12,54,836,586);logRows.gameObject.AddComponent<RectMask2D>();
            for(int y=45;y<650;y+=3)Box(terminal,"LogScanline",0,y,860,1,new Color(1,1,1,.012f));
            logSide=PCard(minigameCanvas,"MinigameSide",880,92,400,650,Color.white,22);DecisionPanel(logSide);MinigameGloss(logSide,400,650);
            PText(logSide,"LogKnowledgeHeading","状況の把握",18,18,364,26,17);
            for(int i=0;i<4;i++)PCard(logSide,"LogGauge"+i,18+i*92.5f,54,86.5f,12,PlanTrack,12,false);
            PText(logSide,"LogCluesHeading","見つけた手がかり",18,80,364,26,17);
            logClues=DecisionScroll(logSide,"LogClues",18,114,364,290);
            var speech=PCard(logSide,"MinigameSpeech",18,118,240,64,Color.white,16,false);
            var outline=speech.gameObject.AddComponent<Outline>();outline.effectColor=Hex("ffd3de");outline.effectDistance=new Vector2(2,-2);
            PText(speech,"NavigatorSpeech",CaptionsEnabled?"普段と違うところを探そう。":"",12,8,216,48,15,null,false);
            Portrait(logSide,"NavigatorPortrait",188,414,206,230,"pose_magnifier");
            logDanger=MinigameShape(minigameCanvas,"MinigameDanger","danger-edge",0,0,1280,760,new Color(.88f,.25f,.37f,.45f));MinigameVisual(logDanger,"danger",.8f);logDanger.gameObject.SetActive(false);
            RefreshLogPresentation();
        }
        private void RefreshLogPresentation()
        {
            var game=Minigame as OpsLogMinigame;if(game==null)return;
            FindMinigameText("DecisionGood").text="発見 <color=#2bb673>"+game.Found+"</color> / "+game.Total;
            FindMinigameText("DecisionFalse").text="空振り <color=#ff8a3d>"+game.Wrong+"</color>";
            if(logRevision!=game.Revision)
            {
                logRevision=game.Revision;
                foreach(Transform child in logRows){child.gameObject.SetActive(false);Destroy(child.gameObject);}
                for(int i=0;i<game.Visible.Count;i++)
                {
                    var data=game.Visible[i];int id=data.Id;
                    var button=PButton(logRows,"LogRow_"+id,"",0,i*37,836,35,()=>HitLog(id),data.Hit&&data.Suspicious?new Color(.88f,.25f,.37f,.28f):Color.clear,Color.white,12,null,!data.Hit&&game.Phase==OpsMinigamePhase.Playing);
                    foreach(var shadow in button.GetComponents<Shadow>())shadow.enabled=false;
                    if(data.Hit&&data.Suspicious){var outline=button.gameObject.AddComponent<Outline>();outline.effectColor=Hex("ff7a93");outline.effectDistance=new Vector2(2,-2);}
                    foreach(var text in button.GetComponentsInChildren<TMPro.TextMeshProUGUI>())text.gameObject.SetActive(false);
                    var row=(RectTransform)button.transform;
                    // 手本の等幅15px相当。自動縮小で読みづらくしない。
                    LogText(row,"LogTime",data.Time,10,70,Hex("7f93b8"));
                    LogText(row,"LogUser",data.User,94,92,Hex("ffd48a"));
                    LogText(row,"LogIp",data.Ip,200,190,Hex("9fd9ff"));
                    LogText(row,"LogEvent",data.Event,404,402,Hex("cfe0ff"));
                    if(data.Suspicious&&game.Monitor)PCard(row,"LogHint",816,13,8,8,Hex("ffc02e"),12,false);
                }
                foreach(Transform child in logClues){child.gameObject.SetActive(false);Destroy(child.gameObject);}
                for(int i=0;i<game.Clues.Count;i++)
                {
                    var clue=PCard(logClues,"LogClue"+i,0,i*54,364,48,Hex("fff4c4"),12,false);
                    clue.gameObject.AddComponent<LayoutElement>().preferredHeight=48;
                    PText(clue,"LogClueText",game.Clues[i],10,4,344,40,13,null,false);
                }
                ((RectTransform)logSide.Find("MinigameSpeech")).anchoredPosition=new Vector2(18,-(118+Mathf.Min(290,game.Clues.Count*54)));
                for(int i=0;i<4;i++)logSide.Find("LogGauge"+i).GetComponent<Image>().color=i<game.Knowledge?PlanBlue:PlanTrack;
            }
            RefreshDecisionDanger(logDanger);
        }
        public void HitLog(int id)
        {
            var game=Minigame as OpsLogMinigame;if(game==null)return;
            var row=game.Visible.FirstOrDefault(r=>r.Id==id);if(row==null||!game.Hit(id))return;
            if(row.Suspicious){MinigameTone(600*Mathf.Pow(1.1f,game.Found));MinigamePop(new Vector2(540,180),"発見！",Hex("e0405f"));if(game.Found==3)SpeakSceneLine("mg_combo_01",0);}
            else{MinigameTone(220,"square");MinigameVisual(logRows,"shake",.35f);SpeakSceneLine("mg_miss_01",0);}
            RefreshLogPresentation();
        }
        private void LogText(Transform row,string name,string value,float x,float width,Color color)
        {
            var label=PText(row,name,value,x,0,width,35,15,color,false);label.enableAutoSizing=false;
            label.fontStyle=TMPro.FontStyles.Normal;label.textWrappingMode=TMPro.TextWrappingModes.NoWrap;
            // 日本語は既存のCJK書体、ASCIIは等幅でログの列を揃える。
            if(name!="LogEvent")label.text="<mspace=8.8>"+value+"</mspace>";
        }
    }
}
