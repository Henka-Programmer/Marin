using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DynamicFilter.Integration.Tests
{
    [Table("testTable")]
    public class TestModel
    {
        [Key]
        public int ID { get; set; }
        public string? StringProperty { get; set; }
        public decimal DecimalProperty { get; set; }
        public DateTime DateTimeProperty { get; set; }
        public double DoubleProperty { get; set; }
        public bool BooleanProperty { get; set; }

        public int? NullableIntegerProperty { get; set; }
    }
}

