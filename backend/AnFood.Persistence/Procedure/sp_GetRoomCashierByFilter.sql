CREATE   PROCEDURE dbo.sp_GetRoomCashierByFilter
    @BranchCode NVARCHAR(50),
    @GroupRoomCode NVARCHAR(50),
    @FilterDate DATE = NULL,
    @EmployeeCodes NVARCHAR(MAX) = NULL,
    @CustomerCodes NVARCHAR(MAX) = NULL,
    @ShiftCodes NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @EffectiveFilterDate DATE = ISNULL(@FilterDate, CAST(GETDATE() AS DATE));
    DECLARE @NextDate DATE = DATEADD(DAY, 1, @EffectiveFilterDate);

    DECLARE @EmployeeFilter TABLE (Value NVARCHAR(50) PRIMARY KEY);
    DECLARE @CustomerFilter TABLE (Value NVARCHAR(50) PRIMARY KEY);
    DECLARE @ShiftFilter TABLE (Value NVARCHAR(50) PRIMARY KEY);

    IF NULLIF(LTRIM(RTRIM(@EmployeeCodes)), N'') IS NOT NULL
    BEGIN
        INSERT INTO @EmployeeFilter (Value)
        SELECT DISTINCT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@EmployeeCodes, ',')
        WHERE NULLIF(LTRIM(RTRIM(value)), N'') IS NOT NULL;
    END;

    IF NULLIF(LTRIM(RTRIM(@CustomerCodes)), N'') IS NOT NULL
    BEGIN
        INSERT INTO @CustomerFilter (Value)
        SELECT DISTINCT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@CustomerCodes, ',')
        WHERE NULLIF(LTRIM(RTRIM(value)), N'') IS NOT NULL;
    END;

    IF NULLIF(LTRIM(RTRIM(@ShiftCodes)), N'') IS NOT NULL
    BEGIN
        INSERT INTO @ShiftFilter (Value)
        SELECT DISTINCT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@ShiftCodes, ',')
        WHERE NULLIF(LTRIM(RTRIM(value)), N'') IS NOT NULL;
    END;

    DECLARE @HasEmployeeFilter BIT = CASE WHEN EXISTS (SELECT 1 FROM @EmployeeFilter) THEN 1 ELSE 0 END;
    DECLARE @HasCustomerFilter BIT = CASE WHEN EXISTS (SELECT 1 FROM @CustomerFilter) THEN 1 ELSE 0 END;
    DECLARE @HasShiftFilter BIT = CASE WHEN EXISTS (SELECT 1 FROM @ShiftFilter) THEN 1 ELSE 0 END;

    SELECT
        rg.Code AS GroupRoomCode,
        rg.Name AS GroupRoomName,
        b.Code AS BranchCode,
        b.Name AS BranchName
    FROM RoomGroups rg
    LEFT JOIN Branchs b ON b.Code = @BranchCode
    WHERE rg.Code = @GroupRoomCode;

    SELECT
        r.Id,
        r.Code,
        r.Name,
        r.Status,
        r.Time,
        r.[Index]
    INTO #Rooms
    FROM Room r
    WHERE r.RoomGroupCode = @GroupRoomCode
        AND r.BranchCode = @BranchCode;

    SELECT
        r.Id AS RoomId,
        r.Code AS RoomCode,
        r.Name AS RoomName,
        r.Status AS RoomStatus,
        r.Time AS RoomTime,
        r.[Index] AS [Index]
    FROM #Rooms r
    ORDER BY r.Code;

    SELECT
        dt.RoomCode,
        dt.Id AS SaleServiceDetailId,
        ssv.Id AS SaleServiceId,
        dt.STTRec,
        ssv.DocumentDate,
        dt.TimeIn,
        dt.TimeOut,
        dt.TimeUsed,
        dt.Quantity AS TimePlus,
        dt.TimePause AS TimeStop,
        p.Id AS ProductId,
        p.Code AS ProductCode,
        p.Name AS ProductName,
        p.ProductGroupCode AS ProductGroupCode,
        p.SellingPrice AS ProductPrice,
        u.Id AS UnitId,
        u.Code AS UnitCode,
        u.Name AS UnitName,
        cus.Id AS CustomerId,
        cus.CustomerCode,
        cus.CustomerName,
        dt.Gender AS CustomerGender,
        e1.Code AS Emp1Code,
        e1.Name AS Emp1Name,
        dt.TherapistMoney1 AS Emp1Money,
        e2.Code AS Emp2Code,
        e2.Name AS Emp2Name,
        dt.TherapistMoney2 AS Emp2Money,
        e3.Code AS Emp3Code,
        e3.Name AS Emp3Name,
        dt.TherapistMoney3 AS Emp3Money
    FROM SaleServiceDetail dt
    INNER JOIN #Rooms r ON r.Code = dt.RoomCode
    INNER JOIN SaleService ssv ON ssv.STTRec = dt.STTRec
    LEFT JOIN Product p ON p.Code = dt.ServiceCode
    LEFT JOIN Unit u ON u.Code = dt.UnitCode
    LEFT JOIN Customer cus ON cus.CustomerCode = ssv.CustomerCode
        AND (@HasCustomerFilter = 0 OR EXISTS (SELECT 1 FROM @CustomerFilter cf WHERE cf.Value = cus.CustomerCode))
    LEFT JOIN Employee e1 ON e1.Code = dt.TherapistCode1
        AND (@HasEmployeeFilter = 0 OR EXISTS (SELECT 1 FROM @EmployeeFilter ef WHERE ef.Value = e1.Code))
    LEFT JOIN Employee e2 ON e2.Code = dt.TherapistCode2
        AND (@HasEmployeeFilter = 0 OR EXISTS (SELECT 1 FROM @EmployeeFilter ef WHERE ef.Value = e2.Code))
    LEFT JOIN Employee e3 ON e3.Code = dt.TherapistCode3
        AND (@HasEmployeeFilter = 0 OR EXISTS (SELECT 1 FROM @EmployeeFilter ef WHERE ef.Value = e3.Code))
    WHERE ssv.CreatedOnDate >= @EffectiveFilterDate
        AND ssv.CreatedOnDate < @NextDate
        AND ssv.BranchCode = @BranchCode
        AND (
            @HasEmployeeFilter = 0
            OR EXISTS
            (
                SELECT 1
                FROM @EmployeeFilter ef
                WHERE ef.Value = dt.TherapistCode1
                    OR ef.Value = dt.TherapistCode2
                    OR ef.Value = dt.TherapistCode3
            )
        )
        AND (
            @HasCustomerFilter = 0
            OR EXISTS (SELECT 1 FROM @CustomerFilter cf WHERE cf.Value = dt.CustomerCode)
        )
    ORDER BY ssv.CreatedOnDate DESC;

    SELECT
        re.RoomCode,
        re.ShiftCode,
        sh.Name AS ShiftName,
        sh.StartTime AS ShiftStartTime,
        sh.EndTime AS ShiftEndTime,
        re.StartDate,
        re.EndDate,
        re.StartTime,
        re.EndTime,
        e1.Code AS OrderEmp1Code,
        e1.Name AS OrderEmp1Name,
        e2.Code AS OrderEmp2Code,
        e2.Name AS OrderEmp2Name,
        e3.Code AS OrderEmp3Code,
        e3.Name AS OrderEmp3Name,
        e4.Code AS OrderEmp4Code,
        e4.Name AS OrderEmp4Name,
        e5.Code AS OrderEmp5Code,
        e5.Name AS OrderEmp5Name
    FROM RoomEmployees re
    INNER JOIN #Rooms r ON r.Code = re.RoomCode
    LEFT JOIN Shifts sh ON sh.Code = re.ShiftCode
    LEFT JOIN Employee e1 ON e1.Code = re.EmployeeCode1
        AND (@HasEmployeeFilter = 0 OR EXISTS (SELECT 1 FROM @EmployeeFilter ef WHERE ef.Value = e1.Code))
    LEFT JOIN Employee e2 ON e2.Code = re.EmployeeCode2
        AND (@HasEmployeeFilter = 0 OR EXISTS (SELECT 1 FROM @EmployeeFilter ef WHERE ef.Value = e2.Code))
    LEFT JOIN Employee e3 ON e3.Code = re.EmployeeCode3
        AND (@HasEmployeeFilter = 0 OR EXISTS (SELECT 1 FROM @EmployeeFilter ef WHERE ef.Value = e3.Code))
    LEFT JOIN Employee e4 ON e4.Code = re.EmployeeCode4
        AND (@HasEmployeeFilter = 0 OR EXISTS (SELECT 1 FROM @EmployeeFilter ef WHERE ef.Value = e4.Code))
    LEFT JOIN Employee e5 ON e5.Code = re.EmployeeCode5
        AND (@HasEmployeeFilter = 0 OR EXISTS (SELECT 1 FROM @EmployeeFilter ef WHERE ef.Value = e5.Code))
    WHERE re.StartDate IS NOT NULL
        AND re.EndDate IS NOT NULL
        AND CAST(re.StartDate AS DATE) <= @EffectiveFilterDate
        AND CAST(re.EndDate AS DATE) >= @EffectiveFilterDate
        AND (
            @HasEmployeeFilter = 0
            OR EXISTS
            (
                SELECT 1
                FROM @EmployeeFilter ef
                WHERE ef.Value = re.EmployeeCode1
                    OR ef.Value = re.EmployeeCode2
                    OR ef.Value = re.EmployeeCode3
                    OR ef.Value = re.EmployeeCode4
                    OR ef.Value = re.EmployeeCode5
            )
        )
        AND (
            @HasShiftFilter = 0
            OR EXISTS (SELECT 1 FROM @ShiftFilter sf WHERE sf.Value = re.ShiftCode)
        )
    ORDER BY re.ShiftCode;

    DROP TABLE #Rooms;
END;
