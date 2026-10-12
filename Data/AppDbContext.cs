using Microsoft.EntityFrameworkCore;
using SariSariPOS.API.Entities;
using SariSariPOS.API.Interfaces;

namespace SariSariPOS.API.Data;

public class AppDbContext : DbContext
{
    private readonly ITenantContext _tenant;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<Store> Stores => Set<Store>();
    public DbSet<User> Users => Set<User>();
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CreditTransaction> CreditTransactions => Set<CreditTransaction>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // ── Store (tenant root — NO query filter) ──────────────────
        mb.Entity<Store>(e =>
        {
            e.ToTable("stores");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(16).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
        });

        // ── User (NO query filter — read pre-auth at login) ────────
        mb.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.StoreName).HasMaxLength(200).IsRequired();
            e.Property(x => x.StoreCode).HasMaxLength(16).IsRequired();
            e.Property(x => x.PinHash).HasMaxLength(200).IsRequired();
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(20).IsRequired();

            e.HasIndex(x => x.StoreCode);
            e.HasIndex(x => new { x.StoreId, x.Role });

            e.HasOne(x => x.Store)
                .WithMany(s => s.Users)
                .HasForeignKey(x => x.StoreId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── AppSettings ────────────────────────────────────────────
        mb.Entity<AppSettings>(e =>
        {
            e.ToTable("app_settings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Currency).HasMaxLength(10).IsRequired();
            e.HasIndex(x => x.StoreId).IsUnique();
            e.HasQueryFilter(x => x.StoreId == _tenant.StoreId);
        });

        // ── Category ───────────────────────────────────────────────
        mb.Entity<Category>(e =>
        {
            e.ToTable("categories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(x => new { x.StoreId, x.Name }).IsUnique();
            e.HasQueryFilter(x => x.StoreId == _tenant.StoreId);
        });

        // ── Product ────────────────────────────────────────────────
        mb.Entity<Product>(e =>
        {
            e.ToTable("products");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.QrCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.Barcode).HasMaxLength(50);
            e.Property(x => x.Unit).HasMaxLength(20).IsRequired();
            e.Property(x => x.CostPrice).HasPrecision(18, 2);
            e.Property(x => x.SellingPrice).HasPrecision(18, 2);

            e.HasIndex(x => new { x.StoreId, x.QrCode }).IsUnique();
            e.HasIndex(x => new { x.StoreId, x.Barcode })
                .IsUnique()
                .HasFilter("\"Barcode\" IS NOT NULL");
            e.HasIndex(x => new { x.StoreId, x.CategoryId });
            e.HasIndex(x => new { x.StoreId, x.IsFavorite });

            e.HasOne(x => x.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasQueryFilter(x => x.StoreId == _tenant.StoreId);
        });

        // ── Supplier ───────────────────────────────────────────────
        mb.Entity<Supplier>(e =>
        {
            e.ToTable("suppliers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasQueryFilter(x => x.StoreId == _tenant.StoreId);
        });

        // ── StockMovement ──────────────────────────────────────────
        mb.Entity<StockMovement>(e =>
        {
            e.ToTable("stock_movements");
            e.HasKey(x => x.Id);
            e.Property(x => x.UnitCost).HasPrecision(18, 2);
            e.HasIndex(x => new { x.StoreId, x.ProductId });
            e.HasIndex(x => new { x.StoreId, x.CreatedAt });

            e.HasOne(x => x.Product)
                .WithMany(p => p.StockMovements)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Supplier)
                .WithMany()
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasQueryFilter(x => x.StoreId == _tenant.StoreId);
        });

        // ── Sale ───────────────────────────────────────────────────
        mb.Entity<Sale>(e =>
        {
            e.ToTable("sales");
            e.HasKey(x => x.Id);
            e.Property(x => x.ReceiptNo).HasMaxLength(30).IsRequired();
            e.Property(x => x.ClientId).HasMaxLength(100).IsRequired();
            e.Property(x => x.Subtotal).HasPrecision(18, 2);
            e.Property(x => x.Discount).HasPrecision(18, 2);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.AmountPaid).HasPrecision(18, 2);
            e.Property(x => x.Change).HasPrecision(18, 2);

            e.HasIndex(x => new { x.StoreId, x.ReceiptNo }).IsUnique();
            e.HasIndex(x => new { x.StoreId, x.ClientId }).IsUnique();
            e.HasIndex(x => new { x.StoreId, x.ServerCreatedAt });

            e.HasOne(x => x.Customer)
                .WithMany(c => c.Sales)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasQueryFilter(x => x.StoreId == _tenant.StoreId);
        });

        // ── SaleItem ───────────────────────────────────────────────
        mb.Entity<SaleItem>(e =>
        {
            e.ToTable("sale_items");
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Property(x => x.Subtotal).HasPrecision(18, 2);
            e.HasIndex(x => new { x.StoreId, x.SaleId });

            e.HasOne(x => x.Sale)
                .WithMany(s => s.Items)
                .HasForeignKey(x => x.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Product)
                .WithMany(p => p.SaleItems)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasQueryFilter(x => x.StoreId == _tenant.StoreId);
        });

        // ── Customer ───────────────────────────────────────────────
        mb.Entity<Customer>(e =>
        {
            e.ToTable("customers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.QrCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.CreditBalance).HasPrecision(18, 2);

            e.HasIndex(x => new { x.StoreId, x.QrCode }).IsUnique();
            e.HasIndex(x => new { x.StoreId, x.IsShortcut });

            e.HasQueryFilter(x => x.StoreId == _tenant.StoreId);
        });

        // ── CreditTransaction ──────────────────────────────────────
        mb.Entity<CreditTransaction>(e =>
        {
            e.ToTable("credit_transactions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.BalanceAfter).HasPrecision(18, 2);

            e.HasIndex(x => new { x.StoreId, x.CustomerId });
            e.HasIndex(x => new { x.StoreId, x.CreatedAt });

            e.HasOne(x => x.Customer)
                .WithMany(c => c.Transactions)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasQueryFilter(x => x.StoreId == _tenant.StoreId);
        });

        // ── IdempotencyRecord ──────────────────────────────────────
        mb.Entity<IdempotencyRecord>(e =>
        {
            e.ToTable("idempotency_records");
            e.HasKey(x => x.Id);
            e.Property(x => x.ClientId).HasMaxLength(100).IsRequired();

            e.HasIndex(x => new { x.StoreId, x.ClientId }).IsUnique();

            e.HasQueryFilter(x => x.StoreId == _tenant.StoreId);
        });
    }

    // ── Auto-stamp StoreId on insert ────────────────────────────────
    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        StampTenant();
        return base.SaveChangesAsync(ct);
    }

    public override int SaveChanges()
    {
        StampTenant();
        return base.SaveChanges();
    }

    private void StampTenant()
    {
        var storeId = _tenant.StoreId;
        if (storeId == Guid.Empty) return; // pre-auth: skip stamping

        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.StoreId == Guid.Empty)
                entry.Entity.StoreId = storeId;
        }
    }
}