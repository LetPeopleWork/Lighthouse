import DeleteIcon from "@mui/icons-material/Delete";
import PictureAsPdfIcon from "@mui/icons-material/PictureAsPdf";
import { Button, Tooltip } from "@mui/material";
import { useState } from "react";
import type { RbacState } from "../../hooks/useRbac";
import type { IReportAcrossOwners } from "../../models/Reports/ReportsAcrossOwners";
import { useTerminology } from "../../services/TerminologyContext";
import { isTauriOnMac } from "../../utils/tauri";
import { PDF_EXPORT_UNAVAILABLE_ON_MAC } from "../Common/Reports/pdfExportCopy";
import { exportPdfButtonProps } from "../Common/Reports/preparingLook";
import { ownerOfReport } from "../Common/Reports/reportOwner";
import { mayEditReportsOf } from "../Common/Reports/useMayEditReports";
import { exportBlockedBecause, removeBlockedBecause } from "./pickLimit";
import RemoveSelectedDialog from "./RemoveSelectedDialog";
import type { ReportsPdfExport } from "./useReportsPdfExport";
import type { RowPicks } from "./useRowPicks";

// Held back only in look on the macOS desktop app, so the tooltip saying why still reaches a keyboard reader.
const HELD_BACK_LOOK = { color: "action.disabled", cursor: "default" };

/** Remove, which asks before removing the picked reports. */
export const useSelectionRemoval = (
	{ picked, clear }: RowPicks,
	rbac: RbacState,
) => {
	const { getTerm } = useTerminology();
	const { mayEdit } = mayEditReportsOf(rbac);
	// Held from the moment Remove is pressed, so the dialog keeps naming them while the list is read again.
	const [confirming, setConfirming] = useState<IReportAcrossOwners[] | null>(
		null,
	);
	const notAdmin = rbac.isLoading
		? 0
		: picked.filter((report) => !mayEdit(ownerOfReport(report))).length;
	const blockedBecause = removeBlockedBecause(
		picked.length,
		rbac.loadFailed,
		notAdmin,
		getTerm,
	);
	const button = (
		<Tooltip title={blockedBecause}>
			<span>
				<Button
					variant="outlined"
					startIcon={<DeleteIcon />}
					disabled={
						picked.length === 0 || rbac.isLoading || blockedBecause !== ""
					}
					onClick={() => setConfirming(picked)}
				>
					{`Remove (${picked.length})`}
				</Button>
			</span>
		</Tooltip>
	);
	const dialog =
		confirming === null ? null : (
			<RemoveSelectedDialog
				reports={confirming}
				onCancel={() => setConfirming(null)}
				onDone={() => {
					setConfirming(null);
					clear();
				}}
			/>
		);
	return { button, dialog };
};

/**
 * Export PDF, which prints every picked report as one PDF, in the order the table shows them; any reader may. On the
 * macOS desktop app no PDF can be made at all, so that reason wins over any about the picks.
 */
export const useSelectionExport = (
	{ picked, inTableOrder }: RowPicks,
	pdfExport: ReportsPdfExport,
) => {
	const label = `Export PDF (${picked.length})`;
	const blockedBecause = pdfExport.preparing
		? ""
		: exportBlockedBecause(picked.length);
	const button = isTauriOnMac() ? (
		<Tooltip describeChild title={PDF_EXPORT_UNAVAILABLE_ON_MAC}>
			<Button
				variant="outlined"
				startIcon={<PictureAsPdfIcon />}
				aria-disabled="true"
				sx={HELD_BACK_LOOK}
			>
				{label}
			</Button>
		</Tooltip>
	) : (
		<Tooltip title={blockedBecause}>
			<span>
				<Button
					variant="outlined"
					disabled={blockedBecause !== ""}
					{...exportPdfButtonProps(
						pdfExport.preparing,
						() => pdfExport.exportPdf(inTableOrder()),
						<PictureAsPdfIcon />,
					)}
				>
					{label}
				</Button>
			</span>
		</Tooltip>
	);
	return { button };
};
