namespace eiibd26.Domain.Constants
{
    public static class EstadoPublicacion
    {
        public const int Borrador = 0;
        public const int Publicado = 1;
        public const int Destacado = 2;
        public const int Archivado = 3;

        public static readonly int[] Visibles = { Publicado, Destacado, Archivado };
    }
}
