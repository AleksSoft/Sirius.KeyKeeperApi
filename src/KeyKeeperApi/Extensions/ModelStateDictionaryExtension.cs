using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace KeyKeeperApi.Extensions
{
    public static class ModelStateDictionaryExtension
    {
        public static void AddFormattedModelError(this ModelStateDictionary dictionary, string name, string error)
        {
            dictionary.AddModelError(name.ToCamelCase(), error);
        }
    }
}
