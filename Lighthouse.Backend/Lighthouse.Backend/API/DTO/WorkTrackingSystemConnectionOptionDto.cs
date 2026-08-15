using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.API.DTO
{
    public class WorkTrackingSystemConnectionOptionDto
    {
        public WorkTrackingSystemConnectionOptionDto()
        {            
        }

        public WorkTrackingSystemConnectionOptionDto(WorkTrackingSystemConnectionOption option)
            : this(option, secretReader: null)
        {
        }

        // Whether a stored credential can still be read is a property of the value and the keys currently
        // held, so it is answered here on the way out rather than written down anywhere: there is nothing
        // to keep up to date, and an operator opening the connection sees the truth at that moment. A
        // caller that passes no reader is asking about a connection that has no stored secret to lose.
        public WorkTrackingSystemConnectionOptionDto(WorkTrackingSystemConnectionOption option, ICryptoService? secretReader)
        {
            Key = option.Key;
            IsSecret = option.IsSecret;
            Value = IsSecret ? string.Empty : option.Value;
            IsOptional = option.IsOptional;
            SecretState = IsSecret ? secretReader?.Read(option.Value).State : null;
        }

        public string Key { get; set; }

        public string Value { get; set; }

        public bool IsSecret { get; set; }

        public bool IsOptional { get; set; }

        public SecretState? SecretState { get; set; }
    }
}
