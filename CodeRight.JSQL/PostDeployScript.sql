USE [Utilities]
GO

/****** Object:  UserDefinedFunction [dbo].[ToJson]    Script Date: 06/05/2013 08:41:36 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ToJson]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
DROP FUNCTION [dbo].[ToJson]
GO

/****** Object:  UserDefinedFunction [dbo].[GetNode]    Script Date: 06/05/2013 08:41:22 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[GetNode]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
DROP FUNCTION [dbo].[GetNode]
GO

/****** Object:  UserDefinedFunction [dbo].[ToJsonTable]    Script Date: 11/13/2011 16:05:07 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ToJsonTable]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
DROP FUNCTION [dbo].[ToJsonTable]
GO

/****** Object:  UserDefinedAggregate [dbo].[JSqlSerializer]    Script Date: 06/05/2013 09:15:02 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[JSqlSerializer]') AND type = N'AF')
DROP AGGREGATE [dbo].[JSqlSerializer]

GO

/****** Object:  UserDefinedAggregate [dbo].[JSqlSerializer]    Script Date: 01/09/2012 00:37:45 ******/
CREATE AGGREGATE [dbo].[JSqlSerializer]
(@itemKey [nvarchar](100), @itemValue [nvarchar](max))
RETURNS[nvarchar](max)
EXTERNAL NAME [CodeRight.JSQL].[JSqlSerializer]
GO


/****** Object:  UserDefinedFunction [dbo].[ToJsonTable]    Script Date: 11/13/2011 16:05:07 ******/
CREATE FUNCTION [dbo].[ToJsonTable](@json [nvarchar](max))
RETURNS  TABLE (
	[ParentID] [int] NULL,
	[ObjectID] [int] NULL,
	[Node] [nvarchar](500) NULL,
	[ItemKey] [nvarchar](500) NULL,
	[ItemValue] [nvarchar](max) NULL,
	[ItemType] [nvarchar](25) NULL
) WITH EXECUTE AS CALLER
AS 
EXTERNAL NAME [CodeRight.JSQL].[UserDefinedFunctions].[ToJsonTable]
GO


/****** Object:  UserDefinedFunction [dbo].[GetNode]    Script Date: 06/04/2013 07:05:49 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

 /*=============================================
 Author:		JG Coding
 Create date: 6/3/2013
 Description:	Returns the most deeply nested node in which no objects or arrays reside
 =============================================*/
CREATE FUNCTION [dbo].[GetNode] 
(
	@jsonTable jsonTable readonly,
	@depth int
)
RETURNS
@node TABLE 
(
	nodeId int,
	json nvarchar(max)
)
AS
BEGIN
	-- Declare the return variable here
	DECLARE @json nvarchar(max), @nodeid int;

	--process objects
	select @nodeid = max(objectid)
		from @jsonTable
			where (itemType in ('object','array')
				and (itemValue like '{@J%'))
				and ((objectid <= @depth) or (@depth = 0))
	
	select @json = dbo.JsqlSerializer(itemKey, itemValue)
		from @jsonTable			
			where parentId = @nodeid
			group by parentId
			
	insert @node(nodeId, json)
		values (@nodeid, @json)
	RETURN 
END
GO

/****** Object:  UserDefinedFunction [dbo].[ToJson]    Script Date: 06/04/2013 07:05:49 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

 /*=============================================
 Author:		JG Coding
 Create date: 6/3/2013
 Description:	Returns the most deeply nested node in which no objects or arrays reside
 =============================================*/
CREATE FUNCTION [dbo].[ToJson] 
(
	@jsonTable jsonTable readonly,
	@depth int
)
RETURNS nvarchar(max)
AS
BEGIN
	-- Declare the return variable here
	DECLARE @json nvarchar(max), @nodeid int, @jsonOut jsonTable;
	insert @jsonOut
		select [parentId], [objectId], [node], [itemKey], [itemValue], [itemType] 
			from @jsonTable
	while 1=1
		begin
			select @nodeid = nodeid from dbo.GetNode(@jsonOut,0);	
			if @nodeid is null
			break;
			update p
			set itemValue = n.json
				from @jsonOut p
					cross apply (
						select nodeId, json 
							from dbo.GetNode(@jsonOut,0)
						)n
					where p.objectId = n.nodeId	
		end		
	select @json = itemValue from @jsonOut
		where parentId = 0
		
	-- Return the result of the function
	RETURN @json

END

GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ReportStore]') AND type in (N'U'))
	BEGIN
	IF  NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF_ReportStore_eventTime]') AND type = 'D')
		BEGIN
			ALTER TABLE [dbo].[ReportStore] ADD  CONSTRAINT [DF_ReportStore_eventTime]  DEFAULT ([dbo].[ToUnixTime](getdate())) FOR [eventTime]
		END
	END
GO


--/****** Object:  UserDefinedFunction [dbo].[rxContains]    Script Date: 02/06/2012 08:12:45 ******/
--IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[rxContains]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
--DROP FUNCTION [dbo].[rxContains]
--GO


--/****** Object:  UserDefinedFunction [dbo].[rxContains]    Script Date: 02/06/2012 08:12:45 ******/
--CREATE FUNCTION [dbo].[rxContains](@json [nvarchar](max), @value [nvarchar](900))
--RETURNS [bit] WITH EXECUTE AS CALLER
--AS 
--EXTERNAL NAME [CodeRight.JSQL].[UserDefinedFunctions].[rxContains]
--GO


/*!!!!!!!!!DEPRECATED FUNCTIONS!!!!!!!!*/

--/****** Object:  UserDefinedFunction [dbo].[SelectIncluded]    Script Date: 11/13/2011 16:06:05 ******/
--IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SelectIncluded]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
--DROP FUNCTION [dbo].[SelectIncluded]
--GO

--/****** Object:  UserDefinedFunction [dbo].[SelectIncluded]    Script Date: 11/13/2011 16:06:05 ******/
--CREATE FUNCTION [dbo].[SelectIncluded](@input [nvarchar](max))
--RETURNS  TABLE (
--	[IncludedKey] [nvarchar](100) NULL,
--	[_id] [uniqueidentifier] NULL
--) WITH EXECUTE AS CALLER
--AS 
--EXTERNAL NAME [CodeRight.JSQL].[UserDefinedFunctions].[SelectIncluded]
--GO


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

--/****** Object:  UserDefinedFunction [dbo].[TemplateJsonUrl]    Script Date: 12/13/2011 08:59:16 ******/
--IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TemplateJsonUrl]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
--DROP FUNCTION [dbo].[TemplateJsonUrl]
--GO

--/****** Object:  UserDefinedFunction [dbo].[TemplateJsonUrl]    Script Date: 12/13/2011 08:59:16 ******/
--CREATE FUNCTION [dbo].[TemplateJsonUrl](@json [nvarchar](max))
--RETURNS [nvarchar](max) WITH EXECUTE AS CALLER
--AS 
--EXTERNAL NAME [CodeRight.JSQL].[UserDefinedFunctions].[TemplateJsonUrl]
--GO

--/****** Object:  UserDefinedFunction [dbo].[MergeUrl]    Script Date: 02/05/2012 20:43:16 ******/
--IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MergeUrl]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
--DROP FUNCTION [dbo].[MergeUrl]
--GO

--/****** Object:  UserDefinedFunction [dbo].[MergeUrl]    Script Date: 02/05/2012 20:43:16 ******/
--CREATE FUNCTION [dbo].[MergeUrl](@sourceUrl [nvarchar](500), @sourceKey [nvarchar](36), @targetUrl [nvarchar](500))
--RETURNS  TABLE (
--	[Url] [nvarchar](500) NULL,
--	[Selector] [nvarchar](500) NULL
--) WITH EXECUTE AS CALLER
--AS 
--EXTERNAL NAME [CodeRight.JSQL].[UserDefinedFunctions].[MergeUrl]
--GO


--/****** Object:  UserDefinedFunction [dbo].[ParseUrl]    Script Date: 02/05/2012 22:10:41 ******/
--IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ParseUrl]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
--DROP FUNCTION [dbo].[ParseUrl]
--GO

--/****** Object:  UserDefinedFunction [dbo].[ParseUrl]    Script Date: 02/05/2012 22:10:41 ******/
--CREATE FUNCTION [dbo].[ParseUrl](@url [nvarchar](500))
--RETURNS  TABLE (
--	[Generation] [int] NULL,
--	[NodeKey] [nvarchar](36) NULL,
--	[Node] [nvarchar](100) NULL
--) WITH EXECUTE AS CALLER
--EXTERNAL NAME [CodeRight.JSQL].[UserDefinedFunctions].[ParseUrl]
--GO

--/****** Object:  UserDefinedFunction [dbo].[CriteriaSearch]    Script Date: 05/18/2013 15:25:42 ******/
--IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CriteriaSearch]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
--DROP FUNCTION [dbo].[CriteriaSearch]
--GO

--USE [Utilities]
--GO

--/****** Object:  UserDefinedFunction [dbo].[CriteriaSearch]    Script Date: 05/18/2013 15:25:42 ******/
--CREATE FUNCTION [dbo].[CriteriaSearch](@json [nvarchar](4000))
--RETURNS  TABLE (
--	[_id] [nvarchar](36) NULL,
--	[view] [nvarchar](max) NULL
--) WITH EXECUTE AS CALLER
--AS 
--EXTERNAL NAME [CodeRight.JSQL].[UserDefinedFunctions].[CriteriaSearch]
--GO


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


