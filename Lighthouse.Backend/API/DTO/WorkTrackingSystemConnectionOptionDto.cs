using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public class WorkTrackingSystemConnectionOptionDto
    {
        public WorkTrackingSystemConnectionOptionDto()
        {            
        }
        public WorkTrackingSystemConnectionOptionDto(WorkTrackingSystemConnectionOption option)
        {
            Key = option.Key;
            Value = option.Value;
            IsSecret = option.IsSecret;
        }

        public string Key { get; set; }

        public string Value { get; set; }

        public bool IsSecret { get; set; }
    }
}
