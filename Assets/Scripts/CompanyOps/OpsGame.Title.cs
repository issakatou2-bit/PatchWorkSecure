using System;
using UnityEngine;
using UnityEngine.UI;
namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private void TitleScreen()
        {
            if(PlanningArt.titleKeyVisual==null)throw new InvalidOperationException("承認済みタイトル一枚絵を設定してください。");
            var kv=PImage(screen,"TitleKeyVisual",PlanningArt.titleKeyVisual,80,0,1600,900);
            IncidentShape(screen,"TitleVeil","story-title-veil",0,0,1600,900,Color.white);
            var brand=Rect(screen,"TitleBrand",0,0,700,250);
            var logo=PImage(brand,"TitleLogoWordmark",PlanningArt.logoWordmark,50,0,640,246);Reveal(logo);if(Application.isPlaying)logo.GetComponent<OpsUIReveal>().Duration=.9f;
            var icon=PImage(brand,"TitleLogoIcon",PlanningArt.logoIcon,-24,118,132,132);icon.pivot=new Vector2(.5f,.5f);icon.anchoredPosition+=new Vector2(66,-66);icon.localEulerAngles=new Vector3(0,0,8);
            StoryModeButton("NewYear","3年の本編","年度ごとの目標を越えて、3年間会社を守り抜く",292,92,30,true,()=>ConfirmNewYear(true));
            StoryModeButton("SingleYear","1年だけ遊ぶ","いつもの「情シスの一年」。練習にも",398,74,23,false,()=>ConfirmNewYear(false));
            var endless=StoryModeButton("EndlessYear","終わりなき年度","年を重ねるほど手強くなる。自己ベストと称号を競う",486,74,23,false,EndlessNotice);
            if(!Career.endlessUnlocked)
            {
                endless.interactable=false;
                KitGradient(endless.GetComponent<Image>(),Hex("e9edf4"),Hex("e9edf4"));
                foreach(var t in endless.GetComponentsInChildren<TMPro.TextMeshProUGUI>())t.color=Hex("9aa3b5");
                var chip=PCard(endless.transform,"EndlessLockTag",195,18,145,26,Hex("dfe5ef"),12,false);
                PText(chip,"EndlessLockText","本編クリアで解放",0,0,145,26,12,PlanGray,true,true);
            }
            var factors=PCard(screen,"StoryFactorsPanel",90,574,470,54,Color.white,24);
            PText(factors,"FactorsCategory","FACTORS",16,0,80,54,12,Hex("ffb020"));
            PText(factors,"StoryFactors","因子 "+Career.factors.Count+"/"+OpsCatalog.StoryFactorSlots,96,0,66,54,14);
            float factorX=164;
            foreach(string id in Career.factors)
            {
                string name=TitleFactorName(id);float width=Mathf.Min(name.Length*13+24,(470-factorX-12)/(Career.factors.Count-Career.factors.IndexOf(id)));
                var chip=PCard(factors,"TitleFactor_"+id,factorX,12,width,30,Hex("fff6d6"),16,false);
                PText(chip,"TitleFactorName_"+id,name,6,0,width-12,30,13,Hex("7a5a00"),true,true);factorX+=width+8;
            }
            var resume=PButton(screen,"ContinueYear","つづきから",90,642,148.67f,52,ContinueSaved,Color.white,PlanInk,20,null,saved!=null);TitleButtonStyle(resume,false);
            Hover(resume,saved==null?"つづきの記録はありません":(savedProgress?.story==null?"":savedProgress.story.year+"年目 / ")+saved.Current.name);
            TitleButtonStyle(PButton(screen,"HomeGuide","遊び方",250.67f,642,148.67f,52,Guide,Color.white,PlanInk),false);
            TitleButtonStyle(PButton(screen,"HomeSettings","設定",411.33f,642,148.67f,52,Menu,Color.white,PlanInk),false);
            var caption=PText(screen,"TitleCaption","",620,836,890,30,16,PlanInk,false,true);caption.textWrappingMode=TMPro.TextWrappingModes.NoWrap;
            if(SaveWarning!="")PText(screen,"SaveWarning",SaveWarning,90,864,1420,28,15,Coral,false);
        }
        private static string TitleFactorName(string id)=>id=="inventory"?"台帳":OpsCatalog.Projects[OpsCatalog.Index(id)].name;
        private void EndlessNotice(){Dialog("終わりなき年度","準備中です。解放の記録は保存されています。",340);}
        private Button StoryModeButton(string id,string title,string hint,float y,float h,float size,bool primary,Action action)
        {
            var b=PButton(screen,id,title,90,y,470,h,action,primary?PlanPink:Color.white,primary?Color.white:PlanInk);TitleButtonStyle(b,primary);
            var t=b.GetComponentInChildren<TMPro.TextMeshProUGUI>();t.rectTransform.anchoredPosition=new Vector2(26,-10);t.rectTransform.sizeDelta=new Vector2(418,primary?50:40);t.fontSize=t.fontSizeMax=size;t.fontSizeMin=size;t.enableAutoSizing=false;t.textWrappingMode=TMPro.TextWrappingModes.NoWrap;t.overflowMode=TMPro.TextOverflowModes.Overflow;t.fontStyle=TMPro.FontStyles.Bold;
            PText(b.transform,id+"Description",hint,26,h-34,418,24,primary?14:13,primary?Color.white:PlanGray);
            if(primary)Shine(b.transform,470,h);return b;
        }
        private void TitleButtonStyle(Button button,bool primary)
        {
            // 保存が無い場合も白いメニューを保つ。操作不可と理由は変えない。
            KitGradient(button.GetComponent<Image>(),primary?Hex("ff94ae"):Color.white,primary?Hex("f45a80"):Hex("eef2f8"));
            var states=button.colors;states.normalColor=states.highlightedColor=states.selectedColor=states.disabledColor=Color.white;
            states.pressedColor=new Color(.96f,.96f,.96f);states.colorMultiplier=1;button.colors=states;
            button.GetComponent<Shadow>().effectColor=primary?Hex("d94a70"):Hex("c7d0e0");
            button.GetComponent<Shadow>().effectDistance=new Vector2(0,-6);
            var topLight=button.transform.Find("KitTopLight") as RectTransform;
            if(topLight!=null){topLight.anchoredPosition=new Vector2(8,-2);topLight.sizeDelta=new Vector2(((RectTransform)button.transform).rect.width-16,2);}
            // 厚みはボタン背面の独立画像。グラデーションやメッシュ効果の順序に依存しない。
            var body=button.GetComponent<RectTransform>();
            var depth=PImage(body.parent,button.name+"Depth",PlanningArt.round20,body.anchoredPosition.x,-body.anchoredPosition.y+6,body.rect.width,body.rect.height,primary?Hex("d94a70"):Hex("c7d0e0"),true);
            depth.SetSiblingIndex(body.GetSiblingIndex());button.GetComponent<Shadow>().enabled=false;
            var label=button.GetComponentInChildren<TMPro.TextMeshProUGUI>();label.fontSizeMax=primary?24:17;label.fontSizeMin=primary?20:14;label.fontSize=label.fontSizeMax;
            var rect=button.GetComponent<RectTransform>();bool large=button.name=="NewYear"||button.name=="SingleYear"||button.name=="EndlessYear";
            label.fontStyle=TMPro.FontStyles.Bold;
            label.alignment=large?TMPro.TextAlignmentOptions.MidlineLeft:TMPro.TextAlignmentOptions.Midline;label.rectTransform.anchorMin=label.rectTransform.anchorMax=label.rectTransform.pivot=new Vector2(0,1);
            label.rectTransform.anchoredPosition=Vector2.zero;label.rectTransform.sizeDelta=rect.rect.size;
        }
        private void ConfirmNewYear(bool story)
        {
            Action begin=()=>{if(story)StartStory(Environment.TickCount);else StartYear(Environment.TickCount);};
            if(Story!=null&&Story.finished&&!Story.rewardClaimed){ShowStoryFactors(begin);return;}
            if(saved==null&&string.IsNullOrEmpty(SaveWarning)){begin();return;}
            var d=Dialog(story?"3年の本編を始めますか？":"新しい一年を始めますか？","進行中の年度を置き換えます。因子・解放状況と旧版のセーブは残ります。",340);
            Button(d,"ConfirmNewYear",story?"3年の本編をはじめる":"1年だけ遊ぶ",32,270,420,48,begin,Accent);
        }
    }
}
