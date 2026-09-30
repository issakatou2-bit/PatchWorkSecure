using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private RectTransform blockBoard,blockTray,blockCheck,blockGhost,blockDanger;
        private int blockRevision=-1,draggedBlock=-1;private float blockCell;private Vector2 blockPointer;
        private readonly string[] blockDays={"月","火","水","木","金","土"};
        private void DrawBlockPresentation()
        {
            blockRevision=-1;draggedBlock=-1;minigameHeartbeatAt=-1;var game=(OpsBlockMinigame)Minigame;
            KitGradient(screen.GetComponent<Image>(),Hex("dfe4ff"),Hex("ffe3ec"));KitGradient(minigameCanvas.GetComponent<Image>(),Hex("dfe4ff"),Hex("ffe3ec"));
            DrawDecisionMinigameTop("作業のはめ込み",238,670,"配置","",Hex("6c63ff"));
            var mode=FindMinigameText("DecisionGood");mode.rectTransform.anchoredPosition=new Vector2(274,-12);mode.rectTransform.sizeDelta=new Vector2(112,46);mode.fontSize=14;
            var timer=(RectTransform)minigameCanvas.Find("MinigameTop/MinigameTimer");timer.anchoredPosition=new Vector2(390,-28);timer.sizeDelta=new Vector2(550,16);
            ((RectTransform)timer.Find("MinigameTimerFill")).sizeDelta=new Vector2(550,16);
            blockBoard=PCard(minigameCanvas,"BlockBoard",0,92,640,560,Hex("fffdf8"),22);DecisionPanel(blockBoard);
            KitGradient(blockBoard.GetComponent<Image>(),Hex("fffdf8"),Hex("fffdf8"));
            IncidentShape(blockBoard,"BlockStitch","dashed",9,9,622,542,new Color(.85f,.29f,.44f,.25f));
            blockCell=Mathf.Min(104,Mathf.Floor((640-92-24-(game.Columns-1)*6f)/game.Columns),Mathf.Floor((560-52-24-(game.Rows-1)*6f)/game.Rows));
            for(int c=0;c<game.Columns;c++)PText(blockBoard,"BlockDay"+c,blockDays[c],92+c*(blockCell+6),14,blockCell,28,18,null,true,true);
            string[] slots=game.Rows==4?new[]{"午前\n9〜10時半","午前\n10時半〜12時","午後\n13〜15時","午後\n15〜17時"}:new[]{"午前\n9〜12時","午後\n13〜15時","午後\n15〜17時"};
            for(int r=0;r<game.Rows;r++)PText(blockBoard,"BlockSlot"+r,slots[r],18,52+r*(blockCell+6),66,blockCell,12,PlanGray,false,true);
            for(int r=0;r<game.Rows;r++)for(int c=0;c<game.Columns;c++)
            {
                int row=r,col=c;string locked=game.Locks[r,c];
                var cell=PButton(blockBoard,"BlockCell_"+r+"_"+c,locked??"",92+c*(blockCell+6),52+r*(blockCell+6),blockCell,blockCell,()=>PlaceSelectedBlock(row,col),locked==null?Hex("f3eee4"):Hex("fff4d1"),Hex("9a6a00"),16,null,locked==null);
                foreach(var shadow in cell.GetComponents<Shadow>())shadow.enabled=false;
                if(locked==null){KitGradient(cell.GetComponent<Image>(),Hex("f3eee4"),Hex("f3eee4"));IncidentShape(cell.transform,"BlockCellEdge","dashed",0,0,blockCell,blockCell,Hex("e3d6c2"));}
                else
                {
                    var stripes=Rect(cell.transform,"BlockLockStripes",0,0,blockCell,blockCell);stripes.SetAsFirstSibling();
                    var stripe=stripes.gameObject.AddComponent<OpsBlockStripe>();stripe.color=Hex("ffe8a8");stripe.raycastTarget=false;
                    KitGradient(cell.GetComponent<Image>(),Hex("fff4d1"),Hex("fff4d1"));
                    var palette=cell.colors;palette.disabledColor=Color.white;cell.colors=palette;
                    var edge=cell.gameObject.AddComponent<Outline>();edge.effectColor=Hex("f2c14e");edge.effectDistance=new Vector2(2,-2);
                }
                var hover=cell.gameObject.AddComponent<OpsBlockPointer>();hover.Owner=this;hover.Row=r;hover.Column=c;
            }
            if(game.Rows==4)IncidentShape(blockBoard,"BlockNoon","dashed",84,52+2*blockCell+5,532,2,Hex("e3c9a8"));
            blockTray=PCard(minigameCanvas,"BlockTray",660,92,620,430,Color.white,22);DecisionPanel(blockTray);
            PText(blockTray,"BlockTrayHeading","今週の作業",16,14,588,26,17);
            PText(blockTray,"BlockInstructions","ドラッグして予定表へ（選んでからマスを押しても置ける）。\nRキー・右クリック・ホイール・ボタンで回転。置いた作業も動かせる。",16,42,588,44,12,PlanGray,false);
            var side=PCard(minigameCanvas,"MinigameSide",660,536,620,210,Color.white,22);DecisionPanel(side);
            var speech=PCard(side,"MinigameSpeech",16,14,300,58,Color.white,16,false);
            var outline=speech.gameObject.AddComponent<Outline>();outline.effectColor=Hex("ffd3de");outline.effectDistance=new Vector2(2,-2);
            PText(speech,"NavigatorSpeech",CaptionsEnabled?"予定のすき間に、作業をはめよう！":"",12,8,276,42,15,null,false);
            blockCheck=Rect(side,"BlockChecks",16,84,330,112);
            Portrait(side,"NavigatorPortrait",264,4,186,200,"pose_think");
            PButton(side,"BlockFinish","この予定で決定",454,136,150,58,FinishBlocks,Hex("6c63ff"),Color.white,16,Hex("4a42c9"));
            blockDanger=MinigameShape(minigameCanvas,"MinigameDanger","danger-edge",0,0,1280,760,new Color(.88f,.25f,.37f,.45f));MinigameVisual(blockDanger,"danger",.8f);blockDanger.gameObject.SetActive(false);
            RefreshBlockPresentation();
        }
        private string BlockRule(OpsBlockTask task)=>task.Deadline>=0?"<b>"+blockDays[task.Deadline]+"曜まで</b>"+(task.Urgent?"・悪用を確認":""):
            task.Avoid>=0?"<b>"+blockDays[task.Avoid]+"曜は避ける</b>":task.Cells.Length==1?"いつでも":"今週中に";
        private void RefreshBlockPresentation()
        {
            var game=Minigame as OpsBlockMinigame;if(game==null)return;
            FindMinigameText("DecisionGood").text=game.Columns+"日×"+game.Rows+"コマ";
            FindMinigameText("DecisionMiss").text="配置 "+game.PlacedCount+" / "+game.Tasks.Count;FindMinigameText("DecisionFalse").text="";
            if(blockRevision!=game.Revision)
            {
                blockRevision=game.Revision;
                foreach(Transform child in blockBoard)if(child.name.StartsWith("BlockPlaced")){child.gameObject.SetActive(false);Destroy(child.gameObject);}
                foreach(Transform child in blockTray)if(child.name.StartsWith("BlockTask")){child.gameObject.SetActive(false);Destroy(child.gameObject);}
                foreach(Transform child in blockCheck){child.gameObject.SetActive(false);Destroy(child.gameObject);}
                foreach(var task in game.Tasks)
                {
                    int id=task.Id;
                    var tile=PCard(blockTray,"BlockTask_"+id,16+id%4*147,94+id/4*132,139,124,Hex("f3f6fb"),16,false);
                    var pointer=tile.gameObject.AddComponent<OpsBlockPointer>();pointer.Owner=this;pointer.Task=id;tile.GetComponent<Image>().raycastTarget=true;
                    if(game.Selected==id){var outline=tile.gameObject.AddComponent<Outline>();outline.effectColor=Hex("ffc02e");outline.effectDistance=new Vector2(3,-3);}
                    var group=tile.gameObject.AddComponent<CanvasGroup>();group.alpha=task.Placed==null?1:.3f;
                    PText(tile,"BlockTaskName",task.Name+(task.Auto?" / 自動":""),10,8,119,46,13,null,false);
                    PText(tile,"BlockTaskRule",BlockRule(task),10,54,119,28,11,PlanGray,false);
                    DrawBlockShape(tile,"BlockMini",task.Cells,10,2,task.Color,88,84,false);
                    if(task.Placed==null){var rotate=PButton(tile,"BlockRotate_"+id,"向きを変える",8,90,78,26,()=>RotateBlock(id),Color.white,PlanInk,12);rotate.GetComponentInChildren<TMPro.TextMeshProUGUI>().fontSize=11;}
                    if(task.Placed!=null)
                    {
                        int r=task.Placed.Min(c=>c.Row),c=task.Placed.Min(x=>x.Column);
                        var piece=Rect(blockBoard,"BlockPlaced_"+id,92+c*(blockCell+6),52+r*(blockCell+6),blockCell*(task.Cells.Max(x=>x.Column)+1)+6*task.Cells.Max(x=>x.Column),blockCell*(task.Cells.Max(x=>x.Row)+1)+6*task.Cells.Max(x=>x.Row));
                        DrawBlockShape(piece,"BlockPiece",task.Cells,blockCell,6,task.Color,0,0,true,id,task.Violates);
                        PText(piece,"BlockPlacedName",task.Name.Split('（')[0],8,8,piece.rect.width-16,54,13,Color.white);
                    }
                    var check=PCard(blockCheck,"BlockCheckCard_"+id,id%2*165,id/2*25,160,23,Hex(task.Violates?"ffe1e6":task.Placed!=null?"e3f7ea":"f3f6fb"),8,false);
                    PText(check,"BlockCheck_"+id,(task.Placed==null?"・ ":task.Violates?"！ ":"○ ")+task.Name, 5,0,150,23,11,task.Violates?Hex("e0405f"):PlanInk,false);
                }
            }
            RefreshDecisionDanger(blockDanger);
        }
        private void DrawBlockShape(Transform parent,string name,OpsBlockCell[] shape,float cell,float gap,string color,float x,float y,bool interactive,int task=-1,bool violated=false)
        {
            foreach(var p in shape)
            {
                var part=cell<30?Box(parent,name,x+p.Column*(cell+gap),y+p.Row*(cell+gap),cell,cell,Hex(color)):PCard(parent,name,x+p.Column*(cell+gap),y+p.Row*(cell+gap),cell,cell,Hex(color),16,false);
                if(cell>=30)MinigameGloss(part,cell,cell);
                if(violated){var rect=Rect(part,"BlockWarning",0,0,cell,cell);var stripe=rect.gameObject.AddComponent<OpsBlockStripe>();stripe.color=new Color(1,1,1,.25f);stripe.raycastTarget=false;}
                if(interactive){part.GetComponent<Image>().raycastTarget=true;var pointer=part.gameObject.AddComponent<OpsBlockPointer>();pointer.Owner=this;pointer.Task=task;pointer.Placed=true;}
            }
        }
        public void SelectBlock(int id){var game=Minigame as OpsBlockMinigame;if(game!=null&&game.Select(id))RefreshBlockPresentation();}
        public void RotateBlock(int id)
        {
            var game=Minigame as OpsBlockMinigame;if(game==null||!game.Rotate(id))return;MinigameTone(760,"triangle");RefreshBlockPresentation();if(draggedBlock>=0)MoveBlockDrag(blockPointer);
        }
        public void PlaceSelectedBlock(int row,int column)
        {
            var game=Minigame as OpsBlockMinigame;if(game==null||game.Selected<0)return;
            int selected=game.Selected;bool placed=game.Place(selected,row,column);
            if(placed)
            {
                MinigameTone(640);MinigamePop(new Vector2(240,350),"ぴったり！",Hex("6c63ff"));
                foreach(int day in game.NewlyFull){MinigameCutBurst(new Vector2(92+day*(blockCell+6)+blockCell/2,350));MinigamePop(new Vector2(92+day*(blockCell+6),210),blockDays[day]+"曜、すき間なし！",Hex("ffc02e"));StartCoroutine(MinigameChord());}
            }
            else{MinigameTone(200,"square");MinigameVisual(blockBoard,"shake",.35f);SpeakSceneLine("mg_miss_01",0);}
            RefreshBlockPresentation();
            if(placed)
            {
                var shape=blockBoard.GetComponentsInChildren<RectTransform>().FirstOrDefault(r=>r.name=="BlockPlaced_"+selected&&r.gameObject.activeSelf);
                if(shape!=null)MinigameVisual(shape,"hit",.09f);
            }
        }
        public void BeginBlockDrag(int id,bool placed,Vector2 position)
        {
            var game=Minigame as OpsBlockMinigame;if(game==null||game.Phase!=OpsMinigamePhase.Playing)return;
            if(placed)game.Lift(id);else game.Select(id);draggedBlock=id;RefreshBlockPresentation();MoveBlockDrag(position);
        }
        public void MoveBlockDrag(Vector2 position)
        {
            var game=Minigame as OpsBlockMinigame;if(game==null||draggedBlock<0)return;blockPointer=position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(minigameCanvas,position,null,out var local);
            if(blockGhost!=null){blockGhost.gameObject.SetActive(false);Destroy(blockGhost.gameObject);}
            blockGhost=Rect(minigameCanvas,"BlockGhost",local.x,-local.y,500,500);blockGhost.gameObject.AddComponent<CanvasGroup>().blocksRaycasts=false;
            var task=game.Tasks.First(t=>t.Id==draggedBlock);DrawBlockShape(blockGhost,"BlockGhostCell",task.Cells,blockCell,6,task.Color,0,0,false);
            int col=Mathf.FloorToInt((local.x-92)/(blockCell+6)),row=Mathf.FloorToInt((-local.y-144)/(blockCell+6));
            foreach(Transform child in blockBoard)if(child.name.StartsWith("BlockCell_"))
            {
                var parts=child.name.Split('_');int r=int.Parse(parts[1]),c=int.Parse(parts[2]);if(game.Locks[r,c]!=null)continue;
                bool covered=task.Cells.Any(p=>p.Row+row==r&&p.Column+col==c);
                child.GetComponent<Image>().color=covered?(game.Fits(task.Id,row,col)?Hex("d5f5e3"):Hex("ffe1e6")):Hex("f3eee4");
            }
        }
        public void EndBlockDrag(Vector2 position)
        {
            if(draggedBlock<0)return;RectTransformUtility.ScreenPointToLocalPointInRectangle(minigameCanvas,position,null,out var local);
            PlaceSelectedBlock(Mathf.FloorToInt((-local.y-144)/(blockCell+6)),Mathf.FloorToInt((local.x-92)/(blockCell+6)));
            draggedBlock=-1;if(blockGhost!=null){blockGhost.gameObject.SetActive(false);Destroy(blockGhost.gameObject);}blockGhost=null;
            var game=(OpsBlockMinigame)Minigame;
            foreach(Transform child in blockBoard)if(child.name.StartsWith("BlockCell_")){var parts=child.name.Split('_');if(game.Locks[int.Parse(parts[1]),int.Parse(parts[2])]==null)child.GetComponent<Image>().color=Color.white;}
        }
        private void TickBlockKeys()
        {
            var game=Minigame as OpsBlockMinigame;if(game==null||game.Phase!=OpsMinigamePhase.Playing)return;
            var key=Keyboard.current;var mouse=Mouse.current;
            // 配置変更でタイルを描き直しても、ドラッグの終点を失わない。
            if(draggedBlock>=0)
            {
                var touch=Touchscreen.current?.primaryTouch;
                if(touch!=null&&touch.press.isPressed)MoveBlockDrag(touch.position.ReadValue());
                else if(touch!=null&&touch.press.wasReleasedThisFrame)EndBlockDrag(touch.position.ReadValue());
                else if(mouse!=null){if(mouse.leftButton.wasReleasedThisFrame)EndBlockDrag(mouse.position.ReadValue());else if(mouse.leftButton.isPressed)MoveBlockDrag(mouse.position.ReadValue());}
            }
            if(key!=null&&key.rKey.wasPressedThisFrame||mouse!=null&&(mouse.rightButton.wasPressedThisFrame||Mathf.Abs(mouse.scroll.ReadValue().y)>0))
            {int id=draggedBlock>=0?draggedBlock:game.Selected;if(id>=0)RotateBlock(id);}
        }
        public void FinishBlocks()
        {
            var game=Minigame as OpsBlockMinigame;if(game==null||!game.Finish())return;
            draggedBlock=-1;if(blockGhost!=null)Destroy(blockGhost.gameObject);blockGhost=null;
            minigameResultAt=Time.unscaledTime+1.1f;ShowMinigameFinish();
        }
    }
    public sealed class OpsBlockPointer:MonoBehaviour,IPointerClickHandler,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        public OpsGame Owner;public int Task=-1,Row=-1,Column=-1;public bool Placed;
        public void OnPointerClick(PointerEventData e){if(Task<0)return;if(e.button==PointerEventData.InputButton.Right)Owner.RotateBlock(Task);else Owner.SelectBlock(Task);}
        public void OnBeginDrag(PointerEventData e){if(Task>=0)Owner.BeginBlockDrag(Task,Placed,e.position);}
        public void OnDrag(PointerEventData e){if(Task>=0)Owner.MoveBlockDrag(e.position);}
        public void OnEndDrag(PointerEventData e){if(Task>=0)Owner.EndBlockDrag(e.position);}
    }
}
