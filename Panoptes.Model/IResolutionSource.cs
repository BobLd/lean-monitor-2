using QuantConnect;

namespace Panoptes.Model
{
    public interface IResolutionSource
    {
        /// <summary>
        /// Gets or sets the resolution of the data
        /// </summary>
        Resolution Resolution { get; set; }
    }
}
