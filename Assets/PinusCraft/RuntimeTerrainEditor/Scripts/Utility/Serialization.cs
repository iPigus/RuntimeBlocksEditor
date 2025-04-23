using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;

namespace RuntimeTerrainEditor
{
    public static class Serialization
    {
        public static byte[] SerializeCompressed<T>(T value)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, value);

                using (MemoryStream input = new MemoryStream(ms.ToArray()))
                using (MemoryStream output = new MemoryStream())
                {
                    using (GZipStream compression = new GZipStream(output, CompressionMode.Compress))
                    {
                        input.CopyTo(compression);
                    }

                    return output.ToArray();
                }
            }
        }

        public static T DeserializeCompressed<T>(byte[] bytes)
        {
            using (var inStream = new MemoryStream(bytes))
            using (var zipStream = new GZipStream(inStream, CompressionMode.Decompress))
            using (var outStream = new MemoryStream())
            {
                zipStream.CopyTo(outStream);

                BinaryFormatter bf = new BinaryFormatter();
                outStream.Position = 0;
                var deserialized = bf.Deserialize(outStream);
                T result = (T)deserialized;

                zipStream.Close();

                return result;
            }
        }
    }
}