using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private RectTransform restoreMap,restoreSide,restoreLogs;
        private readonly Dictionary<string,Button> restoreButtons=new Dictionary<string,Button>();
        private readonly Dictionary<string,OpsMinigameGraphic> restoreWires=new Dictionary<string,OpsMinigameGraphic>();
        private readonly HashSet<string> restoreCompleted=new HashSet<string>();private int restoreRevision=-1;
        private static Vector2 RestorePosition(string id)
        {
            switch(id){case "check":return new Vector2(150,90);case "net":return new Vector2(450,90);case "dns":return new Vector2(230,230);case "auth":return new Vector2(670,230);case "fs":return new Vector2(120,400);case "db":return new Vector2(450,380);case "mail":return new Vector2(780,400);case "order":return new Vector2(330,560);default:return new Vector2(620,560);}
        }
        private void DrawRestorePresentation()
        {
            var game=(OpsRestoreMinigame)Minigame;restoreButtons.Clear();restoreWires.Clear();restoreCompleted.Clear();restoreRevision=-1;
            KitGradient(screen.GetComponent<Image>(),Hex("1d2a44"),Hex("24394f"));KitGradient(minigameCanvas.GetComponent<Image>(),Hex("1d2a44"),Hex("24394f"));
            var top=PCard(minigameCanvas,"MinigameTop",0,0,1280,72,Color.white,22);DecisionPanel(top);
            var badge=PCard(top,"MinigameAlert",20,12,176,46,Hex("2bb673"),16,false);KitGradient(badge.GetComponent<Image>(),Hex("5fd39b"),Hex("2bb673"));
            PText(badge,"MinigameAlertText","復旧の順番",0,0,176,46,24,Color.white,true,true);
            var clock=PCard(top,"RestoreClock",212,13,150,44,PlanInk,12,false);PText(clock,"MinigameTime","停止 0:00",0,0,150,44,26,Color.white,true,true);
            var scene=PCard(top,"RestoreScenario",378,18,126,36,Hex("fff4d6"),12,false);PText(scene,"RestoreScenarioText",game.ScenarioTitle,0,0,126,36,18,Hex("8a5a00"),true,true);
            PText(top,"RestoreDescription",game.Scenario=="change"?"更新後、ログインが必要な仕組みが止まった。":game.Scenario=="storage"?"保存領域が故障。データを使う仕組みを戻す。":game.Scenario=="ransom"?"戻す前に、復元元と侵入経路の安全確認。":"停電後、社内のシステムが止まった。",520,12,438,46,14,PlanGray,false);
            PText(top,"RestoreLoss","業務の損失 <color=#e0405f>0</color> 万円",974,12,286,46,20);
            restoreMap=PCard(minigameCanvas,"RestoreMap",0,92,900,654,Hex("0f172a"),22);restoreMap.gameObject.AddComponent<RectMask2D>();
            var edge=restoreMap.gameObject.AddComponent<Outline>();edge.effectColor=Color.white;edge.effectDistance=new Vector2(3,-3);
            foreach(var node in game.Nodes)foreach(var parent in game.Dependencies(node))
            {
                string id=parent+"-"+node.Id;var wire=MinigameShape(restoreMap,"RestoreWire_"+id,"dashed-line",0,0,900,654,new Color(.62f,.70f,.84f,.35f)).GetComponent<OpsMinigameGraphic>();
                var a=RestorePosition(parent);var b=RestorePosition(node.Id);wire.From=new Vector2(a.x,-a.y);wire.To=new Vector2(b.x,-b.y);restoreWires[id]=wire;
            }
            foreach(var node in game.Nodes)
            {
                string id=node.Id;var p=RestorePosition(id);
                var button=PButton(restoreMap,"RestoreNode_"+id,"",p.x-98,p.y-58,196,116,()=>BootRestore(id),Hex("2b3a55"),Hex("cfe0ff"),20,Hex("0f172a"));
                var palette=button.colors;palette.normalColor=palette.disabledColor=Color.white;button.colors=palette;
                // 手本のノードは平坦な青。共通ボタンの強い白帯はここには載せない。
                foreach(var light in button.GetComponentsInChildren<Image>().Where(i=>i.name=="KitTopLight"))light.gameObject.SetActive(false);
                foreach(var t in button.GetComponentsInChildren<TMPro.TextMeshProUGUI>())t.gameObject.SetActive(false);restoreButtons[id]=button;
                if(node.Cost>0){var border=button.gameObject.AddComponent<Outline>();border.effectColor=Hex("ffc02e");border.effectDistance=new Vector2(3,-3);}
                PText(button.transform,"RestoreName_"+id,node.Name,12,8,154,26,16,Hex("cfe0ff"));
                PText(button.transform,"RestoreSub_"+id,node.Sub,12,36,172,18,12,Hex("cfe0ff"),false);
                PText(button.transform,"RestoreWork_"+id,"作業 "+node.Work+"時間",12,58,172,18,12,Hex("cfe0ff"),false);
                PCard(button.transform,"RestoreLamp_"+id,172,10,14,14,Hex("e0405f"),12,false);
                if(node.Cost>0)PText(button.transform,"RestoreMoney_"+id,"1時間 "+node.Cost+"万円",12,82,80,20,12,Hex("ffe38a"));
                if(node.Rto>=0)
                {
                    var rto=PCard(button.transform,"RestoreRto_"+id,94,82,90,20,new Color(1,1,1,.14f),12,false);
                    PText(rto,"RestoreRtoLabel_"+id,"目標 "+node.Rto.ToString("0.#")+"時間",0,0,90,20,11,Color.white,true,true);
                }
                if(node.Encrypted)
                {
                    var tag=PCard(button.transform,"RestoreEncrypted",12,-12,56,20,Hex("e0405f"),6,false);
                    PText(tag,"RestoreEncryptedText","暗号化",0,0,56,20,11,Color.white,true,true);
                }
                var progress=PCard(button.transform,"RestoreProgress_"+id,10,104,176,6,new Color(1,1,1,.2f),12,false);
                PCard(progress,"Fill",0,0,176,6,Hex("7fd0ff"),12,false);progress.gameObject.SetActive(false);
                if(node.Up)restoreCompleted.Add(id);
            }
            restoreSide=PCard(minigameCanvas,"MinigameSide",920,92,360,654,Color.white,22);DecisionPanel(restoreSide);
            PText(restoreSide,"RestoreCrewHeading","作業できる人",18,18,324,28,17);
            for(int w=0;w<2;w++){var crew=PCard(restoreSide,"RestoreCrew_"+w,18+w*166,52,158,58,Hex("e3f7ea"),12,false);PText(crew,"RestoreCrewText_"+w,"",0,0,158,58,13,PlanInk,true,true);}
            PText(restoreSide,"RestoreLogHeading","記録",18,126,324,26,17);restoreLogs=DecisionScroll(restoreSide,"RestoreLogs",18,162,324,220);
            var speech=PCard(restoreSide,"MinigameSpeech",18,174,240,64,Color.white,16,false);
            var outline=speech.gameObject.AddComponent<Outline>();outline.effectColor=Hex("ffd3de");outline.effectDistance=new Vector2(2,-2);
            PText(speech,"NavigatorSpeech",CaptionsEnabled?"支えている仕組みから戻そう。":"",12,8,216,48,15,null,false);
            Portrait(restoreSide,"NavigatorPortrait",154,418,200,230,game.Scenario=="ransom"?"pose_magnifier":"pose_typing");RefreshRestorePresentation();
        }
        private static string RestoreClock(double hour)=>((int)hour)+":"+((int)System.Math.Round((hour%1)*60)).ToString("00");
        private void RefreshRestorePresentation()
        {
            var game=Minigame as OpsRestoreMinigame;if(game==null)return;
            FindMinigameText("MinigameTime").text="停止 "+RestoreClock(game.Hour);FindMinigameText("RestoreLoss").text="業務の損失 <color=#e0405f>"+System.Math.Round(game.Loss)+"</color> 万円";
            if(restoreRevision==game.Revision)return;restoreRevision=game.Revision;
            foreach(var node in game.Nodes)
            {
                var button=restoreButtons[node.Id];Color down=node.Encrypted?Hex("4a2438"):Hex("2b3a55");KitGradient(button.GetComponent<Image>(),node.Up?Hex("5fd39b"):node.Busy?Hex("34507a"):down,node.Up?Hex("2bb673"):node.Busy?Hex("34507a"):down);
                var encrypted=button.transform.Find("RestoreEncrypted");if(encrypted!=null)encrypted.gameObject.SetActive(!node.Up);
                button.interactable=game.Phase==OpsMinigamePhase.Playing&&!node.Up&&!node.Busy;
                foreach(string label in new[]{"RestoreName_","RestoreSub_","RestoreWork_"})FindMinigameText(label+node.Id).color=node.Up?Color.white:Hex("cfe0ff");
                var lamp=button.transform.Find("RestoreLamp_"+node.Id).GetComponent<Image>();lamp.color=node.Up?Color.white:Hex("e0405f");
                var progress=button.transform.Find("RestoreProgress_"+node.Id);progress.gameObject.SetActive(node.Busy);
                if(node.Busy)((RectTransform)progress.Find("Fill")).sizeDelta=new Vector2(176*(1-(float)node.Left/node.Work),6);
                if(node.Rto>=0)
                {
                    var rto=button.transform.Find("RestoreRto_"+node.Id);bool overdue=!node.Up&&game.Hour>node.Rto;
                    rto.GetComponent<Image>().color=overdue?Hex("e0405f"):new Color(1,1,1,.14f);
                    var visual=rto.GetComponent<OpsMinigameVisual>();if(overdue&&visual==null)MinigameVisual((RectTransform)rto,"danger",.6f);
                    if(!overdue&&visual!=null){visual.Stop();Destroy(visual);}
                }
                if(node.Up&&restoreCompleted.Add(node.Id))
                {
                    var p=RestorePosition(node.Id);MinigameCutBurst(new Vector2(p.x,p.y+92));MinigameVisual((RectTransform)button.transform,"hit",.09f);MinigameTone(660);StartCoroutine(DelayedMinigameTone(990,.08f));MinigamePop(new Vector2(p.x,p.y+92),node.Rto>=0?(node.Finished>node.Rto?"目標を超過…":"目標内に復旧！"):"復旧！",PlanMint);
                    if(node.Rto>=0&&node.Finished<=node.Rto)SpeakSceneLine("mg_combo_0"+(1+minigameComboVoice++%2),0);
                }
            }
            foreach(var wire in restoreWires)
            {
                var pair=wire.Key.Split('-');bool live=game.Nodes.First(n=>n.Id==pair[0]).Up&&game.Nodes.First(n=>n.Id==pair[1]).Up;
                wire.Value.gameObject.SetActive(game.Revealed.Contains(wire.Key));wire.Value.Kind=live||!game.Runbook?"line":"dashed-line";
                wire.Value.color=live?Hex("5fd39b"):game.Runbook?new Color(.62f,.70f,.84f,.35f):Hex("ffc02e");wire.Value.SetVerticesDirty();
            }
            for(int w=0;w<2;w++)
            {
                var task=game.Nodes.FirstOrDefault(n=>n.Worker==w);var crew=restoreSide.Find("RestoreCrew_"+w);
                crew.GetComponent<Image>().color=task!=null?Hex("dff1ff"):w==1&&game.Hour<game.LateJoin?Hex("eef0f4"):Hex("e3f7ea");
                FindMinigameText("RestoreCrewText_"+w).text=(w==0?"ひなた":"大野さん")+"\n"+(task!=null?task.Name:w==1&&game.Hour<game.LateJoin?"移動中":"待機中");
            }
            foreach(Transform child in restoreLogs){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            for(int i=0;i<game.Logs.Count;i++)
            {
                string text=game.Logs[i];var row=PCard(restoreLogs,"RestoreLog"+i,0,i*48,324,44,Hex(text.StartsWith("失敗")||text.StartsWith("再暗号化")?"ffe1e6":text.StartsWith("復旧")?"e3f7ea":"f3f6fb"),12,false);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight=44;PText(row,"RestoreLogText",text,8,4,308,36,13,null,false);
            }
            ((RectTransform)restoreSide.Find("MinigameSpeech")).anchoredPosition=new Vector2(18,-(174+Mathf.Min(220,game.Logs.Count*48)));
        }
        public void BootRestore(string id)
        {
            var game=Minigame as OpsRestoreMinigame;if(game==null)return;
            var status=game.Boot(id);if(status==OpsRestoreBoot.Started)MinigameTone(520);
            else if(status==OpsRestoreBoot.Failed){MinigameTone(180,"saw");MinigameVisual((RectTransform)restoreButtons[id].transform,"shake",.4f);SpeakSceneLine("mg_miss_01",0);}
            RefreshRestorePresentation();
        }
        private void DrawRestoreResults(RectTransform card)
        {
            var game=(OpsRestoreMinigame)Minigame;
            PText(card,"RestoreResultHeading","業務",26,344,190,28,14,PlanGray);
            PText(card,"RestoreResultTimeHeading","戻った時刻",220,344,130,28,14,PlanGray);
            PText(card,"RestoreResultRtoHeading","目標復旧時間",358,344,130,28,14,PlanGray);
            PText(card,"RestoreResultStatusHeading","結果",500,344,130,28,14,PlanGray);
            Box(card,"RestoreTableEdge",26,375,628,2,Hex("e6ebf3"));
            int row=0;foreach(var n in game.Nodes.Where(n=>n.Rto>=0))
            {
                float y=378+row++*32;PText(card,"RestoreResultName_"+n.Id,n.Name,26,y,190,30,14);
                PText(card,"RestoreResultTime_"+n.Id,n.Finished<0?"未復旧":n.Finished.ToString("0.#")+"時間",220,y,130,30,14);
                PText(card,"RestoreResultRto_"+n.Id,n.Rto.ToString("0.#")+"時間",358,y,130,30,14);
                PText(card,"RestoreResultStatus_"+n.Id,n.Finished>=0&&n.Finished<=n.Rto?"目標内":"超過",500,y,130,30,14,n.Finished>=0&&n.Finished<=n.Rto?Hex("2bb673"):Hex("e0405f"));
                Box(card,"RestoreResultEdge_"+n.Id,26,y+30,628,1,Hex("eef1f6"));
            }
            if(row==0)PText(card,"RestoreResultNoBusiness","業務の損失なし。止めすぎには気をつけよう。",26,380,628,46,14,PlanGray,false);
        }
    }
}
