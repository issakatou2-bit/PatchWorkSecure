using System;

namespace PatchWorkSecure.CompanyOps
{
    [Serializable] public sealed class OpsYearEquipment
    { public string id,name,icon,description; }
    [Serializable] public sealed class OpsYearRival
    { public string id,name,hint,identity,profile; public string[] equipment; public int month,stars,shape; }
    [Serializable] public sealed class OpsYearDefinition
    {
        public int year,employees,devices,branches,partners;
        public string theme,growthHeading,location,cry,unlockNote,rivalNote;
        public OpsYearEquipment[] equipment;
        public OpsYearRival[] rivals;
        public string[] allyNames,allyDescriptions;
    }
    public static partial class OpsCatalog
    {
        public static readonly float[] YearOpeningSeconds={2.4f,2.2f,2.4f,2.2f,2.6f};
        public const float BossAppearSeconds=1.5f;
        public static readonly OpsYearDefinition[] StoryCompanies={
            new OpsYearDefinition{year=1,employees=24,devices=30,branches=1,partners=1},
            new OpsYearDefinition{year=2,employees=38,devices=56,branches=2,partners=1,theme="広がる会社",growthHeading="会社が広がった",location="横浜拠点",cry="迎え撃て！",
                unlockNote="広がった分だけ、守り方も増える",rivalNote="正体は、山場の月に明らかになる",
                equipment=new[]{
                    new OpsYearEquipment{id="zeroTrust",name="ゼロトラスト接続",icon="ゼ",description="場所で信じず、毎回確かめる。\n在宅と拠点の入口を、1つの仕組みで守る"},
                    new OpsYearEquipment{id="edr",name="端末の検知と対応（EDR）",icon="E",description="怪しい動きを端末で見つけて、\nその場で切り離せる"}},
                rivals=new[]{
                    new OpsYearRival{id="y2-supply",month=JunePeak,stars=3,shape=0,name="委託先から忍び寄る影",hint="信頼している相手の入口から来る",identity="保守会社の使われていないはずのIDに兆候。\n信頼している相手の入口を確かめる。",profile="supply",equipment=new[]{"zeroTrust","inventory"}},
                    new OpsYearRival{id="y2-ai",month=SeptemberPeak,stars=3,shape=1,name="AIで化ける詐欺師",hint="声も文面も、本物そっくり",identity="声も文面も本物そっくりの依頼。\n既知の連絡経路で確かめる。",profile="bec",equipment=new[]{"education","runbook"}},
                    new OpsYearRival{id="y2-ransom",month=DecemberPeak,stars=4,shape=2,name="暗号化の群れ",hint="拠点をまたいで、一気に広がる",identity="複数の端末で暗号化らしい兆候。\n端末と拠点の広がりを確かめる。",profile="ransom",equipment=new[]{"edr","segment","backup"}}},
                allyNames=new[]{"エンジニアさん","ひなた","かのん"},allyDescriptions=new[]{"週2日、常駐してくれることに。\n月に1回、調査を頼める","情報セキュリティマネジメント試験に合格！\n拠点の担当を任された","予算の交渉に、味方してくれる。\n山場の月は臨時予算が通りやすい"}},
            new OpsYearDefinition{year=3,employees=52,devices=80,branches=2,partners=4,theme="狙われる会社",growthHeading="取引先とつながった",location="取引先連携",cry="守り抜け！",
                unlockNote="広がった分だけ、守り方も増える",rivalNote="そして3月、まだ誰も見たことのない「？？？」が来る",
                equipment=new[]{
                    new OpsYearEquipment{id="threatSharing",name="脅威情報の共有",icon="情",description="業界の仲間と、攻撃の手口を\n先に知らせ合う"},
                    new OpsYearEquipment{id="csirt",name="事件対応の体制（CSIRT）",icon="C",description="いざという時、誰が何をするかを\n決めて、すぐ動ける"}},
                rivals=new[]{
                    new OpsYearRival{id="y3-bec",month=JunePeak,stars=3,shape=0,name="なりすます取引先",hint="大きな取引ほど、確認を装って来る",identity="取引先を名乗る送金依頼。\n既知の連絡先と承認記録で確かめる。",profile="bec",equipment=new[]{"threatSharing","education"}},
                    new OpsYearRival{id="y3-ai",month=SeptemberPeak,stars=4,shape=1,name="声をまとう偽役員",hint="映像も声も、本物そっくり",identity="役員の声や映像を使った急ぎの依頼。\n別の経路で本人と承認を確かめる。",profile="bec",equipment=new[]{"education","runbook"}},
                    new OpsYearRival{id="y3-targeted",month=DecemberPeak,stars=4,shape=2,name="拠点を渡る侵入者",hint="つながった分だけ、道も増える",identity="拠点をまたぐ不審な接続の兆候。\n入口と端末の記録をつなげて調べる。",profile="targeted",equipment=new[]{"edr","zeroTrust","threatSharing"}},
                    new OpsYearRival{id="y3-final",month=MarchPeak,stars=5,shape=2,name="3年目の総決算",hint="3年間の備えを試す",identity="複数の兆候と復旧の依頼が重なった。\n3年間の備えで対応を組み立てる。",profile="ransom",equipment=new[]{"csirt","backup","drill"}}},
                allyNames=new[]{"エンジニアさん","ひなた","かのん"},allyDescriptions=new[]{"週2日の常駐に慣れた。\n強敵の正体を、真っ先に見抜く","応用情報に合格！\n後輩の先輩になった","取引先の監査にも同席。\n説明の数字を一緒に作る"}}
        };
        public static OpsYearDefinition CompanyYear(int year)=>StoryCompanies[Math.Max(1,Math.Min(StoryYears,year))-1];
    }
}
