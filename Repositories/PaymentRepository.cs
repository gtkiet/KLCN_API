using KLCN_API.Data;
using KLCN_API.Models.Entities;
using KLCN_API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KLCN_API.Repositories;

// ── PaymentRepository ─────────────────────────────────────────────

public class PaymentRepository : IPaymentRepository
{
    private readonly SportPlusDbContext _ctx;

    public PaymentRepository(SportPlusDbContext ctx) => _ctx = ctx;

    public async Task<List<Payment>> GetByBookingAsync(int bookingId)
        => await _ctx.Payments
            .Include(p => p.Status)
            .Include(p => p.Method)
            .Where(p => p.BookingId == bookingId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task<decimal> GetTotalPaidAsync(int bookingId)
        => await _ctx.Payments
            .Where(p => p.BookingId == bookingId
                     && p.StatusId == (int)Models.Enums.PaymentStatusEnum.Paid)
            .SumAsync(p => p.Amount);

    public async Task<Payment> AddAsync(Payment payment)
    {
        await _ctx.Payments.AddAsync(payment);
        await _ctx.SaveChangesAsync();
        return payment;
    }
}

// ── DepositRepository ─────────────────────────────────────────────

public class DepositRepository : IDepositRepository
{
    private readonly SportPlusDbContext _ctx;

    public DepositRepository(SportPlusDbContext ctx) => _ctx = ctx;

    public async Task<Deposit?> GetByBookingAsync(int bookingId)
        => await _ctx.Deposits
            .Include(d => d.Status)
            .FirstOrDefaultAsync(d => d.BookingId == bookingId);

    public async Task<Deposit> AddAsync(Deposit deposit)
    {
        await _ctx.Deposits.AddAsync(deposit);
        await _ctx.SaveChangesAsync();
        return deposit;
    }

    public async Task UpdateAsync(Deposit deposit)
    {
        deposit.UpdatedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync();
    }
}