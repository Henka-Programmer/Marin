using DynamicFilter;

namespace DynamicFilterTests
{
    [TestClass]
    public abstract class DynamicFilterTestsBase
    {
        protected Model? model;

        [TestInitialize]
        public void Setup()
        {
            model = new Model("users");
            model.Columns["Name"] = new Column { Name = "Name", Type = typeof(string) };
            model.Columns["ID"] = new Column { Name = "ID", Type = typeof(int) };
            model.Columns["Birthday"] = new Column { Name = "Birthday", Type = typeof(DateTime) };
        }
    }
}
