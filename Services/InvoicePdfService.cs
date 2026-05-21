using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Entities;
using KLCN_API.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace KLCN_API.Services;

/// <summary>
/// Tạo PDF hóa đơn theo mẫu cố định SportPlus.
///
/// Cài đặt: thêm vào KLCN_API.csproj:
///   dotnet add package QuestPDF
///
/// Đăng ký DI trong ServiceCollectionExtensions.cs:
///   services.AddScoped&lt;IInvoicePdfService, InvoicePdfService&gt;();
///
/// QuestPDF Community License — miễn phí cho dự án không thương mại.
/// Khai báo ở Program.cs (1 lần duy nhất):
///   QuestPDF.Settings.License = LicenseType.Community;
/// </summary>
public class InvoicePdfService : IInvoicePdfService
{
    // ── Màu sắc thương hiệu SportPlus ────────────────────────────
    private static readonly string PrimaryGreen = "#2E7D32";  // xanh lá đậm
    private static readonly string LightGreen = "#E8F5E9";  // nền xanh nhạt
    private static readonly string BorderGray = "#E0E0E0";
    private static readonly string TextGray = "#757575";
    private static readonly string TextDark = "#212121";

    public Task<byte[]> GenerateAsync(InvoiceDetailResponse invoice)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x
                    .FontFamily("Arial")
                    .FontSize(10)
                    .FontColor(TextDark));

                page.Header().Element(ComposeHeader);
                page.Content().Element(c => ComposeContent(c, invoice));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();

        return Task.FromResult(pdfBytes);
    }

    // ── Header: Logo + Tiêu đề ───────────────────────────────────

    private static void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            // Dải màu xanh trên cùng
            col.Item()
               .Background(PrimaryGreen)
               .Padding(10)
               .Row(row =>
               {
                   row.RelativeItem().Column(c =>
                   {
                       c.Item().Text("SPORT PLUS")
                           .FontSize(22).Bold().FontColor("#FFFFFF");
                       c.Item().Text("HỆ THỐNG QUẢN LÝ SÂN BÓNG ĐÁ")
                           .FontSize(9).FontColor("#C8E6C9");
                   });

                   row.AutoItem().AlignRight().Column(c =>
                   {
                       c.Item().Text("HÓA ĐƠN THANH TOÁN")
                           .FontSize(16).Bold().FontColor("#FFFFFF");
                       c.Item().Text("PAYMENT INVOICE")
                           .FontSize(9).FontColor("#C8E6C9");
                   });
               });

            // Khoảng cách
            col.Item().Height(8);
        });
    }

    // ── Content chính ─────────────────────────────────────────────

    private static void ComposeContent(IContainer container, InvoiceDetailResponse inv)
    {
        container.Column(col =>
        {
            // ── Thông tin hóa đơn + khách hàng ──────────────────
            col.Item().Row(row =>
            {
                // Thông tin hóa đơn
                row.RelativeItem().Border(1).BorderColor(BorderGray)
                   .Padding(10).Column(c =>
                   {
                       c.Item().Text("THÔNG TIN HÓA ĐƠN")
                           .FontSize(9).Bold().FontColor(PrimaryGreen);
                       c.Item().Height(4);
                       InfoRow(c, "Mã hóa đơn", inv.InvoiceCode);
                       InfoRow(c, "Mã booking", $"#{inv.BookingId}");
                       InfoRow(c, "Ngày thanh toán",
                           inv.PaidAt?.ToString("dd/MM/yyyy HH:mm") ?? "—");
                       InfoRow(c, "Phương thức", inv.PaymentMethod);
                       InfoRow(c, "Trạng thái", inv.PaymentStatus);
                       if (!string.IsNullOrWhiteSpace(inv.TransactionCode))
                           InfoRow(c, "Mã giao dịch", inv.TransactionCode);
                   });

                row.ConstantItem(10); // khoảng cách giữa 2 box

                // Thông tin khách hàng
                row.RelativeItem().Border(1).BorderColor(BorderGray)
                   .Padding(10).Column(c =>
                   {
                       c.Item().Text("THÔNG TIN KHÁCH HÀNG")
                           .FontSize(9).Bold().FontColor(PrimaryGreen);
                       c.Item().Height(4);
                       InfoRow(c, "Họ tên", inv.CustomerName);
                       InfoRow(c, "SĐT", inv.CustomerPhone);
                       InfoRow(c, "Email", inv.CustomerEmail);
                   });
            });

            col.Item().Height(12);

            // ── Bảng chi tiết slot ───────────────────────────────
            col.Item().Text("CHI TIẾT ĐẶT SÂN")
               .FontSize(10).Bold().FontColor(PrimaryGreen);
            col.Item().Height(4);

            col.Item().Table(table =>
            {
                // Cột: STT | Sân | Loại | Ngày | Giờ | Giá
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(30);   // STT
                    cols.RelativeColumn(3);    // Tên sân
                    cols.RelativeColumn(2);    // Loại
                    cols.RelativeColumn(2);    // Ngày
                    cols.RelativeColumn(2);    // Giờ
                    cols.RelativeColumn(2);    // Giá
                });

                // Header hàng
                static IContainer TableHeaderCell(IContainer c) =>
                    c.Background(PrimaryGreen).Padding(6);

                static IContainer TableCell(IContainer c, int row) =>
                    c.Background(row % 2 == 0 ? "#FFFFFF" : LightGreen)
                     .Border(1).BorderColor(BorderGray).Padding(5);

                table.Header(h =>
                {
                    h.Cell().Element(TableHeaderCell).Text("STT")
                        .FontSize(9).Bold().FontColor("#FFFFFF").AlignCenter();
                    h.Cell().Element(TableHeaderCell).Text("Tên sân")
                        .FontSize(9).Bold().FontColor("#FFFFFF");
                    h.Cell().Element(TableHeaderCell).Text("Loại sân")
                        .FontSize(9).Bold().FontColor("#FFFFFF");
                    h.Cell().Element(TableHeaderCell).Text("Ngày")
                        .FontSize(9).Bold().FontColor("#FFFFFF").AlignCenter();
                    h.Cell().Element(TableHeaderCell).Text("Khung giờ")
                        .FontSize(9).Bold().FontColor("#FFFFFF").AlignCenter();
                    h.Cell().Element(TableHeaderCell).Text("Đơn giá")
                        .FontSize(9).Bold().FontColor("#FFFFFF").AlignRight();
                });

                var details = inv.Details ?? [];
                for (var i = 0; i < details.Count; i++)
                {
                    var d = details[i];
                    var rowIdx = i;

                    table.Cell().Element(c => TableCell(c, rowIdx))
                        .Text((rowIdx + 1).ToString()).AlignCenter();
                    table.Cell().Element(c => TableCell(c, rowIdx))
                        .Text(d.FieldName);
                    table.Cell().Element(c => TableCell(c, rowIdx))
                        .Text(d.FieldType);
                    table.Cell().Element(c => TableCell(c, rowIdx))
                        .Text(d.SlotDate.ToString("dd/MM/yyyy")).AlignCenter();
                    table.Cell().Element(c => TableCell(c, rowIdx))
                        .Text($"{d.StartTime:hh\\:mm} – {d.EndTime:hh\\:mm}").AlignCenter();
                    table.Cell().Element(c => TableCell(c, rowIdx))
                        .Text(FormatVnd(d.Price)).AlignRight();
                }
            });

            // ── Dịch vụ đi kèm (nếu có) ─────────────────────────
            if (inv.Services?.Count > 0)
            {
                col.Item().Height(10);
                col.Item().Text("DỊCH VỤ ĐI KÈM")
                   .FontSize(10).Bold().FontColor(PrimaryGreen);
                col.Item().Height(4);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(30);
                        cols.RelativeColumn(4);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                    });

                    static IContainer SvcHeaderCell(IContainer c) =>
                        c.Background(PrimaryGreen).Padding(6);

                    static IContainer SvcCell(IContainer c, int row) =>
                        c.Background(row % 2 == 0 ? "#FFFFFF" : LightGreen)
                         .Border(1).BorderColor(BorderGray).Padding(5);

                    table.Header(h =>
                    {
                        h.Cell().Element(SvcHeaderCell).Text("STT")
                            .FontSize(9).Bold().FontColor("#FFFFFF").AlignCenter();
                        h.Cell().Element(SvcHeaderCell).Text("Dịch vụ")
                            .FontSize(9).Bold().FontColor("#FFFFFF");
                        h.Cell().Element(SvcHeaderCell).Text("SL")
                            .FontSize(9).Bold().FontColor("#FFFFFF").AlignCenter();
                        h.Cell().Element(SvcHeaderCell).Text("Đơn giá")
                            .FontSize(9).Bold().FontColor("#FFFFFF").AlignRight();
                        h.Cell().Element(SvcHeaderCell).Text("Thành tiền")
                            .FontSize(9).Bold().FontColor("#FFFFFF").AlignRight();
                    });

                    for (var i = 0; i < inv.Services.Count; i++)
                    {
                        var s = inv.Services[i];
                        var rowIdx = i;

                        table.Cell().Element(c => SvcCell(c, rowIdx))
                            .Text((rowIdx + 1).ToString()).AlignCenter();
                        table.Cell().Element(c => SvcCell(c, rowIdx))
                            .Text(s.ServiceName);
                        table.Cell().Element(c => SvcCell(c, rowIdx))
                            .Text(s.Quantity.ToString()).AlignCenter();
                        table.Cell().Element(c => SvcCell(c, rowIdx))
                            .Text(FormatVnd(s.UnitPrice)).AlignRight();
                        table.Cell().Element(c => SvcCell(c, rowIdx))
                            .Text(FormatVnd(s.UnitPrice * s.Quantity)).AlignRight();
                    }
                });
            }

            col.Item().Height(14);

            // ── Tổng tiền ─────────────────────────────────────────
            col.Item().AlignRight().Width(280).Border(1).BorderColor(BorderGray)
               .Column(c =>
               {
                   c.Item().Background(LightGreen).Padding(8).Row(r =>
                   {
                       r.RelativeItem().Text("TỔNG TIỀN THANH TOÁN:")
                           .FontSize(12).Bold().FontColor(PrimaryGreen);
                       r.AutoItem().Text(FormatVnd(inv.Amount))
                           .FontSize(14).Bold().FontColor(PrimaryGreen);
                   });
               });

            // ── Ghi chú ──────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(inv.Note))
            {
                col.Item().Height(10);
                col.Item().Border(1).BorderColor(BorderGray)
                   .Background(LightGreen).Padding(8).Column(c =>
                   {
                       c.Item().Text("GHI CHÚ").FontSize(9).Bold().FontColor(PrimaryGreen);
                       c.Item().Height(3);
                       c.Item().Text(inv.Note).FontSize(9).FontColor(TextGray);
                   });
            }
        });
    }

    // ── Footer ────────────────────────────────────────────────────

    private static void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Height(1).Background(PrimaryGreen);
            col.Item().Height(5);
            col.Item().Row(row =>
            {
                row.RelativeItem().Text("Sport Plus — Hệ thống quản lý sân bóng đá")
                    .FontSize(8).FontColor(TextGray);
                row.AutoItem().Text(ctx =>
                {
                    ctx.Span("Trang ").FontSize(8).FontColor(TextGray);
                    ctx.CurrentPageNumber().FontSize(8).FontColor(TextGray);
                    ctx.Span("/").FontSize(8).FontColor(TextGray);
                    ctx.TotalPages().FontSize(8).FontColor(TextGray);
                });
            });
        });
    }

    // ── Private helpers ───────────────────────────────────────────

    private static void InfoRow(ColumnDescriptor col, string label, string? value)
    {
        col.Item().Row(r =>
        {
            r.ConstantItem(110).Text(label + ":").FontSize(9).FontColor(TextGray);
            r.RelativeItem().Text(value ?? "—").FontSize(9).Bold();
        });
        col.Item().Height(3);
    }

    private static string FormatVnd(decimal amount)
        => string.Format("{0:N0} ₫", amount);
}