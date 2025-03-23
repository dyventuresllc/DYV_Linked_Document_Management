
namespace DYV_Linked_Document_Management.Models
{
    /// <summary>
    /// Class to store field length statistics
    /// </summary>
    public class FieldStats
    {
        public string FieldName { get; set; }
        public int MinLength { get; set; } = int.MaxValue;
        public int MaxLength { get; set; } = 0;        
        public int Count { get; set; } = 0;
        public int TotalLength { get; set; } = 0;

        public void AddValue(string value)
        {
            int length = value?.Length ?? 0;

            if (length < MinLength)
                MinLength = length;

            if (length > MaxLength)
                MaxLength = length;

            TotalLength += length;
            Count++;            
        }

        public override string ToString()
        {
            return $"Field: {FieldName}, Min: {MinLength}, Max: {MaxLength}, Count: {Count}";
        }
    }
}
