SET QUOTED_IDENTIFIER OFF
GO
SET ANSI_NULLS OFF
GO
CREATE FUNCTION [dbo].[AllDigits] (@s [nvarchar] (max))
RETURNS [bit]
WITH EXECUTE AS CALLER
EXTERNAL NAME [CmsSqlClr].[UserDefinedFunctions].[AllDigits]
GO
IF @@ERROR <> 0 SET NOEXEC ON
GO
