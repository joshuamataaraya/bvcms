DROP PROCEDURE IF EXISTS [dbo].[CreateZipCodesRange]
GO
CREATE PROCEDURE [dbo].[CreateZipCodesRange](@StartWith INT, @EndWith INT, @MarginalCode INT)
AS
BEGIN
	DECLARE @TotalZipAdded INT = 0;
	DECLARE @TotalZipUpdated INT = 0;	
	
	DECLARE @zipsToAdd TABLE (
			ZipCode NVARCHAR(10) NOT NULL,
			MetroMarginalCode INT NOT NULL);

	DECLARE @zipsToUpdate TABLE (
			ZipCode NVARCHAR(10) NOT NULL,
			MetroMarginalCode INT NOT NULL);

	WHILE (@StartWith <= @EndWith)
	BEGIN
		IF NOT EXISTS(SELECT 1 FROM dbo.Zips WHERE ZipCode = @StartWith)
		BEGIN 
			INSERT INTO @zipsToAdd (ZipCode, MetroMarginalCode)
			VALUES (dbo.CompleteZip(@StartWith), @MarginalCode);
			SET @TotalZipAdded = @TotalZipAdded + 1;		
		END ELSE
        BEGIN
			INSERT INTO @zipsToUpdate (ZipCode, MetroMarginalCode)
			VALUES (dbo.CompleteZip(@StartWith), @MarginalCode);
			SET @TotalZipUpdated = @TotalZipUpdated + 1;
		END
        		 
		SET @StartWith = @StartWith + 1;		
	END

	INSERT INTO dbo.Zips (ZipCode, MetroMarginalCode)
	SELECT ZipCode, MetroMarginalCode
	FROM @zipsToAdd

	UPDATE z
	SET z.MetroMarginalCode = zu.MetroMarginalCode
    FROM dbo.Zips AS z
    INNER JOIN @zipsToUpdate zu ON z.ZipCode = zu.ZipCode

	RETURN @TotalZipAdded
END
GO

