import { CloudUpload } from "@mui/icons-material";
import {
	Box,
	Button,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
	Typography,
} from "@mui/material";
import { styled } from "@mui/material/styles";
import { useCallback, useState } from "react";
import type { IBoardInformation } from "../../models/Boards/BoardInformation";
import type { DataRetrievalWizardProps } from "../../models/DataRetrievalWizard/DataRetrievalWizard";

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

const DropZone = styled(Box)(({ theme }) => ({
	border: `2px dashed ${theme.palette.divider}`,
	borderRadius: theme.shape.borderRadius,
	padding: theme.spacing(4),
	textAlign: "center",
	cursor: "pointer",
	transition:
		"border-color 0.2s ease-in-out, background-color 0.2s ease-in-out",
	"&:hover": {
		borderColor: theme.palette.primary.main,
		backgroundColor: theme.palette.action.hover,
	},
}));

const MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB

const CsvUploadWizard: React.FC<DataRetrievalWizardProps> = ({
	open,
	workTrackingSystemConnectionId: _workTrackingSystemConnectionId,
	onComplete,
	onCancel,
}) => {
	const [selectedFile, setSelectedFile] = useState<File | null>(null);
	const [fileContent, setFileContent] = useState<string>("");
	const [error, setError] = useState<string>("");

	const validateAndProcessFile = useCallback((file: File | null) => {
		if (!file) {
			setSelectedFile(null);
			setFileContent("");
			setError("");
			return;
		}

		// Validate file type
		const validTypes = [".csv", "text/csv", "application/csv"];
		const extension = file.name.toLowerCase().split(".").pop();
		if (!(extension === "csv" || validTypes.includes(file.type))) {
			setError("Please select a valid CSV file.");
			return;
		}

		// Validate file size
		if (file.size > MAX_FILE_SIZE) {
			setError("File size must be less than 10MB.");
			return;
		}

		setError("");
		setSelectedFile(file);

		// Read file content
		const reader = new FileReader();
		reader.onload = (e) => {
			const content = e.target?.result as string;
			if (content) {
				setFileContent(content);
			}
		};
		reader.onerror = () => {
			setError("Failed to read file.");
		};
		reader.readAsText(file);
	}, []);

	const handleFileChange = useCallback(
		(event: React.ChangeEvent<HTMLInputElement>) => {
			const file = event.target.files?.[0] || null;
			validateAndProcessFile(file);
		},
		[validateAndProcessFile],
	);

	const handleDrop = useCallback(
		(event: React.DragEvent<HTMLDivElement>) => {
			event.preventDefault();
			const file = event.dataTransfer.files[0];
			validateAndProcessFile(file);
		},
		[validateAndProcessFile],
	);

	const handleDragOver = useCallback(
		(event: React.DragEvent<HTMLDivElement>) => {
			event.preventDefault();
		},
		[],
	);

	const handleConfirm = () => {
		if (fileContent) {
			const boardInfo: IBoardInformation = {
				dataRetrievalValue: fileContent,
				workItemTypes: [],
				toDoStates: [],
				doingStates: [],
				doneStates: [],
			};
			onComplete(boardInfo);
			// Reset state
			setSelectedFile(null);
			setFileContent("");
			setError("");
		}
	};

	const handleCancel = () => {
		setSelectedFile(null);
		setFileContent("");
		setError("");
		onCancel();
	};

	return (
		<Dialog open={open} onClose={handleCancel} maxWidth="sm" fullWidth>
			<DialogTitle>Upload CSV File</DialogTitle>
			<DialogContent>
				<DropZone
					onDrop={handleDrop}
					onDragOver={handleDragOver}
					sx={{ mt: 2 }}
				>
					<CloudUpload sx={{ fontSize: 48, color: "primary.main", mb: 2 }} />
					<Typography variant="h6" gutterBottom>
						{selectedFile ? selectedFile.name : "Upload CSV File"}
					</Typography>
					<Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
						{selectedFile
							? `Size: ${(selectedFile.size / 1024).toFixed(1)} KB`
							: "Drag and drop a CSV file here, or click to select"}
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
				{error && (
					<Typography color="error" sx={{ mt: 2 }}>
						{error}
					</Typography>
				)}
			</DialogContent>
			<DialogActions>
				<Button onClick={handleCancel}>Cancel</Button>
				<Button
					onClick={handleConfirm}
					variant="contained"
					disabled={!fileContent}
				>
					Use File
				</Button>
			</DialogActions>
		</Dialog>
	);
};

export default CsvUploadWizard;
