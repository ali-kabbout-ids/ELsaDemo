namespace PurchaseOrderApi.Dtos
{
    public record ActionField
    {
        /// <summary>Key sent in Extra payload — e.g. "requiresMo5atabat"</summary>
        public string Key { get; init; } = default!;

        /// <summary>Label shown in UI — e.g. "Requires Mokhatabat?"</summary>
        public string Label { get; init; } = default!;

        /// <summary>checkbox | text | number | date</summary>
        public string FieldType { get; init; } = "text";

        public bool IsRequired { get; init; } = false;
    }
}
