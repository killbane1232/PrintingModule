using PrintingModule.EDS;
using System.Text;

if (!Directory.Exists("out"))
    Directory.CreateDirectory("out");

EDS eds;

if (File.Exists($"./eds.config"))
{
    using var reader = new StreamReader($"./eds.config");

    BigInteger p = new(reader.ReadLine(), 16);
    BigInteger a = new(reader.ReadLine(), 10);
    BigInteger b = new(reader.ReadLine(), 16);
    byte[] xG = EDS.FromHexStringToByte(reader.ReadLine()!);
    BigInteger n = new(reader.ReadLine(), 16);

    eds = new EDS(p, a, b, n, xG);
}
else
    return;

if (Directory.Exists("in"))
{
    foreach (var file in Directory.EnumerateFiles("in"))
    {
        var text = File.ReadAllLines(file);
        var allBytesStr = new StringBuilder();
        for (var i = 0; i < text.Length; i++)
        {
            var str = text[i];
            allBytesStr.AppendLine(str);
        }
        Stribog stribog = new(Stribog.Mode.m512);
        var privateKey = eds.GenPrivateKey(192);
        var publicKey = eds.GenPublicKey(privateKey);

        var sign = eds.GenDS(stribog.GetHash(allBytesStr.ToString().Select(x => (byte)x).ToArray()), privateKey);
        var newStr = new StringBuilder();
        newStr.AppendLine(sign);
        newStr.AppendLine(publicKey.ToHexString());
        foreach (var line in text)
        {
            newStr.AppendLine(line);
        }
        var info = new FileInfo(file);
        using var writer = new StreamWriter($"./out/{info.Name}");
        writer.Write(newStr);
        writer.Close();
    }
}