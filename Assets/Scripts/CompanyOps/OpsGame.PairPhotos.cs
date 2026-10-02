using UnityEngine;
using UnityEngine.UI;

namespace PatchWorkSecure.CompanyOps
{
    public partial class OpsGame
    {
        private RectTransform meetingPhoto,meetingPhotoShadow;
        // 原画の顔を残す16:9の窓。縦長のDiaryPhotoと混ぜず、画像を引き伸ばさない。
        private RectTransform PairPolaroid(Transform parent,string name,Sprite art,float x,float y,string caption,float angle)
        {
            var shadow=PImage(parent,name+"Shadow",PlanningArt.shadow,x-24,y-16,368,268,Color.white,true);shadow.localEulerAngles=new Vector3(0,0,-angle);
            var photo=PImage(parent,name+"Polaroid",null,x,y,320,220,Color.white);photo.localEulerAngles=new Vector3(0,0,-angle);
            float height=296*art.rect.height/art.rect.width;
            var inner=PImage(photo,name+"Photo",art,12,12,296,height);inner.GetComponent<Image>().preserveAspect=true;
            DiaryText(photo,name+"Caption",caption,0,184,320,28,17,true);
            return photo;
        }
        private void UpdateMeetingPhoto()
        {
            bool visible=State!=null&&RunYear==3&&State.month==1&&State.phase==OpsPhase.Planning&&
                !YearOpeningActive&&!DiaryActive&&!MinigameActive&&(LastReactionId=="kanon_meeting"||LastReactionId=="eng_forgot");
            if(!visible){ClearMeetingPhoto();return;}
            if(meetingPhoto!=null||PlanningArt.pairKanonEngineer==null||screen.Find("Navigator")==null)return;
            meetingPhoto=PairPolaroid(screen,"MeetingPair",PlanningArt.pairKanonEngineer,780,365,"5分前",2);
            meetingPhotoShadow=screen.Find("MeetingPairShadow") as RectTransform;
            // 写真は吹き出しの後ろ。顔は写真の上半分にあり、名札・本文とは重ならない。
            var bubble=screen.Find("Navigator");meetingPhotoShadow.SetSiblingIndex(bubble.GetSiblingIndex());meetingPhoto.SetSiblingIndex(bubble.GetSiblingIndex());
        }
        private void ClearMeetingPhoto()
        {
            foreach(var r in new[]{meetingPhoto,meetingPhotoShadow})if(r!=null){r.gameObject.SetActive(false);if(Application.isPlaying)Destroy(r.gameObject);else DestroyImmediate(r.gameObject);}
            meetingPhoto=meetingPhotoShadow=null;
        }
    }
}
