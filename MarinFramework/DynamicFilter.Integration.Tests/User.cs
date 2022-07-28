using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DynamicFilter.Integration.Tests
{
    [Table("users")]
    public class User
    {
        [Key]
        public int ID { get; set; }

        public string Username { get; set; }
        public string FirstName { get; set; }
        public DateTime LastLogin { get; set; }
        public bool IsAdmin { get; set; }
        public SubscriptionType SubscriptionType { get; set; }
    }
}

