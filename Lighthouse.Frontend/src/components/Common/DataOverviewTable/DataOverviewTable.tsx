import ContentCopyIcon from "@mui/icons-material/ContentCopy";
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
	Link as MuiLink,
	Tooltip,
	Typography,
	useMediaQuery,
	useTheme,
} from "@mui/material";
import type { GridValidRowModel } from "@mui/x-data-grid";
import type React from "react";
import { useCallback, useMemo } from "react";
import { Link, useNavigate } from "react-router-dom";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
import type { IPortfolio } from "../../../models/Portfolio/Portfolio";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import DataGridBase from "../DataGrid/DataGridBase";
import type { DataGridColumn } from "../DataGrid/types";
import LocalDateTimeDisplay from "../LocalDateTimeDisplay/LocalDateTimeDisplay";
import { DeliveriesChips } from "./DeliveriesChips";

interface DataOverviewTableProps<IFeatureOwner> {
	data: IFeatureOwner[];
	api: "teams" | "portfolios";
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
	const navigate = useNavigate();
	const isMobile = useMediaQuery(theme.breakpoints.down("sm"));
	const isTablet = useMediaQuery(theme.breakpoints.down("md"));
	const { getTerm } = useTerminology();

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

	// Type guard to check if item is a Portfolio
	const isPortfolio = useCallback((item: IFeatureOwner): item is IPortfolio => {
		return "involvedTeams" in item;
	}, []);

	// Check if any of the data items are portfolios
	const hasAnyPortfolios = data.some(isPortfolio);

	const handleClone = useCallback(
		(item: IFeatureOwner) => {
			if (api === "teams") {
				navigate(`/teams/new?cloneFrom=${item.id}`);
			} else if (api === "portfolios") {
				navigate(`/portfolios/new?cloneFrom=${item.id}`);
			}
		},
		[navigate, api],
	);

	const columns: DataGridColumn<IFeatureOwner & GridValidRowModel>[] =
		useMemo(() => {
			const baseColumns: DataGridColumn<IFeatureOwner & GridValidRowModel>[] = [
				{
					field: "name",
					headerName: "Name",
					width: isMobile ? 200 : 250,
					flex: 1,
					hideable: false,
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

			if (hasAnyPortfolios) {
				baseColumns.push({
					field: "deliveries",
					headerName: getTerm(TERMINOLOGY_KEYS.DELIVERIES),
					width: 300,
					hideable: !isMobile,
					sortable: false,
					renderCell: ({ row }) => {
						if (!isPortfolio(row)) return null;
						return <DeliveriesChips portfolioId={row.id} />;
					},
				});
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
					headerName: "Actions",
					width: isMobile ? 160 : 200,
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
							{(api === "teams" || api === "portfolios") && (
								<Tooltip title="Clone">
									<IconButton
										onClick={() => handleClone(row)}
										size={isTablet ? "small" : "medium"}
										sx={{
											color: theme.palette.primary.main,
											transition: "transform 0.2s",
											"&:hover": { transform: "scale(1.1)" },
										}}
										aria-label="Clone"
									>
										<ContentCopyIcon fontSize={isTablet ? "small" : "medium"} />
									</IconButton>
								</Tooltip>
							)}
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
			hasAnyPortfolios,
			getTerm,
			isPortfolio,
			onDelete,
			handleClone,
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
						storageKey={`data-overview-${api}`}
					/>
				</Box>
			)}
		</Container>
	);
};

export default DataOverviewTable;
