using TMPro;
using UnityEngine;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        public bool FactorRevealActive {get;private set;}
        public bool FactorRevealPaused {get;set;}
        private float factorRevealTime;
        private RectTransform[] factorFlips;
        private GameObject[] factorFronts,factorBacks;
        private OpsFactorBurstGraphic[] factorBursts;
        private bool[] factorFlipSound;
        private void FactorRevealScreen()
        {
            // 選択画面のカードを使い回す。めくる間は選択・因子の取得を行わない。
            FactorScreen();FactorRevealActive=true;FactorRevealPaused=false;factorRevealTime=0;
            factorTiming=BeginPresentation("factor",OpsPresentationTiming.Factor);
            screen.Find("FactorCategory").GetComponent<TextMeshProUGUI>().text="FACTOR";
            screen.Find("EndingTitle").GetComponent<TextMeshProUGUI>().text=Story.year+"年目まで届いた挑戦の因子";
            screen.Find("FactorHint").GetComponent<TextMeshProUGUI>().text="カードを1枚ずつめくる。★が多いほど、光が強い";
            foreach(Transform child in screen)
                if(child.name=="OwnedFactorHeading"||child.name.StartsWith("ConfirmFactor")||child.name=="ReplaceFactorHint"||child.name.StartsWith("FactorSlot")||child.name.StartsWith("ReplaceFactor")||child.name.StartsWith("FactorMote"))child.gameObject.SetActive(false);
            int n=factorCandidates.Length;factorFlips=new RectTransform[n];factorFronts=new GameObject[n];factorBacks=new GameObject[n];factorBursts=new OpsFactorBurstGraphic[n];factorFlipSound=new bool[n];
            for(int i=0;i<n;i++)
            {
                var root=(RectTransform)screen.Find("ChooseFactor"+i);root.GetComponent<UnityEngine.UI.Button>().interactable=false;
                var feedback=root.GetComponent<OpsButtonFeedback>();feedback.enabled=false;
                root.pivot=new Vector2(.5f,.5f);root.anchoredPosition=new Vector2(370+i*430,-450);factorFlips[i]=root;
                var selection=root.Find("FactorSelection"+i);if(selection!=null)selection.gameObject.SetActive(false);
                var card=(RectTransform)root.Find("FactorCard"+i);card.anchoredPosition=Vector2.zero;factorFronts[i]=card.gameObject;
                var back=PImage(root,"FactorBack"+i,PlanningArt.round28,0,0,360,560,Color.white,true);back.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic=true;
                IncidentShape(back,"FactorBackGradient"+i,"story-factor-background",0,0,360,560,Color.white);
                var stitch=IncidentShape(back,"FactorBackStitch"+i,"rounded-dashed",14,14,332,532,new Color(1,.843f,.353f,.55f));stitch.GetComponent<OpsIncidentGraphic>().StrokeWidth=3;stitch.GetComponent<OpsIncidentGraphic>().CornerRadius=18;
                var icon=PImage(back,"FactorBackLogo"+i,PlanningArt.logoIcon,105,205,150,150,new Color(1,1,1,.95f));
                var shadow=icon.gameObject.AddComponent<UnityEngine.UI.Shadow>();shadow.effectColor=new Color(0,0,0,.4f);shadow.effectDistance=new Vector2(0,-6);
                factorBacks[i]=back.gameObject;
                foreach(var g in root.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                {
                    var perspective=g.gameObject.AddComponent<OpsFactorPerspective>();perspective.Root=root;
                }
                var burst=Rect(screen,"FactorBurst"+i,190+i*430-200,70,760,760);burst.pivot=new Vector2(.5f,.5f);burst.anchoredPosition+=new Vector2(380,-380);
                factorBursts[i]=burst.gameObject.AddComponent<OpsFactorBurstGraphic>();factorBursts[i].Stars=factorCandidates[i].stars;factorBursts[i].raycastTarget=false;
            }
            var skip=PButton(screen,"SkipFactorReveal","すべてめくる",680,804,240,52,SkipFactorReveal,Color.white,PlanInk,17);TitleButtonStyle(skip,false);
            SetFactorRevealPose();
        }
        public void SkipFactorReveal()
        {
            if(!FactorRevealActive)return;
            FactorRevealActive=false;FactorRevealPaused=false;FactorScreen();
        }
        public void TickFactorReveal(float delta)
        {
            if(!FactorRevealActive)return;
            factorTiming.Advance(delta,FastPresentation);factorRevealTime=factorTiming.Elapsed;SetFactorRevealPose();
            if(factorTiming.Done)SkipFactorReveal();
        }
        private void SetFactorRevealPose()
        {
            for(int i=0;i<factorFlips.Length;i++)
            {
                float elapsed=factorRevealTime-(OpsPresentationTiming.FactorStart+i*OpsPresentationTiming.FactorStep),t=Mathf.Clamp01(elapsed/OpsPresentationTiming.FactorFlip);
                float angle=t<.55f?Mathf.Lerp(180,80,FactorEase(t/.55f)):Mathf.Lerp(80,0,FactorEase((t-.55f)/.45f));
                float scale=t<.55f?Mathf.Lerp(.92f,1.04f,FactorEase(t/.55f)):Mathf.Lerp(1.04f,1,FactorEase((t-.55f)/.45f));
                bool front=angle<=90;
                factorFronts[i].SetActive(front);factorBacks[i].SetActive(!front);
                factorFlips[i].localEulerAngles=new Vector3(0,ReducedMotion?0:front?-angle:180-angle,0);
                factorFlips[i].localScale=Vector3.one*(ReducedMotion?1:scale);
                foreach(var projection in factorFlips[i].GetComponentsInChildren<OpsFactorPerspective>())projection.Refresh();
                float light=(factorRevealTime-(.55f+i*OpsPresentationTiming.FactorStep))/OpsPresentationTiming.FactorLight;
                var burst=factorBursts[i];burst.Progress=light;burst.gameObject.SetActive(!ReducedMotion&&light>0&&light<1);burst.rectTransform.localScale=Vector3.one*Mathf.Lerp(.2f,1.15f,Mathf.Clamp01(light));burst.SetVerticesDirty();
                if(elapsed>=.35f&&!factorFlipSound[i]){factorFlipSound[i]=true;PlayPresentationCue(OpsCue.Growth);}
            }
        }
        // CSS cubic-bezier(.3,.7,.2,1)を各キーフレーム区間へ適用する。
        private static float FactorEase(float value)
        {
            float lo=0,hi=1,t=0;for(int i=0;i<12;i++){t=(lo+hi)*.5f;float x=3*(1-t)*(1-t)*t*.3f+3*(1-t)*t*t*.2f+t*t*t;if(x<value)lo=t;else hi=t;}
            return 3*(1-t)*(1-t)*t*.7f+3*(1-t)*t*t+t*t*t;
        }
    }
}
