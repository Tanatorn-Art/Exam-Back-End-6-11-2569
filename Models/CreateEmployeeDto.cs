using System.ComponentModel.DataAnnotations;

namespace ToyotaExamApp.Models
{
    /// <summary>
    /// DTO สำหรับรับข้อมูลการสร้างพนักงานใหม่จากหน้าบ้าน (POST Request Body)
    /// มี Data Annotations เพื่อตรวจสอบความถูกต้องของข้อมูล (Validation)
    /// </summary>
    public class CreateEmployeeDto
    {
        [Required(ErrorMessage = "กรุณากรอกรหัสพนักงาน")]
        [StringLength(50, ErrorMessage = "รหัสพนักงานต้องมีความยาวไม่เกิน 50 ตัวอักษร")]
        public string EmpCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอกชื่อ-นามสกุล")]
        [StringLength(150, ErrorMessage = "ชื่อ-นามสกุลต้องมีความยาวไม่เกิน 150 ตัวอักษร")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอกแผนก")]
        [StringLength(100, ErrorMessage = "แผนกต้องมีความยาวไม่เกิน 100 ตัวอักษร")]
        public string Department { get; set; } = string.Empty;

        [Range(0, 999999999.99, ErrorMessage = "เงินเดือนต้องเป็นตัวเลขมากกว่าหรือเท่ากับ 0")]
        public decimal Salary { get; set; }
    }
}
