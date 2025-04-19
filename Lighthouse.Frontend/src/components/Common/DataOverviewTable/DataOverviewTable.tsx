import {
	Alert,
	Box,
	Container,
	Fade,
	IconButton,
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
import { useState } from "react";

import { Link, useNavigate } from "react-router-dom";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";

import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import InfoIcon from "@mui/icons-material/Info";
import ActionButton from "../ActionButton/ActionButton";
import FilterBar from "../FilterBar/FilterBar";
import ProgressIndicator from "../ProgressIndicator/ProgressIndicator";

interface DataOverviewTableProps<IFeatureOwner> {
	data: IFeatureOwner[];
	api: string;
	onDelete: (item: IFeatureOwner) => void;
}

const DataOverviewTable: React.FC<DataOverviewTableProps<IFeatureOwner>> = ({
	data,
	api,
	onDelete,
}) => {
	const [filterText, setFilterText] = useState("");
	const navigate = useNavigate();
	const theme = useTheme();
	const isMobile = useMediaQuery(theme.breakpoints.down("sm"));
	const isTablet = useMediaQuery(theme.breakpoints.down("md"));

	const filteredData = data.filter((item) => isMatchingFilterText(item.name));

	function isMatchingFilterText(textToCheck: string) {
		if (!filterText) {
			return true;
		}

		return textToCheck.toLowerCase().includes(filterText.toLowerCase());
	}

	const handleRedirect = async () => {
		navigate(`/${api}/new`);
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
					{api} Overview
				</Typography>

				<ActionButton
					buttonText={`Add New ${api.slice(0, -1)}`}
					startIcon={<AddIcon />}
					onClickHandler={handleRedirect}
					buttonVariant="contained"
				/>
			</Box>

			<FilterBar
				filterText={filterText}
				onFilterTextChange={setFilterText}
				data-testid="filter-bar"
			/>

			{data.length === 0 ? (
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
							No {api} Defined.{" "}
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
			) : filteredData.length === 0 ? (
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
						No {api} found matching the filter: <strong>"{filterText}"</strong>
					</Alert>
				</Fade>
			) : (
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
								bgcolor:
									theme.palette.mode === "dark"
										? "rgba(255, 255, 255, 0.08)"
										: "rgba(0, 0, 0, 0.03)",
							}}
						>
							<TableRow>
								<TableCell sx={{ width: isMobile ? "35%" : "20%" }}>
									<Typography variant="subtitle1" fontWeight="bold">
										Name
									</Typography>
								</TableCell>
								{!isMobile && (
									<TableCell sx={{ width: "15%" }}>
										<Typography variant="subtitle1" fontWeight="bold">
											Features
										</Typography>
									</TableCell>
								)}
								<TableCell>
									<Typography variant="subtitle1" fontWeight="bold">
										Progress
									</Typography>
								</TableCell>
								<TableCell
									sx={{ width: isMobile ? "25%" : "15%", textAlign: "right" }}
								/>
							</TableRow>
						</TableHead>
						<TableBody>
							{filteredData.map((item: IFeatureOwner, index) => (
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
											bgcolor:
												theme.palette.mode === "dark"
													? "rgba(255, 255, 255, 0.05)"
													: "rgba(0, 0, 0, 0.02)",
										},
									}}
								>
									<TableCell>
										<MuiLink
											href={`/${api}/${item.id}`}
											style={{
												textDecoration: "none",
												color: theme.palette.primary.main,
												fontWeight: "bold",
											}}
										>
											{item.name}
										</MuiLink>
									</TableCell>
									{!isMobile && (
										<TableCell>
											<Typography variant="body2">
												{item.remainingFeatures} feature
												{item.remainingFeatures !== 1 ? "s" : ""}
											</Typography>
										</TableCell>
									)}
									<TableCell>
										<ProgressIndicator progressableItem={item} title={""} />
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
							))}
						</TableBody>
					</Table>
				</TableContainer>
			)}
		</Container>
	);
};

export default DataOverviewTable;
