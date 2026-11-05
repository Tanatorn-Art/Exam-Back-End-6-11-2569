namespace ToyotaExamApp.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string EmpCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}