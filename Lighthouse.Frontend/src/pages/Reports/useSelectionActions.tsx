import DeleteIcon from "@mui/icons-material/Delete";
import PictureAsPdfIcon from "@mui/icons-material/PictureAsPdf";
import { Button, Tooltip } from "@mui/material";
import { useState } from "react";
import type { RbacState } from "../../hooks/useRbac";
import {
	type IReportAcrossOwners,
	MOST_PER_BULK_RUN,
} from "../../models/Reports/ReportsAcrossOwners";
import { TERMINOLOGY_KEYS } from "../../models/TerminologyKeys";
import { useTerminology } from "../../services/TerminologyContext";
import { isTauriOnMac } from "../../utils/tauri";
import { PDF_EXPORT_UNAVAILABLE_ON_MAC } from "../Common/Reports/pdfExportCopy";
import { exportPdfButtonProps } from "../Common/Reports/preparingLook";
import { ownerOfReport } from "../Common/Reports/reportOwner";
import { mayEditReportsOf } from "../Common/Reports/useMayEditReports";
import RemoveSelectedDialog from "./RemoveSelectedDialog";
import { useReportsPdfExport } from "./useReportsPdfExport";
import type { RowPicks } from "./useRowPicks";

// Held back only in look on the macOS desktop app, so the tooltip saying why still reaches a keyboard reader.
const HELD_BACK_LOOK = { color: "action.disabled", cursor: "default" };

const TOO_MANY_PICKED = `Pick at most ${MOST_PER_BULK_RUN} reports.`;

/**
 * Why Remove is held back, or "" when it is not. Too many picked comes first; then permissions that never arrived,
 * because the hook then answers as if the reader were Admin everywhere; then picks on owners the reader cannot change.
 */
const removeBlockedBecause = (
	picked: number,
	permissionsFailed: boolean,
	notAdmin: number,
	getTerm: (key: string) => string,
): string => {
	if (picked > MOST_PER_BULK_RUN) return TOO_MANY_PICKED;
	if (permissionsFailed)
		return "Your permissions couldn't be loaded. Reload the page to try again.";
	if (notAdmin === 0) return "";
	return `${notAdmin} of the ${picked} selected reports ${notAdmin === 1 ? "is" : "are"} on a ${getTerm(TERMINOLOGY_KEYS.TEAM)} or ${getTerm(TERMINOLOGY_KEYS.PORTFOLIO)} you are not Admin of.`;
};

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

/** Why Export PDF is held back, or "" when it is not. */
const exportBlockedBecause = (picked: number): string => {
	if (picked === 0) return "Select at least one report.";
	if (picked > MOST_PER_BULK_RUN) return TOO_MANY_PICKED;
	return "";
};

/**
 * Export PDF, which prints every picked report as one PDF, in the order the table shows them; any reader may. On the
 * macOS desktop app no PDF can be made at all, so that reason wins over any about the picks.
 */
export const useSelectionExport = ({
	picked,
	inTableOrder,
	unpick,
}: RowPicks) => {
	const pdfExport = useReportsPdfExport(unpick);
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
	return { button, frame: pdfExport.frame };
};
