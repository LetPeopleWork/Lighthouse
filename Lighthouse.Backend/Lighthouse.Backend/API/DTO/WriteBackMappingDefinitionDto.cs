using Lighthouse.Backend.Models.WriteBack;

namespace Lighthouse.Backend.API.DTO
{
    public class WriteBackMappingDefinitionDto
    {
        public WriteBackMappingDefinitionDto()
        {
        }

        public WriteBackMappingDefinitionDto(WriteBackMappingDefinition mapping)
        {
            Id = mapping.Id;
            ValueSource = mapping.ValueSource;
            AppliesTo = mapping.AppliesTo;
            TargetFieldReference = mapping.TargetFieldReference;
            TargetValueType = mapping.TargetValueType;
            DateFormat = mapping.DateFormat;
        }

        public int Id { get; set; }

        public WriteBackValueSource ValueSource { get; set; }

        public WriteBackAppliesTo AppliesTo { get; set; }

        public string TargetFieldReference { get; set; } = string.Empty;

        public WriteBackTargetValueType TargetValueType { get; set; } = WriteBackTargetValueType.Date;

        public string? DateFormat { get; set; }

        public WriteBackMappingDefinition ToModel()
        {
            if (Id < 0)
            {
                Id = 0;
            }

            return new WriteBackMappingDefinition
            {
                Id = Id,
                ValueSource = ValueSource,
                AppliesTo = AppliesTo,
                TargetFieldReference = TargetFieldReference,
                TargetValueType = TargetValueType,
                DateFormat = DateFormat
            };
        }
    }
}
