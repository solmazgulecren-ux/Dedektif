namespace DedektiflikRPG.Models;

/// <summary>
/// Kasabadaki şüpheli karakterleri temsil eder.
/// Her NPC'nin güven/korku seviyesi, suçluluk durumu ve sakladığı bir sır vardır.
/// </summary>
public class NPC
{
    public int NPCId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int TrustLevel { get; set; } = 50;   // 0-100 arası, 50 başlangıç
    public int FearLevel { get; set; } = 30;     // 0-100 arası
    public bool IsGuilty { get; set; } = false;
    public string SecretInfo { get; set; } = string.Empty;
}
