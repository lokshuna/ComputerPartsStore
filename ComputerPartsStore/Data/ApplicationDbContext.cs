using Microsoft.EntityFrameworkCore;
using ComputerPartsStore.Models;
using System.Net;

namespace ComputerPartsStore.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Catalog> Catalogs { get; set; }
        public DbSet<Accessories> Accessories { get; set; }
        public DbSet<Order_Status> Order_Statuses { get; set; }
        public DbSet<Order_list> Order_lists { get; set; }
        public DbSet<Order_Item> Order_Items { get; set; }
        public DbSet<Log> Logs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure composite keys
            modelBuilder.Entity<Order_Item>()
                .HasKey(oi => new { oi.Order_id, oi.Accessory_id });

            modelBuilder.Entity<Log>()
                .HasKey(l => new { l.Order_id, l.User_id });

            // Configure relationships
            modelBuilder.Entity<User>()
                .HasOne(u => u.Address)
                .WithMany(a => a.Users)
                .HasForeignKey(u => u.Address_id)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Accessories>()
                .HasOne(a => a.Catalog)
                .WithMany(c => c.Accessories)
                .HasForeignKey(a => a.Catalog_id);

            modelBuilder.Entity<Order_list>()
                .HasOne(ol => ol.Order_Status)
                .WithMany(os => os.Order_lists)
                .HasForeignKey(ol => ol.Order_status_id);

            modelBuilder.Entity<Order_list>()
                .HasOne(ol => ol.Customer)
                .WithMany()
                .HasForeignKey(ol => ol.Customer_id);

            modelBuilder.Entity<Order_Item>()
                .HasOne(oi => oi.Order_list)
                .WithMany(ol => ol.Order_Items)
                .HasForeignKey(oi => oi.Order_id);

            modelBuilder.Entity<Order_Item>()
                .HasOne(oi => oi.Accessories)
                .WithMany(a => a.Order_Items)
                .HasForeignKey(oi => oi.Accessory_id);

            modelBuilder.Entity<Log>()
                .HasOne(l => l.Order_list)
                .WithMany(ol => ol.Logs)
                .HasForeignKey(l => l.Order_id);

            modelBuilder.Entity<Log>()
                .HasOne(l => l.User)
                .WithMany(u => u.Logs)
                .HasForeignKey(l => l.User_id);

            // Seed data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Order Statuses
            modelBuilder.Entity<Order_Status>().HasData(
                new Order_Status { Order_status_id = 1, Status = "Нове" },
                new Order_Status { Order_status_id = 2, Status = "Прийняте" },
                new Order_Status { Order_status_id = 3, Status = "Формується" },
                new Order_Status { Order_status_id = 4, Status = "Сформоване" },
                new Order_Status { Order_status_id = 5, Status = "Доставляється" },
                new Order_Status { Order_status_id = 6, Status = "Доставлене" },
                new Order_Status { Order_status_id = 7, Status = "Скасоване" }
            );

            // Seed Catalogs
            modelBuilder.Entity<Catalog>().HasData(
                new Catalog { Catalog_id = 1, Accessory_type = "Процесори" },
                new Catalog { Catalog_id = 2, Accessory_type = "Відеокарти" },
                new Catalog { Catalog_id = 3, Accessory_type = "Оперативна пам'ять" },
                new Catalog { Catalog_id = 4, Accessory_type = "Материнські плати" },
                new Catalog { Catalog_id = 5, Accessory_type = "Блоки живлення" },
                new Catalog { Catalog_id = 6, Accessory_type = "Накопичувачі" },
                new Catalog { Catalog_id = 7, Accessory_type = "Корпуси" }
            );

            // Seed Sample Accessories
            modelBuilder.Entity<Accessories>().HasData(
                new Accessories { Accessory_id = 1, Accessory_Name = "Intel Core i7-12700K", Accessory_Price = 12000, Catalog_id = 1, Accessory_Availability = "В наявності", Specifications = "12 ядер, 3.6-5.0 GHz, LGA1700" },
                new Accessories { Accessory_id = 2, Accessory_Name = "AMD Ryzen 7 5800X", Accessory_Price = 10500, Catalog_id = 1, Accessory_Availability = "В наявності", Specifications = "8 ядер, 3.8-4.7 GHz, AM4" },
                new Accessories { Accessory_id = 3, Accessory_Name = "NVIDIA RTX 4070", Accessory_Price = 23000, Catalog_id = 2, Accessory_Availability = "В наявності", Specifications = "12GB GDDR6X, 2475 MHz" },
                new Accessories { Accessory_id = 4, Accessory_Name = "AMD RX 7800 XT", Accessory_Price = 21000, Catalog_id = 2, Accessory_Availability = "В наявності", Specifications = "16GB GDDR6, 2565 MHz" },
                new Accessories { Accessory_id = 5, Accessory_Name = "Corsair Vengeance LPX 16GB", Accessory_Price = 2800, Catalog_id = 3, Accessory_Availability = "В наявності", Specifications = "DDR4-3200, 2x8GB, C16" },
                new Accessories { Accessory_id = 6, Accessory_Name = "G.Skill Trident Z5 32GB", Accessory_Price = 5500, Catalog_id = 3, Accessory_Availability = "В наявності", Specifications = "DDR5-6000, 2x16GB, C36" }
            );

            // Seed Default Users
            modelBuilder.Entity<Address>().HasData(
                new Address { Address_id = 1, City = "Львів", Region = "Львівська", House_Number = 1 },
                new Address { Address_id = 2, City = "Львів", Region = "Львівська", House_Number = 2 }
            );

            modelBuilder.Entity<User>().HasData(
                new User { User_id = 1, User_login = "operator", User_password = "operator123", Name = "Оксана", Second_Name = "Операторівна", Patronymic = "Петрівна", Phone_Number = 380671234567, Role_Name = "Operator", Address_id = 1 },
                new User { User_id = 2, User_login = "storekeeper", User_password = "store123", Name = "Микола", Second_Name = "Комірник", Patronymic = "Іванович", Phone_Number = 380671234568, Role_Name = "Storekeeper", Address_id = 2 }
            );
        }
    }
}