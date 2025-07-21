using Dryv.Rules;

namespace Dryv.SampleConsole.Models
{
    public class HomeModel
    {
        public MyEnum Enum { get; set; }

        public string Value { get; set; }
        // public Address BillingAddress { get; set; }
        //
        // public Person Person { get; set; }
        //
        // public Address ShippingAddress { get; set; }

        private static DryvRules<HomeModel> ValidationRules = DryvRules.For<HomeModel>()
            .Parameter("hallo", () => "welt")
            .Rule<DryvParameters>(a => a.Enum, (a, p) => a.Value == p.Get<string>("hallo") ? "Please select a value." : null);
    }
}