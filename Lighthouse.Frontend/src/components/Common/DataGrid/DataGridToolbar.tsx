import ContentCopyIcon from "@mui/icons-material/ContentCopy";
import FileDownloadIcon from "@mui/icons-material/FileDownload";
import GridViewIcon from "@mui/icons-material/GridView";
import ViewColumnIcon from "@mui/icons-material/ViewColumn";
import { Box, IconButton, Tooltip } from "@mui/material";
import { useGridApiContext } from "@mui/x-data-grid";
import type React from "react";
import { useCallback, useState } from "react";
import type { DataGridToolbarProps } from "./types";

const generateFileName = (exportFileName?: string): string => {
	const timestamp = new Date().toISOString().split("T")[0];
	return exportFileName
		? `${exportFileName}_${timestamp}`
		: `data_export_${timestamp}`;
};

const escapeHtml = (text: string): string => {
	return text
		.replaceAll("&", "&amp;")
		.replaceAll("<", "&lt;")
		.replaceAll(">", "&gt;")
		.replaceAll('"', "&quot;");
};

const escapeCSV = (value: string): string => {
	if (value.includes(",") || value.includes("\n") || value.includes('"')) {
		return `"${value.replaceAll('"', '""')}"`;
	}
	return value;
};

const formatCellValue = (value: unknown): string => {
	if (value === null || value === undefined) return "";
	if (typeof value === "object") return JSON.stringify(value);
	return String(value);
};

const useDataGridExport = (
	apiRef: ReturnType<typeof useGridApiContext>,
	canUsePremiumFeatures: boolean,
) => {
	const [copyStatus, setCopyStatus] = useState<"idle" | "copied">("idle");

	const getGridData = useCallback(() => {
		const visibleRows = apiRef.current.getSortedRowIds();
		const visibleColumns = apiRef.current.getVisibleColumns();

		const headers = visibleColumns.map((col) => col.headerName || col.field);

		const dataRows = visibleRows.map((rowId) => {
			return visibleColumns.map((col) => {
				const value = apiRef.current.getCellValue(rowId, col.field);
				return formatCellValue(value);
			});
		});

		return { headers, dataRows };
	}, [apiRef]);

	const handleCopyToClipboard = useCallback(async () => {
		if (!canUsePremiumFeatures) {
			console.warn("Copy to clipboard requires premium license");
			return;
		}

		try {
			const { headers, dataRows } = getGridData();
			const allRows = [headers, ...dataRows];

			const textContent = allRows.map((row) => row.join("\t")).join("\n");

			const htmlContent = `
				<table border="1" cellpadding="4" cellspacing="0" style="border-collapse: collapse;">
					<thead>
						<tr>
							${headers.map((h) => `<th style="background-color: #30574E; color: white; font-weight: bold; text-align: left;">${escapeHtml(h)}</th>`).join("")}
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

			const htmlBlob = new Blob([htmlContent], { type: "text/html" });
			const textBlob = new Blob([textContent], { type: "text/plain" });

			await navigator.clipboard.write([
				new ClipboardItem({
					"text/html": htmlBlob,
					"text/plain": textBlob,
				}),
			]);

			setCopyStatus("copied");
			setTimeout(() => setCopyStatus("idle"), 2000);
		} catch (error) {
			console.error("Failed to copy to clipboard:", error);
		}
	}, [getGridData, canUsePremiumFeatures]);

	const handleExportToCSV = useCallback(
		async (fileName: string) => {
			if (!canUsePremiumFeatures) {
				console.warn("CSV export requires premium license");
				return;
			}

			try {
				const { headers, dataRows } = getGridData();

				const csvRows = dataRows.map((row) =>
					row.map((element) => escapeCSV(element)),
				);
				const allRows = [headers, ...csvRows];
				const csvContent = allRows.map((row) => row.join(",")).join("\n");

				const bom = "\uFEFF";
				const blob = new Blob([bom + csvContent], {
					type: "text/csv;charset=utf-8;",
				});

				const url = URL.createObjectURL(blob);
				const link = document.createElement("a");
				link.href = url;
				link.download = `${fileName}.csv`;
				document.body.appendChild(link);
				link.click();
				link.remove();
				URL.revokeObjectURL(url);
			} catch (error) {
				console.error("Failed to export CSV:", error);
			}
		},
		[getGridData, canUsePremiumFeatures],
	);

	return {
		copyStatus,
		handleCopyToClipboard,
		handleExportToCSV,
	};
};

const DataGridToolbar: React.FC<DataGridToolbarProps> = ({
	canUsePremiumFeatures = false,
	enableExport = false,
	exportFileName,
	onResetLayout,
	onOpenColumnOrder,
	allowColumnReorder = false,
	customActions,
}) => {
	const apiRef = useGridApiContext();

	const { copyStatus, handleCopyToClipboard, handleExportToCSV } =
		useDataGridExport(apiRef, canUsePremiumFeatures);

	const getCopyTooltipText = () => {
		if (!canUsePremiumFeatures) {
			return "Premium feature - Upgrade to use";
		}
		return copyStatus === "copied" ? "Copied!" : "Copy to Clipboard";
	};

	const copyTooltip = getCopyTooltipText();

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
								onClick={() =>
									handleExportToCSV(generateFileName(exportFileName))
								}
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
			{customActions}
			{onResetLayout && (
				<Tooltip title="Reset layout to defaults">
					<span>
						<IconButton
							onClick={onResetLayout}
							size="small"
							data-testid="reset-layout-button"
						>
							<GridViewIcon fontSize="small" />
						</IconButton>
					</span>
				</Tooltip>
			)}
			{allowColumnReorder && onOpenColumnOrder && (
				<Tooltip title="Reorder columns">
					<span>
						<IconButton
							onClick={onOpenColumnOrder}
							size="small"
							data-testid="open-column-order-button"
						>
							<ViewColumnIcon fontSize="small" />
						</IconButton>
					</span>
				</Tooltip>
			)}
		</Box>
	);
};

export default DataGridToolbar;
