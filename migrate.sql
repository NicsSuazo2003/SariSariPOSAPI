START TRANSACTION;
CREATE TABLE stores (
    "Id" uuid NOT NULL,
    "Code" character varying(16) NOT NULL,
    "Name" character varying(200) NOT NULL,
    "OwnerUserId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_stores" PRIMARY KEY ("Id")
);

ALTER TABLE app_settings ADD "StoreId" uuid;

ALTER TABLE categories ADD "StoreId" uuid;

ALTER TABLE products ADD "StoreId" uuid;

ALTER TABLE suppliers ADD "StoreId" uuid;

ALTER TABLE stock_movements ADD "StoreId" uuid;

ALTER TABLE sales ADD "StoreId" uuid;

ALTER TABLE sale_items ADD "StoreId" uuid;

ALTER TABLE customers ADD "StoreId" uuid;

ALTER TABLE credit_transactions ADD "StoreId" uuid;

ALTER TABLE idempotency_records ADD "StoreId" uuid;

ALTER TABLE users ADD "StoreId" uuid;

ALTER TABLE users ADD "StoreCode" character varying(16);

ALTER TABLE users ADD "Role" character varying(20);

DROP INDEX "IX_stock_movements_CreatedAt";

DROP INDEX "IX_sales_ClientId";

DROP INDEX "IX_sales_ReceiptNo";

DROP INDEX "IX_sales_ServerCreatedAt";

DROP INDEX "IX_products_Barcode";

DROP INDEX "IX_products_IsFavorite";

DROP INDEX "IX_products_QrCode";

DROP INDEX "IX_idempotency_records_ClientId";

DROP INDEX "IX_customers_IsShortcut";

DROP INDEX "IX_customers_QrCode";

DROP INDEX "IX_credit_transactions_CreatedAt";

DROP INDEX "IX_categories_Name";


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
                    SELECT "Id", "StoreName"
                      INTO v_user_id, v_store_name
                      FROM users
                      ORDER BY "CreatedAt" ASC
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
                            SELECT 1 FROM stores WHERE "Code" = v_store_code
                        );

                        v_attempt := v_attempt + 1;
                        IF v_attempt > 50 THEN
                            RAISE EXCEPTION 'Could not generate a unique store code';
                        END IF;
                    END LOOP;

                    -- 3. Insert the new store
                    INSERT INTO stores ("Id", "Code", "Name", "OwnerUserId", "CreatedAt")
                    VALUES (v_store_id, v_store_code, v_store_name, v_user_id, now());

                    RAISE NOTICE 'AddMultiTenancySupport: created store % (%)', v_store_code, v_store_id;

                    -- 4. Backfill every tenant table
                    UPDATE app_settings         SET "StoreId" = v_store_id WHERE "StoreId" IS NULL;
                    UPDATE categories           SET "StoreId" = v_store_id WHERE "StoreId" IS NULL;
                    UPDATE products             SET "StoreId" = v_store_id WHERE "StoreId" IS NULL;
                    UPDATE suppliers            SET "StoreId" = v_store_id WHERE "StoreId" IS NULL;
                    UPDATE stock_movements      SET "StoreId" = v_store_id WHERE "StoreId" IS NULL;
                    UPDATE sales                SET "StoreId" = v_store_id WHERE "StoreId" IS NULL;
                    UPDATE sale_items           SET "StoreId" = v_store_id WHERE "StoreId" IS NULL;
                    UPDATE customers            SET "StoreId" = v_store_id WHERE "StoreId" IS NULL;
                    UPDATE credit_transactions  SET "StoreId" = v_store_id WHERE "StoreId" IS NULL;
                    UPDATE idempotency_records  SET "StoreId" = v_store_id WHERE "StoreId" IS NULL;

                    -- 5. Backfill the user
                    UPDATE users
                       SET "StoreId"   = v_store_id,
                           "StoreCode" = v_store_code,
                           "Role"      = 'Owner'
                     WHERE "Id" = v_user_id;

                END $$;
            

ALTER TABLE users ALTER COLUMN "StoreId" SET NOT NULL;

ALTER TABLE users ALTER COLUMN "StoreCode" SET NOT NULL;

UPDATE users SET "Role" = 'Owner' WHERE "Role" IS NULL;
ALTER TABLE users ALTER COLUMN "Role" SET NOT NULL;
ALTER TABLE users ALTER COLUMN "Role" SET DEFAULT 'Owner';

ALTER TABLE app_settings ALTER COLUMN "StoreId" SET NOT NULL;

ALTER TABLE categories ALTER COLUMN "StoreId" SET NOT NULL;

ALTER TABLE products ALTER COLUMN "StoreId" SET NOT NULL;

ALTER TABLE suppliers ALTER COLUMN "StoreId" SET NOT NULL;

ALTER TABLE stock_movements ALTER COLUMN "StoreId" SET NOT NULL;

ALTER TABLE sales ALTER COLUMN "StoreId" SET NOT NULL;

ALTER TABLE sale_items ALTER COLUMN "StoreId" SET NOT NULL;

ALTER TABLE customers ALTER COLUMN "StoreId" SET NOT NULL;

ALTER TABLE credit_transactions ALTER COLUMN "StoreId" SET NOT NULL;

ALTER TABLE idempotency_records ALTER COLUMN "StoreId" SET NOT NULL;

ALTER TABLE users ADD CONSTRAINT "FK_users_stores_StoreId" FOREIGN KEY ("StoreId") REFERENCES stores ("Id") ON DELETE CASCADE;

CREATE INDEX "IX_users_StoreCode" ON users ("StoreCode");

CREATE INDEX "IX_users_StoreId_Role" ON users ("StoreId", "Role");

CREATE UNIQUE INDEX "IX_app_settings_StoreId" ON app_settings ("StoreId");

CREATE UNIQUE INDEX "IX_categories_StoreId_Name" ON categories ("StoreId", "Name");

CREATE UNIQUE INDEX "IX_products_StoreId_QrCode" ON products ("StoreId", "QrCode");

CREATE UNIQUE INDEX "IX_products_StoreId_Barcode" ON products ("StoreId", "Barcode") WHERE "Barcode" IS NOT NULL;

CREATE INDEX "IX_products_StoreId_CategoryId" ON products ("StoreId", "CategoryId");

CREATE INDEX "IX_products_StoreId_IsFavorite" ON products ("StoreId", "IsFavorite");

CREATE INDEX "IX_stock_movements_StoreId_ProductId" ON stock_movements ("StoreId", "ProductId");

CREATE INDEX "IX_stock_movements_StoreId_CreatedAt" ON stock_movements ("StoreId", "CreatedAt");

CREATE UNIQUE INDEX "IX_sales_StoreId_ReceiptNo" ON sales ("StoreId", "ReceiptNo");

CREATE UNIQUE INDEX "IX_sales_StoreId_ClientId" ON sales ("StoreId", "ClientId");

CREATE INDEX "IX_sales_StoreId_ServerCreatedAt" ON sales ("StoreId", "ServerCreatedAt");

CREATE INDEX "IX_sale_items_StoreId_SaleId" ON sale_items ("StoreId", "SaleId");

CREATE UNIQUE INDEX "IX_customers_StoreId_QrCode" ON customers ("StoreId", "QrCode");

CREATE INDEX "IX_customers_StoreId_IsShortcut" ON customers ("StoreId", "IsShortcut");

CREATE INDEX "IX_credit_transactions_StoreId_CustomerId" ON credit_transactions ("StoreId", "CustomerId");

CREATE INDEX "IX_credit_transactions_StoreId_CreatedAt" ON credit_transactions ("StoreId", "CreatedAt");

CREATE UNIQUE INDEX "IX_idempotency_records_StoreId_ClientId" ON idempotency_records ("StoreId", "ClientId");

CREATE UNIQUE INDEX "IX_stores_Code" ON stores ("Code");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261012132842_AddMultiTenancySupport', '10.0.12');

COMMIT;

