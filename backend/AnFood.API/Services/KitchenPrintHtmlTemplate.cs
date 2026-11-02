using System.Net;
using System.Text;
using AnFood.Application.Bussiness.KitchenBussiness;

namespace AnFood.API.Services;

internal static class KitchenPrintHtmlTemplate
{
    public static string Render(KitchenPrintSlipDto slip)
    {
        var areaName = slip.KitchenAreaName ?? string.Empty;
        var title = GetTitle(slip.SlipType, areaName);
        var tableName = slip.TableName ?? "Tại bàn";
        if (!tableName.Contains(" - ", StringComparison.Ordinal) &&
            !tableName.Contains("Sàn:", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(slip.BranchCode))
            tableName = $"{tableName} - {slip.BranchCode}";
        var tableHtml = string.Join("<br>", tableName.Split(" - ").Select(Encode));
        var rows = new StringBuilder();

        foreach (var item in slip.Items)
        {
            var note = string.IsNullOrWhiteSpace(item.Reason)
                ? item.Note
                : string.IsNullOrWhiteSpace(item.Note) ? item.Reason : $"{item.Reason} - {item.Note}";

            rows.Append("<tr class=\"item-row\"><td class=\"item-name\"><div>")
                .Append(Encode(item.MenuItemName))
                .Append("</div>");
            if (!string.IsNullOrWhiteSpace(note))
                rows.Append("<div class=\"item-note\">* ").Append(Encode(note)).Append("</div>");
            rows.Append("</td><td class=\"item-table\">").Append(tableHtml)
                .Append("</td><td class=\"item-quantity\">").Append(Encode(item.Quantity.ToString("0.##")))
                .Append("</td></tr>");
        }

        var orderNote = string.IsNullOrWhiteSpace(slip.OrderNote)
            ? string.Empty
            : $"<div class=\"order-note\">* Ghi chú đơn: {Encode(slip.OrderNote)}</div><div class=\"divider\"></div>";

        return $$"""
            <!DOCTYPE html>
            <html lang="vi">
            <head>
                <meta charset="utf-8">
                <title>{{Encode(title)}}</title>
                <link rel="preconnect" href="https://fonts.googleapis.com">
                <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
                <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800;900&display=swap" rel="stylesheet">
                <style>
                    @page { size: 80mm auto; margin: 0mm; }
                    html, body { margin: 0; padding: 0; width: 80mm; background: #fff; }
                    body { font-family: Inter, Arial, sans-serif; font-size: 13px; line-height: 1.35; color: #000; }
                    .ticket-slip { width: 80mm; padding: 0 4px 4px 4px; box-sizing: border-box; }
                    .text-center { text-align: center; }
                    .title { font-size: 18px; font-weight: 900; text-transform: uppercase; margin-top: 0; margin-bottom: 4px; letter-spacing: .5px; }
                    .divider { border-top: 2px solid #000; margin: 6px 0; }
                    .divider-dashed { border-top: 1px dashed #000; margin: 6px 0 4px; }
                    .info-row { font-size: 12px; font-weight: 700; margin-bottom: 2px; }
                    table { width: 100%; border-collapse: collapse; }
                    thead tr { border-bottom: 2px solid #000; font-size: 13px; font-weight: 900; }
                    thead td { padding-bottom: 4px; }
                    .item-row { border-bottom: 1px dashed #000; font-size: 13px; }
                    .item-name { width: 48%; padding: 6px 4px 6px 0; font-weight: 700; vertical-align: middle; overflow-wrap: anywhere; }
                    .item-note { font-size: 13px; font-weight: 700; font-style: italic; margin-top: 2px; }
                    .item-table { width: 38%; padding: 6px 4px; font-size: 12px; line-height: 1.3; text-align: center; vertical-align: middle; }
                    .item-quantity { width: 14%; padding: 6px 4px 6px 0; font-size: 18px; font-weight: 900; text-align: right; vertical-align: middle; }
                    .order-note { font-size: 13px; font-weight: 800; padding: 4px 0; }
                    .end-mark { margin-top: 8px; font-size: 14px; font-weight: 900; }
                    .feed { height: 35mm; min-height: 35mm; clear: both; }
                </style>
            </head>
            <body>
                <div class="ticket-slip">
                    <div class="text-center"><div class="title">{{Encode(title)}}</div></div>
                    <div class="divider"></div>
                    <div class="info-row">Thời gian: {{slip.PrintTime:HH\:mm\:ss dd/MM/yyyy}}</div>
                    <div class="divider"></div>
                    <table>
                        <thead><tr><td style="text-align:left;width:48%">Tên món</td><td style="text-align:center;width:38%">Bàn</td><td style="text-align:right;width:14%;padding-right:4px">SL</td></tr></thead>
                        <tbody>{{rows}}</tbody>
                    </table>
                    <div class="divider-dashed"></div>
                    <div class="divider"></div>
                    {{orderNote}}
                    <div class="text-center end-mark">--- HẾT ---</div>
                    <div class="feed">&nbsp;</div>
                </div>
            </body>
            </html>
            """;
    }

    private static string GetTitle(string slipType, string areaName)
    {
        if (string.Equals(slipType, "Serve", StringComparison.OrdinalIgnoreCase)) return "PHIẾU TRẢ MÓN";
        if (string.Equals(slipType, "SoldOut", StringComparison.OrdinalIgnoreCase)) return "PHIẾU BÁO HẾT MÓN";
        if (string.Equals(slipType, "Cancel", StringComparison.OrdinalIgnoreCase)) return "PHIẾU HUỶ MÓN";
        var upperArea = areaName.ToUpperInvariant();
        if (upperArea.Contains("BAR")) return "PHIẾU BÁO BAR";
        if (upperArea.Contains("BẾP")) return $"PHIẾU BÁO {upperArea}";
        return string.IsNullOrWhiteSpace(upperArea) ? "PHIẾU GỬI BẾP" : $"PHIẾU GỬI BẾP - {upperArea}";
    }

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
