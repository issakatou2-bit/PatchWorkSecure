using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private void StoryEndingScreen()
        {
            bool special=Story.records.Last().rank=="SS";
            if(special)StorySpecialEnding();else StoryClearEnding();
            // 台本の短い喜びを再利用。三年の文を一年用の全文で上書きしない。
            SpeakSceneLine("clear_04",.9f,"StoryEndingVoiceCaption");
        }
        private void StorySpecialEnding()
        {
            var kv=PImage(screen,"EndingKeyVisual",PlanningArt.titleKeyVisual,0,0,1600,900);
            kv.pivot=new Vector2(.5f,.5f);kv.anchoredPosition=new Vector2(800,-450);Motion(kv,"ending-kv",18);
            IncidentShape(screen,"EndingVeil","story-ending-veil",0,0,1600,900,Color.white);StoryConfetti();
            var badge=PCard(screen,"SpecialEndingBadge",70,44,214,38,Hex("ffd23f"),20,false);
            KitGradient(badge.GetComponent<Image>(),Hex("ffe38a"),Hex("ffd23f"),true);
            StoryText(badge,"SpecialEndingLabel","SPECIAL ENDING",0,0,214,38,16,Hex("7a5a00"),true);Shine(badge,214,38);Reveal(badge,.4f);
            var title=StoryText(screen,"EndingTitle","なにごともない、いつもの平穏。",70,560,1460,74,52,Color.white);
            var shadow=title.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.4f);shadow.effectDistance=new Vector2(0,-4);Reveal(title.rectTransform,.8f);
            StoryText(screen,"EndingNarrative","3年間、事件は毎月やってきた。それでも、誰も責められず、仕事は止まらなかった。\nツギハギを重ねた会社は、いつのまにか、ちゃんと強くなっていた。",70,642,1460,77,19,new Color(1,1,1,.95f),false,true);
            float x=70;
            for(int i=0;i<Story.records.Count;i++)
            {
                var record=Story.records[i];float w=record.rank=="SS"?142:123;
                var chip=PCard(screen,"EndingYear"+i,x,780,w,56,new Color(1,1,1,.14f),16,false);
                StoryText(chip,"EndingYearName"+i,(i+1)+"年目",16,0,42,56,13,new Color(1,1,1,.8f));
                StoryText(chip,"EndingYearRank"+i,record.rank,68,0,w-76,56,26,Hex("ffd23f"));Reveal(chip,1.4f);x+=w+12;
            }
            var total=PCard(screen,"EndingTotal",x,780,226,56,new Color(1,1,1,.14f),16,false);
            StoryText(total,"EndingTotalHeading","3年の合計",16,0,65,56,13,new Color(1,1,1,.8f));
            StoryText(total,"EndingTotalScore",Story.records.Sum(r=>r.score).ToString("N0")+"点",91,0,119,56,24,Color.white);
            var unlock=PCard(screen,"EndingUnlock",x+238,780,201,56,new Color(1,.824f,.247f,.22f),16,false);
            StoryText(unlock,"EndingUnlockLabel","終わりなき年度 解放！",10,0,181,56,15,Hex("ffe38a"),true);
            StoryButton("StoryRecord","3年を振り返る",1036,780,220,StoryRecord);
            StoryButton("BackHome","タイトルへ",1270,780,260,StoryTitleOrFactor,true);
            StoryText(screen,"StoryEndingVoiceCaption","",70,850,800,30,16,Color.white,false,true);
        }
        private void StoryClearEnding()
        {
            StoryBackground();var rays=screen.Find("StoryRays") as RectTransform;rays.sizeDelta=new Vector2(1800,1800);rays.anchoredPosition=new Vector2(800,-330);
            StoryCategory(screen,"StoryCategory","3 YEARS CLEAR",500,40,600,PlanPink);screen.Find("StoryCategory").GetComponent<TextMeshProUGUI>().alignment=TextAlignmentOptions.Midline;
            StoryText(screen,"EndingTitle","3年間守り抜いた",0,68,1600,74,46,null,true);StoryRoad(529,170);
            Portrait(screen,"StoryPortrait",640,320,320,380,"pose_jump");Motion(screen.Find("StoryPortrait") as RectTransform,"hop",1.4f);
            StorySpeech("3年間、本当にありがとう！\n次は「特別な結末」も見てみたいね。\n3年目で運用ランクSSが目印だよ。",1000,380,420,130);
            var panel=PCard(screen,"EndingUnlock",180,420,400,160,Color.white,24);
            StoryCategory(panel,"UnlockCategory","UNLOCK",22,18,340,Hex("ffb020"));
            StoryText(panel,"EndingUnlockLabel","終わりなき年度 解放",22,44,356,34,22);
            StoryText(panel,"UnlockHint","4年目から先へ。年を重ねるほど手強くなり、自己ベストと称号を競う。",22,84,356,65,14,PlanGray,false,true);
            StoryButton("StoryRecord","3年を振り返る",404,760,240,StoryRecord);
            StoryButton("StoryEndless","終わりなき年度へ",660,760,280,()=>Dialog("終わりなき年度","解放を記録しました。本体は準備中です。",280),false,true);
            StoryButton("BackHome","タイトルへ",956,760,240,StoryTitleOrFactor,true);
            StoryText(screen,"StoryEndingVoiceCaption","",570,710,460,30,16,PlanInk,true,true).textWrappingMode=TextWrappingModes.NoWrap;
        }
    }
}
