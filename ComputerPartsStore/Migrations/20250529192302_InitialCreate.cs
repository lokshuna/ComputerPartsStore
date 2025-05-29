using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ComputerPartsStore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Addresses",
                columns: table => new
                {
                    Address_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    City = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Region = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    House_Number = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addresses", x => x.Address_id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Catalogs",
                columns: table => new
                {
                    Catalog_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Accessory_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Catalogs", x => x.Catalog_id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Order_Statuses",
                columns: table => new
                {
                    Order_status_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Order_Statuses", x => x.Order_status_id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    User_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    User_login = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false),
                    User_password = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Address_id = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Patronymic = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Second_Name = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Phone_Number = table.Column<long>(type: "bigint", nullable: false),
                    Role_Name = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.User_id);
                    table.ForeignKey(
                        name: "FK_Users_Addresses_Address_id",
                        column: x => x.Address_id,
                        principalTable: "Addresses",
                        principalColumn: "Address_id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Accessories",
                columns: table => new
                {
                    Accessory_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Accessory_Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Accessory_Price = table.Column<float>(type: "float", nullable: false),
                    Catalog_id = table.Column<int>(type: "int", nullable: false),
                    Accessory_Availability = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Specifications = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accessories", x => x.Accessory_id);
                    table.ForeignKey(
                        name: "FK_Accessories_Catalogs_Catalog_id",
                        column: x => x.Catalog_id,
                        principalTable: "Catalogs",
                        principalColumn: "Catalog_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Order_lists",
                columns: table => new
                {
                    Order_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Overlay_id = table.Column<int>(type: "int", nullable: false),
                    Order_status_id = table.Column<int>(type: "int", nullable: false),
                    Customer_id = table.Column<int>(type: "int", nullable: false),
                    Order_Date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TrackingNumber = table.Column<string>(type: "longtext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Order_lists", x => x.Order_id);
                    table.ForeignKey(
                        name: "FK_Order_lists_Order_Statuses_Order_status_id",
                        column: x => x.Order_status_id,
                        principalTable: "Order_Statuses",
                        principalColumn: "Order_status_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Order_lists_Users_Customer_id",
                        column: x => x.Customer_id,
                        principalTable: "Users",
                        principalColumn: "User_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    Order_id = table.Column<int>(type: "int", nullable: false),
                    User_id = table.Column<int>(type: "int", nullable: false),
                    Last_Change = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Action = table.Column<string>(type: "longtext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => new { x.Order_id, x.User_id });
                    table.ForeignKey(
                        name: "FK_Logs_Order_lists_Order_id",
                        column: x => x.Order_id,
                        principalTable: "Order_lists",
                        principalColumn: "Order_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Logs_Users_User_id",
                        column: x => x.User_id,
                        principalTable: "Users",
                        principalColumn: "User_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Order_Items",
                columns: table => new
                {
                    Order_id = table.Column<int>(type: "int", nullable: false),
                    Accessory_id = table.Column<int>(type: "int", nullable: false),
                    Item_Price = table.Column<int>(type: "int", nullable: false),
                    Item_Count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Order_Items", x => new { x.Order_id, x.Accessory_id });
                    table.ForeignKey(
                        name: "FK_Order_Items_Accessories_Accessory_id",
                        column: x => x.Accessory_id,
                        principalTable: "Accessories",
                        principalColumn: "Accessory_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Order_Items_Order_lists_Order_id",
                        column: x => x.Order_id,
                        principalTable: "Order_lists",
                        principalColumn: "Order_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Addresses",
                columns: new[] { "Address_id", "City", "House_Number", "Region" },
                values: new object[,]
                {
                    { 1, "Львів", 1, "Львівська" },
                    { 2, "Львів", 2, "Львівська" }
                });

            migrationBuilder.InsertData(
                table: "Catalogs",
                columns: new[] { "Catalog_id", "Accessory_type" },
                values: new object[,]
                {
                    { 1, "Процесори" },
                    { 2, "Відеокарти" },
                    { 3, "Оперативна пам'ять" },
                    { 4, "Материнські плати" },
                    { 5, "Блоки живлення" },
                    { 6, "Накопичувачі" },
                    { 7, "Корпуси" }
                });

            migrationBuilder.InsertData(
                table: "Order_Statuses",
                columns: new[] { "Order_status_id", "Status" },
                values: new object[,]
                {
                    { 1, "Нове" },
                    { 2, "Прийняте" },
                    { 3, "Формується" },
                    { 4, "Сформоване" },
                    { 5, "Доставляється" },
                    { 6, "Доставлене" },
                    { 7, "Скасоване" }
                });

            migrationBuilder.InsertData(
                table: "Accessories",
                columns: new[] { "Accessory_id", "Accessory_Availability", "Accessory_Name", "Accessory_Price", "Catalog_id", "Specifications" },
                values: new object[,]
                {
                    { 1, "В наявності", "Intel Core i7-12700K", 12000f, 1, "12 ядер, 3.6-5.0 GHz, LGA1700" },
                    { 2, "В наявності", "AMD Ryzen 7 5800X", 10500f, 1, "8 ядер, 3.8-4.7 GHz, AM4" },
                    { 3, "В наявності", "NVIDIA RTX 4070", 23000f, 2, "12GB GDDR6X, 2475 MHz" },
                    { 4, "В наявності", "AMD RX 7800 XT", 21000f, 2, "16GB GDDR6, 2565 MHz" },
                    { 5, "В наявності", "Corsair Vengeance LPX 16GB", 2800f, 3, "DDR4-3200, 2x8GB, C16" },
                    { 6, "В наявності", "G.Skill Trident Z5 32GB", 5500f, 3, "DDR5-6000, 2x16GB, C36" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "User_id", "Address_id", "Name", "Patronymic", "Phone_Number", "Role_Name", "Second_Name", "User_login", "User_password" },
                values: new object[,]
                {
                    { 1, 1, "Оксана", "Петрівна", 380671234567L, "Operator", "Операторівна", "operator", "operator123" },
                    { 2, 2, "Микола", "Іванович", 380671234568L, "Storekeeper", "Комірник", "storekeeper", "store123" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accessories_Catalog_id",
                table: "Accessories",
                column: "Catalog_id");

            migrationBuilder.CreateIndex(
                name: "IX_Logs_User_id",
                table: "Logs",
                column: "User_id");

            migrationBuilder.CreateIndex(
                name: "IX_Order_Items_Accessory_id",
                table: "Order_Items",
                column: "Accessory_id");

            migrationBuilder.CreateIndex(
                name: "IX_Order_lists_Customer_id",
                table: "Order_lists",
                column: "Customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_Order_lists_Order_status_id",
                table: "Order_lists",
                column: "Order_status_id");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Address_id",
                table: "Users",
                column: "Address_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Logs");

            migrationBuilder.DropTable(
                name: "Order_Items");

            migrationBuilder.DropTable(
                name: "Accessories");

            migrationBuilder.DropTable(
                name: "Order_lists");

            migrationBuilder.DropTable(
                name: "Catalogs");

            migrationBuilder.DropTable(
                name: "Order_Statuses");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Addresses");
        }
    }
}
