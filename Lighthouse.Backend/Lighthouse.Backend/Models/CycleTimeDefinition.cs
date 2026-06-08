namespace Lighthouse.Backend.Models
{
    public class CycleTimeDefinition
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string StartState { get; set; } = string.Empty;

        public string EndState { get; set; } = string.Empty;
    }
}
