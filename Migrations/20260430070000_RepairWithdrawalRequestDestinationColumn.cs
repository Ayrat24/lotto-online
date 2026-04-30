using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniApp.Migrations
{
    [DbContext(typeof(Data.AppDbContext))]
    [Migration("20260430070000_RepairWithdrawalRequestDestinationColumn")]
    public sealed class RepairWithdrawalRequestDestinationColumn : Migration
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
                              AND column_name = 'Destination'
                        ) AND NOT EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = 'public'
                              AND table_name = 'withdrawal_requests'
                              AND column_name = 'Number'
                        ) THEN
                            EXECUTE 'ALTER TABLE public.withdrawal_requests RENAME COLUMN "Destination" TO "Number"';
                        ELSIF EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = 'public'
                              AND table_name = 'withdrawal_requests'
                              AND column_name = 'destination'
                        ) AND NOT EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = 'public'
                              AND table_name = 'withdrawal_requests'
                              AND column_name = 'Number'
                        ) THEN
                            EXECUTE 'ALTER TABLE public.withdrawal_requests RENAME COLUMN destination TO "Number"';
                        END IF;

                        IF EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = 'public'
                              AND table_name = 'withdrawal_requests'
                              AND column_name = 'Destination'
                        ) AND EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = 'public'
                              AND table_name = 'withdrawal_requests'
                              AND column_name = 'Number'
                        ) THEN
                            EXECUTE 'UPDATE public.withdrawal_requests SET "Number" = COALESCE(NULLIF("Number", ''''), "Destination") WHERE "Destination" IS NOT NULL';
                            EXECUTE 'ALTER TABLE public.withdrawal_requests DROP COLUMN "Destination"';
                        END IF;

                        IF EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = 'public'
                              AND table_name = 'withdrawal_requests'
                              AND column_name = 'destination'
                        ) AND EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = 'public'
                              AND table_name = 'withdrawal_requests'
                              AND column_name = 'Number'
                        ) THEN
                            EXECUTE 'UPDATE public.withdrawal_requests SET "Number" = COALESCE(NULLIF("Number", ''''), destination) WHERE destination IS NOT NULL';
                            EXECUTE 'ALTER TABLE public.withdrawal_requests DROP COLUMN destination';
                        END IF;
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

