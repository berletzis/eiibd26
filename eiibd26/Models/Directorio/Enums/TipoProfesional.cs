namespace eiibd26.Models.Directorio.Enums;

/// <summary>
/// Tipo de profesional de la salud. Es el dato ESTRUCTURADO que ramifica lógica
/// (qué se le sugiere validar primero); <see cref="MedicoDirectorio.Especialidad"/>
/// sigue siendo el texto libre que se muestra ("Gastroenterología pediátrica").
///
/// NO es un permiso: el candado de validación sigue siendo el rol "Medico".
/// Un nutriólogo puede validar un término clínico si quiere — esto solo ordena
/// lo que ve primero.
///
/// Nullable en la ficha: los profesionales existentes quedan en null = "general"
/// y conservan el comportamiento actual (TOP clínico).
/// </summary>
public enum TipoProfesional : byte
{
    MedicoEspecialistaEII = 1,
    Nutriologo = 2,
    Otro = 3
}
