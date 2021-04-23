// ReSharper disable once CheckNamespace
// ReSharper disable once IdentifierTypo
namespace DTCPB
{
    public partial class HistoricalPriceDataResponseHeader
    {
        public bool IsZipped
        {
            get => useZLibCompression_ != 0;
            set => useZLibCompression_ = value ? 1u : 0u;
        }
    }
}