// 元音源・設定を変更せず、終端6秒→先頭6秒の試聴WAVと音量を記録する。
foreach(var name in new[]{"planning","incident"})
{
    string track=name;
    string path=System.IO.Path.GetFullPath("ArtSource/CompanyYear/music-20260928/"+track+"-original.mp3");
    var request=UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(new System.Uri(path).AbsoluteUri,UnityEngine.AudioType.MPEG);
    var pending=request.SendWebRequest();
    pending.completed+=operation=>
    {
        if(request.result!=UnityEngine.Networking.UnityWebRequest.Result.Success) {UnityEngine.Debug.LogError("Loop preview: "+request.error);request.Dispose();return;}
        var clip=UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(request);
        var samples=new float[clip.samples*clip.channels];
        if(!clip.GetData(samples,0))throw new System.InvalidOperationException("BGMのPCMを読み出せません。");
        int part=6*clip.frequency*clip.channels;
        string file="Artifacts/"+track+"-loop-junction.wav";
        using(var writer=new System.IO.BinaryWriter(System.IO.File.Create(file)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+part*4);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16);writer.Write((short)1);writer.Write((short)clip.channels);writer.Write(clip.frequency);writer.Write(clip.frequency*clip.channels*2);
            writer.Write((short)(clip.channels*2));writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(part*4);
            for(int i=samples.Length-part;i<samples.Length;i++)writer.Write((short)UnityEngine.Mathf.Clamp(samples[i]*32767,-32768,32767));
            for(int i=0;i<part;i++)writer.Write((short)UnityEngine.Mathf.Clamp(samples[i]*32767,-32768,32767));
        }
        var report=new System.Text.StringBuilder(track+" / "+clip.length+"s\n");
        int window=clip.frequency*clip.channels;
        for(int second=0;second<6;second++)
        {
            double head=0,tail=0;
            for(int i=0;i<window;i++){head+=samples[second*window+i]*samples[second*window+i];int end=samples.Length-(6-second)*window+i;tail+=samples[end]*samples[end];}
            report.AppendLine("head "+second+"s "+(20*System.Math.Log10(System.Math.Max(1e-9,System.Math.Sqrt(head/window)))).ToString("F1")+"dB / tail "+second+"s "+(20*System.Math.Log10(System.Math.Max(1e-9,System.Math.Sqrt(tail/window)))).ToString("F1")+"dB");
        }
        System.IO.File.WriteAllText("Artifacts/"+track+"-loop-check.txt",report.ToString());
        UnityEngine.Object.DestroyImmediate(clip);request.Dispose();UnityEngine.Debug.Log("[BGM loop] "+report);
    };
}
