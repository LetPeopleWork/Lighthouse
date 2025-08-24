import CloudUpload from "@mui/icons-material/CloudUpload";
import { Box, Button, styled, Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import { useCallback } from "react";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { useTerminology } from "../../../services/TerminologyContext";
import { appColors, hexToRgba } from "../../../utils/theme/colors";
import InputGroup from "../InputGroup/InputGroup";

const VisuallyHiddenInput = styled("input")({
	clip: "rect(0 0 0 0)",
	clipPath: "inset(50%)",
	height: 1,
	overflow: "hidden",
	position: "absolute",
	bottom: 0,
	left: 0,
	whiteSpace: "nowrap",
	width: 1,
});

const DropZone = styled(Box, {
	shouldForwardProp: (prop) => prop !== "isDragActive",
})<{ isDragActive: boolean }>(({ theme, isDragActive }) => {
	const isDark = theme.palette.mode === "dark";

	const getBackgroundColor = () => {
		if (isDragActive) {
			return isDark
				? hexToRgba(appColors.primary.light, 0.15) // primary.light with opacity for dark mode
				: theme.palette.primary.light;
		}
		return isDark
			? hexToRgba("#ffffff", 0.03) // Very subtle background for dark mode
			: theme.palette.grey[50];
	};

	const getHoverBackground = () => {
		return isDark
			? hexToRgba("#ffffff", 0.08) // Subtle hover for dark mode
			: theme.palette.grey[100];
	};

	return {
		border: `2px dashed ${isDragActive ? theme.palette.primary.main : theme.palette.divider}`,
		borderRadius: theme.spacing(1),
		padding: theme.spacing(3),
		textAlign: "center",
		backgroundColor: getBackgroundColor(),
		cursor: "pointer",
		transition: "all 0.3s ease",
		"&:hover": {
			backgroundColor: getHoverBackground(),
			borderColor: theme.palette.primary.main,
		},
	};
});

interface FileUploadComponentProps {
	workTrackingSystemConnection: IWorkTrackingSystemConnection | null;
	selectedFile: File | null;
	onFileSelect: (file: File | null) => void;
	uploadProgress?: number;
	validationErrors?: string[];
}

const FileUploadComponent: React.FC<FileUploadComponentProps> = ({
	workTrackingSystemConnection,
	selectedFile,
	onFileSelect,
	uploadProgress,
	validationErrors = [],
}) => {
	const { getTerm } = useTerminology();
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);

	const validateAndSetFile = useCallback(
		(file: File | null) => {
			if (!file) {
				onFileSelect(null);
				return;
			}

			// Validate file type
			const validTypes = [".csv", "text/csv", "application/csv"];
			const fileExtension = file.name.toLowerCase().split(".").pop();
			const isValidType =
				fileExtension === "csv" || validTypes.includes(file.type);

			if (!isValidType) {
				alert("Please select a valid CSV file.");
				return;
			}

			// Validate file size (10MB max)
			const maxSize = 10 * 1024 * 1024; // 10MB in bytes
			if (file.size > maxSize) {
				alert("File size must be less than 10MB.");
				return;
			}

			onFileSelect(file);
		},
		[onFileSelect],
	);

	const handleFileChange = useCallback(
		(event: React.ChangeEvent<HTMLInputElement>) => {
			const file = event.target.files?.[0] || null;
			validateAndSetFile(file);
		},
		[validateAndSetFile],
	);

	const handleDrop = useCallback(
		(event: React.DragEvent<HTMLDivElement>) => {
			event.preventDefault();
			const file = event.dataTransfer.files[0];
			validateAndSetFile(file);
		},
		[validateAndSetFile],
	);

	const handleDragOver = useCallback(
		(event: React.DragEvent<HTMLDivElement>) => {
			event.preventDefault();
		},
		[],
	);

	if (workTrackingSystemConnection?.dataSourceType !== "File") {
		return null;
	}

	return (
		<InputGroup title={`${workItemsTerm} Data File`}>
			<Grid size={{ xs: 12 }}>
				<DropZone
					onDrop={handleDrop}
					onDragOver={handleDragOver}
					isDragActive={false}
				>
					<CloudUpload sx={{ fontSize: 48, color: "primary.main", mb: 2 }} />
					<Typography variant="h6" gutterBottom>
						Upload CSV File
					</Typography>
					<Typography variant="body2" color="textSecondary" paragraph>
						Drag and drop a CSV file here, or click to select
					</Typography>
					<Button
						component="label"
						variant="contained"
						startIcon={<CloudUpload />}
					>
						Choose File
						<VisuallyHiddenInput
							type="file"
							accept=".csv,text/csv,application/csv"
							onChange={handleFileChange}
						/>
					</Button>
				</DropZone>
			</Grid>

			{selectedFile && (
				<Grid size={{ xs: 12 }}>
					<Box
						sx={{
							mt: 2,
							p: 2,
							backgroundColor: (theme) =>
								theme.palette.mode === "dark"
									? hexToRgba("#ffffff", 0.05)
									: theme.palette.grey[50],
							borderRadius: 1,
							border: (theme) =>
								theme.palette.mode === "dark"
									? `1px solid ${hexToRgba("#ffffff", 0.12)}`
									: "none",
						}}
					>
						<Typography variant="body2" color="textSecondary">
							Selected file: <strong>{selectedFile.name}</strong>
						</Typography>
						<Typography variant="caption" color="textSecondary">
							Size: {(selectedFile.size / 1024).toFixed(1)} KB
						</Typography>
					</Box>
				</Grid>
			)}

			{uploadProgress !== undefined && uploadProgress > 0 && (
				<Grid size={{ xs: 12 }}>
					<Box sx={{ mt: 2 }}>
						<Typography variant="body2">
							Processing: {uploadProgress}%
						</Typography>
						<Box
							sx={{
								width: "100%",
								height: 8,
								backgroundColor: (theme) =>
									theme.palette.mode === "dark"
										? hexToRgba("#ffffff", 0.12)
										: theme.palette.grey[300],
								borderRadius: 4,
								overflow: "hidden",
							}}
						>
							<Box
								sx={{
									width: `${uploadProgress}%`,
									height: "100%",
									backgroundColor: "primary.main",
									transition: "width 0.3s ease",
								}}
							/>
						</Box>
					</Box>
				</Grid>
			)}

			{validationErrors.length > 0 && (
				<Grid size={{ xs: 12 }}>
					<Box
						sx={{
							mt: 2,
							p: 2,
							backgroundColor: (theme) =>
								theme.palette.mode === "dark"
									? hexToRgba(appColors.status.error, 0.15) // error color with opacity for dark mode
									: theme.palette.error.light,
							borderRadius: 1,
							border: (theme) =>
								theme.palette.mode === "dark"
									? `1px solid ${theme.palette.error.main}`
									: "none",
						}}
					>
						<Typography variant="body2" color="error.main" gutterBottom>
							Validation Errors:
						</Typography>
						{validationErrors.slice(0, 10).map((error, index) => (
							<Typography
								key={`error-${index}-${error.substring(0, 10)}`}
								variant="caption"
								color="error.main"
								display="block"
							>
								• {error}
							</Typography>
						))}
						{validationErrors.length > 10 && (
							<Typography variant="caption" color="error.main" display="block">
								... and {validationErrors.length - 10} more errors
							</Typography>
						)}
					</Box>
				</Grid>
			)}
		</InputGroup>
	);
};

export default FileUploadComponent;
