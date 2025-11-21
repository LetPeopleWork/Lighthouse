import ContentCopyIcon from "@mui/icons-material/ContentCopy";
import FileDownloadIcon from "@mui/icons-material/FileDownload";
import { Box, IconButton, Tooltip } from "@mui/material";
import { useGridApiContext } from "@mui/x-data-grid";
import type React from "react";
import { useCallback, useState } from "react";

interface DataGridToolbarProps {
	/** Whether user has premium features available */
	canUsePremiumFeatures?: boolean;
	/** Whether export functionality is enabled */
	enableExport?: boolean;
	/** Custom filename for CSV export (without extension) */
	exportFileName?: string;
}

/**
 * Custom toolbar for DataGrid with CSV export functionality
 * Export features are gated behind premium license
 */
const DataGridToolbar: React.FC<DataGridToolbarProps> = ({
	canUsePremiumFeatures = false,
	enableExport = false,
	exportFileName,
}) => {
	const apiRef = useGridApiContext();
	const [copyStatus, setCopyStatus] = useState<"idle" | "copied">("idle");

	// Generate filename with timestamp
	const generateFileName = useCallback(() => {
		const timestamp = new Date().toISOString().split("T")[0];
		return exportFileName
			? `${exportFileName}_${timestamp}`
			: `data_export_${timestamp}`;
	}, [exportFileName]);

	// Copy visible grid data to clipboard with both HTML table and plain text formats
	const handleCopyToClipboard = useCallback(async () => {
		// Runtime premium check
		if (!canUsePremiumFeatures) {
			console.warn("Copy to clipboard requires premium license");
			return;
		}

		try {
			// Get all visible rows (after filtering/sorting)
			const visibleRows = apiRef.current.getSortedRowIds();
			const visibleColumns = apiRef.current.getVisibleColumns();

			// Build header row
			const headers = visibleColumns.map((col) => col.headerName || col.field);

			// Build data rows
			const dataRows = visibleRows.map((rowId) => {
				return visibleColumns.map((col) => {
					// Use getCellValue to properly handle valueGetter columns
					const value = apiRef.current.getCellValue(rowId, col.field);
					// Handle different value types
					if (value === null || value === undefined) return "";
					if (typeof value === "object") return JSON.stringify(value);
					return String(value);
				});
			});

			// Plain text format (tab-separated)
			const allRows = [headers, ...dataRows];
			const textContent = allRows.map((row) => row.join("\t")).join("\n");

			// HTML table format (preserves formatting in emails/rich text editors)
			const escapeHtml = (text: string) => {
				let result = text;
				result = result.replaceAll("&", "&amp;");
				result = result.replaceAll("<", "&lt;");
				result = result.replaceAll(">", "&gt;");
				result = result.replaceAll('"', "&quot;");
				return result;
			};
			const htmlContent = `
				<table border="1" cellpadding="4" cellspacing="0" style="border-collapse: collapse;">
					<thead>
						<tr>
							${headers.map((h) => `<th style="background-color: #30574E; color: white; font-weight: bold; text-align: left;"> ${escapeHtml(h)}</th>`).join("")}
						</tr>
					</thead>
					<tbody>
						${dataRows
							.map(
								(row) => `
							<tr>
								${row.map((cell) => `<td>${escapeHtml(cell)}</td>`).join("")}
							</tr>
						`,
							)
							.join("")}
					</tbody>
				</table>
			`.trim();

			// Write both formats to clipboard
			const htmlBlob = new Blob([htmlContent], { type: "text/html" });
			const textBlob = new Blob([textContent], { type: "text/plain" });

			await navigator.clipboard.write([
				new ClipboardItem({
					"text/html": htmlBlob,
					"text/plain": textBlob,
				}),
			]);

			// Show feedback
			setCopyStatus("copied");
			setTimeout(() => setCopyStatus("idle"), 2000);
		} catch (error) {
			console.error("Failed to copy to clipboard:", error);
		}
	}, [apiRef, canUsePremiumFeatures]);

	// Export visible grid data to CSV file
	const handleExportToCSV = useCallback(async () => {
		// Runtime premium check
		if (!canUsePremiumFeatures) {
			console.warn("CSV export requires premium license");
			return;
		}

		try {
			// Get all visible rows (after filtering/sorting)
			const visibleRows = apiRef.current.getSortedRowIds();
			const visibleColumns = apiRef.current.getVisibleColumns();

			// Build header row
			const headers = visibleColumns.map((col) => col.headerName || col.field);

			// Build data rows
			const dataRows = visibleRows.map((rowId) => {
				return visibleColumns.map((col) => {
					// Use getCellValue to properly handle valueGetter columns
					const value = apiRef.current.getCellValue(rowId, col.field);
					// Handle different value types
					if (value === null || value === undefined) return "";
					if (typeof value === "object") return JSON.stringify(value);
					// Escape quotes and wrap in quotes if contains comma, newline, or quote
					const stringValue = String(value);
					if (
						stringValue.includes(",") ||
						stringValue.includes("\n") ||
						stringValue.includes('"')
					) {
						return `"${stringValue.replaceAll('"', '""')}"`;
					}
					return stringValue;
				});
			});

			// Combine header and data
			const allRows = [headers, ...dataRows];
			const csvContent = allRows.map((row) => row.join(",")).join("\n");

			// Add UTF-8 BOM for better Excel support
			const bom = "\uFEFF";
			const blob = new Blob([bom + csvContent], {
				type: "text/csv;charset=utf-8;",
			});

			// Create download link
			const url = URL.createObjectURL(blob);
			const link = document.createElement("a");
			link.href = url;
			link.download = `${generateFileName()}.csv`;
			document.body.appendChild(link);
			link.click();
			link.remove();
			URL.revokeObjectURL(url);
		} catch (error) {
			console.error("Failed to export CSV:", error);
		}
	}, [apiRef, canUsePremiumFeatures, generateFileName]);

	// Determine tooltip messages
	let copyTooltip = "Copy to Clipboard";
	if (!canUsePremiumFeatures) {
		copyTooltip = "Premium feature - Upgrade to use";
	} else if (copyStatus === "copied") {
		copyTooltip = "Copied!";
	}

	const csvTooltip = canUsePremiumFeatures
		? "Export to CSV"
		: "Premium feature - Upgrade to use";

	return (
		<Box
			sx={{
				display: "flex",
				gap: 0.5,
				p: 1,
				justifyContent: "flex-end",
				alignItems: "center",
				borderBottom: "1px solid",
				borderColor: "divider",
			}}
		>
			{/* Export features */}
			{enableExport && (
				<>
					<Tooltip title={copyTooltip}>
						<span>
							<IconButton
								onClick={handleCopyToClipboard}
								disabled={!canUsePremiumFeatures}
								size="small"
								data-testid="copy-button"
							>
								<ContentCopyIcon fontSize="small" />
							</IconButton>
						</span>
					</Tooltip>
					<Tooltip title={csvTooltip}>
						<span>
							<IconButton
								onClick={handleExportToCSV}
								disabled={!canUsePremiumFeatures}
								size="small"
								data-testid="export-button"
							>
								<FileDownloadIcon fontSize="small" />
							</IconButton>
						</span>
					</Tooltip>
				</>
			)}
		</Box>
	);
};

export default DataGridToolbar;
