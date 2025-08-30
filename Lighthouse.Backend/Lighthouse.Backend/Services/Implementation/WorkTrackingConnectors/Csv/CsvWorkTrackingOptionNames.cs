namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear
{
    public static class CsvWorkTrackingOptionNames
    {
        public const string Delimiter = "Delimiter";

        public const string DateTimeFormat = "Date Time Format";

        public const string TagSeparator = "Tag Separator";

        // Common Required Columns
        public const string IdHeader = "ID Column";

        public const string NameHeader = "Name Column";

        public const string StateHeader = "State Column";

        public const string TypeHeader = "Type Column";

        public const string StartedDateHeader = "Started Date Column";

        public const string ClosedDateHeader = "Closed Date Column";

        // Common Optional Columns
        public const string CreatedDateHeader = "Created Date Column";

        public const string ParentReferenceIdHeader = "Parent Reference Id Column";

        public const string TagsHeader = "Tags Column";

        public const string UrlHeader = "Url Column";

        // Feature Optional Columns
        public const string OwningTeamHeader = "Owning Team Column";

        public const string EstimatedSizeHeader = "Estimated Size Column";
    }
}