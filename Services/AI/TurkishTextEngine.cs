using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DedektiflikRPG.Services.AI;

/// <summary>
/// %100 Türkçeye Duyarlı, Sokak Ağzı, Devrik Cümle ve Kök Analiz Motoru.
/// Türkçedeki tüm özel harfleri (ş, ç, ı, ü, ö, ğ, İ, Ş, Ç, Ü, Ö, Ğ) hem orijinal hem esnek işler.
/// Devrik cümleleri ve karmaşık Türkçe soru kalıplarını anlamlı niyete (Intent) dönüştürür.
/// </summary>
public static class TurkishTextEngine
{
    private static readonly CultureInfo _cultureTr = new CultureInfo("tr-TR");

    public static string NormalizeToAscii(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        string s = text.ToLower(_cultureTr);
        StringBuilder sb = new StringBuilder(s.Length);
        foreach (char c in s)
        {
            switch (c)
            {
                case 'ç': sb.Append('c'); break;
                case 'ğ': sb.Append('g'); break;
                case 'ı': sb.Append('i'); break;
                case 'i': sb.Append('i'); break;
                case 'ö': sb.Append('o'); break;
                case 'ş': sb.Append('s'); break;
                case 'ü': sb.Append('u'); break;
                default:
                    if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                        sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    public static string PreprocessSentence(string text)
    {
        string normalized = NormalizeToAscii(text);
        var tokens = normalized.Split(new[] { ' ', '.', ',', '?', '!', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);
        var stopWords = new HashSet<string> { "mi", "mu", "miyim", "mısın", "musun", "müsün", "var", "yok", "bir", "ve", "ile", "için", "icin", "diye", "bu", "şu", "su", "da", "de", "ki", "işte", "iste" };

        List<string> processed = new List<string>();
        foreach (var t in tokens)
        {
            if (t.Length <= 2 && t != "ne") continue;
            if (stopWords.Contains(t)) continue;

            string stemmed = Stem(t);
            string mapped = MapSlang(stemmed);
            processed.Add(mapped);
        }
        return string.Join(" ", processed);
    }

    public static string Stem(string word)
    {
        if (word.Length <= 3) return word;

        // Türkçe Çekim ve Yapım Eklerini Temizleme
        string[] suffixes = {
            "yordu", "yorsun", "yorsunuz", "lerdir", "lardir", "lardan", "lerden", "larina", "lerine",
            "misin", "musun", "misiniz", "musunuz", "miyor", "kiyor", "diler", "dilar", "tilar", "tiler",
            "acak", "ecek", "iyor", "lar", "ler", "dan", "den", "tan", "ten", "nin", "nun",
            "yla", "yle", "siz", "suz", "sun", "sunuz", "siniz", "sin", "yim", "dik", "tik", "duk", "tuk",
            "di", "ti", "du", "tu", "yi", "ya", "ye", "in", "un", "im", "um", "miz", "muz"
        };

        foreach (var suffix in suffixes)
        {
            if (word.EndsWith(suffix) && word.Length - suffix.Length >= 3)
            {
                return word.Substring(0, word.Length - suffix.Length);
            }
        }

        if ((word.EndsWith("a") || word.EndsWith("e") || word.EndsWith("i") || word.EndsWith("u")) && word.Length >= 4)
        {
            return word.Substring(0, word.Length - 1);
        }

        return word;
    }

    public static string MapSlang(string word)
    {
        return word switch
        {
            "kanki" or "kral" or "abi" or "dayi" or "usta" or "aga" or "haci" or "bilader" or "sef" or "toprak" => "amirim",
            "sikti" or "kesti" or "deldi" or "cizdi" or "vurdu" or "indirdi" or "desti" or "kiydi" or "gebertti" or "boctu" or "oldurdu" or "yapti" or "ett" => "oldur",
            "para" or "mangir" or "sakal" or "avanta" or "cukka" or "veresiye" => "borc",
            "cirkef" or "pislik" or "kavga" or "gurultu" or "dalaş" or "husumet" => "tartisma",
            "suphe" or "kusku" or "gizli" => "suphe",
            "slm" or "s.a" or "sa" or "selamin" or "aleykum" or "selamlar" or "selam" or "selamun" or "hey" or "heyy" => "selam",
            "mrb" or "meraba" or "mrhb" or "merhaba" or "merhabalar" => "merhaba",
            "nbr" or "naber" or "naptin" or "napiyosun" or "napiyorsun" or "nabion" or "nasil" or "nasilsin" => "nasilsin",
            "kim" or "kimdir" or "katil" or "suclu" or "kimyapti" => "kim",
            "neden" or "niye" or "nicin" or "sebep" => "neden",
            _ => word
        };
    }

    public static bool ContainsAnyConcept(string rawTrLower, string normalizedAscii, params string[] concepts)
    {
        var inputTokens = normalizedAscii.Split(new[] { ' ', '.', ',', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);
        string noSpaceInput = normalizedAscii.Replace(" ", "");

        foreach (var concept in concepts)
        {
            string conceptNorm = NormalizeToAscii(concept);
            string noSpaceConcept = conceptNorm.Replace(" ", "");

            if (rawTrLower.Contains(concept) || normalizedAscii.Contains(conceptNorm) || noSpaceInput.Contains(noSpaceConcept))
            {
                return true;
            }

            var conceptTokens = conceptNorm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (noSpaceConcept.Length >= 5)
            {
                int maxTypos = Math.Max(1, noSpaceConcept.Length / 4);
                if (Math.Abs(noSpaceInput.Length - noSpaceConcept.Length) <= maxTypos + 2)
                {
                    int dist = LevenshteinDistance(noSpaceInput, noSpaceConcept);
                    if (dist <= maxTypos) return true;
                }
            }

            int matchedTokens = 0;
            int meaningfulTokens = 0;
            foreach (var cToken in conceptTokens)
            {
                if (cToken.Length <= 2) continue;
                meaningfulTokens++;
                bool foundToken = false;
                foreach (var iToken in inputTokens)
                {
                    if (iToken.Length > 2)
                    {
                        if (iToken == cToken || iToken.StartsWith(cToken) || iToken.EndsWith(cToken))
                        {
                            foundToken = true;
                            break;
                        }

                        int dist = LevenshteinDistance(iToken, cToken);
                        int maxTypos = cToken.Length >= 6 ? 2 : (cToken.Length >= 4 ? 1 : 0);
                        if (dist <= maxTypos)
                        {
                            foundToken = true;
                            break;
                        }
                    }
                }
                if (foundToken) matchedTokens++;
            }

            if (meaningfulTokens > 0 && matchedTokens >= meaningfulTokens)
            {
                return true;
            }
        }
        return false;
    }

    public static int LevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s)) return string.IsNullOrEmpty(t) ? 0 : t.Length;
        if (string.IsNullOrEmpty(t)) return s.Length;

        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; d[i, 0] = i++) { }
        for (int j = 0; j <= m; d[0, j] = j++) { }

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }
}
