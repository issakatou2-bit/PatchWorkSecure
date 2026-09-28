using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private bool budgetGainPending;
        private void BudgetGainEffect(RectTransform target)
        {
            if(!Application.isPlaying||!budgetGainPending)return;budgetGainPending=false;
            var layer=Rect(screen,"BudgetGainEffect",0,0,1600,900);
            var effect=layer.gameObject.AddComponent<OpsBudgetGain>();effect.Owner=this;effect.Target=target;
            effect.Coins=new RectTransform[5];
            for(int i=0;i<effect.Coins.Length;i++)
            {
                var coin=PCard(layer,"GainCoin"+i,1000,148,22,22,Hex("ffd23f"),20,false);
                PText(coin,"CoinMark","円",0,0,22,22,11,Hex("7a5a00"),true,true);
                coin.gameObject.AddComponent<CanvasGroup>().blocksRaycasts=false;
                effect.Coins[i]=coin;
            }
        }
    }
    // 表示専用。Unity/ゲームの抽選乱数や資源には触れない。
    public sealed class OpsBudgetGain : MonoBehaviour
    {
        public OpsGame Owner;public RectTransform Target;public RectTransform[] Coins;
        private float started;private Vector2 end;
        private void Start(){started=Time.realtimeSinceStartup;end=((RectTransform)transform).InverseTransformPoint(Target.TransformPoint(new Vector3(37,-32,0)));}
        private void LateUpdate()
        {
            float t=Mathf.Clamp01((Time.realtimeSinceStartup-started)/.6f);
            for(int i=0;i<Coins.Length;i++)
            {
                var start=new Vector2(1000,-148);float a=(i-2)*.6f;
                var scatter=start+new Vector2(Mathf.Sin(a)*85,Mathf.Cos(a)*45);
                float travel=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.25f)/.65f));
                Coins[i].anchoredPosition=Owner.ReducedMotion?start+new Vector2(i*26,0):t<.25f?Vector2.Lerp(start,scatter,t/.25f):Vector2.Lerp(scatter,end,travel);
                Coins[i].GetComponent<CanvasGroup>().alpha=t<.85f?1:1-(t-.85f)/.15f;
            }
            if(Target!=null)Target.localScale=Vector3.one*(Owner.ReducedMotion?1:1+.1f*Mathf.Sin(Mathf.Clamp01((t-.65f)/.35f)*Mathf.PI));
            if(t>=1){if(Target!=null)Target.localScale=Vector3.one;Destroy(gameObject);}
        }
    }
}
