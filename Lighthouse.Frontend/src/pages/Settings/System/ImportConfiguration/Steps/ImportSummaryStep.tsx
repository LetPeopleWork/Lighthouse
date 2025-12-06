import CheckCircleOutlineIcon from "@mui/icons-material/CheckCircleOutline";
import ErrorOutlineIcon from "@mui/icons-material/ErrorOutline";
import WarningAmberIcon from "@mui/icons-material/WarningAmber";
import {
	Alert,
	Box,
	Button,
	Divider,
	Paper,
	Stack,
	Table,
	TableBody,
	TableCell,
	TableContainer,
	TableHead,
	TableRow,
	Typography,
} from "@mui/material";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import type { IProjectService } from "../../../../../services/Api/ProjectService";
import type { ITeamService } from "../../../../../services/Api/TeamService";
import { useTerminology } from "../../../../../services/TerminologyContext";
import type { ImportResults } from "../ImportResults";

interface ImportSummaryStepProps {
	importResults: ImportResults;
	teamService: ITeamService;
	projectService: IProjectService;
	onClose: () => void;
}

const ImportSummaryStep: React.FC<ImportSummaryStepProps> = ({
	importResults,
	teamService,
	projectService,
	onClose,
}) => {
	interface StatusCount {
		Success: number;
		"Validation Failed": number;
		Error: number;
	}

	const { getTerm } = useTerminology();
	const teamsTerm = getTerm(TERMINOLOGY_KEYS.TEAMS);
	const portfoliosTerm = getTerm(TERMINOLOGY_KEYS.PORTFOLIOS);
	const workTrackingSystemsTerm = getTerm(
		TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEMS,
	);

	const countResults = (entities: Array<{ status: string }>) => {
		return entities.reduce(
			(counts: StatusCount, item) => {
				const status = item.status as keyof StatusCount;
				counts[status] = (counts[status] || 0) + 1;
				return counts;
			},
			{ Success: 0, "Validation Failed": 0, Error: 0 },
		);
	};

	const workTrackingSystemsCounts = countResults(
		importResults.workTrackingSystems,
	);
	const teamsCounts = countResults(importResults.teams);
	const projectsCounts = countResults(importResults.projects);

	const hasSuccessful =
		importResults.teams.some((t) => t.status === "Success") ||
		importResults.projects.some((p) => p.status === "Success") ||
		importResults.workTrackingSystems.some((w) => w.status === "Success");

	const hasErrors =
		importResults.teams.some((t) => t.status === "Error") ||
		importResults.projects.some((p) => p.status === "Error") ||
		importResults.workTrackingSystems.some((w) => w.status === "Error");

	const hasValidationIssues =
		importResults.teams.some((t) => t.status === "Validation Failed") ||
		importResults.projects.some((p) => p.status === "Validation Failed") ||
		importResults.workTrackingSystems.some(
			(w) => w.status === "Validation Failed",
		);

	const onUpdateData = async () => {
		const successfulTeams = importResults.teams.filter(
			(team) => team.status === "Success",
		);

		for (const teamResult of successfulTeams) {
			await teamService.updateTeamData(teamResult.entity.id);
		}

		const successfulProjects = importResults.projects.filter(
			(project) => project.status === "Success",
		);

		for (const projectResult of successfulProjects) {
			await projectService.refreshFeaturesForProject(projectResult.entity.id);
		}

		onClose();
	};

	const renderStatusIcon = (status: string) => {
		switch (status) {
			case "Success":
				return <CheckCircleOutlineIcon color="success" />;
			case "Validation Failed":
				return <WarningAmberIcon color="warning" />;
			case "Error":
				return <ErrorOutlineIcon color="error" />;
			default:
				return null;
		}
	};

	const renderEntityTable = <
		T extends {
			entity: { id?: number | null; name: string };
			status: string;
			errorMessage?: string;
		},
	>(
		title: string,
		entities: T[],
	) => {
		if (entities.length === 0) return null;

		return (
			<Box sx={{ mt: 3 }}>
				<Typography variant="h6" gutterBottom>
					{title}
				</Typography>
				<TableContainer component={Paper}>
					<Table size="small">
						<TableHead>
							<TableRow>
								<TableCell>Name</TableCell>
								<TableCell>Status</TableCell>
								<TableCell>Details</TableCell>
							</TableRow>
						</TableHead>
						<TableBody>
							{entities.map((item) => (
								<TableRow key={`${item.entity.id}-${item.entity.name}`}>
									<TableCell>{item.entity.name}</TableCell>
									<TableCell>
										<Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
											{renderStatusIcon(item.status)}
											{item.status}
										</Box>
									</TableCell>
									<TableCell>{item.errorMessage ?? "-"}</TableCell>
								</TableRow>
							))}
						</TableBody>
					</Table>
				</TableContainer>
			</Box>
		);
	};

	return (
		<Box sx={{ width: "100%" }}>
			<Typography variant="h5" gutterBottom>
				Import Completed
			</Typography>
			<Box sx={{ mt: 2, mb: 3 }}>
				{hasErrors && (
					<Alert severity="error" sx={{ mb: 2 }}>
						Some items failed to import. Review the details below.
					</Alert>
				)}
				{hasValidationIssues && !hasErrors && (
					<Alert severity="warning" sx={{ mb: 2 }}>
						All items were imported but some have validation issues that need
						manual configuration.
					</Alert>
				)}
				{!hasValidationIssues && !hasErrors && (
					<Alert severity="success" sx={{ mb: 2 }}>
						All items were successfully imported.
					</Alert>
				)}
			</Box>{" "}
			<Box sx={{ mb: 3 }}>
				<Typography variant="h6" gutterBottom>
					Summary
				</Typography>
				<Stack direction="row" spacing={2}>
					<Paper sx={{ p: 2, flex: 1 }} elevation={2}>
						<Typography variant="subtitle1">
							{workTrackingSystemsTerm}
						</Typography>
						<Typography variant="body2">
							Success: {workTrackingSystemsCounts.Success}
						</Typography>
						<Typography variant="body2">
							Validation Issues:{" "}
							{workTrackingSystemsCounts["Validation Failed"]}
						</Typography>
						<Typography variant="body2">
							Errors: {workTrackingSystemsCounts.Error}
						</Typography>
					</Paper>
					<Paper sx={{ p: 2, flex: 1 }} elevation={2}>
						<Typography variant="subtitle1">{teamsTerm}</Typography>
						<Typography variant="body2">
							Success: {teamsCounts.Success}
						</Typography>
						<Typography variant="body2">
							Validation Issues: {teamsCounts["Validation Failed"]}
						</Typography>
						<Typography variant="body2">Errors: {teamsCounts.Error}</Typography>
					</Paper>
					<Paper sx={{ p: 2, flex: 1 }} elevation={2}>
						<Typography variant="subtitle1">{portfoliosTerm}</Typography>
						<Typography variant="body2">
							Success: {projectsCounts.Success}
						</Typography>
						<Typography variant="body2">
							Validation Issues: {projectsCounts["Validation Failed"]}
						</Typography>
						<Typography variant="body2">
							Errors: {projectsCounts.Error}
						</Typography>
					</Paper>
				</Stack>
			</Box>
			<Box
				sx={{
					height: "calc(100vh - 600px)",
					overflow: "auto",
					mb: 2,
				}}
			>
				{renderEntityTable(
					workTrackingSystemsTerm,
					importResults.workTrackingSystems,
				)}
				{renderEntityTable(teamsTerm, importResults.teams)}
				{renderEntityTable(portfoliosTerm, importResults.projects)}
			</Box>
			<Divider sx={{ my: 2 }} />
			<Box
				sx={{
					display: "flex",
					justifyContent: "space-between",
					alignItems: "center",
					position: "sticky",
					bottom: 0,
					padding: 2,
					borderTop: (theme) => `1px solid ${theme.palette.divider}`,
					zIndex: 10,
				}}
			>
				<Box />
				<Box>
					<Button
						variant="contained"
						color="primary"
						onClick={onUpdateData}
						disabled={!hasSuccessful}
						data-testid="update-data-button"
					>
						{`Update ${teamsTerm} and Project Data`}
					</Button>
					<Button
						sx={{ ml: 2 }}
						variant="outlined"
						color="secondary"
						onClick={onClose}
						data-testid="close-button"
					>
						Close
					</Button>
				</Box>
			</Box>
		</Box>
	);
};

export default ImportSummaryStep;
