using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        public bool DiaryActive {get;private set;}
        private TMP_FontAsset diaryFont;
        private static readonly Color DiaryInk=Hex("3a3550");
        private List<OpsDiaryRecord> DiaryPages=>Career.diary??(Career.diary=new List<OpsDiaryRecord>());
        // 本文は字幕設定と独立。音声の字幕をOFFにしても日記を消さない。
        private TextMeshProUGUI DiaryText(Transform p,string name,string text,float x,float y,float w,float h,float size=26,bool center=false)
        {
            if(diaryFont==null){var original=Resources.Load<TMP_FontAsset>("KleeOneDiary");diaryFont=TestMode?TestFont(original):original;}
            var t=PText(p,name,text,x,y,w,h,size,DiaryInk,false,center);t.font=diaryFont??Font;
            t.fontStyle=FontStyles.Normal;t.enableAutoSizing=false;t.alignment=center?TextAlignmentOptions.Midline:TextAlignmentOptions.TopLeft;
            float lineHeight=size>=22?45:size*1.55f;
            t.lineSpacing=lineHeight*t.font.faceInfo.pointSize/size-t.font.faceInfo.lineHeight;t.margin=Vector4.zero;return t;
        }
        private RectTransform DiaryPaper()
        {
            homeVisible=false;NewScreen();DiaryActive=true;
            var desk=Rect(screen,"DiaryDesk",0,0,1600,900);desk.gameObject.AddComponent<OpsDiaryGraphic>().Kind="desk";
            var book=Rect(screen,"DiaryNotebook",110,60,1380,780);
            PImage(book,"DiaryBookShadow",PlanningArt.shadow,-32,-2,1444,840,new Color(.2f,.1f,.05f,.65f),true);
            for(int i=0;i<2;i++)
            {
                var page=Rect(book,"DiaryPage"+i,i*690,0,690,780);var paper=page.gameObject.AddComponent<OpsDiaryGraphic>();paper.Kind=i==0?"page-left":"page-right";paper.color=Hex("fffdf6");
                var binding=Rect(page,"BindingShade",i==0?605:0,0,85,780);var graphic=binding.gameObject.AddComponent<OpsDiaryGraphic>();graphic.Kind=i==0?"binding-left":"binding-right";
                for(int row=0;row<14;row++){int y=(i==0?150:110)+43+row*45;if(y>740)break;PImage(page,"Rule"+row,null,i==0?56:40,y,i==0?594:610,2,Hex("dfe7f3"));}
            }
            for(int i=0;i<10;i++)
            {
                var ring=PCard(book,"DiaryRing"+i,672,40+i*686f/9,36,14,Color.white,12,false);
                KitGradient(ring.GetComponent<Image>(),Hex("f2f2f2"),Hex("9aa0aa"));var s=ring.gameObject.AddComponent<Shadow>();s.effectColor=new Color(0,0,0,.3f);s.effectDistance=new Vector2(0,-2);
            }
            if(Application.isPlaying){var motion=book.gameObject.AddComponent<OpsDiaryMotion>();motion.Kind="open";motion.Owner=this;}
            return book;
        }
        private void DiaryTape(Transform p,float x,float y,float w,bool blue,float rotation)
        {
            var r=Rect(p,"DiaryTape",x,y,w,34);r.localEulerAngles=new Vector3(0,0,-rotation);var g=r.gameObject.AddComponent<OpsDiaryGraphic>();g.Kind=blue?"blue-tape":"pink-tape";
        }
        private void DiaryTag(Transform p,string text,float x,float y,float w,Color bg,Color fg,float height=49)
        {
            var r=PCard(p,"DiaryTag",x,y,w,height,bg,24,false);var t=PText(r,"DiaryTagText",text,0,0,w,height,text=="NEW"?10:14,fg,true,true);t.enableAutoSizing=false;
        }
        public void OpenMonthlyDiary()
        {
            if(State?.Latest==null||State.phase!=OpsPhase.Review)return;
            // 連載は36か月分。4年目以降の記録で既読ページを上書きしない。
            if(Endless!=null&&RunYear>3){if(State.QuarterRewardPending)QuarterRewardDialogAfterDiary();else Next();return;}
            var r=State.Latest;int year=RunYear,key=OpsDiaryCatalog.Page(year,r.month),content=key;
            bool ends=r.month==11||State.budget<0||State.stability==0;
            bool met=r.month==11&&State.budget>=0&&State.stability>0&&(Story==null||OpsStory.RankValue(State.RankCode)>=OpsStory.RankValue(Story.Goal));
            int ending=!met?OpsDiaryCatalog.FailEnding:State.RankCode=="SS"?OpsDiaryCatalog.SSEnding:OpsDiaryCatalog.ClearEnding;
            if(year==3&&r.month==11)content=ending;
            var prior=DiaryPages.FirstOrDefault(p=>p.key==key);
            var record=new OpsDiaryRecord{key=key,content=content,mood=OpsDiaryCatalog.Mood(r),recap=OpsDiaryCatalog.Recap(State,r),thought=OpsDiaryCatalog.Thought(r),rank=State.RankCode,season=State.Current.season,yearEnd=r.month==11,continues=Story!=null&&met&&year<3,
                minigame=r.minigameRecorded&&!r.delegated?"対応 "+(r.minigameScore>=OpsCatalog.MinigameS?"S":r.minigameScore>=OpsCatalog.MinigameA?"A":r.minigameScore>=OpsCatalog.MinigameB?"B":"C"):"",
                monthNotes=State.history.Select(h=>h.peakGoalRecorded?(h.peakGoalMet?"山場突破":"山場未達"):h.eventTitle??"未記録").ToArray()};
            var equipment=State.history.Where(h=>h.investmentEffects!=null).SelectMany(h=>h.investmentEffects).GroupBy(e=>e.projectId).OrderByDescending(g=>g.Sum(e=>e.avoidedLoss*7+e.avoidedDowntime*4)).FirstOrDefault(g=>g.Any(e=>e.avoidedLoss>0||e.avoidedDowntime>0));
            record.bestEquipment=equipment==null?"比較は未記録":OpsCatalog.AllProjects[OpsCatalog.Index(equipment.Key)].name;
            record.bestSupport=State.history.Where(h=>!h.benign&&h.power!=null&&h.power.staff>0).GroupBy(h=>h.power.support.Split('：')[0]).OrderByDescending(g=>g.Sum(h=>h.power.staff)).FirstOrDefault()?.Key??"支援なし";
            if(prior!=null)DiaryPages.Remove(prior);DiaryPages.Add(record);
            // 結末のページは到達したものだけ。未来の月や別の結末は解放しない。
            if(ends&&Story!=null&&(!met||year==3))
            {
                var final=new OpsDiaryRecord{key=ending,content=ending,mood=record.mood,recap=record.recap,thought=record.thought,rank=record.rank,minigame="",season="結末",yearEnd=true,monthNotes=record.monthNotes,bestEquipment=record.bestEquipment,bestSupport=record.bestSupport};
                DiaryPages.RemoveAll(p=>p.key==ending);DiaryPages.Add(final);
            }
            Career.RecordDiaryTitle();Save();DiarySpread(record,()=>{DiaryActive=false;if(State.QuarterRewardPending)QuarterRewardDialogAfterDiary();else Next();},prior!=null);
        }
        private void QuarterRewardDialogAfterDiary(){Render();QuarterRewardDialog();}
        public void OpenDiaryBook()
        {
            var book=DiaryPaper();
            DiaryTape(book,220,-12,200,false,-2);
            // 一覧には罫線を出さない（モックの白いページ）。
            foreach(var tr in book.GetComponentsInChildren<Transform>())if(tr.name.StartsWith("Rule"))tr.gameObject.SetActive(false);
            PText(book,"DiaryBookTitle","ひなたの日記帳",66,46,568,46,34,DiaryInk);
            PText(book,"DiaryReadCount","読んだページ "+DiaryPages.Count+" / 39　・　結末のページ "+DiaryPages.Count(p=>p.key>=36)+" / 3",66,94,568,26,15,Hex("8a8399"));
            DiaryShelf(book,1,66,140,"ひとりで抱えない");DiaryShelf(book,2,66,398,"広がる会社");DiaryShelf(book,3,760,108,"狙われる会社");
            DiaryText(book,"DiaryEndingLabel","結末のページ",760,366,540,32,20);
            string[] names={"それでも","守り抜いた","特別な結末"};
            for(int i=0;i<3;i++)DiaryThumb(book,36+i,names[i],760+i*122,408,112,92);
            DiaryPhoto(book,1164,492,160,192,"pose_please","つづき 読んでね",4);
            DiaryButton(book,"DiaryClose","タイトルへ",1220,700,114,50,()=>{DiaryActive=false;RenderHome();},false);
        }
        private void DiaryShelf(Transform p,int year,float x,float y,string title)
        {
            DiaryText(p,"DiaryShelf"+year,year+"年目「"+title+"」",x,y,568,32,20);
            for(int m=0;m<12;m++)DiaryThumb(p,(year-1)*12+m,OpsCatalog.Months[m].name,x+(m%6)*88,y+42+(m/6)*102,78,92);
        }
        private void DiaryThumb(Transform p,int key,string label,float x,float y,float w,float h)
        {
            var record=DiaryPages.FirstOrDefault(r=>r.key==key);bool locked=record==null;
            var b=PButton(p,"DiaryPage_"+key,locked?"?":label,x,y,w,h,()=>DiarySpread(record,OpenDiaryBook,false,true),locked?Hex("e9e4d8"):key>=36?Hex("fff6d6"):Hex("fffdf6"),locked?Hex("b5ad9c"):DiaryInk,16,null,!locked);
            var t=b.GetComponentInChildren<TextMeshProUGUI>();t.fontSize=t.fontSizeMin=t.fontSizeMax=14;t.enableAutoSizing=false;
            Color bg=locked?Hex("e9e4d8"):key>=36?Hex("fff6d6"):Hex("fffdf6");KitGradient(b.GetComponent<Image>(),bg,bg);
            var colors=b.colors;colors.disabledColor=colors.normalColor=colors.selectedColor=colors.highlightedColor=Color.white;b.colors=colors;
            var light=b.transform.Find("KitTopLight");if(light!=null)light.gameObject.SetActive(false);b.GetComponent<Image>().pixelsPerUnitMultiplier=2.7f;
            var shadow=b.GetComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.18f);shadow.effectDistance=new Vector2(0,-3);
            if(!locked&&key<36)PText(b.transform,"DiaryPageHint",OpsDiaryRecord.PageTitles[key],4,62,w-8,20,10,Hex("8a8399"),false,true);
            if(!locked&&record==DiaryPages.LastOrDefault())DiaryTag(b.transform,"NEW",w-32,-8,40,PlanPink,Color.white,18);
        }
        private void DiarySpread(OpsDiaryRecord r,Action close,bool collapsed,bool archive=false)
        {
            if(r==null)return;var e=OpsDiaryCatalog.Entries[r.content];var book=DiaryPaper();bool annual=r.yearEnd;
            if(annual)DiaryTape(book,230,-12,170,false,-3);
            else if(r.mood==2)DiaryTape(book,230,-12,150,true,3);
            else {DiaryTape(book,250,-12,150,false,-4);DiaryTape(book,1190,-10,130,true,5);}
            int year=r.key<36?r.key/12+1:e.year,month=r.key<36?r.key%12:e.month;
            var weather=Rect(book,"DiaryWeather",66,54,46,46);var graphic=weather.gameObject.AddComponent<OpsDiaryGraphic>();graphic.Owner=this;graphic.Kind=r.mood==2?"rain":r.mood==0||r.mood==4?"sun":"cloud";
            DiaryText(book,"DiaryDate",annual?year+"年目のおわりに":year+"年目　"+OpsCatalog.Months[month].name+"の日記",126,54,405,46,30);
            if(!annual)DiaryTag(book,r.season??"",386,62,Mathf.Clamp((r.season??"").Length*14+24,50,280),r.mood==2?Hex("eef2f8"):Hex("e3f2ff"),r.mood==2?Hex("52607a"):Hex("1f75b8"),24);
            if(annual)
            {
                foreach(Transform child in book.Find("DiaryPage0"))if(child.name.StartsWith("Rule"))child.gameObject.SetActive(false);
                DiaryText(book,"DiaryAnnualLabel","この一年のできごと",66,118,568,30,18);
                for(int m=0;m<12;m++)
                {
                    string note=r.monthNotes!=null&&m<r.monthNotes.Length?r.monthNotes[m]:"未記録";
                    var card=PCard(book,"DiaryMonth"+m,66+m%4*145,162+m/4*96,133,84,note=="山場突破"?Hex("fff6d6"):note=="山場未達"?Hex("ffe9ee"):Hex("fffdf6"),12,true);
                    PText(card,"DiaryMonthLabel",OpsCatalog.Months[m].name,0,8,133,28,15,DiaryInk,true,true);
                    var t=PText(card,"DiaryMonthNote",note,8,38,117,38,11,Hex("8a8399"),false,true);t.textWrappingMode=TextWrappingModes.Normal;
                }
                var best=DiaryText(book,"DiaryAnnualBest","いちばん効いた備え："+(r.bestEquipment??"比較は未記録")+"\nいちばん助けてくれた人："+(r.bestSupport??"未記録"),66,464,568,150,22);
                string[] prefixes={"いちばん効いた備え：","いちばん助けてくれた人："},values={r.bestEquipment??"比較は未記録",r.bestSupport??"未記録"};
                for(int i=0;i<2;i++){float offset=best.GetPreferredValues(prefixes[i]).x,width=Mathf.Min(best.GetPreferredValues(values[i]).x,568-offset);var mark=PImage(book,"DiaryAnnualBestMarker"+i,null,66+offset,484+i*45,width,10,new Color(1,.878f,.4f,.9f));mark.SetSiblingIndex(best.transform.GetSiblingIndex());}
                DiarySticker(book,"DiaryRankSticker",year+"年目",r.rank,500,600,130,Hex("e0a500"),-10,0);
                DiaryTag(book,"先輩へ",760,98,86,Hex("ffe9ee"),Hex("d94a70"));
                DiaryPhoto(book,1120,470,190,226,e.pose,year+"年目 おしまい",-6);
            }
            else
            {
                DiaryTag(book,"今月のできごと",66,134,146,Hex("fff0f4"),Hex("d94a70"));
                var marker=PImage(book,"DiaryMarker",null,66,253,260,14,new Color(1,.878f,.4f,.9f));
                DiaryText(book,"DiaryActualRecord",r.recap,66,204,568,240,22);
                float memoHeight=e.memo.Length>40?138:110;
                var memoShadow=PImage(book,"DiaryMemoShadow",PlanningArt.shadow,276,420,350,memoHeight+48,Color.white,true);memoShadow.localEulerAngles=new Vector3(0,0,r.mood==2?-2:3);
                var memo=PImage(book,"DiaryMemo",null,300,436,302,memoHeight,Hex("fff6b8"));memo.localEulerAngles=new Vector3(0,0,r.mood==2?-2:3);KitGradient(memo.GetComponent<Image>(),Hex("fff6b8"),Hex("ffef8a"));
                PImage(memo,"MemoTape",null,116,-10,70,20,new Color(1,1,1,.55f));PText(memo,"MemoCategory","情シスあるある",16,14,270,24,12,Hex("9a7a00"));DiaryText(memo,"DiaryMemoText",e.memo,16,42,270,memoHeight-44,17);
                DiaryPhoto(book,90,470,190,226,r.mood==2?"pose_exhausted":e.pose,r.mood==2?"くやしい日":"今月のひなた",r.mood==2?-4:-5,r.mood==2);
                DiarySticker(book,"DiaryRankSticker","運用ランク",r.rank,470,r.mood==2?600:585,r.mood==2?110:120,r.mood==2?Hex("7d8aa3"):Hex("f45a80"),r.mood==2?8:-12,0);
                if(!string.IsNullOrEmpty(r.minigame))DiarySticker(book,"DiaryMinigameSticker","対応",r.minigame.Split(' ').Last(),350,655,90,Hex("2f93dc"),10,.25f);
                DiaryTag(book,"ひなたのひとこと",760,98,158,Hex("fff6d6"),Hex("7a5a00"));
                DiaryText(book,"DiaryThought",r.thought,760,174,568,145,26);
                if(r.mood==0){IncidentShape(book,"DiaryHeart","heart",1270,140,60,54,Hex("ffb3c4"));IncidentShape(book,"DiaryStar","solid-star",300,610,44,44,Hex("ffd23f"));}
                DiaryTag(book,"つづき",760,316,74,Hex("ffe9ee"),Hex("d94a70"));
            }
            float bodyY=annual?174:388;var body=DiaryText(book,"DiaryBody",collapsed?e.intro:e.body,760,bodyY,568,annual?298:290,26);
            if(collapsed)DiaryButton(book,"DiaryExpand","つづきを読む",760,bodyY+136,200,42,()=>{body.text=e.body;body.maxVisibleCharacters=int.MaxValue;var expand=book.Find("DiaryExpand");if(expand!=null)expand.gameObject.SetActive(false);},false);
            else if(Application.isPlaying)StartCoroutine(DiaryWrite(body));
            DiaryButton(book,"DiaryReplay","ひなたの声で聞く",760,704,242,44,()=>SpeakSceneLine(e.voiceId,0,"DiaryVoiceCaption"),true);
            for(int i=0;i<5;i++){var wave=PCard(book,"DiaryWave"+i,774+i*7,716+(i%2)*4,4,18-(i%2)*8,Color.white,12,false);var motion=wave.gameObject.AddComponent<OpsDiaryMotion>();motion.Owner=this;motion.Kind="wave";motion.Delay=i*.15f;}
            var replay=book.Find("DiaryReplay").GetComponent<Button>();var rt=replay.GetComponentInChildren<TextMeshProUGUI>();rt.rectTransform.anchoredPosition=new Vector2(34,0);rt.rectTransform.sizeDelta=new Vector2(208,44);
            DiaryButton(book,"DiaryClose",archive?"日記帳へ":annual&&r.continues?year+1+"年目へ":"とじる",1220,700,114,50,()=>{StopVoice();DiaryActive=false;close();},false);
            PText(book,"DiaryVoiceCaption","",66,740,1100,26,13,Hex("8a8399"),false);
            if(!collapsed)SpeakSceneLine(e.voiceId,.5f,"DiaryVoiceCaption");
        }
        private IEnumerator DiaryWrite(TextMeshProUGUI text)
        {
            text.ForceMeshUpdate();int first=text.textInfo.lineCount>0?text.textInfo.lineInfo[0].lastCharacterIndex+1:0;
            // 冒頭だけ書き出す。残りの本文や閉じる操作を待たせない。
            text.maxVisibleCharacters=0;float elapsed=0;
            while(text!=null&&elapsed<2.4f){elapsed+=Time.unscaledDeltaTime;text.maxVisibleCharacters=Mathf.CeilToInt(first*elapsed/2.4f);yield return null;}
            if(text!=null)text.maxVisibleCharacters=int.MaxValue;
        }
        private void DiaryPhoto(Transform p,float x,float y,float w,float h,string pose,string label,float angle,bool quiet=false)
        {
            var photoShadow=PImage(p,"DiaryPhotoShadow",PlanningArt.shadow,x-24,y-16,w+48,h+48,Color.white,true);photoShadow.localEulerAngles=new Vector3(0,0,-angle);
            var photo=PImage(p,"DiaryPolaroid",null,x,y,w,h,Color.white);photo.localEulerAngles=new Vector3(0,0,-angle);
            var inner=PImage(photo,"DiaryPhoto",null,12,12,w-24,h-52,Color.white);KitGradient(inner.GetComponent<Image>(),quiet?Hex("e6ecf8"):Hex("ffe3ec"),quiet?Hex("d6deee"):Hex("dcefff"));inner.gameObject.AddComponent<RectMask2D>();
            Portrait(inner,"DiaryPortrait",-18,8-(h-52)*.2f,w+12,(h-52)*1.2f,pose);DiaryText(photo,"DiaryPhotoCaption",label,0,h-36,w,28,17,true);
        }
        private void DiarySticker(Transform p,string name,string title,string rank,float x,float y,float size,Color color,float angle,float delay)
        {
            var outer=Rect(p,name,x-5,y-5,size+10,size+10);
            var shape=Rect(outer,"StickerCircle",5,5,size,size);var g=shape.gameObject.AddComponent<OpsDiaryGraphic>();g.Kind="sticker";g.color=color;
            // 円形の白い縁もメッシュで描く。
            var edge=outer.gameObject.AddComponent<OpsDiaryGraphic>();edge.Kind="circle";edge.color=Color.white;
            var shadow=outer.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.18f);shadow.effectDistance=new Vector2(0,-6);
            PText(outer,"StickerCategory",title,5,size*.24f,size,22,size*.108f,Color.white,true,true);PText(outer,"StickerRank",rank,5,size*.4f,size,size*.5f,size*.4f,Color.white,true,true);
            outer.localEulerAngles=new Vector3(0,0,-angle);if(Application.isPlaying){var m=outer.gameObject.AddComponent<OpsDiaryMotion>();m.Owner=this;m.Kind="stick";m.Delay=delay;m.Angle=-angle;}
        }
        private Button DiaryButton(Transform p,string id,string label,float x,float y,float w,float h,Action action,bool pink)
        {
            var b=PButton(p,id,label,x,y,w,h,()=>{StopVoice();action();},pink?PlanPink:Color.white,pink?Color.white:DiaryInk,20);TitleButtonStyle(b,pink);
            var text=b.GetComponentInChildren<TextMeshProUGUI>();text.enableAutoSizing=false;text.fontSize=text.fontSizeMin=text.fontSizeMax=pink?17:18;return b;
        }
    }
}
