namespace Domain.Entities
{
    /// <summary>
    /// Representa un comerciante registrado en el sistema.
    /// </summary>
    public class Merchant
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int MunicipalityId { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public Guid? CreatedByUserId { get; set; }

        // Relaciones
        public virtual Municipality? Municipality { get; set; }
        public virtual User? CreatedByUser { get; set; }
        public virtual ICollection<Establishment> Establishments { get; set; } = new List<Establishment>();
    }
}