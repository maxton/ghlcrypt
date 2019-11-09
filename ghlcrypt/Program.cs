using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GHLCrypt
{
  class Program
  {
    static void Main(string[] args)
    {
      Verb.Run(Verbs, args, AppDomain.CurrentDomain.FriendlyName);
    }

    public static Verb[] Verbs = new[]
    {
      Verb.Create(
        "encrypt",
        "Encrypts a given file. Selects a key using the track ID or key index. Uses the FAR key if both are unspecified.",
        ArgDef.Multi(ArgDef.Bool("network_salt"), ArgDef.Option("track_id", "key_name", "index"), "input_file.wem", "output_file.wem"),
        (switches, optional, args) => DoCrypt(true, switches, optional, args)),
      Verb.Create(
        "decrypt",
        "Decrypts a given file. Selects a key using the track ID or key name. Uses the FAR key if both are unspecified.",
        ArgDef.Multi(ArgDef.Bool("network_salt"), ArgDef.Option("track_id", "key_name", "index"), "input_file.wem", "output_file.wem"),
        (switches, optional, args) => DoCrypt(false, switches, optional, args)),
    };

    static byte[] FSGCHeader = new byte[]
    {
      (byte)'F', (byte)'S', (byte)'G', (byte)'C',
      5, 0, 0, 0
    };

    static void DoCrypt(bool encrypt, Dictionary<string,bool> switches, Dictionary<string, string> optional, string[] args)
    {
      Tuple<byte[], byte[]> keys;
      var trackId = optional["track_id"];
      var keyName = optional["key_name"]
        ?? (trackId == null ? "t0" : Keys.GetKeyName("GHL", trackId));

      if(!Keys.keys.ContainsKey(keyName))
      {
        Console.WriteLine("Error: Don't have the key named "+keyName);
        return;
      }
      keys = Keys.GetKeysByName(keyName);
      if (switches["network_salt"])
      {
        Keys.ApplyNetworkSalt(Keys.UnkNetworkSalt, int.Parse(optional["index"] ?? "0"), keys);
      }
      else
      {
        Keys.ApplyMask(keys, Keys.mask);
      }

      using (var i = File.OpenRead(args[1]))
      using (var o = File.Create(args[2]))
      {
        var cryptStream = new AesCtrStream(i, keys.Item1, keys.Item2, encrypt ? 0 : 8);
        if(encrypt)
        {
          o.Write(FSGCHeader, 0, 8);
        }
        cryptStream.CopyTo(o);
      }
    }
  }
}
