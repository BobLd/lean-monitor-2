using QuantConnect.Orders;

namespace Panoptes.ViewModels
{
    internal static class Extensions
    {
        public static string ToShortString(this OrderStatus orderStatus)
        {
            switch (orderStatus)
            {
                case OrderStatus.Invalid:
                    return "INVL";

                case OrderStatus.Canceled:
                    return "CNCL";

                case OrderStatus.CancelPending:
                    return "CNCLP";

                case OrderStatus.Submitted:
                    return "SUBM";

                case OrderStatus.UpdateSubmitted:
                    return "USUBM";

                case OrderStatus.PartiallyFilled:
                    return "PFILL";

                case OrderStatus.Filled:
                    return "FILL";

                case OrderStatus.None:
                    return "NONE";

                case OrderStatus.New:
                    return "NEW";
            }

            return "#ERROR";
        }
    }
}
