import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import HistoryToggleOffIcon from "@mui/icons-material/HistoryToggleOff";
import PictureAsPdfIcon from "@mui/icons-material/PictureAsPdf";
import {
	Box,
	CircularProgress,
	IconButton,
	TextField,
	Tooltip,
} from "@mui/material";
import type React from "react";
import { useMemo, useState } from "react";
import DataGridBase from "../../../components/Common/DataGrid/DataGridBase";
import type {
	DataGridColumn,
	RowPicking,
} from "../../../components/Common/DataGrid/types";
import StyledLink from "../../../components/Common/StyledLink/StyledLink";
import type {
	IReportListItem,
	IReportOwner,
} from "../../../models/Reports/Report";
import type { IReportAcrossOwners } from "../../../models/Reports/ReportsAcrossOwners";
import { useTerminology } from "../../../services/TerminologyContext";
import { parseLocalDate } from "../../../utils/date/localDate";
import { isTauriOnMac } from "../../../utils/tauri";
import { OwnerKindIcon } from "./OwnerKindIcon";
import { PDF_EXPORT_UNAVAILABLE_ON_MAC } from "./pdfExportCopy";
import { ownerOfReport, ownerTermKeyOf, reportPathOf } from "./reportOwner";
import { CANNOT_BE_REBUILT } from "./thenRebuildCopy";
import { isShownByThisVersion, reportTypeTitleOf } from "./wizard/reportTypes";

/** What somebody who may change the reports can do from a row. */
export interface ReportListEditorActions {
	onEdit: (report: IReportListItem) => void;
	onDelete: (report: IReportListItem) => void;
	/** The report being read before its edit opens, whose Edit waits meanwhile. */
	readingId: number | undefined;
}

/** What every reader can do from a row, and, for somebody who may change the reports, what else. */
export interface ReportListActions {
	onExportPdf: (report: IReportListItem) => void;
	/** The report whose PDF is being prepared, whose icon waits while every other PDF icon is held back. */
	preparingId: number | undefined;
	/** Holds back every PDF icon while some other PDF on the page is prepared, such as one of several reports. */
	exportHeldBack?: boolean;
	editor?: ReportListEditorActions;
	/** Narrows the editor's icons to the rows it may change, when the rows have different owners. */
	mayEditRow?: (report: IReportListItem) => boolean;
}

interface ReportListProps {
	/** The owner whose Reports tab this is; absent on the page that lists every owner's reports. */
	owner?: IReportOwner;
	reports: IReportListItem[];
	/** Without them a row offers no icons. */
	actions?: ReportListActions;
	/** Adds a column naming the Team or Portfolio each report is on. */
	showOwner?: boolean;
	/** Lets the reader pick rows by their checkboxes; without it the list offers none. */
	picking?: RowPicking;
}

// Held back only in look while one PDF is prepared: a truly disabled icon would throw a keyboard reader's focus to
// the page.
const HELD_BACK_LOOK = { color: "action.disabled", cursor: "default" };

const ExportPdfButton: React.FC<{
	readonly report: IReportListItem;
	readonly actions: ReportListActions;
}> = ({ report, actions }) => {
	const isHeldBack =
		actions.preparingId !== undefined || actions.exportHeldBack === true;
	if (isTauriOnMac()) {
		return (
			<Tooltip describeChild title={PDF_EXPORT_UNAVAILABLE_ON_MAC}>
				<IconButton
					aria-label={`Export ${report.name} as PDF`}
					aria-disabled="true"
					color="primary"
					sx={HELD_BACK_LOOK}
				>
					<PictureAsPdfIcon />
				</IconButton>
			</Tooltip>
		);
	}
	return (
		<Tooltip title="Export PDF">
			<IconButton
				aria-label={`Export ${report.name} as PDF`}
				aria-disabled={isHeldBack ? "true" : undefined}
				color="primary"
				onClick={isHeldBack ? undefined : () => actions.onExportPdf(report)}
				sx={isHeldBack ? HELD_BACK_LOOK : undefined}
			>
				{actions.preparingId === report.id ? (
					<CircularProgress size={20} />
				) : (
					<PictureAsPdfIcon />
				)}
			</IconButton>
		</Tooltip>
	);
};

const EditButton: React.FC<{
	readonly report: IReportListItem;
	readonly actions: ReportListEditorActions;
}> = ({ report, actions }) => {
	const isReading = actions.readingId === report.id;
	return (
		<Tooltip title="Edit report">
			<span>
				<IconButton
					aria-label={`Edit ${report.name}`}
					color="primary"
					disabled={actions.readingId !== undefined}
					onClick={() => actions.onEdit(report)}
				>
					{isReading ? <CircularProgress size={20} /> : <EditIcon />}
				</IconButton>
			</span>
		</Tooltip>
	);
};

const DeleteButton: React.FC<{
	readonly report: IReportListItem;
	readonly actions: ReportListEditorActions;
}> = ({ report, actions }) => (
	<Tooltip title="Delete">
		<IconButton
			aria-label={`Delete ${report.name}`}
			color="primary"
			onClick={() => actions.onDelete(report)}
		>
			<DeleteIcon />
		</IconButton>
	</Tooltip>
);

const RowActions: React.FC<{
	readonly report: IReportListItem;
	readonly actions: ReportListActions;
}> = ({ report, actions }) => {
	const editor =
		actions.mayEditRow?.(report) === false ? undefined : actions.editor;
	const isShown = isShownByThisVersion(report.templateKey);
	return (
		<>
			{isShown && <ExportPdfButton report={report} actions={actions} />}
			{isShown && editor !== undefined && (
				<EditButton report={report} actions={editor} />
			)}
			{editor !== undefined && (
				<DeleteButton report={report} actions={editor} />
			)}
		</>
	);
};

const actionsColumn = (
	actions: ReportListActions,
): DataGridColumn<IReportListItem> => ({
	field: "actions",
	headerName: "Actions",
	width: actions.editor === undefined ? 100 : 150,
	sortable: false,
	hideable: false,
	renderCell: ({ row }) => <RowActions report={row} actions={actions} />,
});

const OwnerLink: React.FC<{ readonly report: IReportAcrossOwners }> = ({
	report,
}) => {
	const { getTerm } = useTerminology();
	const owner = ownerOfReport(report);
	return (
		<Box sx={{ display: "flex", alignItems: "center", gap: 0.5 }}>
			<OwnerKindIcon kind={owner.kind} />
			<StyledLink
				to={`/${owner.kind}s/${owner.id}`}
				aria-label={`${report.ownerName}, ${getTerm(ownerTermKeyOf(owner))}`}
			>
				{report.ownerName}
			</StyledLink>
		</Box>
	);
};

const NameCell: React.FC<{ readonly report: IReportListItem }> = ({
	report,
}) => (
	<Box sx={{ display: "flex", alignItems: "center", gap: 0.5 }}>
		<StyledLink to={reportPathOf(ownerOfReport(report), report.id)}>
			{report.name}
		</StyledLink>
		{report.thenAndNow?.thenRebuildable === false && (
			<Tooltip title={CANNOT_BE_REBUILT}>
				<HistoryToggleOffIcon
					fontSize="small"
					color="warning"
					aria-label={CANNOT_BE_REBUILT}
				/>
			</Tooltip>
		)}
	</Box>
);

const ownerColumn: DataGridColumn<IReportListItem> = {
	field: "ownerName",
	headerName: "Owner",
	width: 200,
	renderCell: ({ row }) => <OwnerLink report={row as IReportAcrossOwners} />,
};

const FILTER_LABEL = "Filter: name / owner / type";

const matchesFilter = (
	report: IReportListItem,
	needle: string,
	ownerKindWord: (owner: IReportOwner) => string,
): boolean => {
	const owner = ownerOfReport(report);
	const { ownerName = "" } = report as Partial<IReportAcrossOwners>;
	return [
		report.name,
		ownerName,
		ownerKindWord(owner),
		reportTypeTitleOf(report.templateKey) ?? "",
	].some((text) => text.toLowerCase().includes(needle));
};

const ReportList: React.FC<Readonly<ReportListProps>> = ({
	owner,
	reports,
	actions,
	showOwner = false,
	picking,
}) => {
	const { getTerm } = useTerminology();
	const [filter, setFilter] = useState("");
	const needle = filter.trim().toLowerCase();
	const shownReports = useMemo(
		() =>
			needle === ""
				? reports
				: reports.filter((report) =>
						matchesFilter(report, needle, (o) => getTerm(ownerTermKeyOf(o))),
					),
		[reports, needle, getTerm],
	);
	const columns: DataGridColumn<IReportListItem>[] = useMemo(() => {
		const shown: DataGridColumn<IReportListItem>[] = [
			{
				field: "name",
				headerName: "Name",
				flex: 1,
				hideable: false,
				renderCell: ({ row }) => <NameCell report={row} />,
			},
			{
				field: "templateKey",
				headerName: "Type",
				width: 200,
				valueGetter: (_value, row) =>
					reportTypeTitleOf(row.templateKey) ?? "Not supported in this version",
			},
			...(showOwner ? [ownerColumn] : []),
			{
				field: "createdOn",
				headerName: "Created",
				width: 160,
				renderCell: ({ row }) =>
					parseLocalDate(row.createdOn)?.toLocaleDateString(),
			},
		];
		return actions === undefined ? shown : [...shown, actionsColumn(actions)];
	}, [actions, showOwner]);

	return (
		<>
			{showOwner && (
				<TextField
					size="small"
					placeholder={FILTER_LABEL}
					value={filter}
					onChange={(event) => {
						setFilter(event.target.value);
						picking?.onSelectionChange([]);
					}}
					slotProps={{ htmlInput: { "aria-label": FILTER_LABEL } }}
					sx={{ mb: 1, minWidth: 300 }}
				/>
			)}
			<DataGridBase
				rows={shownReports}
				columns={columns}
				storageKey={
					owner === undefined ? "reports-all" : `reports-${owner.kind}`
				}
				checkboxSelection={picking}
			/>
		</>
	);
};

export default ReportList;
