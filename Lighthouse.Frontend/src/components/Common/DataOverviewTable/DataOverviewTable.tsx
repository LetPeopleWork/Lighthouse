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
	Tooltip,
	Typography,
	useMediaQuery,
	useTheme,
} from "@mui/material";
import type { GridValidRowModel } from "@mui/x-data-grid";
import type React from "react";
import { useCallback, useMemo } from "react";
import { Link } from "react-router-dom";
import type { IWhenForecast } from "../../../models/Forecasts/WhenForecast";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
import type { IProject } from "../../../models/Project/Project";
import DataGridBase from "../DataGrid/DataGridBase";
import type { DataGridColumn } from "../DataGrid/types";
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
	const isProject = useCallback((item: IFeatureOwner): item is IProject => {
		return (
			"totalWorkItems" in item &&
			"remainingWorkItems" in item &&
			"forecasts" in item
		);
	}, []);

	// Check if any of the data items are projects
	const hasAnyProjects = data.some(isProject);

	// Get the key forecasts (50/70/85/95 percentile)
	const getKeyForecasts = useCallback((project: IProject) => {
		return [50, 70, 85, 95]
			.map((percentile) =>
				project.forecasts.find((f) => f.probability === percentile),
			)
			.filter((f) => f !== undefined);
	}, []);

	const renderProgressCell = useCallback(
		(item: IProject) => {
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
		},
		[theme],
	);

	const renderForecastsCell = useCallback((keyForecasts: IWhenForecast[]) => {
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
	}, []);

	// Define DataGrid columns - dynamically include project-specific columns
	const columns: DataGridColumn<IFeatureOwner & GridValidRowModel>[] =
		useMemo(() => {
			const baseColumns: DataGridColumn<IFeatureOwner & GridValidRowModel>[] = [
				{
					field: "name",
					headerName: "Name",
					width: isMobile ? 200 : 250,
					flex: 1,
					renderCell: ({ row }) => (
						<Link
							to={`/${api}/${row.id}`}
							style={{
								textDecoration: "none",
								color: theme.palette.primary.main,
								fontWeight: "bold",
							}}
						>
							{row.name}
						</Link>
					),
				},
				{
					field: "remainingFeatures",
					headerName: "Features",
					width: 120,
					hideable: !isMobile,
					renderCell: ({ value }) => {
						const count = value as number;
						return (
							<Typography variant="body2">
								{count} feature{count === 1 ? "" : "s"}
							</Typography>
						);
					},
				},
				{
					field: "tags",
					headerName: "Tags",
					width: 200,
					flex: 1,
					hideable: !isMobile,
					sortable: false,
					renderCell: ({ value }) => {
						const tags = value as string[];
						return (
							<Box sx={{ display: "flex", flexWrap: "wrap", gap: 0.5 }}>
								{tags
									.filter((t) => t.trim() !== "")
									.map((tag) => (
										<Chip
											key={tag}
											label={tag}
											size="small"
											color="primary"
											variant="outlined"
											sx={{ fontWeight: "bold" }}
										/>
									))}
							</Box>
						);
					},
				},
			];

			// Add project-specific columns if we have projects
			if (hasAnyProjects) {
				baseColumns.push(
					{
						field: "progress",
						headerName: "Progress",
						width: 220,
						hideable: !isMobile,
						sortable: false,
						renderCell: ({ row }) => {
							if (!isProject(row)) {
								return (
									<Typography variant="body2" color="text.secondary">
										—
									</Typography>
								);
							}
							return renderProgressCell(row);
						},
					},
					{
						field: "forecasts",
						headerName: "Forecasts",
						width: 280,
						hideable: !isTablet,
						sortable: false,
						renderCell: ({ row }) => {
							if (!isProject(row)) {
								return (
									<Typography variant="body2" color="text.secondary">
										—
									</Typography>
								);
							}
							const keyForecasts = getKeyForecasts(row);
							return renderForecastsCell(keyForecasts);
						},
					},
				);
			}

			// Add common columns
			baseColumns.push(
				{
					field: "lastUpdated",
					headerName: "Last Updated",
					width: 180,
					renderCell: ({ value }) => (
						<LocalDateTimeDisplay utcDate={value as Date} showTime={true} />
					),
				},
				{
					field: "actions",
					headerName: "",
					width: isMobile ? 120 : 150,
					sortable: false,
					hideable: false,
					renderCell: ({ row }) => (
						<Box
							sx={{
								display: "flex",
								justifyContent: "flex-end",
								gap: isTablet ? 0 : 1,
								width: "100%",
							}}
						>
							<Tooltip title="Details">
								<IconButton
									component={Link}
									to={`/${api}/${row.id}`}
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
									to={`/${api}/edit/${row.id}`}
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
									onClick={() => onDelete(row)}
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
					),
				},
			);

			return baseColumns;
		}, [
			api,
			theme,
			isMobile,
			isTablet,
			hasAnyProjects,
			onDelete,
			isProject,
			renderProgressCell,
			getKeyForecasts,
			renderForecastsCell,
		]);

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
				<Box data-testid="datagrid-container">
					<DataGridBase
						rows={filteredData as (IFeatureOwner & GridValidRowModel)[]}
						columns={columns}
						loading={false}
						autoHeight={true}
						hidePagination={true}
						enableFiltering={true}
						disableColumnSelector={true}
					/>
				</Box>
			)}
		</Container>
	);
};

export default DataOverviewTable;
