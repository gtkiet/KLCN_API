using KLCN_API.Filters;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Entities;
using KLCN_API.Models.Enums;
using KLCN_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KLCN_API.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly IInvoicePdfService _invoicePdfService;

    public InvoicesController(
        IInvoiceService invoiceService,
        IInvoicePdfService invoicePdfService)
    {
        _invoiceService = invoiceService;
        _invoicePdfService = invoicePdfService;
    }

    /// <summary>Lấy danh sách hóa đơn theo ngày — Admin/Staff.</summary>
    [HttpGet]
    [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<InvoiceListItemResponse>>), 200)]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] DateOnly? date,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _invoiceService.GetInvoicesAsync(date, page, pageSize);
        return Ok(ApiResponse<PagedResponse<InvoiceListItemResponse>>.Ok(result));
    }

    /// <summary>Lấy chi tiết 1 hóa đơn — Admin/Staff hoặc chính khách hàng.</summary>
    [HttpGet("{paymentId:int}")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDetailResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> GetInvoiceDetail(int paymentId)
    {
        var result = await _invoiceService.GetInvoiceByPaymentIdAsync(paymentId);
        return Ok(ApiResponse<InvoiceDetailResponse>.Ok(result));
    }

    /// <summary>
    /// Xuất hóa đơn PDF theo mẫu cố định — Admin/Staff/Customer.
    ///
    /// Trả về file PDF trực tiếp (Content-Type: application/pdf).
    /// Frontend/App chỉ cần mở URL này trong browser hoặc download.
    ///
    /// Flutter: dùng url_launcher để mở, hoặc dio để download file về máy.
    /// Web: window.open(url) hoặc thẻ <a href="..." download>.
    /// </summary>
    [HttpGet("{paymentId:int}/pdf")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> DownloadPdf(int paymentId)
    {
        var invoice = await _invoiceService.GetInvoiceByPaymentIdAsync(paymentId);
        var pdfBytes = await _invoicePdfService.GenerateAsync(invoice);

        var fileName = $"HoaDon_{invoice.InvoiceCode}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }
}