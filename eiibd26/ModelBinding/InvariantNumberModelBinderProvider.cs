using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace eiibd26.ModelBinding
{
    /// <summary>
    /// Provee <see cref="InvariantNumberModelBinder"/> para decimal/double/float (y nullables).
    /// Se registra en Program.cs JUSTO ANTES del <c>FloatingPointTypeModelBinderProvider</c> del
    /// framework, de modo que solo lo sustituye a él y no pisa a los providers de mayor prioridad
    /// ([FromBody], [FromServices], [FromHeader], IModelBinder custom).
    /// </summary>
    public sealed class InvariantNumberModelBinderProvider : IModelBinderProvider
    {
        /// <summary>Nombre del provider del framework al que sustituimos (es un tipo internal, por eso por nombre).</summary>
        public const string TargetProviderTypeName = "FloatingPointTypeModelBinderProvider";

        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var modelType = context.Metadata.ModelType;
            var underlying = Nullable.GetUnderlyingType(modelType) ?? modelType;

            return underlying == typeof(decimal) || underlying == typeof(double) || underlying == typeof(float)
                ? new InvariantNumberModelBinder(modelType)
                : null;
        }
    }
}
