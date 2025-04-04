using System.ComponentModel.DataAnnotations;

namespace AKSwingTerminal.Models.DTOs
{
    public class FyersApiResponse<T>
    {
        public string? S { get; set; }  // Status (ok, error)
        public string? Code { get; set; }  // Response code
        public string? Message { get; set; }  // Response message
        public T? Data { get; set; }  // Response data
    }

    public class FyersProfileData
    {
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string? Email { get; set; }
        public string? PAN { get; set; }
        public string? MobileNumber { get; set; }
        public string? ClientId { get; set; }
        public string? BrokerName { get; set; }
        public string? Exchanges { get; set; }
        public string? Products { get; set; }
        public string? OrderTypes { get; set; }
    }

    public class FyersFundsData
    {
        public decimal? EquityAvailable { get; set; }
        public decimal? CommodityAvailable { get; set; }
        public decimal? TotalAvailable { get; set; }
        public decimal? UsedMargin { get; set; }
        public decimal? AvailableMargin { get; set; }
    }

    public class FyersHoldingsData
    {
        public string? Symbol { get; set; }
        public string? ISINCode { get; set; }
        public int? Quantity { get; set; }
        public decimal? AveragePrice { get; set; }
        public decimal? LastPrice { get; set; }
        public decimal? PNL { get; set; }
        public decimal? PNLPercentage { get; set; }
        public string? ProductType { get; set; }
        public string? HoldingType { get; set; }
    }

    public class FyersOrderData
    {
        public string? Id { get; set; }
        public string? Symbol { get; set; }
        public string? Side { get; set; }  // Buy/Sell
        public string? Type { get; set; }  // Market/Limit/SL etc.
        public int? Quantity { get; set; }
        public decimal? Price { get; set; }
        public decimal? StopPrice { get; set; }
        public string? Status { get; set; }
        public string? ProductType { get; set; }
        public DateTime? OrderDateTime { get; set; }
        public string? Exchange { get; set; }
        public string? Segment { get; set; }
        public string? TradingSymbol { get; set; }
    }

    public class FyersPositionData
    {
        public string? Symbol { get; set; }
        public string? Side { get; set; }  // Long/Short
        public int? Quantity { get; set; }
        public decimal? AveragePrice { get; set; }
        public decimal? LastPrice { get; set; }
        public decimal? PNL { get; set; }
        public string? ProductType { get; set; }
        public string? Exchange { get; set; }
        public string? Segment { get; set; }
        public string? TradingSymbol { get; set; }
    }
}
