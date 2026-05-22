namespace eiibd26.Models.Medico;

public class MedicoAreaEii
{
    public int MedicoPerfilId { get; set; }
    public int CondicionId { get; set; }

    public virtual MedicoPerfilExtendido? MedicoPerfil { get; set; }
    public virtual eiibd26.Models.condiciones? Condicion { get; set; }
}
