USE [coactivate_rep]
GO

/****** Object:  Table [PTOOLS].[RECORDINGEXPORT]    Script Date: 2024/05/24 08:55:29 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [PTOOLS].[RECORDINGEXPORT](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[created] [datetime] NOT NULL,
	[pres_rec_id] [numeric](18, 0) NOT NULL,
	[unique_id] [varchar](45) NOT NULL,
	[instance] [varchar](100) NULL,
	[status] [int] NOT NULL,
	[updated] [datetime] NOT NULL
) ON [PRIMARY]
GO

ALTER TABLE [PTOOLS].[RECORDINGEXPORT] ADD  CONSTRAINT [DF_RECORDINGEXPORT_created]  DEFAULT (getdate()) FOR [created]
GO

ALTER TABLE [PTOOLS].[RECORDINGEXPORT] ADD  CONSTRAINT [DF_RECORDINGEXPORT_status]  DEFAULT ((0)) FOR [status]
GO

ALTER TABLE [PTOOLS].[RECORDINGEXPORT] ADD  CONSTRAINT [DF_RECORDINGEXPORT_updated]  DEFAULT (getdate()) FOR [updated]
GO
