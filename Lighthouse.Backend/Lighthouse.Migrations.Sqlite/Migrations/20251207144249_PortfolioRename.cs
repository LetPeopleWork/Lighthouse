﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class PortfolioRename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop existing foreign key constraints before renaming
            migrationBuilder.DropForeignKey(
                name: "FK_Milestone_Projects_ProjectId",
                table: "Milestone");

            // Step 2: Rename the main Projects table to Portfolios
            migrationBuilder.RenameTable(
                name: "Projects",
                newName: "Portfolios");

            // Step 3: Rename the foreign key column in Milestone table
            migrationBuilder.RenameColumn(
                name: "ProjectId",
                table: "Milestone",
                newName: "PortfolioId");

            migrationBuilder.RenameIndex(
                name: "IX_Milestone_ProjectId",
                table: "Milestone",
                newName: "IX_Milestone_PortfolioId");

            // Step 4: Rename junction tables and their columns
            migrationBuilder.RenameTable(
                name: "FeatureProject",
                newName: "FeaturePortfolio");

            migrationBuilder.RenameColumn(
                name: "ProjectsId",
                table: "FeaturePortfolio",
                newName: "PortfoliosId");

            migrationBuilder.RenameTable(
                name: "ProjectTeam",
                newName: "PortfolioTeam");

            migrationBuilder.RenameColumn(
                name: "ProjectsId",
                table: "PortfolioTeam",
                newName: "PortfoliosId");

            // Step 5: Update index names for the renamed tables
            migrationBuilder.RenameIndex(
                name: "IX_FeatureProject_ProjectsId",
                table: "FeaturePortfolio",
                newName: "IX_FeaturePortfolio_PortfoliosId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectTeam_TeamsId",
                table: "PortfolioTeam",
                newName: "IX_PortfolioTeam_TeamsId");

            // Step 6: Re-create foreign key constraints with new names
            migrationBuilder.AddForeignKey(
                name: "FK_Milestone_Portfolios_PortfolioId",
                table: "Milestone",
                column: "PortfolioId",
                principalTable: "Portfolios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FeaturePortfolio_Portfolios_PortfoliosId",
                column: "PortfoliosId",
                table: "FeaturePortfolio",
                principalTable: "Portfolios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PortfolioTeam_Portfolios_PortfoliosId",
                column: "PortfoliosId", 
                table: "PortfolioTeam",
                principalTable: "Portfolios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop foreign key constraints 
            migrationBuilder.DropForeignKey(
                name: "FK_Milestone_Portfolios_PortfolioId",
                table: "Milestone");

            migrationBuilder.DropForeignKey(
                name: "FK_FeaturePortfolio_Portfolios_PortfoliosId",
                table: "FeaturePortfolio");

            migrationBuilder.DropForeignKey(
                name: "FK_PortfolioTeam_Portfolios_PortfoliosId",
                table: "PortfolioTeam");

            // Step 2: Rename junction tables back
            migrationBuilder.RenameTable(
                name: "FeaturePortfolio",
                newName: "FeatureProject");

            migrationBuilder.RenameColumn(
                name: "PortfoliosId",
                table: "FeatureProject",
                newName: "ProjectsId");

            migrationBuilder.RenameTable(
                name: "PortfolioTeam", 
                newName: "ProjectTeam");

            migrationBuilder.RenameColumn(
                name: "PortfoliosId",
                table: "ProjectTeam",
                newName: "ProjectsId");

            // Step 3: Rename main table back
            migrationBuilder.RenameTable(
                name: "Portfolios",
                newName: "Projects");

            // Step 4: Rename foreign key column back
            migrationBuilder.RenameColumn(
                name: "PortfolioId",
                table: "Milestone",
                newName: "ProjectId");

            // Step 5: Rename indexes back
            migrationBuilder.RenameIndex(
                name: "IX_Milestone_PortfolioId",
                table: "Milestone",
                newName: "IX_Milestone_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_FeaturePortfolio_PortfoliosId",
                table: "FeatureProject",
                newName: "IX_FeatureProject_ProjectsId");

            migrationBuilder.RenameIndex(
                name: "IX_PortfolioTeam_TeamsId",
                table: "ProjectTeam", 
                newName: "IX_ProjectTeam_TeamsId");

            // Step 6: Re-create foreign key constraints with old names
            migrationBuilder.AddForeignKey(
                name: "FK_Milestone_Projects_ProjectId",
                table: "Milestone",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FeatureProject_Projects_ProjectsId",
                table: "FeatureProject",
                column: "ProjectsId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTeam_Projects_ProjectsId",
                table: "ProjectTeam",
                column: "ProjectsId",
                principalTable: "Projects", 
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
