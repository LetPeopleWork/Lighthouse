using Lighthouse.Backend.Services.Interfaces.DeliverySources;

namespace Lighthouse.Backend.Services.Implementation.DeliverySources
{
    public class DeliveryForecastBlockRenderer : IDeliveryForecastBlockRenderer
    {
        /// <summary>
        /// The crystal ball opens and closes the block. Detection anchors on the whole opening line
        /// rather than this character alone, so a stray emoji in a human sentence is never mistaken for
        /// a marker.
        /// </summary>
        public static readonly string Marker = char.ConvertFromUtf32(0x1F52E);

        public static readonly string OpeningLinePrefix = Marker + " Lighthouse forecast";

        public string MergeInto(string? existingDescription, string blockText)
        {
            throw new NotImplementedException("Not yet implemented - DISTILL scaffold");
        }
    }
}
