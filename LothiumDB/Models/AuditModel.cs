// System Class
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// Custom Class
using LothiumDB.Attributes;
using LothiumDB.Databases;

namespace LothiumDB.Models
{
    [TableName("AuditEvents")]
    [PrimaryKey("AuditID", true)]
    internal sealed class AuditModel
    {
        [Required]
        [ColumnName("AuditLevel")]
        public string? Level { get; set; }

        [Required]
        [ColumnName("AuditUser")]
        public string? User { get; set; }

        [Required]
        [ColumnName("ExecutedOn")]
        public DateTime ExecutedOnDate { get; set; }

        [Required]
        [ColumnName("DbCommandType")]
        public string? DatabaseCommandType { get; set; }

        [Required]
        [ColumnName("SqlCommandType")]
        public string? SqlCommandType { get; set; }

        [ColumnName("SqlCommandOnly")]
        public string? SqlCommandWithoutParams { get; set; }

        [ColumnName("SqlCommandComplete")]
        public string? SqlCommandWithParams { get; set; }

        [ColumnName("ErrorMessage")]
        public string? ErrorMsg { get; set; }

        /// <summary>
        /// Methods that generate a new sql query for checking if the audit table already exists or not
        /// </summary>
        /// <returns></returns>
        public static SqlBuilder GenerateQueryForCheckTableExist()
        {
            return new SqlBuilder(@"
                IF (EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @0 AND TABLE_NAME = @1))
                BEGIN
                    SELECT 1
                END
                ELSE
                BEGIN
                	SELECT 0
                END
            ", "dbo", "AuditEvents");
        }

        /// <summary>
        /// Methods that generate a new sql query for the creation of the Audit Table inside the database instance
        /// </summary>
        /// <returns></returns>
        public static SqlBuilder GenerateQueryForTableCreation()
        {
            return new SqlBuilder(@"
                SET ANSI_NULLS ON
                SET QUOTED_IDENTIFIER ON

                CREATE TABLE [dbo].[AuditEvents](
	                [AuditID] [int] IDENTITY(1,1) NOT NULL,
                    [AuditLevel] [nvarchar](32) NOT NULL,
	                [AuditUser] [nvarchar](64) NOT NULL,
	                [ExecutedOn] [date] NOT NULL,
	                [DbCommandType] [nvarchar](32) NOT NULL,
	                [SqlCommandType] [nvarchar](32) NOT NULL,
	                [SqlCommandOnly] [nvarchar](max) NULL,
	                [SqlCommandComplete] [nvarchar](max) NULL,
                    [ErrorMessage] [nvarchar](max) NULL
                ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
                ALTER TABLE [dbo].[AuditEvents] ADD  CONSTRAINT [PK_AuditEvents] PRIMARY KEY CLUSTERED 
                (
	                [AuditID] ASC
                ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            ");
        }
    }
}
