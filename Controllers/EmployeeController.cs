using ClosedXML.Excel;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using ToyotaExamApp.Models;

namespace ToyotaExamApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeController : ControllerBase
    {
        // Connection String สำหรับเชื่อมต่อไปยังฐานข้อมูล SQL Server (TestExamDB)
        private readonly string _connectionString;

        public EmployeeController(IConfiguration configuration)
        {
            // ดึง Connection String จาก appsettings.json หรือใช้ค่า Default
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Server=localhost;Database=TestExamDB;User Id=sa;Password=1;TrustServerCertificate=true;";
        }

        // =========================================================================
        // 1. GET: ดึงข้อมูลพนักงานตามแผนก (ถ้าไม่ส่งแผนกจะดึงตาม stored procedure)
        // URL: GET /api/Employee/by-department?department={department}
        // =========================================================================
        [HttpGet("by-department")]
        public async Task<IActionResult> GetByDepartment([FromQuery] string? department)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                var parameter = new DynamicParameters();
                parameter.Add(@"Department", department, DbType.String);

                // เรียกใช้งาน Stored Procedure: sp_GetEmployeesByDept
                var result = await connection.QueryAsync<Employee>(
                    "sp_GetEmployeesByDept",
                    parameter,
                    commandType: CommandType.StoredProcedure
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "เกิดข้อผิดพลาดในการดึงข้อมูลตามแผนก",
                    error = ex.Message
                });
            }
        }

        // =========================================================================
        // 2. GET: ดึงข้อมูลพนักงานที่ได้รับเงินเดือนสูงสุด (จาก View: vw_HighSalaryEmployee)
        // URL: GET /api/Employee/highest-salary
        // =========================================================================
        [HttpGet("highest-salary")]
        public async Task<IActionResult> GetHighestSalary()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                // ดึงข้อมูลจาก View ใน SQL Server
                var result = await connection.QueryAsync<Employee>(
                    "SELECT * FROM vw_HighSalaryEmployee"
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "เกิดข้อผิดพลาดในการดึงข้อมูลเงินเดือนสูงสุด",
                    error = ex.Message
                });
            }
        }

        // =========================================================================
        // 3. GET: ส่งออกข้อมูลพนักงานทั้งหมดเป็นไฟล์ Excel (.xlsx)
        // URL: GET /api/Employee/export-excel
        // =========================================================================
        [HttpGet("export-excel")]
        public async Task<IActionResult> ExportToExcel()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                var result = (await connection.QueryAsync<Employee>("SELECT * FROM Employees")).ToList();

                // สร้าง Workbook ด้วย ClosedXML
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Employees");

                // กำหนดหัวตาราง (Header) ในแถวที่ 1
                worksheet.Cell(1, 1).Value = "รหัสพนักงาน";
                worksheet.Cell(1, 2).Value = "ชื่อ-นามสกุล";
                worksheet.Cell(1, 3).Value = "แผนก";
                worksheet.Cell(1, 4).Value = "เงินเดือน";
                worksheet.Row(1).Style.Font.Bold = true;
                worksheet.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");

                // วนลูปใส่ข้อมูลพนักงานตั้งแต่แถวที่ 2 เป็นต้นไป
                for (int i = 0; i < result.Count; i++)
                {
                    var row = i + 2;
                    worksheet.Cell(row, 1).Value = result[i].EmpCode;
                    worksheet.Cell(row, 2).Value = result[i].FullName;
                    worksheet.Cell(row, 3).Value = result[i].Department;
                    worksheet.Cell(row, 4).Value = result[i].Salary;
                }

                worksheet.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                var content = stream.ToArray();

                // ส่งไฟล์กลับเป็น application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
                return File(
                    content,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Employees_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
                );
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "เกิดข้อผิดพลาดในการส่งออกไฟล์ Excel",
                    error = ex.Message
                });
            }
        }

        // =========================================================================
        // 4. POST: เพิ่มพนักงานใหม่ (Create Employee)
        // URL: POST /api/Employee
        // Request Body: JSON { empCode, fullName, department, salary }
        // =========================================================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEmployeeDto dto)
        {
            // 4.1 ตรวจสอบ Data Annotations Validation (Required, Range, StringLength)
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "ข้อมูลไม่ถูกต้อง กรุณาตรวจสอบข้อมูลที่กรอก",
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            try
            {
                using var connection = new SqlConnection(_connectionString);

                // 4.2 ตรวจสอบว่ามีรหัสพนักงาน (EmpCode) ซ้ำในระบบหรือไม่
                const string checkSql = "SELECT COUNT(1) FROM Employees WHERE EmpCode = @EmpCode";
                var count = await connection.ExecuteScalarAsync<int>(checkSql, new { dto.EmpCode });
                if (count > 0)
                {
                    return Conflict(new
                    {
                        message = $"รหัสพนักงาน '{dto.EmpCode}' มีอยู่ในระบบแล้ว กรุณาใช้รหัสอื่น"
                    });
                }

                // 4.3 คำสั่ง SQL Parameterized เพื่อป้องกัน SQL Injection
                // SCOPE_IDENTITY() ใช้ดึงค่า Id (Auto Increment) ที่เพิ่งถูก Insert
                const string insertSql = @"
                    INSERT INTO Employees (EmpCode, FullName, Department, Salary, CreatedAt)
                    VALUES (@EmpCode, @FullName, @Department, @Salary, GETDATE());
                    SELECT CAST(SCOPE_IDENTITY() as int);";

                var newId = await connection.QuerySingleAsync<int>(insertSql, new
                {
                    dto.EmpCode,
                    dto.FullName,
                    dto.Department,
                    dto.Salary
                });

                var createdEmployee = new Employee
                {
                    Id = newId,
                    EmpCode = dto.EmpCode,
                    FullName = dto.FullName,
                    Department = dto.Department,
                    Salary = dto.Salary,
                    CreatedAt = DateTime.Now
                };

                // ส่งกลับเป็น 201 Created พร้อมข้อมูลพนักงานที่ถูกสร้าง
                return StatusCode(StatusCodes.Status201Created, new
                {
                    message = "เพิ่มพนักงานสำเร็จเรียบร้อยแล้ว",
                    data = createdEmployee
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "เกิดข้อผิดพลาดในการบันทึกข้อมูลพนักงาน",
                    error = ex.Message
                });
            }
        }

        // =========================================================================
        // 5. POST: อัปโหลดและนำเข้าพนักงานจากไฟล์ Excel (Import Excel)
        // URL: POST /api/Employee/import-excel
        // Content-Type: multipart/form-data (Key: 'file')
        // =========================================================================
        [HttpPost("import-excel")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            // 5.1 ตรวจสอบว่าผู้ใช้ส่งไฟล์มาหรือไม่
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "กรุณาแนบไฟล์ Excel (.xlsx หรือ .xls) สำหรับนำเข้าข้อมูล" });
            }

            // 5.2 ตรวจสอบนามสกุลไฟล์
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".xlsx" && extension != ".xls")
            {
                return BadRequest(new { message = "รูปแบบไฟล์ไม่ถูกต้อง ระบบรองรับเฉพาะนามสกุล .xlsx หรือ .xls เท่านั้น" });
            }

            try
            {
                // 5.3 อ่านไฟล์ Excel จาก Memory Stream โดยใช้ ClosedXML
                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);

                // ดึง Worksheet แผ่นแรก
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    return BadRequest(new { message = "ไม่พบแผ่นงาน (Worksheet) ในไฟล์ Excel นี้" });
                }

                // ดึงแถวทั้งหมดที่มีการใช้งาน
                var rows = worksheet.RangeUsed()?.RowsUsed()?.ToList();
                if (rows == null || rows.Count <= 1)
                {
                    return BadRequest(new { message = "ไม่พบข้อมูลในไฟล์ Excel (ต้องมีข้อมูลอย่างน้อย 1 แถวต่อจาก Header)" });
                }

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var importedList = new List<Employee>();
                var errorList = new List<string>();

                // 5.4 เริ่มวนลูปอ่านแถวข้อมูลตั้งแต่แถวที่ 2 (index 1 ใน rows) เพื่อข้ามหัวตาราง
                for (int i = 1; i < rows.Count; i++)
                {
                    var row = rows[i];
                    int rowNumber = row.RowNumber();

                    try
                    {
                        // คอลัมน์ 1: รหัสพนักงาน (EmpCode)
                        string empCode = row.Cell(1).GetString().Trim();
                        // คอลัมน์ 2: ชื่อ-นามสกุล (FullName)
                        string fullName = row.Cell(2).GetString().Trim();
                        // คอลัมน์ 3: แผนก (Department)
                        string department = row.Cell(3).GetString().Trim();
                        // คอลัมน์ 4: เงินเดือน (Salary)
                        string salaryRaw = row.Cell(4).GetString().Trim();

                        // ตรวจสอบข้อมูลบังคับ
                        if (string.IsNullOrWhiteSpace(empCode))
                        {
                            errorList.Add($"แถวที่ {rowNumber}: ไม่พบรหัสพนักงาน (EmpCode)");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(fullName))
                        {
                            errorList.Add($"แถวที่ {rowNumber}: ไม่พบชื่อพนักงาน (FullName)");
                            continue;
                        }

                        // แปลงค่าเงินเดือน หากแปลงไม่ได้หรือเป็นค่าว่างจะกำหนดเป็น 0
                        decimal salary = 0;
                        if (!string.IsNullOrWhiteSpace(salaryRaw))
                        {
                            decimal.TryParse(salaryRaw, out salary);
                        }

                        // ตรวจสอบรหัสพนักงานซ้ำในฐานข้อมูล
                        const string checkSql = "SELECT COUNT(1) FROM Employees WHERE EmpCode = @EmpCode";
                        var exists = await connection.ExecuteScalarAsync<int>(checkSql, new { EmpCode = empCode });
                        if (exists > 0)
                        {
                            errorList.Add($"แถวที่ {rowNumber}: รหัสพนักงาน '{empCode}' มีอยู่ในระบบแล้ว");
                            continue;
                        }

                        // บันทึกข้อมูลลงฐานข้อมูลผ่าน Dapper
                        const string insertSql = @"
                            INSERT INTO Employees (EmpCode, FullName, Department, Salary, CreatedAt)
                            VALUES (@EmpCode, @FullName, @Department, @Salary, GETDATE());
                            SELECT CAST(SCOPE_IDENTITY() as int);";

                        var newId = await connection.QuerySingleAsync<int>(insertSql, new
                        {
                            EmpCode = empCode,
                            FullName = fullName,
                            Department = department,
                            Salary = salary
                        });

                        importedList.Add(new Employee
                        {
                            Id = newId,
                            EmpCode = empCode,
                            FullName = fullName,
                            Department = department,
                            Salary = salary,
                            CreatedAt = DateTime.Now
                        });
                    }
                    catch (Exception exRow)
                    {
                        errorList.Add($"แถวที่ {rowNumber}: เกิดข้อผิดพลาด ({exRow.Message})");
                    }
                }

                // 5.5 ส่งผลสรุปกลับไปยังหน้าบ้าน
                return Ok(new
                {
                    message = $"นำเข้าข้อมูลเสร็จสมบูรณ์: สำเร็จ {importedList.Count} รายการ, ผิดพลาด {errorList.Count} รายการ",
                    totalRows = rows.Count - 1,
                    successCount = importedList.Count,
                    errorCount = errorList.Count,
                    errors = errorList,
                    data = importedList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "เกิดข้อผิดพลาดในการประมวลผลไฟล์ Excel",
                    error = ex.Message
                });
            }
        }
    }
}
