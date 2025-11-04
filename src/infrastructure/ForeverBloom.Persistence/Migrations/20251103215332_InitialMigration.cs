using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ForeverBloom.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:ltree", ",,");

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    description = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    current_slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    image_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    image_alt_text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    path = table.Column<string>(type: "ltree", nullable: false),
                    parent_category_id = table.Column<long>(type: "bigint", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    publish_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp(6) with time zone", precision: 6, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp(6) with time zone", precision: 6, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp(6) with time zone", precision: 6, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                    table.CheckConstraint("ck_categories_current_slug_format", "\"current_slug\" ~ '^[a-z0-9]+(?:-[a-z0-9]+)*$'");
                    table.CheckConstraint("ck_categories_path_segments", "nlevel(\"path\") <= 10");
                    table.CheckConstraint("ck_categories_publish_status_valid_codes", "\"publish_status\" IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "fk_categories_categories_parent_category_id",
                        column: x => x.parent_category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "slug_registry",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    entity_type = table.Column<int>(type: "integer", nullable: false),
                    entity_id = table.Column<long>(type: "bigint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_slug_registry", x => x.id);
                    table.CheckConstraint("ck_slug_registry_entity_type_valid_codes", "\"entity_type\" IN (1, 2)");
                    table.CheckConstraint("ck_slug_registry_slug_format", "\"slug\" ~ '^[a-z0-9]+(?:-[a-z0-9]+)*$'");
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    seo_title = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    full_description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    meta_description = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    current_slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: false),
                    price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_featured = table.Column<bool>(type: "boolean", nullable: false),
                    publish_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    availability = table.Column<int>(type: "integer", nullable: false, defaultValue: 6),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp(6) with time zone", precision: 6, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp(6) with time zone", precision: 6, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp(6) with time zone", precision: 6, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                    table.CheckConstraint("ck_products_availability_valid_codes", "\"availability\" IN (1, 2, 3, 5, 6)");
                    table.CheckConstraint("ck_products_current_slug_format", "\"current_slug\" ~ '^[a-z0-9]+(?:-[a-z0-9]+)*$'");
                    table.CheckConstraint("ck_products_price_positive", "\"price\" > 0");
                    table.CheckConstraint("ck_products_publish_status_valid_codes", "\"publish_status\" IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "fk_products_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_images",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    image_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    image_alt_text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_images", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_images_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_categories_name_parent_category_id",
                table: "categories",
                columns: new[] { "name", "parent_category_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_categories_parent_category_id",
                table: "categories",
                column: "parent_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_categories_parent_category_id_publish_status_display_order",
                table: "categories",
                columns: new[] { "parent_category_id", "publish_status", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_categories_path",
                table: "categories",
                column: "path")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "ix_categories_publish_status_display_order",
                table: "categories",
                columns: new[] { "publish_status", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_product_images_product_id",
                table: "product_images",
                column: "product_id",
                unique: true,
                filter: "is_primary = true");

            migrationBuilder.CreateIndex(
                name: "ix_product_images_product_id_display_order",
                table: "product_images",
                columns: new[] { "product_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_products_category_id",
                table: "products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_category_id_publish_status_display_order",
                table: "products",
                columns: new[] { "category_id", "publish_status", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_slug_registry_entity_type_entity_id",
                table: "slug_registry",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_slug_registry_entity_type_entity_id_is_active",
                table: "slug_registry",
                columns: new[] { "entity_type", "entity_id", "is_active" },
                unique: true,
                filter: "\"is_active\" = true");

            migrationBuilder.CreateIndex(
                name: "ix_slug_registry_is_active",
                table: "slug_registry",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_slug_registry_slug",
                table: "slug_registry",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_images");

            migrationBuilder.DropTable(
                name: "slug_registry");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "categories");
        }
    }
}
