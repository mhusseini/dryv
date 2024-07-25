namespace Dryv.SampleConsole.Models
{
    public class HomeModel
    {
        public MyEnum Enum { get; set; }
        // public Address BillingAddress { get; set; }
        //
        // public Person Person { get; set; }
        //
        // public Address ShippingAddress { get; set; }

        private static DryvRules<HomeModel> ValidationRules = DryvRules.For<HomeModel>()
            .Rule(a => a.Enum, a => a.Enum == MyEnum.None ? "Please select a value." : null);
    }
}