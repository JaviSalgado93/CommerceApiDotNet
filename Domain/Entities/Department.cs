namespace Domain.Entities
{
    /// <summary>
    /// Representa un departamento de Colombia.
    /// </summary>
    public class Department
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string? Region { get; set; }
        public DateTime CreatedAt { get; set; }

        // Relaciones
        public virtual ICollection<Municipality> Municipalities { get; set; } = new List<Municipality>();
    }
}
