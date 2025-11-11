import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import InfoIcon from "@mui/icons-material/Info";
import {
	Alert,
	Box,
	Chip,
	Container,
	Fade,
	IconButton,
	LinearProgress,
	Link as MuiLink,
	Paper,
	Table,
	TableBody,
	TableCell,
	TableContainer,
	TableHead,
	TableRow,
	Tooltip,
	Typography,
	useMediaQuery,
	useTheme,
} from "@mui/material";
import type React from "react";
import { Link } from "react-router-dom";
import type { IWhenForecast } from "../../../models/Forecasts/WhenForecast";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
import type { IProject } from "../../../models/Project/Project";
import { ForecastLevel } from "../Forecasts/ForecastLevel";
import LocalDateTimeDisplay from "../LocalDateTimeDisplay/LocalDateTimeDisplay";

interface DataOverviewTableProps<IFeatureOwner> {
	data: IFeatureOwner[];
	api: string;
	title: string;
	onDelete: (item: IFeatureOwner) => void;
	filterText: string;
}

const DataOverviewTable: React.FC<DataOverviewTableProps<IFeatureOwner>> = ({
	data,
	api,
	title,
	onDelete,
	filterText,
}) => {
	const theme = useTheme();
	const isMobile = useMediaQuery(theme.breakpoints.down("sm"));
	const isTablet = useMediaQuery(theme.breakpoints.down("md"));

	const filteredData = data
		.filter((item) => isMatchingFilterText(item))
		.sort((a, b) => a.name.localeCompare(b.name));

	function isMatchingFilterText(item: IFeatureOwner) {
		const searchTerm = filterText.toLowerCase();

		if (item.name.toLowerCase().includes(searchTerm)) {
			return true;
		}

		if (item.tags?.some((tag) => tag.toLowerCase().includes(searchTerm))) {
			return true;
		}

		return false;
	}

	// Type guard to check if item is a Project
	const isProject = (item: IFeatureOwner): item is IProject => {
		return (
			"totalWorkItems" in item &&
			"remainingWorkItems" in item &&
			"forecasts" in item
		);
	};

	// Check if any of the data items are projects
	const hasAnyProjects = data.some(isProject);

	// Get the key forecasts (50/70/85/95 percentile)
	const getKeyForecasts = (project: IProject) => {
		return [50, 70, 85, 95]
			.map((percentile) =>
				project.forecasts.find((f) => f.probability === percentile),
			)
			.filter((f) => f !== undefined);
	};

	const renderProgressCell = (item: IProject) => {
		return (
			<Box sx={{ width: "100%" }}>
				<Box
					sx={{
						display: "flex",
						justifyContent: "space-between",
						mb: 0.5,
					}}
				>
					<Typography variant="caption" color="text.secondary">
						{item.totalWorkItems - item.remainingWorkItems} done out of{" "}
						{item.totalWorkItems}
					</Typography>
					<Typography variant="caption" color="text.secondary">
						{item.totalWorkItems > 0
							? Math.round(
									((item.totalWorkItems - item.remainingWorkItems) /
										item.totalWorkItems) *
										100,
								)
							: 0}
						%
					</Typography>
				</Box>
				<LinearProgress
					variant="determinate"
					value={
						item.totalWorkItems > 0
							? ((item.totalWorkItems - item.remainingWorkItems) /
									item.totalWorkItems) *
								100
							: 0
					}
					sx={{
						height: 8,
						borderRadius: 1,
						bgcolor: theme.palette.grey[200],
						"& .MuiLinearProgress-bar": {
							bgcolor: theme.palette.primary.main,
						},
					}}
				/>
			</Box>
		);
	};

	const renderForecastsCell = (keyForecasts: IWhenForecast[]) => {
		if (keyForecasts.length === 0) {
			return (
				<Typography variant="body2" color="text.secondary">
					—
				</Typography>
			);
		}

		return (
			<Box
				sx={{
					display: "flex",
					flexWrap: "wrap",
					gap: 0.5,
					alignItems: "center",
				}}
			>
				{keyForecasts.map((forecast) => {
					const forecastLevel = new ForecastLevel(forecast.probability);
					const date = new Date(forecast.expectedDate);
					const formattedDate = date.toLocaleDateString();

					return (
						<Tooltip
							key={forecast.probability}
							title={`${forecast.probability}% confidence: ${date.toLocaleDateString()}`}
						>
							<Chip
								label={`${formattedDate}`}
								size="small"
								sx={{
									bgcolor: forecastLevel.color,
									color: "#fff",
									fontWeight: "bold",
									fontSize: "0.7rem",
								}}
							/>
						</Tooltip>
					);
				})}
			</Box>
		);
	};

	const renderTableRow = (item: IFeatureOwner, index: number) => {
		const isProjectItem = isProject(item);
		const keyForecasts = isProjectItem ? getKeyForecasts(item) : [];

		return (
			<TableRow
				key={item.id}
				data-testid={`table-row-${item.id}`}
				sx={{
					opacity: 0,
					animation: "fadeIn 0.5s forwards",
					animationDelay: `${index * 0.05}s`,
					"@keyframes fadeIn": {
						"0%": { opacity: 0, transform: "translateY(5px)" },
						"100%": { opacity: 1, transform: "translateY(0)" },
					},
					"&:hover": {
						bgcolor: theme.palette.action.hover,
					},
				}}
			>
				<TableCell>
					<Link
						to={`/${api}/${item.id}`}
						style={{
							textDecoration: "none",
							color: theme.palette.primary.main,
							fontWeight: "bold",
						}}
					>
						{item.name}
					</Link>
				</TableCell>
				{!isMobile && (
					<TableCell>
						<Typography variant="body2">
							{item.remainingFeatures} feature
							{item.remainingFeatures === 1 ? "" : "s"}
						</Typography>
					</TableCell>
				)}
				{!isMobile && (
					<TableCell>
						<Box sx={{ display: "flex", flexWrap: "wrap", gap: 0.5 }}>
							{item.tags
								.filter((t) => t.trim() !== "")
								.map((tag) => (
									<Chip
										key={`${item.id}-${tag}`}
										label={tag}
										size="small"
										color="primary"
										variant="outlined"
										sx={{ fontWeight: "bold" }}
									/>
								))}
						</Box>
					</TableCell>
				)}
				{!isMobile && hasAnyProjects && (
					<TableCell>
						{isProjectItem ? (
							renderProgressCell(item)
						) : (
							<Typography variant="body2" color="text.secondary">
								—
							</Typography>
						)}
					</TableCell>
				)}
				{!isTablet && hasAnyProjects && (
					<TableCell>
						{isProjectItem ? (
							renderForecastsCell(keyForecasts)
						) : (
							<Typography variant="body2" color="text.secondary">
								—
							</Typography>
						)}
					</TableCell>
				)}
				<TableCell>
					<LocalDateTimeDisplay utcDate={item.lastUpdated} showTime={true} />
				</TableCell>
				<TableCell align="right">
					<Box
						sx={{
							display: "flex",
							justifyContent: "flex-end",
							gap: isTablet ? 0 : 1,
						}}
					>
						<Tooltip title="Details">
							<IconButton
								component={Link}
								to={`/${api}/${item.id}`}
								size={isTablet ? "small" : "medium"}
								sx={{
									color: theme.palette.primary.main,
									transition: "transform 0.2s",
									"&:hover": { transform: "scale(1.1)" },
								}}
							>
								<InfoIcon fontSize={isTablet ? "small" : "medium"} />
							</IconButton>
						</Tooltip>
						<Tooltip title="Edit">
							<IconButton
								component={Link}
								to={`/${api}/edit/${item.id}`}
								size={isTablet ? "small" : "medium"}
								sx={{
									color: theme.palette.primary.main,
									transition: "transform 0.2s",
									"&:hover": { transform: "scale(1.1)" },
								}}
							>
								<EditIcon fontSize={isTablet ? "small" : "medium"} />
							</IconButton>
						</Tooltip>
						<Tooltip title="Delete">
							<IconButton
								onClick={() => onDelete(item)}
								size={isTablet ? "small" : "medium"}
								sx={{
									color: theme.palette.primary.main,
									transition: "transform 0.2s",
									"&:hover": { transform: "scale(1.1)" },
								}}
							>
								<DeleteIcon
									fontSize={isTablet ? "small" : "medium"}
									data-testid="delete-item-button"
								/>
							</IconButton>
						</Tooltip>
					</Box>
				</TableCell>
			</TableRow>
		);
	};

	return (
		<Container maxWidth={false} sx={{ pb: 4 }}>
			<Box
				sx={{
					display: "flex",
					justifyContent: "space-between",
					alignItems: "center",
					flexDirection: isMobile ? "column" : "row",
					gap: 2,
					mb: 2,
				}}
			>
				<Typography
					variant="h5"
					sx={{
						fontWeight: 600,
						color: theme.palette.primary.main,
						textTransform: "capitalize",
					}}
				>
					{title}
				</Typography>
			</Box>

			{data.length === 0 && (
				<Fade in={true} timeout={800}>
					<Alert
						severity="info"
						variant="outlined"
						sx={{
							mb: 3,
							borderRadius: 2,
							boxShadow: theme.shadows[1],
						}}
						data-testid="empty-items-message"
					>
						<Typography variant="body1">
							No {title} found.{" "}
							<MuiLink
								component={Link}
								to="/settings?tab=demodata"
								style={{
									color: theme.palette.primary.main,
									textDecoration: "none",
									fontWeight: 500,
								}}
								sx={{
									"&:hover": {
										textDecoration: "underline",
									},
								}}
							>
								Load Demo Data
							</MuiLink>{" "}
							or{" "}
							<MuiLink
								href="https://docs.lighthouse.letpeople.work"
								target="_blank"
								rel="noopener noreferrer"
								style={{
									color: theme.palette.primary.main,
									textDecoration: "none",
									fontWeight: 500,
								}}
								sx={{
									"&:hover": {
										textDecoration: "underline",
									},
								}}
							>
								Check the documentation
							</MuiLink>{" "}
							for more information.
						</Typography>
					</Alert>
				</Fade>
			)}

			{data.length > 0 && filteredData.length === 0 && (
				<Fade in={true} timeout={500}>
					<Alert
						severity="warning"
						variant="outlined"
						sx={{
							mb: 3,
							borderRadius: 2,
							boxShadow: theme.shadows[1],
						}}
						data-testid="no-items-message"
					>
						No {title} found matching the filter <strong>{filterText}</strong>
					</Alert>
				</Fade>
			)}

			{data.length > 0 && filteredData.length > 0 && (
				<TableContainer
					component={Paper}
					data-testid="table-container"
					sx={{
						borderRadius: 2,
						boxShadow: theme.shadows[3],
						overflow: "hidden",
					}}
				>
					<Table>
						<TableHead
							sx={{
								bgcolor: theme.palette.action.hover,
							}}
						>
							<TableRow>
								<TableCell sx={{ width: isMobile ? "35%" : "15%" }}>
									<Typography variant="subtitle1" fontWeight="bold">
										Name
									</Typography>
								</TableCell>
								{!isMobile && (
									<TableCell sx={{ width: "10%" }}>
										<Typography variant="subtitle1" fontWeight="bold">
											Features
										</Typography>
									</TableCell>
								)}
								{!isMobile && (
									<TableCell sx={{ width: "15%" }}>
										<Typography variant="subtitle1" fontWeight="bold">
											Tags
										</Typography>
									</TableCell>
								)}
								{!isMobile && hasAnyProjects && (
									<TableCell sx={{ width: "15%" }}>
										<Typography variant="subtitle1" fontWeight="bold">
											Progress
										</Typography>
									</TableCell>
								)}
								{!isTablet && hasAnyProjects && (
									<TableCell sx={{ width: "20%" }}>
										<Typography variant="subtitle1" fontWeight="bold">
											Forecasts
										</Typography>
									</TableCell>
								)}
								<TableCell sx={{ width: "10%" }}>
									<Typography variant="subtitle1" fontWeight="bold">
										Last Updated
									</Typography>
								</TableCell>
								<TableCell
									sx={{ width: isMobile ? "25%" : "10%", textAlign: "right" }}
								/>
							</TableRow>
						</TableHead>
						<TableBody>{filteredData.map(renderTableRow)}</TableBody>
					</Table>
				</TableContainer>
			)}
		</Container>
	);
};

export default DataOverviewTable;
