using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Erp.Migrations
{
    /// <inheritdoc />
    public partial class TagsArray : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL manual porque o AlterColumn gerado pelo EF (text -> text[]) é
            // recusado pelo Postgres: não existe cast implícito entre os dois, e
            // o EF não emite a cláusula USING. Além disso a coluna pode ou não
            // existir no banco (foi dropada à mão enquanto a migration anterior
            // falhava), então os dois casos são tratados aqui.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns
                               WHERE table_name = 'AspNetUsers' AND column_name = 'Tag') THEN

                        -- O default '' da coluna text não é um text[] válido:
                        -- precisa sair antes do ALTER TYPE.
                        ALTER TABLE "AspNetUsers" ALTER COLUMN "Tag" DROP DEFAULT;

                        ALTER TABLE "AspNetUsers"
                        ALTER COLUMN "Tag" TYPE text[]
                        USING CASE
                            WHEN "Tag" IS NULL OR btrim("Tag") = '' THEN ARRAY[]::text[]
                            ELSE string_to_array("Tag", ',')
                        END;

                        ALTER TABLE "AspNetUsers" ALTER COLUMN "Tag" DROP NOT NULL;
                    ELSE
                        ALTER TABLE "AspNetUsers" ADD COLUMN "Tag" text[];
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "AspNetUsers"
                ALTER COLUMN "Tag" TYPE text
                USING COALESCE(array_to_string("Tag", ','), '');

                ALTER TABLE "AspNetUsers" ALTER COLUMN "Tag" SET DEFAULT '';
                ALTER TABLE "AspNetUsers" ALTER COLUMN "Tag" SET NOT NULL;
                """);
        }
    }
}
