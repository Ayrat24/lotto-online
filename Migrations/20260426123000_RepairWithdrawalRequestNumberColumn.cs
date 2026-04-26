using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniApp.Migrations
{
    [DbContext(typeof(Data.AppDbContext))]
    [Migration("20260426123000_RepairWithdrawalRequestNumberColumn")]
    public sealed class RepairWithdrawalRequestNumberColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.tables
                        WHERE table_schema = 'public'
                          AND table_name = 'withdrawal_requests'
                    ) THEN
                        IF EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = 'public'
                              AND table_name = 'withdrawal_requests'
                              AND column_name = 'number'
                        ) AND NOT EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = 'public'
                              AND table_name = 'withdrawal_requests'
                              AND column_name = 'Number'
                        ) THEN
                            EXECUTE 'ALTER TABLE public.withdrawal_requests RENAME COLUMN number TO "Number"';
                        ELSIF EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = 'public'
                              AND table_name = 'withdrawal_requests'
                              AND column_name = 'Address'
                        ) AND NOT EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = 'public'
                              AND table_name = 'withdrawal_requests'
                              AND column_name = 'Number'
                        ) THEN
                            EXECUTE 'ALTER TABLE public.withdrawal_requests RENAME COLUMN "Address" TO "Number"';
                        ELSIF EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = 'public'
                              AND table_name = 'withdrawal_requests'
                              AND column_name = 'WalletAddress'
                        ) AND NOT EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = 'public'
                              AND table_name = 'withdrawal_requests'
                              AND column_name = 'Number'
                        ) THEN
                            EXECUTE 'ALTER TABLE public.withdrawal_requests RENAME COLUMN "WalletAddress" TO "Number"';
                        END IF;

                        IF NOT EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = 'public'
                              AND table_name = 'withdrawal_requests'
                              AND column_name = 'Number'
                        ) THEN
                            EXECUTE 'ALTER TABLE public.withdrawal_requests ADD COLUMN "Number" character varying(256)';

                            IF EXISTS (
                                SELECT 1
                                FROM information_schema.columns
                                WHERE table_schema = 'public'
                                  AND table_name = 'users'
                                  AND column_name = 'WalletAddress'
                            ) THEN
                                EXECUTE 'UPDATE public.withdrawal_requests AS w SET "Number" = COALESCE(u."WalletAddress", '''') FROM public.users AS u WHERE u."Id" = w."UserId"';
                            END IF;
                        END IF;

                        IF NOT EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = 'public'
                              AND table_name = 'withdrawal_requests'
                              AND column_name = 'ExternalPayoutId'
                        ) THEN
                            EXECUTE 'ALTER TABLE public.withdrawal_requests ADD COLUMN "ExternalPayoutId" character varying(128)';
                        END IF;

                        IF NOT EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = 'public'
                              AND table_name = 'withdrawal_requests'
                              AND column_name = 'ExternalPayoutState'
                        ) THEN
                            EXECUTE 'ALTER TABLE public.withdrawal_requests ADD COLUMN "ExternalPayoutState" character varying(64)';
                        END IF;

                        IF NOT EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = 'public'
                              AND table_name = 'withdrawal_requests'
                              AND column_name = 'ExternalPayoutCreatedAtUtc'
                        ) THEN
                            EXECUTE 'ALTER TABLE public.withdrawal_requests ADD COLUMN "ExternalPayoutCreatedAtUtc" timestamp with time zone';
                        END IF;

                        EXECUTE 'UPDATE public.withdrawal_requests SET "Number" = '''' WHERE "Number" IS NULL';
                        EXECUTE 'ALTER TABLE public.withdrawal_requests ALTER COLUMN "Number" TYPE character varying(256)';
                        EXECUTE 'ALTER TABLE public.withdrawal_requests ALTER COLUMN "Number" SET NOT NULL';
                        EXECUTE 'CREATE UNIQUE INDEX IF NOT EXISTS "IX_withdrawal_requests_ExternalPayoutId" ON public.withdrawal_requests ("ExternalPayoutId") WHERE "ExternalPayoutId" IS NOT NULL';
                    END IF;
                END
                $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}


