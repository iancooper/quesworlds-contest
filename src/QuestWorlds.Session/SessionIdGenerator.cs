using System.Security.Cryptography;

namespace QuestWorlds.Session;

internal interface ISessionIdGenerator
{
    string Generate();
}

internal class SessionIdGenerator : ISessionIdGenerator
{
    private const string ALPHABET = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int ID_LENGTH = 6;

    public string Generate()
    {
        var bytes = new byte[ID_LENGTH];
        RandomNumberGenerator.Fill(bytes);
        return new string(bytes.Select(b => ALPHABET[b % ALPHABET.Length]).ToArray());
    }
}
