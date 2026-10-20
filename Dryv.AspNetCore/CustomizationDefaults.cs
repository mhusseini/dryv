using Dryv.AspNetCore.DynamicControllers;
using Dryv.AspNetCore.Internal;

namespace Dryv.AspNetCore
{
    public static class CustomizationDefaults
    {
        public static DryvDynamicControllerMethods DefaultHttpMethod(DryvControllerGenerationContext context) => DryvDynamicControllerMethods.Post;

        public static string DefaultRoute(DryvControllerGenerationContext context) => $"_v/c{Md5Helper.GetShortMd5(context.ControllerFullName, 8)}";
    }
}