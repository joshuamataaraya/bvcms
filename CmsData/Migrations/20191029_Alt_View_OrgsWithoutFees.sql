IF OBJECT_ID('[dbo].[OrgsWithoutFees]') IS NOT NULL
DROP VIEW [dbo].[OrgsWithoutFees]
GO
CREATE VIEW [dbo].[OrgsWithoutFees]
AS
(
	SELECT OrganizationId 
	FROM (
		SELECT 
			OrganizationId, 
			CONVERT(MONEY, dbo.RegexMatch(ISNULL(RegSetting, ''), '(?<=^Fee:\s)(.*)$')) fee
		FROM dbo.Organizations
	) tt
	WHERE ISNULL(fee,0) = 0
)
GO
