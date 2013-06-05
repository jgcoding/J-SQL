USE [Utilities]
GO

--IF  EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[DF_ReportStore_eventTime]') AND type = 'D')
--BEGIN
--ALTER TABLE [dbo].[ReportStore] DROP CONSTRAINT [DF_ReportStore_eventTime]
--END
