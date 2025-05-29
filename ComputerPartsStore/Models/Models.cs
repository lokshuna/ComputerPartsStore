using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerPartsStore.Models
{
    public class User
    {
        [Key]
        public int User_id { get; set; }

        [Required]
        [StringLength(60)]
        public string User_login { get; set; }

        [Required]
        [StringLength(30)]
        public string User_password { get; set; }

        public int? Address_id { get; set; }

        [Required]
        [StringLength(20)]
        public string Name { get; set; }

        [StringLength(20)]
        public string Patronymic { get; set; }

        [Required]
        [StringLength(20)]
        public string Second_Name { get; set; }

        [Required]
        public long Phone_Number { get; set; }

        [Required]
        [StringLength(30)]
        public string Role_Name { get; set; }

        // Navigation properties
        public virtual Address Address { get; set; }
        public virtual ICollection<Log> Logs { get; set; }
    }

    public class Address
    {
        [Key]
        public int Address_id { get; set; }

        [Required]
        [StringLength(20)]
        public string City { get; set; }

        [Required]
        [StringLength(20)]
        public string Region { get; set; }

        [Required]
        public int House_Number { get; set; }

        // Navigation properties
        public virtual ICollection<User> Users { get; set; }
    }

    public class Catalog
    {
        [Key]
        public int Catalog_id { get; set; }

        [Required]
        [StringLength(40)]
        public string Accessory_type { get; set; }

        // Navigation properties
        public virtual ICollection<Accessories> Accessories { get; set; }
    }

    public class Accessories
    {
        [Key]
        public int Accessory_id { get; set; }

        [Required]
        [StringLength(200)]
        public string Accessory_Name { get; set; }

        [Required]
        public float Accessory_Price { get; set; }

        [Required]
        public int Catalog_id { get; set; }

        [Required]
        [StringLength(20)]
        public string Accessory_Availability { get; set; }

        [Required]
        [StringLength(200)]
        public string Specifications { get; set; }

        // Navigation properties
        public virtual Catalog Catalog { get; set; }
        public virtual ICollection<Order_Item> Order_Items { get; set; }
    }

    public class Order_Status
    {
        [Key]
        public int Order_status_id { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; }

        // Navigation properties
        public virtual ICollection<Order_list> Order_lists { get; set; }
    }

    public class Order_list
    {
        [Key]
        public int Order_id { get; set; }

        [Required]
        public int Overlay_id { get; set; }

        [Required]
        public int Order_status_id { get; set; }

        [Required]
        public int Customer_id { get; set; }

        public DateTime Order_Date { get; set; } = DateTime.Now;

        public string TrackingNumber { get; set; }

        // Navigation properties
        public virtual Order_Status Order_Status { get; set; }
        public virtual User Customer { get; set; }
        public virtual ICollection<Order_Item> Order_Items { get; set; }
        public virtual ICollection<Log> Logs { get; set; }
    }

    public class Order_Item
    {
        [Key, Column(Order = 0)]
        public int Order_id { get; set; }

        [Key, Column(Order = 1)]
        public int Accessory_id { get; set; }

        [Required]
        public int Item_Price { get; set; }

        [Required]
        public int Item_Count { get; set; }

        // Navigation properties
        public virtual Order_list Order_list { get; set; }
        public virtual Accessories Accessories { get; set; }
    }

    public class Log
    {
        [Key, Column(Order = 0)]
        public int Order_id { get; set; }

        [Key, Column(Order = 1)]
        public int User_id { get; set; }

        [Required]
        public DateTime Last_Change { get; set; }

        public string Action { get; set; }

        // Navigation properties
        public virtual Order_list Order_list { get; set; }
        public virtual User User { get; set; }
    }

    // View Models
    public class CartItem
    {
        public int Accessory_id { get; set; }
        public string Accessory_Name { get; set; }
        public float Accessory_Price { get; set; }
        public int Quantity { get; set; }
        public float Total => Accessory_Price * Quantity;
    }

    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Логін")]
        public string Login { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; }

        [Display(Name = "Запам'ятати мене")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Логін")]
        public string Login { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; }

        [Required]
        [Display(Name = "Ім'я")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Прізвище")]
        public string SecondName { get; set; }

        [Display(Name = "По батькові")]
        public string Patronymic { get; set; }

        [Required]
        [Display(Name = "Телефон")]
        public long PhoneNumber { get; set; }

        [Required]
        [Display(Name = "Місто")]
        public string City { get; set; }

        [Required]
        [Display(Name = "Регіон")]
        public string Region { get; set; }

        [Required]
        [Display(Name = "Номер будинку")]
        public int HouseNumber { get; set; }
    }

    public class CheckoutViewModel
    {
        public List<CartItem> CartItems { get; set; }
        public float TotalAmount { get; set; }
        public User Customer { get; set; }
        public Address DeliveryAddress { get; set; }
        public string Notes { get; set; }
    }
}