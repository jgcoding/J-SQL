USE [master]
GO

/****** Object:  Database [Utilities]    Script Date: 05/14/2013 10:37:20 ******/
CREATE DATABASE [Utilities] ON  PRIMARY 
( NAME = N'Utilities', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL10_50.SQLEXPRESS\MSSQL\DATA\Utilities.mdf' , SIZE = 3072KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024KB )
 LOG ON 
( NAME = N'Utilities_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL10_50.SQLEXPRESS\MSSQL\DATA\Utilities_log.ldf' , SIZE = 1280KB , MAXSIZE = 2048GB , FILEGROWTH = 10%)
GO

ALTER DATABASE [Utilities] SET COMPATIBILITY_LEVEL = 100
GO

IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [Utilities].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO

ALTER DATABASE [Utilities] SET ANSI_NULL_DEFAULT ON 
GO

ALTER DATABASE [Utilities] SET ANSI_NULLS ON 
GO

ALTER DATABASE [Utilities] SET ANSI_PADDING ON 
GO

ALTER DATABASE [Utilities] SET ANSI_WARNINGS ON 
GO

ALTER DATABASE [Utilities] SET ARITHABORT ON 
GO

ALTER DATABASE [Utilities] SET AUTO_CLOSE ON 
GO

ALTER DATABASE [Utilities] SET AUTO_CREATE_STATISTICS ON 
GO

ALTER DATABASE [Utilities] SET AUTO_SHRINK OFF 
GO

ALTER DATABASE [Utilities] SET AUTO_UPDATE_STATISTICS ON 
GO

ALTER DATABASE [Utilities] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO

ALTER DATABASE [Utilities] SET CURSOR_DEFAULT  LOCAL 
GO

ALTER DATABASE [Utilities] SET CONCAT_NULL_YIELDS_NULL ON 
GO

ALTER DATABASE [Utilities] SET NUMERIC_ROUNDABORT OFF 
GO

ALTER DATABASE [Utilities] SET QUOTED_IDENTIFIER ON 
GO

ALTER DATABASE [Utilities] SET RECURSIVE_TRIGGERS OFF 
GO

ALTER DATABASE [Utilities] SET  DISABLE_BROKER 
GO

ALTER DATABASE [Utilities] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO

ALTER DATABASE [Utilities] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO

ALTER DATABASE [Utilities] SET TRUSTWORTHY OFF 
GO

ALTER DATABASE [Utilities] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO

ALTER DATABASE [Utilities] SET PARAMETERIZATION SIMPLE 
GO

ALTER DATABASE [Utilities] SET READ_COMMITTED_SNAPSHOT OFF 
GO

ALTER DATABASE [Utilities] SET HONOR_BROKER_PRIORITY OFF 
GO

ALTER DATABASE [Utilities] SET  READ_WRITE 
GO

ALTER DATABASE [Utilities] SET RECOVERY FULL 
GO

ALTER DATABASE [Utilities] SET  MULTI_USER 
GO

ALTER DATABASE [Utilities] SET PAGE_VERIFY NONE  
GO

ALTER DATABASE [Utilities] SET DB_CHAINING OFF 
GO


