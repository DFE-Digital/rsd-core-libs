using System.Security.Cryptography;
using System.Text;

namespace GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Identifiers;

/// <summary>
/// RFC 4122 version 5 (SHA-1, name-based) UUIDs. The same namespace and name always produce the same GUID.
/// </summary>
public static class DeterministicGuid
{
    public static Guid Create(Guid namespaceId, string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var namespaceBytes = namespaceId.ToByteArray();
        SwapByteOrder(namespaceBytes);

        var nameBytes = Encoding.UTF8.GetBytes(name);
        var input = new byte[namespaceBytes.Length + nameBytes.Length];
        Buffer.BlockCopy(namespaceBytes, 0, input, 0, namespaceBytes.Length);
        Buffer.BlockCopy(nameBytes, 0, input, namespaceBytes.Length, nameBytes.Length);

#pragma warning disable CA5350 // SHA-1 is mandated by RFC 4122 for version 5 UUIDs; not used for security.
        var hash = SHA1.HashData(input);
#pragma warning restore CA5350

        var result = new byte[16];
        Array.Copy(hash, result, 16);
        result[6] = (byte)((result[6] & 0x0F) | 0x50);
        result[8] = (byte)((result[8] & 0x3F) | 0x80);

        SwapByteOrder(result);
        return new Guid(result);
    }

    // System.Guid stores the first three fields little-endian; RFC 4122 uses network (big-endian) order.
    private static void SwapByteOrder(byte[] guid)
    {
        (guid[0], guid[3]) = (guid[3], guid[0]);
        (guid[1], guid[2]) = (guid[2], guid[1]);
        (guid[4], guid[5]) = (guid[5], guid[4]);
        (guid[6], guid[7]) = (guid[7], guid[6]);
    }
}
