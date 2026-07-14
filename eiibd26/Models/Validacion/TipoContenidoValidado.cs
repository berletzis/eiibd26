namespace eiibd26.Models.Validacion
{
    public enum TipoContenidoValidado
    {
        Termino      = 1,
        Articulo     = 2,
        PerfilMedico = 3,
        // Nota clínica de Platillos (grupo o ingrediente). ContenidoId = PlatNotaClinica.Id.
        // La llave única (TipoContenido, ContenidoId, UsuarioMedicoId) está particionada por tipo,
        // así que el Id de una nota nunca colisiona con el de un Termino/Articulo/PerfilMedico.
        NotaClinicaIngrediente = 4
    }
}
