using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SariSariPOS.API.Migrations
{
    /// <summary>
    /// Multi-tenancy migration. Runs in three ordered phases inside Up():
    ///
    ///   PHASE 1 — Schema (additive, safe):
    ///     • Create `stores` table.
    ///     • Add NULLABLE `StoreId` to every tenant table.
    ///     • Add NULLABLE `StoreCode` / `StoreId` / `Role` to `users`.
    ///
    ///   PHASE 2 — Data backfill (raw SQL):
    ///     • Insert ONE `stores` row derived from the existing `users` row.
    ///     • Backfill `StoreId` on all tenant tables with that store's Id.
    ///     • Backfill `StoreCode` / `StoreId` / `Role` on `users`.
    ///
    ///   PHASE 3 — Constraints (NOT NULL + indexes + FK):
    ///     • ALTER COLUMN SET NOT NULL on every StoreId column.
    ///     • Add unique indexes scoped by StoreId.
    ///     • Add FK from users.StoreId → stores.Id.
    /// </summary>
    public partial class AddMultiTenancySupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ════════════════════════════════════════════════════════
            // PHASE 1 — SCHEMA (nullable, non-destructive)
            // ════════════════════════════════════════════════════════

            // ── Create `stores` ─────────────────────────────────
            migrationBuilder.CreateTable(
                name: "stores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stores", x => x.Id);
                });

            // ── Add NULLABLE StoreId to every tenant table ──────
            AddNullableStoreId(migrationBuilder, "app_settings");
            AddNullableStoreId(migrationBuilder, "categories");
            AddNullableStoreId(migrationBuilder, "products");
            AddNullableStoreId(migrationBuilder, "suppliers");
            AddNullableStoreId(migrationBuilder, "stock_movements");
            AddNullableStoreId(migrationBuilder, "sales");
            AddNullableStoreId(migrationBuilder, "sale_items");
            AddNullableStoreId(migrationBuilder, "customers");
            AddNullableStoreId(migrationBuilder, "credit_transactions");
            AddNullableStoreId(migrationBuilder, "idempotency_records");

            // ── Add NULLABLE multi-tenant columns to `users` ────
            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoreCode",
                table: "users",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            // ── Drop OLD global unique indexes ─────────────────
            // These block multi-tenant uniqueness. New per-store indexes
            // are added in Phase 3.
            migrationBuilder.DropIndex("IX_stock_movements_CreatedAt", "stock_movements");
            migrationBuilder.DropIndex("IX_sales_ClientId", "sales");
            migrationBuilder.DropIndex("IX_sales_ReceiptNo", "sales");
            migrationBuilder.DropIndex("IX_sales_ServerCreatedAt", "sales");
            migrationBuilder.DropIndex("IX_products_Barcode", "products");
            migrationBuilder.DropIndex("IX_products_IsFavorite", "products");
            migrationBuilder.DropIndex("IX_products_QrCode", "products");
            migrationBuilder.DropIndex("IX_idempotency_records_ClientId", "idempotency_records");
            migrationBuilder.DropIndex("IX_customers_IsShortcut", "customers");
            migrationBuilder.DropIndex("IX_customers_QrCode", "customers");
            migrationBuilder.DropIndex("IX_credit_transactions_CreatedAt", "credit_transactions");
            migrationBuilder.DropIndex("IX_categories_Name", "categories");

            // ════════════════════════════════════════════════════════
            // PHASE 2 — BACKFILL EXISTING DATA (raw SQL)
            // ════════════════════════════════════════════════════════
            //
            // Reads the (single) existing `users` row, mints a new
            // `stores` row from it, and propagates the new StoreId
            // across every tenant table.
            //
            // If there are NO existing users, this block is a no-op
            // and the app starts empty (fresh install).

            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    v_user_id     uuid;
                    v_store_name  text;
                    v_store_id    uuid := gen_random_uuid();
                    v_store_code  text;
                    v_alphabet    text := 'ABCDEFGHJKMNPQRSTUVWXYZ23456789';
                    v_suffix      text := '';
                    v_i           int;
                    v_attempt     int := 0;
                BEGIN
                    -- 1. Find the existing owner (if any)
                    SELECT ""Id"", ""StoreName""
                      INTO v_user_id, v_store_name
                      FROM users
                      ORDER BY ""CreatedAt"" ASC
                      LIMIT 1;

                    IF v_user_id IS NULL THEN
                        RAISE NOTICE 'AddMultiTenancySupport: no existing users found - skipping backfill.';
                        RETURN;
                    END IF;

                    -- 2. Generate a unique store code
                    LOOP
                        v_suffix := '';
                        FOR v_i IN 1..4 LOOP
                            v_suffix := v_suffix ||
                                substr(v_alphabet, floor(random() * length(v_alphabet))::int + 1, 1);
                        END LOOP;

                        v_store_code := 'SAR' || v_suffix;

                        EXIT WHEN NOT EXISTS (
                            SELECT 1 FROM stores WHERE ""Code"" = v_store_code
                        );

                        v_attempt := v_attempt + 1;
                        IF v_attempt > 50 THEN
                            RAISE EXCEPTION 'Could not generate a unique store code';
                        END IF;
                    END LOOP;

                    -- 3. Insert the new store
                    INSERT INTO stores (""Id"", ""Code"", ""Name"", ""OwnerUserId"", ""CreatedAt"")
                    VALUES (v_store_id, v_store_code, v_store_name, v_user_id, now());

                    RAISE NOTICE 'AddMultiTenancySupport: created store % (%)', v_store_code, v_store_id;

                    -- 4. Backfill every tenant table
                    UPDATE app_settings         SET ""StoreId"" = v_store_id WHERE ""StoreId"" IS NULL;
                    UPDATE categories           SET ""StoreId"" = v_store_id WHERE ""StoreId"" IS NULL;
                    UPDATE products             SET ""StoreId"" = v_store_id WHERE ""StoreId"" IS NULL;
                    UPDATE suppliers            SET ""StoreId"" = v_store_id WHERE ""StoreId"" IS NULL;
                    UPDATE stock_movements      SET ""StoreId"" = v_store_id WHERE ""StoreId"" IS NULL;
                    UPDATE sales                SET ""StoreId"" = v_store_id WHERE ""StoreId"" IS NULL;
                    UPDATE sale_items           SET ""StoreId"" = v_store_id WHERE ""StoreId"" IS NULL;
                    UPDATE customers            SET ""StoreId"" = v_store_id WHERE ""StoreId"" IS NULL;
                    UPDATE credit_transactions  SET ""StoreId"" = v_store_id WHERE ""StoreId"" IS NULL;
                    UPDATE idempotency_records  SET ""StoreId"" = v_store_id WHERE ""StoreId"" IS NULL;

                    -- 5. Backfill the user
                    UPDATE users
                       SET ""StoreId""   = v_store_id,
                           ""StoreCode"" = v_store_code,
                           ""Role""      = 'Owner'
                     WHERE ""Id"" = v_user_id;

                END $$;
            ");

            // ════════════════════════════════════════════════════════
            // PHASE 3 — ENFORCE NOT NULL + INDEXES + FK
            // ════════════════════════════════════════════════════════

            // ── users ───────────────────────────────────────────
            migrationBuilder.AlterColumn<Guid>(
                name: "StoreId",
                table: "users",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "StoreCode",
                table: "users",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Owner",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            // ── Tenant tables: flip StoreId to NOT NULL ─────────
            SetStoreIdNotNull(migrationBuilder, "app_settings");
            SetStoreIdNotNull(migrationBuilder, "categories");
            SetStoreIdNotNull(migrationBuilder, "products");
            SetStoreIdNotNull(migrationBuilder, "suppliers");
            SetStoreIdNotNull(migrationBuilder, "stock_movements");
            SetStoreIdNotNull(migrationBuilder, "sales");
            SetStoreIdNotNull(migrationBuilder, "sale_items");
            SetStoreIdNotNull(migrationBuilder, "customers");
            SetStoreIdNotNull(migrationBuilder, "credit_transactions");
            SetStoreIdNotNull(migrationBuilder, "idempotency_records");

            // ── FK: users.StoreId → stores.Id ───────────────────
            migrationBuilder.AddForeignKey(
                name: "FK_users_stores_StoreId",
                table: "users",
                column: "StoreId",
                principalTable: "stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // ── New per-store indexes ───────────────────────────
            migrationBuilder.CreateIndex("IX_users_StoreCode", "users", "StoreCode");
            migrationBuilder.CreateIndex("IX_users_StoreId_Role", "users", new[] { "StoreId", "Role" });

            migrationBuilder.CreateIndex("IX_app_settings_StoreId", "app_settings", "StoreId", unique: true);

            migrationBuilder.CreateIndex("IX_categories_StoreId_Name", "categories",
                new[] { "StoreId", "Name" }, unique: true);

            migrationBuilder.CreateIndex("IX_products_StoreId_QrCode", "products",
                new[] { "StoreId", "QrCode" }, unique: true);

            migrationBuilder.CreateIndex("IX_products_StoreId_Barcode", "products",
                new[] { "StoreId", "Barcode" }, unique: true,
                filter: "\"Barcode\" IS NOT NULL");

            migrationBuilder.CreateIndex("IX_products_StoreId_CategoryId", "products",
                new[] { "StoreId", "CategoryId" });

            migrationBuilder.CreateIndex("IX_products_StoreId_IsFavorite", "products",
                new[] { "StoreId", "IsFavorite" });

            migrationBuilder.CreateIndex("IX_stock_movements_StoreId_ProductId", "stock_movements",
                new[] { "StoreId", "ProductId" });

            migrationBuilder.CreateIndex("IX_stock_movements_StoreId_CreatedAt", "stock_movements",
                new[] { "StoreId", "CreatedAt" });

            migrationBuilder.CreateIndex("IX_sales_StoreId_ReceiptNo", "sales",
                new[] { "StoreId", "ReceiptNo" }, unique: true);

            migrationBuilder.CreateIndex("IX_sales_StoreId_ClientId", "sales",
                new[] { "StoreId", "ClientId" }, unique: true);

            migrationBuilder.CreateIndex("IX_sales_StoreId_ServerCreatedAt", "sales",
                new[] { "StoreId", "ServerCreatedAt" });

            migrationBuilder.CreateIndex("IX_sale_items_StoreId_SaleId", "sale_items",
                new[] { "StoreId", "SaleId" });

            migrationBuilder.CreateIndex("IX_customers_StoreId_QrCode", "customers",
                new[] { "StoreId", "QrCode" }, unique: true);

            migrationBuilder.CreateIndex("IX_customers_StoreId_IsShortcut", "customers",
                new[] { "StoreId", "IsShortcut" });

            migrationBuilder.CreateIndex("IX_credit_transactions_StoreId_CustomerId", "credit_transactions",
                new[] { "StoreId", "CustomerId" });

            migrationBuilder.CreateIndex("IX_credit_transactions_StoreId_CreatedAt", "credit_transactions",
                new[] { "StoreId", "CreatedAt" });

            migrationBuilder.CreateIndex("IX_idempotency_records_StoreId_ClientId", "idempotency_records",
                new[] { "StoreId", "ClientId" }, unique: true);

            migrationBuilder.CreateIndex("IX_stores_Code", "stores", "Code", unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate old global indexes (best-effort rollback)
            migrationBuilder.DropForeignKey("FK_users_stores_StoreId", "users");

            // Drop new per-store indexes
            migrationBuilder.DropIndex("IX_users_StoreCode", "users");
            migrationBuilder.DropIndex("IX_users_StoreId_Role", "users");
            migrationBuilder.DropIndex("IX_app_settings_StoreId", "app_settings");
            migrationBuilder.DropIndex("IX_categories_StoreId_Name", "categories");
            migrationBuilder.DropIndex("IX_products_StoreId_QrCode", "products");
            migrationBuilder.DropIndex("IX_products_StoreId_Barcode", "products");
            migrationBuilder.DropIndex("IX_products_StoreId_CategoryId", "products");
            migrationBuilder.DropIndex("IX_products_StoreId_IsFavorite", "products");
            migrationBuilder.DropIndex("IX_stock_movements_StoreId_ProductId", "stock_movements");
            migrationBuilder.DropIndex("IX_stock_movements_StoreId_CreatedAt", "stock_movements");
            migrationBuilder.DropIndex("IX_sales_StoreId_ReceiptNo", "sales");
            migrationBuilder.DropIndex("IX_sales_StoreId_ClientId", "sales");
            migrationBuilder.DropIndex("IX_sales_StoreId_ServerCreatedAt", "sales");
            migrationBuilder.DropIndex("IX_sale_items_StoreId_SaleId", "sale_items");
            migrationBuilder.DropIndex("IX_customers_StoreId_QrCode", "customers");
            migrationBuilder.DropIndex("IX_customers_StoreId_IsShortcut", "customers");
            migrationBuilder.DropIndex("IX_credit_transactions_StoreId_CustomerId", "credit_transactions");
            migrationBuilder.DropIndex("IX_credit_transactions_StoreId_CreatedAt", "credit_transactions");
            migrationBuilder.DropIndex("IX_idempotency_records_StoreId_ClientId", "idempotency_records");
            migrationBuilder.DropIndex("IX_stores_Code", "stores");

            // Drop columns
            migrationBuilder.DropColumn("StoreId", "app_settings");
            migrationBuilder.DropColumn("StoreId", "categories");
            migrationBuilder.DropColumn("StoreId", "products");
            migrationBuilder.DropColumn("StoreId", "suppliers");
            migrationBuilder.DropColumn("StoreId", "stock_movements");
            migrationBuilder.DropColumn("StoreId", "sales");
            migrationBuilder.DropColumn("StoreId", "sale_items");
            migrationBuilder.DropColumn("StoreId", "customers");
            migrationBuilder.DropColumn("StoreId", "credit_transactions");
            migrationBuilder.DropColumn("StoreId", "idempotency_records");
            migrationBuilder.DropColumn("StoreId", "users");
            migrationBuilder.DropColumn("StoreCode", "users");
            migrationBuilder.DropColumn("Role", "users");

            // Drop stores table
            migrationBuilder.DropTable("stores");

            // Recreate the old global indexes
            migrationBuilder.CreateIndex("IX_stock_movements_CreatedAt", "stock_movements", "CreatedAt");
            migrationBuilder.CreateIndex("IX_sales_ClientId", "sales", "ClientId", unique: true);
            migrationBuilder.CreateIndex("IX_sales_ReceiptNo", "sales", "ReceiptNo", unique: true);
            migrationBuilder.CreateIndex("IX_sales_ServerCreatedAt", "sales", "ServerCreatedAt");
            migrationBuilder.CreateIndex("IX_products_Barcode", "products", "Barcode", unique: true,
                filter: "\"Barcode\" IS NOT NULL");
            migrationBuilder.CreateIndex("IX_products_IsFavorite", "products", "IsFavorite");
            migrationBuilder.CreateIndex("IX_products_QrCode", "products", "QrCode", unique: true);
            migrationBuilder.CreateIndex("IX_idempotency_records_ClientId", "idempotency_records", "ClientId", unique: true);
            migrationBuilder.CreateIndex("IX_customers_IsShortcut", "customers", "IsShortcut");
            migrationBuilder.CreateIndex("IX_customers_QrCode", "customers", "QrCode", unique: true);
            migrationBuilder.CreateIndex("IX_credit_transactions_CreatedAt", "credit_transactions", "CreatedAt");
            migrationBuilder.CreateIndex("IX_categories_Name", "categories", "Name", unique: true);
        }

        // ─────────────────────────────────────────────────────────
        // Helpers — reduce repetition for the 10 tenant tables
        // ─────────────────────────────────────────────────────────

        private static void AddNullableStoreId(MigrationBuilder mb, string table)
        {
            mb.AddColumn<Guid>(
                name: "StoreId",
                table: table,
                type: "uuid",
                nullable: true);
        }

        private static void SetStoreIdNotNull(MigrationBuilder mb, string table)
        {
            mb.AlterColumn<Guid>(
                name: "StoreId",
                table: table,
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}