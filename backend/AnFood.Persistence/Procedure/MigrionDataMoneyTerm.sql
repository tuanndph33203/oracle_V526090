DELETE FROM staging_anpos.dbo.ProductMoneyTermRela ;
DELETE FROM staging_anpos.dbo.PaymentCardMoneyTermRela  ;

UPDATE staging_anpos.dbo.PaymentCardTransaction
SET MoneyTermCode='KY1', MoneyTermId='3F7449ED-B7C1-40C2-B418-6C6888D5BF7C';

INSERT INTO staging_anpos.dbo.PaymentCardMoneyTermRela
(
    Id,
    MoneyTermId,
    MoneyTermCode,
    PaymentCardSeriNumber,
    [Money],
    CreatedOnDate,
    UpdatedOnDate,
    StartDate,
    EndDate
)
SELECT 
    NEWID() AS Id,
    mt.Id AS MoneyTermId,
    mt.Code AS MoneyTermCode,
    pc.SeriNumberCard AS PaymentCardSeriNumber,
    pc.SurplusMoney AS [Money],
    GETDATE() AS CreatedOnDate,
    GETDATE() AS UpdatedOnDate,
    mt.StartDate,
    mt.EndDate
FROM staging_anpos.dbo.PaymentCard pc
CROSS JOIN 
(
    SELECT TOP 1 Id, Code, StartDate, EndDate
    FROM staging_anpos.dbo.MoneyTerm
    where Id = 'C7B16387-68EC-4A91-A19F-7B8970B0BF15'
) mt;

INSERT INTO staging_anpos.dbo.ProductMoneyTermRela
(
    Id,
    MoneyTermId,
    MoneyTermCode,
    ProductId,
    ProductCode,
    Price,
    VatNumber
)
SELECT 
    NEWID() AS Id,
    mt.Id AS MoneyTermId,
    mt.Code AS MoneyTermCode,
    p.Id AS ProductId,
    p.Code AS ProductCode,
    p.SellingPrice AS Price,
    p.ExportTaxMoney AS VatNumber
FROM staging_anpos.dbo.Product p
CROSS JOIN 
(
    SELECT TOP 1 Id, Code
    FROM staging_anpos.dbo.MoneyTerm
    where Id = 'C7B16387-68EC-4A91-A19F-7B8970B0BF15'
) mt;