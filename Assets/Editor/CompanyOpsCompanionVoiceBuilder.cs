using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PatchWorkSecure.CompanyOps;
using UnityEditor;
using UnityEngine;

namespace PatchWorkSecure.EditorTools
{
    public static class CompanyOpsCompanionVoiceBuilder
    {
        [MenuItem("PatchWorkSecure/新しい試作/仲間の台本と音声を更新")]
        public static void UpdateVoices()
        {
            const string folder="Assets/Audio/CompanyYear/Voice/Resources";
            if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Audio/CompanyYear/Voice","Resources");
            const string path=folder+"/CompanionVoices.asset";
            var lines=new List<OpsReactionLine>();
            foreach(string row in File.ReadAllLines("Docs/Voice/kanon-engineer-script.csv",Encoding.UTF8).Skip(1))
            {
                if(string.IsNullOrWhiteSpace(row))continue;
                var cells=Cells(row);
                if(cells.Count!=5||cells[1]!="かのん"&&cells[1]!="エンジニア")throw new FormatException("仲間の台本の形式が不正です");
                string voice="Assets/Audio/CompanyYear/Voice/"+(cells[1]=="かのん"?"Kanon":"Engineer")+"/"+cells[0]+".wav";
                lines.Add(new OpsReactionLine{id=cells[0],speaker=cells[1],scene=cells[2],caption=cells[3],fullSpeech=true,clip=AssetDatabase.LoadAssetAtPath<AudioClip>(voice)});
            }
            if(lines.Count!=24||lines.Select(l=>l.id).Distinct().Count()!=24)throw new FormatException("仲間の台本は重複のない24本が必要です");
            var bank=AssetDatabase.LoadAssetAtPath<OpsReactionBank>(path);
            if(bank==null){bank=ScriptableObject.CreateInstance<OpsReactionBank>();AssetDatabase.CreateAsset(bank,path);}
            bank.lines=lines.ToArray();EditorUtility.SetDirty(bank);AssetDatabase.SaveAssetIfDirty(bank);
            Debug.Log("[CompanyOps] 仲間の台本24行 / 音声 "+lines.Count(l=>l.clip!=null)+"本");
        }
        private static List<string> Cells(string row)
        {
            var result=new List<string>();var cell=new StringBuilder();bool quoted=false;
            for(int i=0;i<row.Length;i++)
            {
                char c=row[i];
                if(c=='"'){if(quoted&&i+1<row.Length&&row[i+1]=='"'){cell.Append('"');i++;}else quoted=!quoted;}
                else if(c==','&&!quoted){result.Add(cell.ToString());cell.Clear();}else cell.Append(c);
            }
            if(quoted)throw new FormatException("台本の引用符が閉じていません");
            result.Add(cell.ToString());return result;
        }
    }
}
