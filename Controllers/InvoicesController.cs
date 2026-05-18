using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Entities;
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

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<InvoiceListItemResponse>>), 200)]
    public async Task<IActionResult> GetInvoices([FromQuery] DateOnly? date, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _invoiceService.GetInvoicesAsync(date, page, pageSize);
        return Ok(ApiResponse<PagedResponse<InvoiceListItemResponse>>.Ok(result));
    }

    [HttpGet("{paymentId:int}")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDetailResponse>), 200)]
    public async Task<IActionResult> GetInvoiceDetail(int paymentId)
    {
        var result = await _invoiceService.GetInvoiceByPaymentIdAsync(paymentId);
        return Ok(ApiResponse<InvoiceDetailResponse>.Ok(result));
    }
}