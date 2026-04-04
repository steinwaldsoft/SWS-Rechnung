using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SWSRechnung.Infrastructure
{
    /// <summary>
    /// Liest decimal-Felder immer mit InvariantCulture (Punkt als Dezimaltrennzeichen),
    /// wie es <input type="number"> im Browser sendet – unabhängig von der Server-Kultur.
    /// </summary>
    public class InvariantDecimalModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var result = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (result == ValueProviderResult.None)
                return Task.CompletedTask;

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, result);

            var raw = result.FirstValue?.Trim();
            if (string.IsNullOrEmpty(raw))
            {
                bindingContext.Result = ModelBindingResult.Success(0m);
                return Task.CompletedTask;
            }

            // Komma → Punkt normalisieren, falls der Browser doch Komma sendet
            raw = raw.Replace(',', '.');

            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                bindingContext.Result = ModelBindingResult.Success(value);
            else
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName,
                    $"'{raw}' ist kein gültiger Dezimalwert.");

            return Task.CompletedTask;
        }
    }

    public class InvariantDecimalModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var type = context.Metadata.ModelType;
            if (type == typeof(decimal) || type == typeof(decimal?))
                return new InvariantDecimalModelBinder();

            return null;
        }
    }
}
