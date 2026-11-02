;WITH FirstOpenCardLevel AS
(
    SELECT
        x.CustomerCode,
        x.CreateOnDate
    FROM
    (
        SELECT
            cl.CustomerCode,
            cl.CreateOnDate,
            ROW_NUMBER() OVER
            (
                PARTITION BY cl.CustomerCode
                ORDER BY cl.CreateOnDate ASC, cl.StartDate ASC, cl.Id ASC
            ) AS RowNum
        FROM CustomerLevel cl
        WHERE cl.LevelCode IN ('SILVER', 'GOLD', 'LASTGOLD', 'PLA', 'PLATINUM', 'LASTPLATIUM')
          AND ISNULL(LTRIM(RTRIM(cl.CustomerCode)), '') <> ''
    ) x
    WHERE x.RowNum = 1
),
FirstCardUpdate AS
(
    SELECT
        cc.CustomerCode,
        MIN(cc.CreatedOnDate) AS CreatedOnDate
    FROM CustomerCards cc
    WHERE ISNULL(LTRIM(RTRIM(cc.CustomerCode)), '') <> ''
      AND cc.CreatedOnDate IS NOT NULL
      AND ISNULL(cc.Status, '') <> '19'
    GROUP BY cc.CustomerCode
)
SELECT
    c.CustomerCode,
    CASE
        WHEN CAST(c.CreatedOnDate AS DATE) >= '20260413' THEN fol.CreateOnDate
        WHEN fol.CreateOnDate IS NULL AND fcu.CreatedOnDate IS NULL THEN c.CreatedOnDate
        WHEN fol.CreateOnDate IS NULL THEN fcu.CreatedOnDate
        WHEN fcu.CreatedOnDate IS NULL THEN fol.CreateOnDate
        WHEN fol.CreateOnDate <= fcu.CreatedOnDate THEN fol.CreateOnDate
        ELSE fcu.CreatedOnDate
    END AS OpenCardDate
FROM Customer c
LEFT JOIN FirstOpenCardLevel fol
    ON fol.CustomerCode = c.CustomerCode
LEFT JOIN FirstCardUpdate fcu
    ON fcu.CustomerCode = c.CustomerCode
WHERE ISNULL(LTRIM(RTRIM(c.CustomerCode)), '') <> ''
  AND (c.IsDelete IS NULL OR c.IsDelete = 0)
ORDER BY c.CustomerCode;
