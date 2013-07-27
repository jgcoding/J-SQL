USE [Utilities]
GO

/****** Object:  UserDefinedTableType [dbo].[JsonTable]    Script Date: 07/27/2013 13:11:06 ******/
IF  EXISTS (SELECT * FROM sys.types st JOIN sys.schemas ss ON st.schema_id = ss.schema_id WHERE st.name = N'JsonTable' AND ss.name = N'dbo')
DROP TYPE [dbo].[JsonTable]
GO

USE [Utilities]
GO

/****** Object:  UserDefinedTableType [dbo].[JsonTable]    Script Date: 07/27/2013 13:11:06 ******/
CREATE TYPE [dbo].[JsonTable] AS TABLE(
	[elementId] [int] IDENTITY(1,1) NOT NULL,
	[parentId] [int] NOT NULL,
	[objectId] [int] NOT NULL,
	[node] [varchar](500) NOT NULL,
	[itemKey] [varchar](200) NULL,
	[itemValue] [nvarchar](max) NULL,
	[itemType] [varchar](50) NOT NULL,
	PRIMARY KEY CLUSTERED 
(
	[elementId] ASC
)WITH (IGNORE_DUP_KEY = OFF)
)
GO


