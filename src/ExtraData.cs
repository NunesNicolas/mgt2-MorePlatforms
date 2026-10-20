using System;
using System.Collections.Generic;

namespace MorePlatforms
{
    internal static class ExtraData
    {
        internal static Dictionary<int, int[]> Decode(int[] data)
        {
            var result = new Dictionary<int, int[]>();
            if (data == null) return result;
            for (int offset = 0; offset < data.Length;)
            {
                if (data.Length - offset < 2) throw new FormatException("Registro de plataformas incompleto.");
                int id = data[offset++];
                int count = data[offset++];
                if (count < 1 || count > 12 || count > data.Length - offset || result.ContainsKey(id))
                    throw new FormatException("Registro de plataformas invalido.");
                var ids = new int[count];
                Array.Copy(data, offset, ids, 0, count);
                for (int i = 0; i < count; i++)
                    if (ids[i] < -1) throw new FormatException("ID de plataforma invalido.");
                result.Add(id, ids);
                offset += count;
            }
            return result;
        }
    }
}
