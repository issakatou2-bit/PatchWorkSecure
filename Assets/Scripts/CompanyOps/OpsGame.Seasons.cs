using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private static readonly string[] SeasonNames={"桜","新緑","雨","夏の日差し","陽炎","秋風","紅葉","落ち葉","雪","正月飾り","冬の雪","桜のつぼみ"};
        private void SeasonLayer(Transform parent,float width,float height,int month)
        {
            var layer=Rect(parent,"SeasonLayer_"+SeasonNames[month],0,0,width,height);
            layer.gameObject.AddComponent<RectMask2D>();
            var group=layer.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
            Color tint=month==1?new Color(.24f,.8f,.47f,.055f):month==2?new Color(.18f,.37f,.72f,.07f):
                month==3||month==4?new Color(1,.86f,.66f,.065f):month==6||month==7?new Color(1,.44f,.27f,.055f):new Color(1,1,1,0);
            PImage(layer,"SeasonTint",null,0,0,width,height,tint);
            for(int j=0;j<8;j++)
            {
                float x=width*(.1f+j*.115f),period=8+j*.4f;RectTransform particle;
                if(month==0||month==5||month==6||month==7)
                {
                    particle=PImage(layer,"SeasonParticle"+j,PlanningArt.petal,x,0,month==0?14:18,10,
                        month==0?Color.white:month==5?Hex("9cab5e"):Hex("e78b35"));
                    Motion(particle,"petal",period,j*.8f);
                }
                else if(month==8||month==10)
                {particle=PImage(layer,"SeasonParticle"+j,PlanningArt.snow,x,0,8,8);Motion(particle,"snow",period+2,j*.7f);}
                else if(month==2)
                {particle=PImage(layer,"SeasonParticle"+j,null,x,0,2,36,new Color(.7f,.84f,1,.35f));Motion(particle,"rain",1.2f+j*.07f,j*.2f);}
                else if(month==1)
                {particle=PCard(layer,"SeasonParticle"+j,x,height*.68f,6,6,new Color(.8f,1,.77f,.45f),12,false);Motion(particle,"mote",period,j*.8f);}
            }
            if(month==3||month==4)
            {
                var heat=PImage(layer,"SeasonHaze",PlanningArt.shine,0,0,width,height,new Color(1,1,1,.055f));Motion(heat,"haze",4);
            }
            if(month==9)
            {
                var ornament=Rect(layer,"SeasonOrnament",width*.57f,height*.48f,44,54);
                PCard(ornament,"OrnamentBase",4,34,36,16,Color.white,12,false);
                for(int j=0;j<3;j++) PCard(ornament,"Bamboo"+j,8+j*10,15-Mathf.Abs(j-1)*-7,7,j==1?30:23,Hex("429e69"),12,false);
                PCard(ornament,"OrnamentRibbon",4,36,36,5,PlanPink,12,false);
            }
            if(month==11)
            {
                for(int j=0;j<4;j++)PImage(layer,"SeasonBud"+j,PlanningArt.petal,width*(.08f+j*.25f),height*.12f,7,6,Color.white);
            }
        }
    }
}
