IF OBJECT_ID('[dbo].[PledgesSummary]') IS NOT NULL
DROP FUNCTION [dbo].[PledgesSummary] 
GO
CREATE FUNCTION [dbo].[PledgesSummary] ( @pid INT )
RETURNS
@pledgesSummary TABLE (FundId INT NOT NULL, FundName NVARCHAR(max), AmountPledged DECIMAL(38,2) NOT NULL, AmountContributed DECIMAL(38,2), Balance DECIMAL(38,2) NOT NULL)
AS
BEGIN
	DECLARE @givingSummary TABLE (FundId INT NOT NULL, AmountContributed DECIMAL(38,2));

	INSERT INTO @pledgesSummary
		SELECT cf.FundId, cf.FundName, SUM(c.ContributionAmount) AmountPledged, 0, 0
		FROM Contribution c
		JOIN ContributionFund cf ON cf.FundId = c.FundId
		JOIN lookup.ContributionType ct ON c.ContributionTypeId = ct.Id 
		WHERE ct.Description = 'Pledge'
		AND c.PeopleId = @pid
		GROUP BY cf.FundId, cf.FundName, ct.Description;


	IF EXISTS(SELECT 1 FROM @pledgesSummary)
	BEGIN
		INSERT INTO @givingSummary
			SELECT cf.FundId, SUM(c.ContributionAmount)
			FROM Contribution c
			JOIN ContributionFund cf ON cf.FundId = c.FundId
			JOIN lookup.ContributionType ct ON c.ContributionTypeId = ct.Id
			JOIN @pledgesSummary ps ON c.FundId = ps.FundId
			WHERE ct.Description <> 'Pledge'
			AND c.PeopleId = @pid
			GROUP BY cf.FundId

	UPDATE ps
		SET 
			ps.AmountContributed = gs.AmountContributed,
			ps.Balance = IIF(gs.AmountContributed > ps.AmountPledged, 0, ps.AmountPledged - gs.AmountContributed)
		FROM 
			@pledgesSummary AS ps
			JOIN @givingSummary AS gs ON ps.FundId = gs.FundId
		WHERE
			ps.FundId = gs.FundId
	END;

RETURN
END;
GO
