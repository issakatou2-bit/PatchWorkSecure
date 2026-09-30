using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        public bool CaptionsEnabled {get;private set;}=true;
        public bool ShortenInterruptions {get;private set;}
        public int TextSize {get;private set;}=1;
        public float TextScale => TextSize==0?.9f:TextSize==2?1.12f:1;
        private void LoadDisplaySettings()
        {
            if(TestMode)return;
            CaptionsEnabled=PlayerPrefs.GetInt("pws_ops_captions",1)!=0;
            ShortenInterruptions=PlayerPrefs.GetInt("pws_ops_shorten",0)!=0;
            TextSize=Mathf.Clamp(PlayerPrefs.GetInt("pws_ops_text_size",1),0,2);
        }
        private void StoreDisplaySettings()
        {
            if(TestMode)return;
            PlayerPrefs.SetInt("pws_ops_captions",CaptionsEnabled?1:0);PlayerPrefs.SetInt("pws_ops_shorten",ShortenInterruptions?1:0);PlayerPrefs.SetInt("pws_ops_text_size",TextSize);PlayerPrefs.Save();
        }
        private void Menu()
        {
            if(modal!=null){modal.gameObject.SetActive(false);Destroy(modal.gameObject);}
            WindowBackdrop();
            var d=Box(modal,"SettingsWindow",330,50,940,800,Color.white,true);KitPanel(d,Color.white,true);WindowHeader(d,"設定","SETTINGS",940);Reveal(d);
            SettingsSection(d,"SOUND",30,100,PlanPink);
            SettingsSlider(d,"MusicVolume","BGM",130,musicVolume,v=>{musicVolume=v;StoreFeedbackSettings();TickMusic();});
            SettingsSlider(d,"SoundVolume","効果音",204,soundVolume,v=>{soundVolume=v;StoreFeedbackSettings();if(buttonAudio!=null)buttonAudio.volume=v*.65f;if(eventAudio!=null)eventAudio.volume=v;SetPresentationVolume(v);});
            SettingsSlider(d,"VoiceVolume","ひなたの声",278,voiceVolume,v=>{voiceVolume=v;StoreFeedbackSettings();if(voiceAudio!=null)voiceAudio.volume=v;if(v<=0)StopVoice();});
            SettingsSection(d,"DISPLAY",30,358,PlanBlue);
            var text=SettingsRow(d,"TextSizeRow","文字の大きさ",388);
            for(int i=0;i<3;i++)
            {
                int selected=i;string[] labels={"小","標準","大"};PButton(text,"TextSize"+i,labels[i],184+i*78,9,72,44,()=>{TextSize=selected;StoreDisplaySettings();ApplyTextPreferences();Menu();},TextSize==i?PlanPink:PlanTrack,TextSize==i?Color.white:Hex("52607a"),16);
            }
            PText(text,"TextSizePreview","あいうえお  ABC 123",440,13,410,36,16,Hex("52607a"),false);
            SettingsToggle(d,"CaptionToggle","字幕","ひなたの声を文字でも表示",462,CaptionsEnabled,()=>{CaptionsEnabled=!CaptionsEnabled;ApplyVoiceCaption();StoreDisplaySettings();Menu();});
            SettingsSection(d,"MOTION",30,542,PlanMint);
            SettingsToggle(d,"ReduceMotion","動きを減らす","揺れ・粒子・画面の揺れを止める",572,ReducedMotion,()=>{ReducedMotion=!ReducedMotion;StoreFeedbackSettings();Menu();});
            SettingsToggle(d,"ShortenInterruptions","演出の短縮","2回目以降の割り込み演出を短くする",646,ShortenInterruptions,()=>{ShortenInterruptions=!ShortenInterruptions;StoreDisplaySettings();Menu();});
            PButton(d,"DeleteRecords","記録を消す…",30,738,142,40,ConfirmDeleteRecords,new Color(0,0,0,0),Hex("c23a60"),16);
            PButton(d,"AdvancedSettings","記録・試聴",188,738,162,40,DiagnosticMenu,Color.white,PlanGray,16);
            PButton(d,"ReplayTutorial","操作ガイド",370,738,164,40,ReplayTutorial,Color.white,PlanGray,16);
            PButton(d,"CloseDialog","閉じる",690,724,220,54,CloseDialog,Color.white,PlanInk,20);
        }
        private void SettingsSection(Transform d,string title,float x,float y,Color color)
        {var t=PText(d,title+"Section",title,x,y,840,22,11,color);t.characterSpacing=3;}
        private RectTransform SettingsRow(Transform d,string id,string label,float y)
        {var row=PCard(d,id,30,y,880,62,Hex("f3f6fb"),16,false);PText(row,id+"Label",label,18,13,150,36,18);return row;}
        private void SettingsSlider(Transform d,string id,string label,float y,float value,Action<float> changed)
        {
            var row=SettingsRow(d,id+"Row",label,y);
            var root=Rect(row,id,184,0,612,62);var hit=root.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
            var slider=root.gameObject.AddComponent<Slider>();slider.minValue=0;slider.maxValue=1;slider.direction=Slider.Direction.LeftToRight;
            var track=PCard(root,id+"Track",0,25,612,12,PlanTrack,12,false);
            var fill=PCard(track,id+"Fill",0,0,612,12,PlanPink,12,false);fill.anchorMin=Vector2.zero;fill.anchorMax=Vector2.one;fill.offsetMin=fill.offsetMax=Vector2.zero;KitGradient(fill.GetComponent<Image>(),Hex("ff94ae"),PlanPink,true);slider.fillRect=fill;
            var area=Rect(root,id+"HandleArea",0,31,612,0);var handle=PCard(area,id+"Knob",0,0,32,32,Color.white,20,false);handle.pivot=new Vector2(.5f,.5f);handle.anchoredPosition=Vector2.zero;var rim=handle.gameObject.AddComponent<Outline>();rim.effectColor=PlanPink;rim.effectDistance=new Vector2(3,-3);handle.GetComponent<Image>().raycastTarget=true;
            slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<Image>();slider.SetValueWithoutNotify(value);
            var percent=PText(row,id+"Percent",Mathf.RoundToInt(value*100).ToString(),810,13,50,36,18,null,true,true);
            slider.onValueChanged.AddListener(v=>{StopVoice();percent.text=Mathf.RoundToInt(v*100).ToString();changed(v);});
        }
        private void SettingsToggle(Transform d,string id,string label,string hint,float y,bool value,Action changed)
        {
            var row=SettingsRow(d,id+"Row",label,y);
            var toggle=PButton(row,id,"",184,13,64,36,changed,value?PlanMint:Hex("c9d1de"),Color.white,20);
            toggle.GetComponent<OpsButtonFeedback>().PressDepth=0;
            PCard(toggle.transform,id+"Knob",value?32:4,4,28,28,Color.white,16,false);
            PText(row,id+"Hint",hint,270,13,590,36,15,Hex("52607a"),false);
        }
        private void ApplyTextPreferences()
        {
            foreach(var size in Surface.GetComponentsInChildren<OpsTextPreference>(true))size.Apply(TextScale);
        }
        private void ReplayTutorial()
        {
            if(State!=null && State.month==0 && State.phase==OpsPhase.Planning && !State.audited && State.capacity>=3){CloseDialog();StartTutorial();return;}
            var d=Dialog("操作ガイドの再表示","このガイドは、新しい一年の4月に実際の操作で進みます。\n現在の進行を変更せず、次のニューゲームで表示するようにできます。",430);
            PButton(d,"EnableNextTutorial","次のニューゲームで表示",32,360,420,48,()=>{if(!TestMode){PlayerPrefs.SetInt(TutorialKey,0);PlayerPrefs.Save();}Menu();},PlanPink,Color.white);
        }
        private void ConfirmDeleteRecords()
        {
            var d=Dialog("この試作の記録を消しますか？","現在の年度・3年の挑戦・因子・解放状況を消します。\n旧版のセーブと音量・表示設定は残ります。削除前の進行ファイルは退避します。",430);
            PButton(d,"ConfirmDeleteRecords","記録を消してタイトルへ",32,360,420,48,()=>
            {
                try
                {
                    if(!TestMode && File.Exists(SavePath))File.Move(SavePath,SavePath+".deleted-"+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"));
                    saved=null;savedProgress=null;State=null;Story=null;Career=new OpsCareer();SaveWarning="";SkipTutorialVisual();tutorialStep=-1;RenderHome();
                }
                catch(Exception ex){Dialog("記録を消せませんでした","元の記録は残っています。\n"+ex.Message,430);}
            },PlanPink,Color.white);
        }
    }
}
