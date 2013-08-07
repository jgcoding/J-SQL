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
	[itemKey] [nvarchar](500) NULL,
	[itemValue] [nvarchar](max) NULL,
	[itemType] [nvarchar](25) NULL
) WITH EXECUTE AS CALLER
AS 
EXTERNAL NAME [CodeRight.JSQL].[UserDefinedFunctions].[ToJsonTable]
GO


/****** Object:  UserDefinedFunction [dbo].[GetNode]    Script Date: 06/27/2013 15:44:35 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[GetNode]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
DROP FUNCTION [dbo].[GetNode]
GO

/****** Object:  UserDefinedFunction [dbo].[GetNode]    Script Date: 06/27/2013 15:44:35 ******/
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



/****** Object:  UserDefinedFunction [dbo].[ToJson]    Script Date: 06/27/2013 15:44:03 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ToJson]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
DROP FUNCTION [dbo].[ToJson]
GO

/****** Object:  UserDefinedFunction [dbo].[ToJson]    Script Date: 06/27/2013 15:44:03 ******/
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
		select [parentId], [objectId], [node], [itemKey], 
		case [itemType] 
			when 'array' then '{@JArray'+cast([objectId] as varchar(10))+'}'
			when 'object' then '{@JObject'+cast([objectId] as varchar(10))+'}'
			else [itemValue] end
		[itemValue], 
		[itemType] 
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



GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ReportStore]') AND type in (N'U'))
	BEGIN
	IF  NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF_ReportStore_eventTime]') AND type = 'D')
		BEGIN
			ALTER TABLE [dbo].[ReportStore] ADD  CONSTRAINT [DF_ReportStore_eventTime]  DEFAULT ([dbo].[ToUnixTime](getdate())) FOR [eventTime]
		END
	END
GO

