using System;
using MorePlatforms;

class ExtraDataTests
{
    static int count;
    static void Check(bool ok)
    {
        count++;
        if (!ok) throw new Exception("Teste falhou: " + count);
    }
    static void Reject(int[] data)
    {
        bool failed = false;
        try { ExtraData.Decode(data); }
        catch (FormatException) { failed = true; }
        Check(failed);
    }
    static void Main()
    {
        Check(ExtraData.Decode(new int[0]).Count == 0);
        Check(ExtraData.Decode(null).Count == 0);
        var records = ExtraData.Decode(new[] { 42, 4, 101, -1, 103, 104, 99, 1, 555 });
        Check(records.Count == 2);
        Check(records[42].Length == 4 && records[42][1] == -1 && records[42][3] == 104);
        Check(records[99][0] == 555);
        Reject(new[] { 1 });
        Reject(new[] { 1, -1 });
        Reject(new[] { 1, 13 });
        Reject(new[] { 1, 2, 5 });
        Reject(new[] { 1, 1, -2 });
        Reject(new[] { 1, 1, 5, 1, 1, 6 });
        var max = new int[14];
        max[0] = 12;
        max[1] = 12;
        Check(ExtraData.Decode(max)[12].Length == 12);
        Console.WriteLine(count + " testes de persistencia passaram.");
    }
}
