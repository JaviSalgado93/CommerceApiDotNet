namespace Domain.Entities
{
    /// <summary>
    /// Representa un municipio de Colombia.
    /// </summary>
    public class Municipality
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public int DepartmentId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Relaciones
        public virtual Department? Department { get; set; }
        public virtual ICollection<Merchant> Merchants { get; set; } = new List<Merchant>();
    }
}
