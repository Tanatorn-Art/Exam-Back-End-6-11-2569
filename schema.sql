-- ============================================================================
-- Database Schema for Toyota Exam (.NET Web API & SQL Server)
-- Database Name: TestExamDB
-- Description: สคริปต์สร้างฐานข้อมูล ตาราง พร็อกซีเดอร์ วิว และข้อมูลเริ่มต้น
-- ============================================================================

-- 1. สร้างฐานข้อมูล TestExamDB (หากยังไม่มี)
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'TestExamDB')
BEGIN
    CREATE DATABASE TestExamDB;
    PRINT N'Database [TestExamDB] created successfully.';
END
GO

USE TestExamDB;
GO

-- ============================================================================
-- 2. สร้างตาราง Employees (พนักงาน)
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Employees] (
        [Id]         INT IDENTITY(1,1) NOT NULL,
        [EmpCode]    NVARCHAR(20)      NOT NULL,
        [FullName]   NVARCHAR(150)     NOT NULL,
        [Department] NVARCHAR(100)     NULL,
        [Salary]     DECIMAL(18, 2)    NULL CONSTRAINT [DF_Employees_Salary] DEFAULT (0.00),
        [CreatedAt]  DATETIME          NULL CONSTRAINT [DF_Employees_CreatedAt] DEFAULT (GETDATE()),

        -- Primary Key
        CONSTRAINT [PK_Employees] PRIMARY KEY CLUSTERED ([Id] ASC),

        -- Unique Constraint ป้องกันรหัสพนักงานซ้ำ
        CONSTRAINT [UQ_Employees_EmpCode] UNIQUE NONCLUSTERED ([EmpCode] ASC)
    );
    PRINT N'Table [Employees] created successfully.';
END
ELSE
BEGIN
    PRINT N'Table [Employees] already exists.';
END
GO

-- สร้าง Non-Clustered Index เพื่อเพิ่มความเร็วในการค้นหาตามแผนก
IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_Employees_Department')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Employees_Department] ON [dbo].[Employees] ([Department] ASC);
END
GO

-- ============================================================================
-- 3. สร้าง View: vw_HighSalaryEmployee (พนักงานเงินเดือนสูง >= 30,000)
-- ============================================================================
CREATE OR ALTER VIEW [dbo].[vw_HighSalaryEmployee] AS
SELECT 
    [EmpCode], 
    [FullName], 
    [Department], 
    [Salary]
FROM [dbo].[Employees]
WHERE [Salary] >= 30000;
GO

PRINT N'View [vw_HighSalaryEmployee] created/updated successfully.';
GO

-- ============================================================================
-- 4. สร้าง Stored Procedure: sp_GetEmployeesByDept (ดึงพนักงานตามแผนก)
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_GetEmployeesByDept] 
    @Department NVARCHAR(100) = NULL
AS
BEGIN 
    SET NOCOUNT ON;

    -- หาก @Department เป็น NULL หรือค่าว่าง จะดึงข้อมูลพนักงานทั้งหมด
    SELECT 
        [Id],
        [EmpCode],
        [FullName],
        [Department],
        [Salary],
        [CreatedAt]
    FROM [dbo].[Employees]
    WHERE 
        (@Department IS NULL OR [Department] = @Department OR LTRIM(RTRIM(@Department)) = '');
END;
GO

PRINT N'Stored Procedure [sp_GetEmployeesByDept] created/updated successfully.';
GO

-- ============================================================================
-- 5. ข้อมูลตัวอย่างเริ่มต้น (Seed Initial Demo Data)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[Employees])
BEGIN
    INSERT INTO [dbo].[Employees] ([EmpCode], [FullName], [Department], [Salary], [CreatedAt])
    VALUES
        (N'TYT-001', N'สมชาย สายฟ้า', N'Engineering', 48000.00, GETDATE()),
        (N'TYT-002', N'สมหญิง จริงใจ', N'Production', 35000.00, GETDATE()),
        (N'TYT-003', N'วิชัย มั่นคง', N'Quality Assurance', 32000.00, GETDATE()),
        (N'TYT-004', N'กานดา พัฒนา', N'IT', 55000.00, GETDATE()),
        (N'TYT-005', N'อนุชา สุขสันต์', N'HR', 28000.00, GETDATE());

    PRINT N'Seed initial demo employees data inserted successfully.';
END
GO
