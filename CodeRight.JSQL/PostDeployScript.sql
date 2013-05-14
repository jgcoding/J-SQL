USE [Utilities]
GO

/****** Object:  UserDefinedAggregate [dbo].[ToJson]    Script Date: 01/09/2012 00:37:45 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ToJson]') AND type = N'AF')
DROP AGGREGATE [dbo].[ToJson]
GO
/****** Object:  UserDefinedAggregate [dbo].[ToJson]    Script Date: 01/09/2012 00:37:45 ******/
CREATE AGGREGATE [dbo].[ToJson]
(@itemKey [nvarchar](100), @itemValue [nvarchar](max))
RETURNS[nvarchar](max)
EXTERNAL NAME [CodeRight.JSQL].[ToJson]
GO

/****** Object:  UserDefinedFunction [dbo].[SelectIncluded]    Script Date: 11/13/2011 16:06:05 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SelectIncluded]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
DROP FUNCTION [dbo].[SelectIncluded]
GO

/****** Object:  UserDefinedFunction [dbo].[SelectIncluded]    Script Date: 11/13/2011 16:06:05 ******/
CREATE FUNCTION [dbo].[SelectIncluded](@input [nvarchar](max))
RETURNS  TABLE (
	[IncludedKey] [nvarchar](100) NULL,
	[DocumentID] [uniqueidentifier] NULL
) WITH EXECUTE AS CALLER
AS 
EXTERNAL NAME [CodeRight.JSQL].[UserDefinedFunctions].[SelectIncluded]
GO

/****** Object:  UserDefinedFunction [dbo].[RxJsonParse]    Script Date: 11/13/2011 16:05:07 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RxJsonParse]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
DROP FUNCTION [dbo].[RxJsonParse]
GO

/****** Object:  UserDefinedFunction [dbo].[RxJsonParse]    Script Date: 11/13/2011 16:05:07 ******/
CREATE FUNCTION [dbo].[RxJsonParse](@json [nvarchar](max))
RETURNS  TABLE (
	[ParentID] [int] NULL,
	[ObjectID] [int] NULL,
	[Url] [nvarchar](500) NULL,
	[NodeKey] [nvarchar](50) NULL,
	[Node] [nvarchar](100) NULL,
	[ItemKey] [nvarchar](500) NULL,
	[ItemValue] [nvarchar](max) NULL,
	[ItemType] [nvarchar](25) NULL,
	[Selector] [nvarchar](500) NULL
) WITH EXECUTE AS CALLER
AS 
EXTERNAL NAME [CodeRight.JSQL].[UserDefinedFunctions].[RxJsonParse]
GO

/****** Object:  UserDefinedFunction [dbo].[rxContains]    Script Date: 02/06/2012 08:12:45 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[rxContains]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
DROP FUNCTION [dbo].[rxContains]
GO


/****** Object:  UserDefinedFunction [dbo].[rxContains]    Script Date: 02/06/2012 08:12:45 ******/
CREATE FUNCTION [dbo].[rxContains](@json [nvarchar](max), @value [nvarchar](900))
RETURNS [bit] WITH EXECUTE AS CALLER
AS 
EXTERNAL NAME [CodeRight.JSQL].[UserDefinedFunctions].[rxContains]
GO

--/****** Object:  UserDefinedFunction [dbo].[ClrRetrieveDocument]    Script Date: 11/28/2011 15:51:09 ******/
--IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ClrRetrieveDocument]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
--DROP FUNCTION [dbo].[ClrRetrieveDocument]
--GO

--/****** Object:  UserDefinedFunction [dbo].[ClrRetrieveDocument]    Script Date: 11/28/2011 15:51:09 ******/
--CREATE FUNCTION [dbo].[ClrRetrieveDocument](@docid [uniqueidentifier])
--RETURNS [nvarchar](max) WITH EXECUTE AS CALLER
--AS 
--EXTERNAL NAME [CodeRight.JSQL].[UserDefinedFunctions].[ClrRetrieveDocument]
--GO

/****** Object:  UserDefinedFunction [dbo].[TemplateJsonUrl]    Script Date: 12/13/2011 08:59:16 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TemplateJsonUrl]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
DROP FUNCTION [dbo].[TemplateJsonUrl]
GO

/****** Object:  UserDefinedFunction [dbo].[TemplateJsonUrl]    Script Date: 12/13/2011 08:59:16 ******/
CREATE FUNCTION [dbo].[TemplateJsonUrl](@json [nvarchar](max))
RETURNS [nvarchar](max) WITH EXECUTE AS CALLER
AS 
EXTERNAL NAME [CodeRight.JSQL].[UserDefinedFunctions].[TemplateJsonUrl]
GO

/****** Object:  UserDefinedFunction [dbo].[MergeUrl]    Script Date: 02/05/2012 20:43:16 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MergeUrl]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
DROP FUNCTION [dbo].[MergeUrl]
GO

/****** Object:  UserDefinedFunction [dbo].[MergeUrl]    Script Date: 02/05/2012 20:43:16 ******/
CREATE FUNCTION [dbo].[MergeUrl](@sourceUrl [nvarchar](500), @sourceKey [nvarchar](36), @targetUrl [nvarchar](500))
RETURNS  TABLE (
	[Url] [nvarchar](500) NULL,
	[Selector] [nvarchar](500) NULL
) WITH EXECUTE AS CALLER
AS 
EXTERNAL NAME [CodeRight.JSQL].[UserDefinedFunctions].[MergeUrl]
GO


/****** Object:  UserDefinedFunction [dbo].[ParseUrl]    Script Date: 02/05/2012 22:10:41 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ParseUrl]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
DROP FUNCTION [dbo].[ParseUrl]
GO

/****** Object:  UserDefinedFunction [dbo].[ParseUrl]    Script Date: 02/05/2012 22:10:41 ******/
CREATE FUNCTION [dbo].[ParseUrl](@url [nvarchar](500))
RETURNS  TABLE (
	[Generation] [int] NULL,
	[NodeKey] [nvarchar](36) NULL,
	[Node] [nvarchar](100) NULL
) WITH EXECUTE AS CALLER
EXTERNAL NAME [CodeRight.JSQL].[UserDefinedFunctions].[ParseUrl]
GO
--/****** Object:  UserDefinedFunction [dbo].[rxIndexJson]    Script Date: 12/23/2011 08:52:57 ******/
--IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RxIndexJson]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
--DROP FUNCTION [dbo].[RxIndexJson]
--GO

----NodeKey nvarchar(36), Url nvarchar(500), Label nvarchar(100), ItemValue nvarchar(max), ItemType nvarchar(25), Selector nvarchar(500), Filter nvarchar(500), IsVisible bit
--CREATE FUNCTION [dbo].[rxIndexJson](@docname [nvarchar](100), @json [nvarchar](max), @index [nvarchar](max))
--RETURNS  TABLE (
--	[DocumentName] [nvarchar](100) NULL,
--	[NodeKey] [nvarchar](36) NULL,
--	[Url] [nvarchar](500) NULL,
--	[Label] [nvarchar](100) NULL,
--	[ItemValue] [nvarchar](max) NULL,
--	[ItemType] [nvarchar](25) NULL,
--	[Selector] [nvarchar](500) NULL,
--	[Filter] [nvarchar](500) NULL,
--	[IsVisible] [bit] NULL
--) WITH EXECUTE AS CALLER
--AS 
--EXTERNAL NAME [CodeRight.JSQL].[UserDefinedFunctions].[rxIndexJson]
--GO


