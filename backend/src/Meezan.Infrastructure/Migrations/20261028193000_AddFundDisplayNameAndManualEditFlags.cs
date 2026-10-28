using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meezan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFundDisplayNameAndManualEditFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[Funds]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('Funds', 'DisplayName') IS NULL
    BEGIN
        ALTER TABLE [Funds] ADD [DisplayName] nvarchar(250) NOT NULL CONSTRAINT [DF_Funds_DisplayName] DEFAULT N'';
    END

    IF COL_LENGTH('Funds', 'IsNameManuallyEdited') IS NULL
    BEGIN
        ALTER TABLE [Funds] ADD [IsNameManuallyEdited] bit NOT NULL CONSTRAINT [DF_Funds_IsNameManuallyEdited] DEFAULT 0;
    END

    IF COL_LENGTH('Funds', 'IsDescriptionManuallyEdited') IS NULL
    BEGIN
        ALTER TABLE [Funds] ADD [IsDescriptionManuallyEdited] bit NOT NULL CONSTRAINT [DF_Funds_IsDescriptionManuallyEdited] DEFAULT 0;
    END

    EXEC(N'UPDATE [Funds] SET [DisplayName] = [NameAr] WHERE [DisplayName] IS NULL OR [DisplayName] = N''''');
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[Funds]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('Funds', 'DisplayName') IS NOT NULL
    BEGIN
        DECLARE @df1 nvarchar(128);
        SELECT @df1 = dc.name FROM sys.default_constraints dc
        INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
        WHERE dc.parent_object_id = OBJECT_ID(N'[Funds]') AND c.name = N'DisplayName';
        IF @df1 IS NOT NULL EXEC(N'ALTER TABLE [Funds] DROP CONSTRAINT [' + @df1 + N']');
        ALTER TABLE [Funds] DROP COLUMN [DisplayName];
    END

    IF COL_LENGTH('Funds', 'IsNameManuallyEdited') IS NOT NULL
    BEGIN
        DECLARE @df2 nvarchar(128);
        SELECT @df2 = dc.name FROM sys.default_constraints dc
        INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
        WHERE dc.parent_object_id = OBJECT_ID(N'[Funds]') AND c.name = N'IsNameManuallyEdited';
        IF @df2 IS NOT NULL EXEC(N'ALTER TABLE [Funds] DROP CONSTRAINT [' + @df2 + N']');
        ALTER TABLE [Funds] DROP COLUMN [IsNameManuallyEdited];
    END

    IF COL_LENGTH('Funds', 'IsDescriptionManuallyEdited') IS NOT NULL
    BEGIN
        DECLARE @df3 nvarchar(128);
        SELECT @df3 = dc.name FROM sys.default_constraints dc
        INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
        WHERE dc.parent_object_id = OBJECT_ID(N'[Funds]') AND c.name = N'IsDescriptionManuallyEdited';
        IF @df3 IS NOT NULL EXEC(N'ALTER TABLE [Funds] DROP CONSTRAINT [' + @df3 + N']');
        ALTER TABLE [Funds] DROP COLUMN [IsDescriptionManuallyEdited];
    END
END
");
        }
    }
}

