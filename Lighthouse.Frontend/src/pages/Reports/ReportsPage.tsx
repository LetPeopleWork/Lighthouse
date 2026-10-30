import AddIcon from "@mui/icons-material/Add";
import {
	Box,
	Button,
	CircularProgress,
	Container,
	Typography,
} from "@mui/material";
import type React from "react";
import { useRef, useState } from "react";
import { LicenseTooltip } from "../../components/App/License/LicenseToolTip";
import SnackbarErrorHandler from "../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import { useLicenseRestrictions } from "../../hooks/useLicenseRestrictions";
import { type RbacState, useRbac } from "../../hooks/useRbac";
import { TERMINOLOGY_KEYS } from "../../models/TerminologyKeys";
import { useTerminology } from "../../services/TerminologyContext";
import { WidgetBody } from "../Common/MetricsView/WidgetShell";
import { queryStatusOf } from "../Common/MetricsView/widgetStatus";
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
import { useAllReports } from "./useAllReports";
import { useAllReportsListedReporter } from "./useAllReportsListedReporter";
import { useReportCreationOptions } from "./useReportCreationOptions";

const COULD_NOT_LOAD = "The reports couldn't be loaded.";
const LIST_MIN_HEIGHT = 120;

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

const ReportsPageContent: React.FC = () => {
	const rbac = useRbac();
	const { getTerm } = useTerminology();
	const bulkCreation = useBulkCreation(rbac);
	const { data, isFetching, isError, refetch } = useAllReports();
	const status = queryStatusOf(isFetching, isError);
	useAllReportsListedReporter(status === "ready" && data !== undefined);
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
			<Box sx={{ display: "flex", mb: 2 }}>{bulkCreation.button}</Box>
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
							<ReportList reports={data.reports} actions={actions} showOwner />
						))}
				</WidgetBody>
			</Box>
			{editing.element}
			{deletion.dialog}
			{pdfExport.frame}
			{bulkCreation.wizard}
		</Container>
	);
};

const ReportsPage: React.FC = () => (
	<SnackbarErrorHandler>
		<ReportsPageContent />
	</SnackbarErrorHandler>
);

export default ReportsPage;
