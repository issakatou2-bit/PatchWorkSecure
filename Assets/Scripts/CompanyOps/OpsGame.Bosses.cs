using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private void DrawBossEntry(RectTransform overlay,OpsYearRival boss)
        {
            OpeningShape(overlay,"BossBackground","appear",0,0,1600,900);
            OpeningShape(overlay,"BossScan","scan",0,0,1600,900);
            OpeningShape(overlay,"BossTapeTop","caution",0,0,1600,120);OpeningShape(overlay,"BossTapeBottom","caution",0,780,1600,120);
            StoryText(overlay,"BossEntryShadow","強敵 出現！",0,138,1600,200,120,Hex("a8193e"),true);
            var title=StoryText(overlay,"BossEntryTitle","強敵 出現！",0,130,1600,200,120,Color.white,true);title.characterSpacing=8;
            var glow=title.gameObject.AddComponent<Shadow>();glow.effectColor=new Color(1,.3f,.45f,.65f);glow.effectDistance=new Vector2(0,-3);
            var card=OpeningCard(overlay,"BossCard",420,330,760,400,Hex("130810"),PlanPink,26);
            var gradient=PImage(card,"BossCardGradient",PlanningArt.round24,0,0,760,400);KitGradient(gradient.GetComponent<Image>(),Hex("2a0f1d"),Hex("130810"));
            OpeningShape(card,"BossSilhouette","bossRival",30,30,220,340,null,0,boss.shape);
            var tag=PCard(card,"BossMonth",280,46,180,27,Hex("e0405f"),12,false);
            StoryText(tag,"BossMonthText",State.storyCalendarYear+"年目 "+OpsCatalog.Months[boss.month].name+"の山場",0,0,180,27,15,Color.white,true);
            StoryText(card,"BossStars",new string('★',boss.stars),280,85,450,42,24,Hex("ff6f91")).characterSpacing=3;
            StoryText(card,"BossName",boss.name,280,117,450,64,34,Color.white).textWrappingMode=TextWrappingModes.NoWrap;
            StoryText(card,"BossIdentity",boss.identity,280,184,450,88,17,Hex("ffd0da"),false,true);
            float x=280,y=284;
            for(int i=0;i<boss.equipment.Length;i++)
            {
                string id=boss.equipment[i],name=OpsCatalog.AllProjects[OpsCatalog.Index(id)].name;
                string label=(i==0?"効く備え：":"")+name;float w=Mathf.Min(450,label.Length*15+26);
                if(x+w>730){x=280;y+=42;}OpeningPill(card,"BossEquipment"+i,label,x,y,w,PlanBlue,Color.white);x+=w+8;
            }
            // 冒頭の白い閃光を軽減設定では省略。未確認の事件に確定の侵害とは書かない。
            OpeningMotion(title.rectTransform,"big",.6f,.1f);OpeningMotion(card,"pop",.5f,.15f);
        }
        private void BossArchive()
        {
            var d=Dialog("強敵の記録","確定した被害・停止が公開見積もりの下半分だった強敵を記録。\n正常と確認した出来事は、倒した攻撃として数えません。",780);
            d.Find("DialogBody").GetComponent<RectTransform>().sizeDelta=new Vector2(748,62);
            var list=Scroll(d,32,180,752,490);
            foreach(var year in OpsCatalog.StoryCompanies.Skip(1))foreach(var boss in year.rivals)
            {
                bool defeated=Career.defeatedBosses?.Any(b=>b.year==year.year&&b.eventId==boss.id)==true;
                var entry=Box(list,"BossArchive_"+boss.id,0,0,732,92,Color.white,true);entry.gameObject.AddComponent<LayoutElement>().preferredHeight=92;
                Text(entry,"BossArchiveName",year.year+"年目 "+OpsCatalog.Months[boss.month].name+" / "+(defeated?boss.name:"？？？"),16,9,620,32,22);
                Text(entry,"BossArchiveStatus",defeated?"撃退済 / "+new string('★',boss.stars):"未記録",16,49,620,28,17,defeated?Mint:Muted);
            }
            foreach(var item in (Career.defeatedBosses??new System.Collections.Generic.List<OpsBossDefeat>()).Where(b=>b.year==2&&b.month==OpsCatalog.MarchPeak))
            {
                var row=Button(list,"BossArchive_"+item.eventId,"2年目 3月 / "+item.Boss.name+" / 撃退済",0,0,732,60,()=>Knowledge(OpsEventCatalog.Profile(item.Boss.profile).lesson),Edge);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight=60;
            }
        }
    }
}
