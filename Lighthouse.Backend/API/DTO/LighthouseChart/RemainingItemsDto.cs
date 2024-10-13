namespace Lighthouse.Backend.API.DTO.LighthouseChart
{
    public class RemainingItemsDto
    {
        public RemainingItemsDto(DateOnly date, int remainingItems)
        {
            Date = date.ToDateTime(TimeOnly.MinValue);
            RemainingItems = remainingItems;    
        }

        public DateTime Date { get; set; }

        public int RemainingItems {  get; set; }
    }
}
