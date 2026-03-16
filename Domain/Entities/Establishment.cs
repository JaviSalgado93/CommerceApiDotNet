namespace Domain.Entities
{
    /// <summary>
    /// Representa un establecimiento/sucursal de un comerciante.
    /// </summary>
    public class Establishment
    {
        public int Id { get; set; }
        public int MerchantId { get; set; }
        public string Name { get; set; }
        public decimal Revenue { get; set; }
        public int EmployeeCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        // Relación
        public virtual Merchant? Merchant { get; set; }
    }
}