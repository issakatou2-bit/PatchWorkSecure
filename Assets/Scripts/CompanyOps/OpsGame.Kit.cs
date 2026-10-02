using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        partial void TutorialAction(string id);
        partial void TutorialWindowClosed();
        partial void RefreshTutorial();
        private void KitGradient(Graphic image, Color top, Color bottom, bool horizontal = false)
        {
            image.color=Color.white;
            var effect = image.GetComponent<OpsKitGradient>(); if(effect == null) effect=image.gameObject.AddComponent<OpsKitGradient>();
            effect.Top = top; effect.Bottom = bottom; effect.Horizontal = horizontal; image.SetVerticesDirty();
        }
        private void KitPanel(RectTransform panel, Color color, bool window = false)
        {
            var image = panel.GetComponent<Image>();
            if (image == null) return;
            image.color = Color.white;
            KitGradient(image, color, Hex("f4f7fb"));
            if (color.r < .85f || color.g < .85f) return;
            foreach(var existing in panel.GetComponents<Outline>())DestroyImmediate(existing);
            var edge = panel.gameObject.AddComponent<Outline>(); edge.effectColor = window ? Hex("ffc4d3") : Hex("e2e7f0"); edge.effectDistance = new Vector2(window ? 5 : 4, window ? -5 : -4);
            var white = panel.gameObject.AddComponent<Outline>(); white.effectColor = Color.white; white.effectDistance = new Vector2(3, -3);
        }
        private void KitButton(Button button, Color requested)
        {
            if (requested.a <= 0) return;
            Color bottom, top, baseColor;
            bool white = requested.r > .85f && requested.g > .85f && requested.b > .85f;
            bool gold = requested.r > .85f && requested.g > .55f && requested.b < .35f;
            bool navy = requested.r < .3f && requested.g < .4f && requested.b < .6f;
            bool pink = requested == Accent || requested == PlanPink || requested == Coral;
            if (!button.interactable) { top = bottom = Hex("e6eaf2"); baseColor = Hex("cfd6e3"); }
            else if (gold) { top = Hex("ffe27a"); bottom = Hex("f5a800"); baseColor = Hex("c98400"); }
            else if (pink) { top = Hex("ff94ae"); bottom = Hex("f45a80"); baseColor = Hex("c9486c"); }
            else if (navy) { top = Hex("34456e"); bottom = PlanInk; baseColor = Hex("0c1226"); }
            else if (white || requested == Edge || requested == Panel) { top = Color.white; bottom = Hex("eef2f8"); baseColor = Hex("c3ccdc"); }
            else { top = Color.Lerp(requested, Color.white, .2f); bottom = requested; baseColor = Color.Lerp(requested, PlanInk, .2f); }
            var image = button.GetComponent<Image>(); image.color = Color.white; KitGradient(image, top, bottom);
            var depth = button.GetComponent<Shadow>(); if(depth == null) depth=button.gameObject.AddComponent<Shadow>(); depth.effectColor = baseColor; depth.effectDistance = new Vector2(0, -6);
            var shine = Rect(button.transform, "KitTopLight", 8, 4, Mathf.Max(0, ((RectTransform)button.transform).rect.width - 16), ((RectTransform)button.transform).rect.height*.38f);
            var light = shine.gameObject.AddComponent<Image>();light.sprite=PlanningArt?.round12;light.type=Image.Type.Sliced;light.raycastTarget=false;
            KitGradient(light,new Color(1,1,1,.42f),new Color(1,1,1,0));
            button.GetComponent<OpsButtonFeedback>().PressDepth = 4;
        }
        private void KitBadge(RectTransform badge,Color tone)
        {
            KitGradient(badge.GetComponent<Image>(),Color.Lerp(tone,Color.white,.38f),tone);
            var edge=badge.gameObject.AddComponent<Outline>();edge.effectColor=Color.white;edge.effectDistance=new Vector2(3,-3);
            var shade=badge.gameObject.AddComponent<Shadow>();shade.effectColor=new Color(.11f,.16f,.27f,.22f);shade.effectDistance=new Vector2(0,-3);
        }
        private void Reveal(RectTransform rect, float delay = 0, bool stamp = false)
        {
            if (!Application.isPlaying) return;
            var reveal = rect.gameObject.AddComponent<OpsUIReveal>(); reveal.Owner = this; reveal.Delay = delay/PresentationRate; reveal.Stamp = stamp;
            reveal.Duration=RepeatDuration("reveal_"+rect.name,.45f,.27f)/PresentationRate;
        }
        public void StampImpact()
        {
            PlayPresentationCue(OpsCue.Stamp);HoldPresentation();DuckMusic(.35f);
        }
        private string WindowCategory(string title) => title.Contains("設定") ? "SETTINGS" : title.Contains("知識") || OpsCatalog.Terms.Any(t => t.name == title) ? "KNOWLEDGE" : title.Contains("育成") || title.Contains("チーム") ? "TEAM" : title.Contains("記録") || title.Contains("評価") ? "REPORT" : title.Contains("導入") || title.Contains("Lv.") ? "PLANNING" : "PATCHWORK";
        private void WindowHeader(RectTransform card, string title, string category, float width)
        {
            var band = PCard(card, "KitHeader", 0, 0, width, 80, Color.white, 28, false);
            KitGradient(band.GetComponent<Image>(), Hex("ff94ae"), PlanPink, true);
            var stripes = IncidentShape(band, "HeaderStripes", "stripes", width - 220, 6, 150, 64, new Color(1, 1, 1, .2f));
            stripes.gameObject.AddComponent<RectMask2D>();
            PImage(band,"WindowLogoIcon",PlanningArt.logoIcon,26,13,54,54);
            var en = PText(band, "WindowCategory", category, 94, 10, width - 178, 20, 11, new Color(1, 1, 1, .85f)); en.characterSpacing = 3;
            PText(band, "DialogHeading", title, 94, 29, width - 178, 44, 26, Color.white);
            var close = PButton(band, "HeaderClose", "×", width - 68, 17, 46, 46, CloseDialog, Color.white, Hex("d94a70"), 24);
            close.GetComponent<OpsButtonFeedback>().PressDepth = 3;
        }
        private RectTransform footerAligned;
        private int footerButtonCount=-1;
        private void AlignDialogFooter()
        {
            if (modal == null) return;
            var card = modal.Find("Dialog") as RectTransform;
            if (card == null) return;
            var close = card.Find("CloseDialog") as RectTransform;
            var buttons = card.GetComponentsInChildren<Button>().Where(b => b.name != "CloseDialog" && b.name != "HeaderClose" && b.transform.parent == card && ((RectTransform)b.transform).anchoredPosition.y <= -card.rect.height + 85).ToArray();
            var primary = buttons.FirstOrDefault();
            if (close == null) return;
            if(footerAligned==card && footerButtonCount==buttons.Length)return;
            footerAligned=card;footerButtonCount=buttons.Length;
            close.anchoredPosition = new Vector2(32, -card.rect.height + 70); close.sizeDelta = new Vector2(236, 48);
            if(primary==null)return;
            var rect = (RectTransform)primary.transform; rect.anchoredPosition = new Vector2(286, -card.rect.height + 70); rect.sizeDelta = new Vector2(card.rect.width - 318, 48);
            if(buttons.Length>1)
            {
                close.sizeDelta=new Vector2(180,48);
                var secondary=(RectTransform)buttons[1].transform;secondary.anchoredPosition=new Vector2(230,-card.rect.height+70);secondary.sizeDelta=new Vector2(184,48);
                rect.anchoredPosition=new Vector2(438,-card.rect.height+70);rect.sizeDelta=new Vector2(card.rect.width-470,48);
                buttons[1].GetComponentInChildren<TextMeshProUGUI>().alignment=TextAlignmentOptions.Center;
            }
            primary.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            // 資料の注記がある行だけ、既存ボタンの余白を注記へ割り当てる。
            var reference=card.Find("EventFictionNote") as RectTransform??card.Find("TicketReference") as RectTransform;
            if(reference!=null)
            {
                if(buttons.Length>1)
                {
                    rect.anchoredPosition=new Vector2(230,-card.rect.height+70);rect.sizeDelta=new Vector2(190,48);
                    var secondary=(RectTransform)buttons[1].transform;secondary.anchoredPosition=new Vector2(438,-card.rect.height+70);secondary.sizeDelta=new Vector2(172,48);
                    reference.anchoredPosition=new Vector2(630,-card.rect.height+70);reference.sizeDelta=new Vector2(158,48);
                }
                else
                {
                    rect.sizeDelta=new Vector2(264,48);
                    reference.anchoredPosition=new Vector2(566,-card.rect.height+70);reference.sizeDelta=new Vector2(222,48);
                }
                foreach(var button in buttons)
                {
                    var light=button.transform.Find("KitTopLight") as RectTransform;
                    if(light!=null)light.sizeDelta=new Vector2(Mathf.Max(0,((RectTransform)button.transform).rect.width-16),light.sizeDelta.y);
                }
            }
        }
        private IEnumerator CloseWindow(RectTransform old)
        {
            var group = old.GetComponent<CanvasGroup>(); if(group == null) group=old.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false;
            foreach (var child in old.GetComponentsInChildren<Transform>()) child.name += "Closing";
            for (float t = 0; t < .2f && old != null; t += Time.unscaledDeltaTime) { group.alpha = 1 - t / .2f; yield return null; }
            if (old != null) Destroy(old.gameObject);
        }
        private IEnumerator ScreenWipe(RectTransform target)
        {
            PhasePresentationRunning=true;
            yield return null;
            if (target == null) {PhasePresentationRunning=false;yield break;}
            string kind=homeVisible||State==null||recordsActive||DailyPracticeActive?"screen":State.phase==OpsPhase.Planning?"month":State.phase==OpsPhase.Incident?"incident":"screen";
            bool skipAllowed=presentationVisits.ContainsKey("transition_"+kind);
            RepeatDuration("transition_"+kind,1,1);
            phasePresentationSkipped=false;PhasePresentationCanSkip=skipAllowed;
            PlayPresentationCue(OpsCue.Transition);
            // 月替わり・事件は専用の入口だけを使い、通常の帯を重ねて待たせない。
            if(kind=="screen")
            {
                var cover = IncidentShape(Surface, "ScreenWipe", "cutin", -1900, 0, 1900, 900, PlanPink);
                var group = cover.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false;
                for (float t = 0; t < .3f; t += Time.unscaledDeltaTime*PresentationRate)
                {
                    if (ReducedMotion) group.alpha = Mathf.Sin(t / .3f * Mathf.PI) * .25f;
                    else cover.anchoredPosition = new Vector2(Mathf.Lerp(-1900, 1700, t / .3f), 0);
                    if(t>=.15f&&outgoingScreen!=null){Destroy(outgoingScreen.gameObject);outgoingScreen=null;}
                    if(PresentationPressed())presentationInputGuardUntil=Time.unscaledTime+.35f;
                    if(t>=.15f&&skipAllowed&&(phasePresentationSkipped||PresentationPressed()))break;
                    yield return null;
                }
                if (cover != null) Destroy(cover.gameObject);
            }
            if(kind=="month"||kind=="incident")
            {
                var boss=kind=="incident"?State.CurrentBoss:null;
                var overlay=Box(Surface,"PhasePresentation",0,0,1600,900,kind=="month"?Color.black:new Color(.76f,.07f,.2f,.22f));
                var alpha=overlay.gameObject.AddComponent<CanvasGroup>();alpha.blocksRaycasts=boss!=null;
                if(boss!=null)overlay.GetComponent<Image>().raycastTarget=true;
                RectTransform page=null;CanvasGroup pageAlpha=null;
                if(kind=="month")
                {
                    page=PCard(overlay,"CalendarPage",610,255,380,330,Color.white,28);
                    pageAlpha=page.gameObject.AddComponent<CanvasGroup>();pageAlpha.alpha=0;pageAlpha.blocksRaycasts=false;
                    PCard(page,"CalendarHeader",0,0,380,56,PlanPink,28,false);
                    PText(page,"CalendarMonth",State.Current.name,0,68,380,126,90,PlanInk,true,true);
                    string[] seasons={"新年度の準備","連休後の点検","雨の季節","夏の備え","夏休みの当番","上期の締め","下期スタート","年末への備え","年末の繁忙期","年始の確認","年度末の準備","一年の総仕上げ"};
                    PText(page,"CalendarSeason",seasons[State.month],20,224,340,50,22,PlanGray,true,true);
                }
                else if(boss!=null){DrawBossEntry(overlay,boss);PlayPresentationCue(OpsCue.Alert);}
                else
                {
                    IncidentShape(overlay,"IncidentEntryTapeTop","tape",0,280,1600,28,Hex("ffd23f"));
                    IncidentShape(overlay,"IncidentEntryTapeBottom","tape",0,590,1600,28,Hex("ffd23f"));
                    PText(overlay,"IncidentEntryTitle","緊急",0,360,1600,160,110,Color.white,true,true);
                    PlayPresentationCue(OpsCue.Alert);
                }
                float duration=kind=="month"?1.5f:boss!=null?OpsCatalog.BossAppearSeconds:.8f;
                for(float t=0;t<duration&&!phasePresentationSkipped;t+=Time.unscaledDeltaTime*PresentationRate)
                {
                    alpha.alpha=Mathf.Min(t/(kind=="month"?.5f:.12f),(duration-t)/.18f,1);
                    if(outgoingScreen!=null&&t>=(kind=="month"?.5f:.12f)){Destroy(outgoingScreen.gameObject);outgoingScreen=null;}
                    if(pageAlpha!=null)pageAlpha.alpha=Mathf.Clamp01((t-.5f)/.12f);
                    if(page!=null&&!ReducedMotion)page.localRotation=Quaternion.Euler(Mathf.Lerp(65,0,Mathf.Clamp01((t-.5f)/.2f)),0,0);
                    if(PresentationPressed())presentationInputGuardUntil=Time.unscaledTime+.35f;
                    if(skipAllowed&&PresentationPressed())break;
                    yield return null;
                }
                if(overlay!=null)Destroy(overlay.gameObject);
            }
            if(outgoingScreen!=null){Destroy(outgoingScreen.gameObject);outgoingScreen=null;}
            PhasePresentationCanSkip=false;
            PhasePresentationRunning=false;
        }
        private RectTransform outgoingScreen;
        private bool phasePresentationSkipped;
        public bool PhasePresentationCanSkip {get;private set;}
        public bool PhasePresentationRunning {get;private set;}
        private float presentationInputGuardUntil;
        private bool ConsumePresentationClick()
        {
            bool actualInput=UnityEngine.InputSystem.Mouse.current?.leftButton.wasReleasedThisFrame==true||UnityEngine.InputSystem.Keyboard.current?.enterKey.wasPressedThisFrame==true||UnityEngine.InputSystem.Keyboard.current?.spaceKey.wasPressedThisFrame==true;
            if(!actualInput||!PhasePresentationRunning&&Time.unscaledTime>=presentationInputGuardUntil)return false;
            SkipPhasePresentation();return true;
        }
        public void SkipPhasePresentation(){if(PhasePresentationCanSkip)phasePresentationSkipped=true;}
        public bool AnnualPresentationCanSkip {get;private set;}
        public bool AnnualPresentationSkipped {get;private set;}
        public void SkipAnnualPresentation(){if(AnnualPresentationCanSkip)AnnualPresentationSkipped=true;}
        private static bool PresentationPressed()=>UnityEngine.InputSystem.Mouse.current?.leftButton.wasPressedThisFrame==true||UnityEngine.InputSystem.Keyboard.current?.spaceKey.wasPressedThisFrame==true;
    }
}
