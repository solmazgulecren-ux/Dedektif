namespace DedektiflikRPG.Models
{
    public class SceneObject
    {
        public int ObjectId { get; set; }
        public int NPCId { get; set; }
        public string ObjectName { get; set; } = "";
        public string Description { get; set; } = "";
        public int IsDiscovered { get; set; }
    }
}
