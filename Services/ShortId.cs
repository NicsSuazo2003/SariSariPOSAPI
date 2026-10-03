namespace SariSariPOS.API.Services;

public static class ShortId
{
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyz0123456789";

    public static string Generate(int length = 8)
    {
        var rng = Random.Shared;
        return new string(Enumerable.Range(0, length)
            .Select(_ => Alphabet[rng.Next(Alphabet.Length)])
            .ToArray());
    }
}