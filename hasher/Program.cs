using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace hasher
{
    [Guid("f0a49a78-432f-4067-a719-ade7a381be09")]
    [ComVisible(true)]
    public interface IMd5Hash
    {
        string GetMd5Hash(byte[] data);
    }

    [ClassInterface(ClassInterfaceType.None)]
    [Guid("bac8ccb9-ea34-43a0-8353-0b4f91a9868c")]
    [ComVisible(true)]
    public class Md5Hash : IMd5Hash
    {
        public string GetMd5Hash(byte[] data)
        {
            var sb = new StringBuilder();
            using (var md5Hash = MD5.Create())
            {
                var hashData = md5Hash.ComputeHash(data);
                for (var i = 0; i < hashData.Length; ++i)
                {
                    sb.Append(hashData[i].ToString("x2"));
                }
            }
            return sb.ToString();
        }
    }
}
