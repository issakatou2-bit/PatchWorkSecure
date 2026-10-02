using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private OpsFactorCandidate[] factorCandidates;
        private int selectedFactor,replaceFactorSlot;
        private Action factorContinuation;
        private void ShowStoryFactors(Action continuation)
        {
            if(Story==null||!Story.finished)return;
            if(Story.rewardClaimed){continuation();return;}
            factorCandidates=Story.FactorCandidates(Career);selectedFactor=Math.Min(1,factorCandidates.Length-1);
            replaceFactorSlot=Career.factors.Count<OpsCatalog.StoryFactorSlots?Career.factors.Count:-1;factorContinuation=continuation;
            FactorRevealScreen();
        }
        private void FactorScreen()
        {
            StopVoice();NewScreen();StoryBackground(false,true);
            StoryCategory(screen,"FactorCategory","CHOOSE A FACTOR",500,34,600,Hex("ffd23f"));
            screen.Find("FactorCategory").GetComponent<TextMeshProUGUI>().alignment=TextAlignmentOptions.Midline;
            StoryText(screen,"EndingTitle","持ち帰る因子を1つ選ぶ",0,54,1600,64,40,Color.white,true);
            StoryText(screen,"FactorHint","次の挑戦は、選んだ設備を最初から Lv1 で始められる",0,116,1600,30,15,new Color(1,1,1,.8f),true);
            Sprite[] art={PlanningArt.focusEngineer,PlanningArt.focusHinata,PlanningArt.focusSecretary};
            string[] speakers={"エンジニアさん","ひなた","かのん"};
            for(int i=0;i<factorCandidates.Length;i++)
            {
                int index=i;var c=factorCandidates[i];bool selected=selectedFactor==i;float x=190+i*430,y=selected?158:170;
                var shadow=PImage(screen,"FactorShadow"+i,PlanningArt.shadow,x-24,y-16,408,608,Color.white,true);
                if(selected)PImage(screen,"FactorSelection"+i,PlanningArt.round28,x-10,y-10,380,580,PlanPink,true);
                PImage(screen,"FactorCardEdge"+i,PlanningArt.round28,x-4,y-4,368,568,Color.white,true);
                var card=PImage(screen,"FactorCard"+i,PlanningArt.round28,x,y,360,560,Color.white,true);card.gameObject.AddComponent<Mask>().showMaskGraphic=true;
                if(art[i]==null)throw new InvalidOperationException("因子カードの一枚絵を設定してください。");
                float scale=Mathf.Max(360/art[i].rect.width,560/art[i].rect.height),w=art[i].rect.width*scale,h=art[i].rect.height*scale;
                PImage(card,"FactorIllustration"+i,art[i],(360-w)*.5f,(560-h)*.18f,w,h);
                IncidentShape(card,"FactorCardShade"+i,"story-card-shade",0,420,360,140,Color.white);
                StoryStars(card,"FactorStars"+i,c.stars,20,450);
                var name=StoryText(card,"FactorCardName"+i,OpsCatalog.AllProjects[OpsCatalog.Index(c.id)].name,20,480,320,40,26,Color.white);name.enableAutoSizing=true;name.fontSizeMin=20;name.textWrappingMode=TextWrappingModes.NoWrap;
                StoryText(card,"FactorRecommendation"+i,speakers[i]+"の推薦："+OpsCatalog.FactorRecommendation(c.id),20,520,320,34,14,new Color(1,1,1,.9f),false,true);
                var foil=PImage(card,"FactorFoil"+i,PlanningArt.shine,-200,0,160,560,new Color(1,1,.85f,.35f));Motion(foil,"shine",3.6f,i*.12f);
                // 絵の上の透明な操作面。カードはマスクで角丸に切り抜き、別のボタンを重ねる。
                var hit=PButton(screen,"ChooseFactor"+i,"",x,y,360,560,()=>{selectedFactor=index;FactorScreen();if(index!=1)QueueCompanionScene(index==0?"factor_engineer":"factor_kanon");},Color.clear,Color.clear);
                // 操作面と絵を同じ根へまとめ、既存のホバー／押下の手応えを絵にも伝える。
                var edge=screen.Find("FactorCardEdge"+i) as RectTransform;var selection=screen.Find("FactorSelection"+i) as RectTransform;
                foreach(var child in new[]{shadow,selection,edge,card})if(child!=null){child.SetParent(hit.transform,false);child.anchoredPosition-=new Vector2(x,-y);}
                var feedback=hit.GetComponent<OpsButtonFeedback>();feedback.PressDepth=4;feedback.LiftOnFocus=true;
            }
            StoryText(screen,"OwnedFactorHeading","持っている因子",258,790,104,74,15,new Color(1,1,1,.85f),true).textWrappingMode=TextWrappingModes.NoWrap;
            for(int i=0;i<OpsCatalog.StoryFactorSlots;i++)
            {
                int slot=i;bool filled=i<Career.factors.Count,chosen=replaceFactorSlot==i;string value=chosen?"＋ "+TitleFactorName(factorCandidates[selectedFactor].id):filled?TitleFactorName(Career.factors[i]):"空き";
                var target=StoryFactorSlot(screen,"FactorSlot"+i,value,378+i*226,790,210,74,filled&&!chosen);
                if(chosen){var g=target.Find("FactorSlot"+i+"Dashes");if(g!=null)g.GetComponent<OpsIncidentGraphic>().color=PlanPink;target.GetComponentInChildren<TextMeshProUGUI>().color=Hex("ff94ae");}
                if(filled){var hit=PButton(screen,"ReplaceFactor"+i,"",378+i*226,790,210,74,()=>{replaceFactorSlot=slot;FactorScreen();},Color.clear,Color.clear);hit.GetComponent<OpsButtonFeedback>().PressDepth=0;}
            }
            var confirm=StoryButton("ConfirmFactor","これにする",1082,796,260,ConfirmStoryFactor,true);confirm.interactable=selectedFactor>=0&&replaceFactorSlot>=0;
            if(replaceFactorSlot<0)StoryText(screen,"ReplaceFactorHint","3枠が満杯です。入れ替える因子の枠を選んでください",322,865,890,27,14,Hex("ff94ae"));
            for(int i=0;i<3;i++){var mote=PCard(screen,"FactorMote"+i,220+i*550,880,6,6,i==1?Color.white:Hex("ffd23f"),12,false);Motion(mote,"mote",4+i*.5f,i);}
        }
        private void ConfirmStoryFactor()
        {
            if(selectedFactor<0||selectedFactor>=factorCandidates.Length||replaceFactorSlot<0)return;
            if(!Career.Claim(Story,factorCandidates[selectedFactor].id,replaceFactorSlot))return;
            Save();PlayPresentationCue(OpsCue.Stamp);var continuation=factorContinuation;factorContinuation=null;continuation?.Invoke();
        }
    }
}
