// See https://aka.ms/new-console-template for more information
using MARSEncoderApps.data;
using MARSEncoderApps.encoder;
using System.Runtime.CompilerServices;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("[BEGIN]\tTRY TO ENCODE MARS AGENT");

var argsWithIdx = args.Select((arg, idx)=>new { arg, idx});
var mode = argsWithIdx.FirstOrDefault(p=>(p!=null) && ("-m".Equals(p.arg, StringComparison.OrdinalIgnoreCase)));
if (mode == null)
{
    PrintUsage("Please ensure -m exist.");
    return;
}

if (mode.idx >= args.Length)
{
    PrintUsage("Please add command after -m.");
    return;
}

var cmd = args[mode.idx+1];
bool isOk = false;
if (cmd.Equals(EnCodeConstant.cnst_mode_EncodePwd))
{
    /// genearte base64 password
    /// 
    string base64key = Convert.ToBase64String(Encoding.UTF8.GetBytes("Jackliew@@@@752381"));
    Console.WriteLine(base64key);
    string strError = "";
    isOk = MarsKeyMgr.WriteKeyToFile(ref strError);
    if (!isOk)
    {
        Console.WriteLine($"[ERROR]\t|{strError}");
        return;
    }
    
    Console.WriteLine("Key has generated");
    //return;
}
string strKey = "", strIv = "";
isOk = MarsKeyMgr.ReadKeyFile(ref strKey,ref strIv);
if (!isOk)
{
    Console.WriteLine($"Can't read key file, or not right format, key and iv should be exists");
    return;
}

MarsEncryptor.EncrypFilesEntry(strKey, strIv);
Console.WriteLine("[END]\tENCODED IS DONE");

void PrintUsage(string strAttachedMessage="")
{
    Console.WriteLine($@"MARSEncoderApp -m {EnCodeConstant.cnst_mode_EncodePwd}, Or
MARSEncoderApp -m {EnCodeConstant.cnst_mode_EncodeBins}");
    Console.WriteLine("-----------------");
    Console.WriteLine(strAttachedMessage);
}