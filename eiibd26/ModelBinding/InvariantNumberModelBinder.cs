using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace eiibd26.ModelBinding
{
    /// <summary>
    /// Model binder para <c>decimal</c>, <c>double</c> y <c>float</c> (y sus nullables) que parsea
    /// PRIMERO con <see cref="CultureInfo.InvariantCulture"/> y solo cae a la cultura de la request
    /// si el intento invariante falla.
    ///
    /// <para><b>Por qué.</b> Un <c>&lt;input type="number"&gt;</c> SIEMPRE postea el decimal con punto
    /// (<c>0.25</c>), sin importar el idioma del navegador — así lo define el estándar HTML. Pero el
    /// binder del framework (<c>FloatingPointTypeModelBinder</c>) parsea con la cultura de la request,
    /// y la app soporta es-ES/es (<c>UseRequestLocalization</c>), donde el punto es separador de MILES.
    /// Resultado: <c>0.25</c> se bindeaba como <c>25</c> (bug real: cantidad de ingrediente en el
    /// editor de platillos, 1/4 de taza guardado como 25).</para>
    ///
    /// <para><b>Por qué el fallback a cultura NO es opcional.</b> La app también tiene inputs
    /// renderizados por el servidor con <c>asp-for</c> (los hidden de Latitud/Longitud en
    /// PerfilMedico, UsuarioPerfil y DirectorioMedicos/Proponer). El InputTagHelper los formatea con
    /// la cultura ACTUAL, así que bajo es-ES el HTML sale con <c>value="19,4326"</c> y eso es lo que
    /// se vuelve a postear. Parsear eso como invariante-a-secas lo convertiría en <c>194326</c>. Por
    /// eso el intento invariante usa <see cref="NumberStyles.Float"/> — que NO admite separador de
    /// miles — y así <c>"19,4326"</c> falla limpio y lo recoge el fallback con la cultura correcta.</para>
    ///
    /// <para><b>Ambigüedad conocida.</b> <c>"1.500"</c> es 1.5 en invariante y 1500 en es-ES; no hay
    /// forma de distinguirlo sin saber el origen. Gana el invariante, que es el formato de los
    /// <c>type=number</c> y del JS. Los campos afectados en la app son <c>type=number</c> o los
    /// escribe JS con punto, así que en la práctica no se toca este caso.</para>
    /// </summary>
    public sealed class InvariantNumberModelBinder : IModelBinder
    {
        // Sin AllowThousands a propósito: es lo que hace fallar "19,4326" y "1.234,5" para que
        // caigan al fallback de cultura en lugar de parsearse mal en silencio.
        private const NumberStyles InvariantStyles = NumberStyles.Float;

        // Mismos estilos que usa el FloatingPointTypeModelBinder del framework.
        private const NumberStyles CultureStyles = NumberStyles.Float | NumberStyles.AllowThousands;

        private readonly Type _underlyingType;

        public InvariantNumberModelBinder(Type modelType)
        {
            _underlyingType = Nullable.GetUnderlyingType(modelType) ?? modelType;
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            ArgumentNullException.ThrowIfNull(bindingContext);

            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult == ValueProviderResult.None)
                return Task.CompletedTask;

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

            var value = valueProviderResult.FirstValue;
            if (string.IsNullOrEmpty(value))
            {
                // Igual que el binder del framework: no se setea Result, la propiedad conserva su
                // valor inicial (null en los nullables) y no se agrega error.
                return Task.CompletedTask;
            }

            var requestCulture = valueProviderResult.Culture ?? CultureInfo.CurrentCulture;

            if (TryParse(value, requestCulture, out var model))
                bindingContext.Result = ModelBindingResult.Success(model);
            else
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    bindingContext.ModelMetadata.ModelBindingMessageProvider
                        .ValueIsInvalidAccessor(valueProviderResult.ToString()));

            return Task.CompletedTask;
        }

        private bool TryParse(string value, CultureInfo requestCulture, out object? model)
        {
            if (_underlyingType == typeof(decimal))
            {
                if (decimal.TryParse(value, InvariantStyles, CultureInfo.InvariantCulture, out var d)
                    || decimal.TryParse(value, CultureStyles, requestCulture, out d))
                {
                    model = d;
                    return true;
                }
            }
            else if (_underlyingType == typeof(double))
            {
                if (double.TryParse(value, InvariantStyles, CultureInfo.InvariantCulture, out var db)
                    || double.TryParse(value, CultureStyles, requestCulture, out db))
                {
                    model = db;
                    return true;
                }
            }
            else if (_underlyingType == typeof(float))
            {
                if (float.TryParse(value, InvariantStyles, CultureInfo.InvariantCulture, out var f)
                    || float.TryParse(value, CultureStyles, requestCulture, out f))
                {
                    model = f;
                    return true;
                }
            }

            model = null;
            return false;
        }
    }
}
