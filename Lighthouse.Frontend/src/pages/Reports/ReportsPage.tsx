import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import PictureAsPdfIcon from "@mui/icons-material/PictureAsPdf";
import {
	Box,
	Button,
	CircularProgress,
	Container,
	Tooltip,
	Typography,
} from "@mui/material";
import type { GridRowId } from "@mui/x-data-grid";
import type React from "react";
import { useRef, useState } from "react";
import { LicenseTooltip } from "../../components/App/License/LicenseToolTip";
import SnackbarErrorHandler from "../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import { useLicenseRestrictions } from "../../hooks/useLicenseRestrictions";
import { type RbacState, useRbac } from "../../hooks/useRbac";
import {
	type IReportAcrossOwners,
	MOST_PER_BULK_RUN,
} from "../../models/Reports/ReportsAcrossOwners";
import { TERMINOLOGY_KEYS } from "../../models/TerminologyKeys";
import { useTerminology } from "../../services/TerminologyContext";
import { isTauriOnMac } from "../../utils/tauri";
import { WidgetBody } from "../Common/MetricsView/WidgetShell";
import { queryStatusOf } from "../Common/MetricsView/widgetStatus";
import { PDF_EXPORT_UNAVAILABLE_ON_MAC } from "../Common/Reports/pdfExportCopy";
import { exportPdfButtonProps } from "../Common/Reports/preparingLook";
import ReportList, {
	type ReportListActions,
} from "../Common/Reports/ReportList";
import { ownerOfReport } from "../Common/Reports/reportOwner";
import { mayEditReportsOf } from "../Common/Reports/useMayEditReports";
import { useReportDeletion } from "../Common/Reports/useReportDeletion";
import { useReportEditing } from "../Common/Reports/useReportEditing";
import { useReportPdfExport } from "../Common/Reports/useReportPdfExport";
import BulkReportWizard from "./BulkReportWizard";
import type { OwnersStatus } from "./OwnerStep";
import RemoveSelectedDialog from "./RemoveSelectedDialog";
import { useAllReports } from "./useAllReports";
import { useAllReportsListedReporter } from "./useAllReportsListedReporter";
import { useReportCreationOptions } from "./useReportCreationOptions";
import { useReportsPdfExport } from "./useReportsPdfExport";

const COULD_NOT_LOAD = "The reports couldn't be loaded.";

/** Portfolios first, then Teams, each by owner name; a sort the reader picks in the table still overrides it. */
const byOwner = (reports: IReportAcrossOwners[]): IReportAcrossOwners[] =>
	[...reports].sort(
		(a, b) =>
			Number(a.ownerKind === "team") - Number(b.ownerKind === "team") ||
			a.ownerName.localeCompare(b.ownerName),
	);
const LIST_MIN_HEIGHT = 120;
// Held back only in look on the macOS desktop app, so the tooltip saying why still reaches a keyboard reader.
const HELD_BACK_LOOK = { color: "action.disabled", cursor: "default" };

/** Whether the viewer administers at least one Team or Portfolio, so has somewhere to create reports. */
const isAdminOfAny = ({
	isRbacEnabled,
	isSystemAdmin,
	summary,
}: RbacState): boolean =>
	!isRbacEnabled ||
	isSystemAdmin ||
	(summary.adminTeamIds?.length ?? 0) > 0 ||
	(summary.adminPortfolioIds?.length ?? 0) > 0;

/** A failed permission load is never read as "Admin everywhere" by the owner step, whatever the hook answers elsewhere. */
const ownersStatusOf = ({ isLoading, loadFailed }: RbacState): OwnersStatus => {
	if (isLoading) return "loading";
	return loadFailed ? "failed" : "ready";
};

/** Create reports, and the wizard it opens once what a report needs has arrived for this opening. */
const useBulkCreation = (rbac: RbacState) => {
	// Counts every opening and never goes back, so no opening shares its key, or an earlier answer, with another.
	const openings = useRef(0);
	const [opening, setOpening] = useState<number | null>(null);
	const [retrying, setRetrying] = useState(false);
	const options = useReportCreationOptions(opening);
	const { licenseStatus, status } = useLicenseRestrictions();
	const { getTerm } = useTerminology();
	const isPremium = licenseStatus?.canUsePremiumFeatures === true;
	const lacksAdmin = !rbac.isLoading && !isAdminOfAny(rbac);
	const open = () => {
		setRetrying(false);
		openings.current += 1;
		setOpening(openings.current);
	};
	const close = () => {
		setRetrying(false);
		setOpening(null);
	};
	const retry = () => {
		setRetrying(true);
		options.refetch();
	};
	// The first answer opens the wizard, and from then on it is the reader's until they close it: a later read, or one
	// that fails, never takes their draft away. Once it has said it could not load, it stays open while Retry asks again.
	const arrived = options.data !== undefined;
	const showWizard =
		opening !== null && (arrived || options.isError || retrying);
	const waitingBeforeWizard = options.isFetching && !showWizard;
	const button = (
		<LicenseTooltip
			canUseFeature={isPremium}
			defaultTooltip={
				lacksAdmin
					? `You are not Admin of any ${getTerm(TERMINOLOGY_KEYS.TEAM)} or ${getTerm(TERMINOLOGY_KEYS.PORTFOLIO)}.`
					: ""
			}
			checkStatus={licenseStatus ? "ready" : status}
		>
			<span>
				<Button
					variant="contained"
					startIcon={
						waitingBeforeWizard ? <CircularProgress size={20} /> : <AddIcon />
					}
					disabled={
						!isPremium || rbac.isLoading || lacksAdmin || options.isFetching
					}
					onClick={open}
				>
					Create reports
				</Button>
			</span>
		</LicenseTooltip>
	);
	const wizard = showWizard ? (
		<BulkReportWizard
			key={opening}
			options={arrived ? options.data : undefined}
			isTeamAdmin={rbac.isTeamAdmin}
			isPortfolioAdmin={rbac.isPortfolioAdmin}
			ownersStatus={ownersStatusOf(rbac)}
			onRetryOwners={rbac.retry}
			retrying={options.isFetching}
			onRetry={retry}
			onClose={close}
		/>
	) : null;
	return { button, wizard };
};

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

/** The reports the reader picked, in the order picked, and a way to read them in the order the table shows them. */
const useRowPicks = (reports: readonly IReportAcrossOwners[] | undefined) => {
	const [selectedIds, setSelectedIds] = useState<GridRowId[]>([]);
	const tableOrder = useRef<(() => GridRowId[]) | undefined>(undefined);
	const reportOf = (id: GridRowId) =>
		reports?.find((report) => report.id === id) ?? [];
	const picked = selectedIds.flatMap((id) => reportOf(id));
	const inTableOrder = (): IReportAcrossOwners[] =>
		(tableOrder.current?.() ?? selectedIds)
			.filter((id) => selectedIds.includes(id))
			.flatMap((id) => reportOf(id));
	return {
		picking: { selectedIds, onSelectionChange: setSelectedIds, tableOrder },
		picked,
		inTableOrder,
		clear: () => setSelectedIds([]),
	};
};

type RowPicks = ReturnType<typeof useRowPicks>;

/** Remove, which asks before removing the picked reports. */
const useSelectionRemoval = ({ picked, clear }: RowPicks, rbac: RbacState) => {
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
const useSelectionExport = ({ picked, inTableOrder }: RowPicks) => {
	const pdfExport = useReportsPdfExport();
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

const ReportsPageContent: React.FC = () => {
	const rbac = useRbac();
	const { getTerm } = useTerminology();
	const bulkCreation = useBulkCreation(rbac);
	const { data, isFetching, isError, refetch } = useAllReports();
	const status = queryStatusOf(isFetching, isError);
	useAllReportsListedReporter(status === "ready" && data !== undefined);
	const picks = useRowPicks(data?.reports);
	const removal = useSelectionRemoval(picks, rbac);
	const selectionExport = useSelectionExport(picks);
	const { mayEdit } = mayEditReportsOf(rbac);
	const deletion = useReportDeletion(undefined);
	const editing = useReportEditing(undefined);
	const pdfExport = useReportPdfExport(undefined);
	const actions: ReportListActions = {
		onExportPdf: (report) => pdfExport.exportPdf(report, ownerOfReport(report)),
		preparingId: pdfExport.preparingId,
		editor: {
			onEdit: (report) => editing.ask(report, ownerOfReport(report)),
			onDelete: (report) => deletion.ask(report, ownerOfReport(report)),
			readingId: editing.readingId,
		},
		mayEditRow: (report) => mayEdit(ownerOfReport(report)),
	};

	return (
		<Container maxWidth={false}>
			<Typography variant="h4" component="h1" sx={{ my: 2 }}>
				Reports
			</Typography>
			<Box sx={{ display: "flex", alignItems: "center", gap: 2, mb: 2 }}>
				{bulkCreation.button}
				{removal.button}
				{selectionExport.button}
				<Typography color="text.secondary">{`${picks.picked.length} selected`}</Typography>
			</Box>
			<Box
				data-testid="all-reports"
				data-widget-status={status}
				aria-busy={status === "loading" ? "true" : undefined}
				sx={{
					display: "flex",
					flexDirection: "column",
					minHeight: LIST_MIN_HEIGHT,
				}}
			>
				<WidgetBody
					widgetKey="all-reports"
					status={status}
					hasContentToDim={data !== undefined}
					failureMessage={COULD_NOT_LOAD}
					onRetry={() => refetch()}
				>
					{data !== undefined &&
						(data.reports.length === 0 ? (
							<Typography color="text.secondary">
								{`Reports are created on a ${getTerm(TERMINOLOGY_KEYS.TEAM)}'s or ${getTerm(TERMINOLOGY_KEYS.PORTFOLIO)}'s Reports tab, or here with Create reports.`}
							</Typography>
						) : (
							<ReportList
								reports={byOwner(data.reports)}
								actions={actions}
								showOwner
								picking={picks.picking}
							/>
						))}
				</WidgetBody>
			</Box>
			{editing.element}
			{deletion.dialog}
			{pdfExport.frame}
			{selectionExport.frame}
			{bulkCreation.wizard}
			{removal.dialog}
		</Container>
	);
};

const ReportsPage: React.FC = () => (
	<SnackbarErrorHandler>
		<ReportsPageContent />
	</SnackbarErrorHandler>
);

export default ReportsPage;
