import {
	Box,
	Chip,
	CircularProgress,
	Typography,
	useTheme,
} from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import type { IBoardInformation } from "../../models/Boards/BoardInformation";
import InputGroup from "../Common/InputGroup/InputGroup";

interface BoardInformationDisplayProps {
	boardInformation: IBoardInformation | null;
	loading?: boolean;
}

const BoardInformationDisplay: React.FC<BoardInformationDisplayProps> = ({
	boardInformation,
	loading = false,
}) => {
	const theme = useTheme();

	if (loading) {
		return (
			<Box
				sx={{
					display: "flex",
					flexDirection: "column",
					alignItems: "center",
					justifyContent: "center",
					padding: 3,
					gap: 2,
				}}
			>
				<CircularProgress />
				<Typography variant="body2" color="text.secondary">
					Loading Board Information
				</Typography>
			</Box>
		);
	}

	if (!boardInformation) {
		return null;
	}

	const renderList = (items: string[], emptyMessage = "None Configured") => {
		if (items.length === 0) {
			return (
				<Typography
					variant="body2"
					color="text.secondary"
					sx={{ fontStyle: "italic" }}
				>
					{emptyMessage}
				</Typography>
			);
		}

		return (
			<Box sx={{ display: "flex", flexWrap: "wrap", gap: 0.5 }}>
				{items.map((item) => (
					<Chip
						key={item}
						label={item}
						size="small"
						color="primary"
						variant="outlined"
					/>
				))}
			</Box>
		);
	};

	return (
		<InputGroup title="Board Information" initiallyExpanded={true}>
			<Grid container spacing={2}>
				<Grid size={{ xs: 12 }}>
					<Typography variant="subtitle2" fontWeight="bold" gutterBottom>
						Query
					</Typography>
					{boardInformation.dataRetrievalValue ? (
						<Box
							sx={{
								backgroundColor:
									theme.palette.mode === "dark"
										? "rgba(255, 255, 255, 0.05)"
										: "rgba(0, 0, 0, 0.04)",
								padding: 1.5,
								borderRadius: 1,
								overflowX: "auto",
								border: `1px solid ${theme.palette.divider}`,
							}}
						>
							<Typography
								component="code"
								sx={{
									fontFamily: "monospace",
									fontSize: "0.875rem",
									whiteSpace: "pre-wrap",
									wordBreak: "break-word",
									color: theme.palette.text.primary,
								}}
							>
								{boardInformation.dataRetrievalValue}
							</Typography>
						</Box>
					) : (
						<Typography
							variant="body2"
							color="text.secondary"
							sx={{ fontStyle: "italic" }}
						>
							None Configured
						</Typography>
					)}
				</Grid>

				<Grid size={{ xs: 12 }}>
					<Typography variant="subtitle2" fontWeight="bold" gutterBottom>
						Work Item Types
					</Typography>
					{renderList(boardInformation.workItemTypes)}
				</Grid>

				<Grid size={{ xs: 12 }}>
					<Typography variant="subtitle2" fontWeight="bold" gutterBottom>
						To Do States
					</Typography>
					{renderList(boardInformation.toDoStates)}
				</Grid>

				<Grid size={{ xs: 12 }}>
					<Typography variant="subtitle2" fontWeight="bold" gutterBottom>
						Doing States
					</Typography>
					{renderList(boardInformation.doingStates)}
				</Grid>

				<Grid size={{ xs: 12 }}>
					<Typography variant="subtitle2" fontWeight="bold" gutterBottom>
						Done States
					</Typography>
					{renderList(boardInformation.doneStates)}
				</Grid>

				<Grid size={{ xs: 12 }}>
					<Box
						sx={{
							mt: 1,
							padding: 1.5,
							backgroundColor:
								theme.palette.mode === "dark"
									? "rgba(41, 182, 246, 0.15)"
									: "rgba(41, 182, 246, 0.1)",
							borderRadius: 1,
							borderLeft: "4px solid",
							borderLeftColor: theme.palette.info.main,
						}}
					>
						<Typography
							variant="body2"
							sx={{ color: theme.palette.text.primary }}
						>
							ℹ️ You can adjust this configuration after closing the wizard in
							the data retrieval settings.
						</Typography>
					</Box>
				</Grid>
			</Grid>
		</InputGroup>
	);
};

export default BoardInformationDisplay;
