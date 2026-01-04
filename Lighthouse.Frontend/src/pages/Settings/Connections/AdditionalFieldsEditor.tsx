import {
	Add as AddIcon,
	Delete as DeleteIcon,
	Edit as EditIcon,
	Info as InfoIcon,
} from "@mui/icons-material";
import {
	Box,
	Button,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
	IconButton,
	List,
	ListItem,
	ListItemText,
	TextField,
	Typography,
} from "@mui/material";
import type React from "react";
import { useEffect, useState } from "react";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import type { WorkTrackingSystemType } from "../../../models/WorkTracking/WorkTrackingSystemConnection";

interface AdditionalFieldsEditorProps {
	workTrackingSystemType: WorkTrackingSystemType | null;
	fields: IAdditionalFieldDefinition[];
	onChange: (fields: IAdditionalFieldDefinition[]) => void;
	onFieldsChanged: () => void;
}

interface FieldEditDialogProps {
	open: boolean;
	field: IAdditionalFieldDefinition | null;
	onSave: (field: IAdditionalFieldDefinition) => void;
	onCancel: () => void;
	workTrackingSystemType: WorkTrackingSystemType | null;
}

const FieldEditDialog: React.FC<FieldEditDialogProps> = ({
	open,
	field,
	onSave,
	onCancel,
	workTrackingSystemType,
}) => {
	const [displayName, setDisplayName] = useState("");
	const [reference, setReference] = useState("");

	const getFieldReferenceHelperText = (): string => {
		switch (workTrackingSystemType) {
			case "AzureDevOps":
				return "Azure DevOps field reference (e.g., 'System.IterationPath', 'Custom.MyField')";
			case "Jira":
				return "'Name' or 'Key' of Jira Field. Can be a custom field or a built-in field (e.g., 'Flagged', 'Our Custom Field', 'customfield_10011')";
			default:
				return "Provider-specific field reference";
		}
	};

	const getInfoLink = (): string => {
		switch (workTrackingSystemType) {
			case "AzureDevOps":
				return "https://learn.microsoft.com/en-us/azure/devops/boards/work-items/guidance/work-item-field?view=azure-devops";
			case "Jira":
				return "https://confluence.atlassian.com/jirakb/find-my-custom-field-id-number-in-jira-744522503.html";
			default:
				return "";
		}
	};

	useEffect(() => {
		if (field) {
			setDisplayName(field.displayName);
			setReference(field.reference);
		} else {
			setDisplayName("");
			setReference("");
		}
	}, [field]);

	const handleSave = () => {
		if (field) {
			onSave({
				...field,
				displayName: displayName.trim(),
				reference: reference.trim(),
			});
		}
	};

	const isValid = displayName.trim() !== "" && reference.trim() !== "";

	return (
		<Dialog open={open} onClose={onCancel} maxWidth="sm" fullWidth>
			<DialogTitle
				sx={{
					display: "flex",
					justifyContent: "space-between",
					alignItems: "center",
				}}
			>
				{field && field.id > 0 ? "Edit Field" : "Add Field"}
				{getInfoLink() !== "" && (
					<IconButton
						size="small"
						href={getInfoLink()}
						target="_blank"
						rel="noopener noreferrer"
						aria-label="View documentation"
					>
						<InfoIcon fontSize="small" />
					</IconButton>
				)}{" "}
			</DialogTitle>
			<DialogContent>
				<TextField
					label="Display Name"
					fullWidth
					margin="normal"
					value={displayName}
					onChange={(e) => setDisplayName(e.target.value)}
					autoFocus
					helperText="A user-friendly name for this field"
				/>
				<TextField
					label="Field Reference"
					fullWidth
					margin="normal"
					value={reference}
					onChange={(e) => setReference(e.target.value)}
					helperText={getFieldReferenceHelperText()}
				/>
			</DialogContent>
			<DialogActions>
				<Button onClick={onCancel}>Cancel</Button>
				<Button onClick={handleSave} variant="contained" disabled={!isValid}>
					Save
				</Button>
			</DialogActions>
		</Dialog>
	);
};

const AdditionalFieldsEditor: React.FC<AdditionalFieldsEditorProps> = ({
	workTrackingSystemType,
	fields,
	onChange,
	onFieldsChanged,
}) => {
	const [editDialogOpen, setEditDialogOpen] = useState(false);
	const [editingField, setEditingField] =
		useState<IAdditionalFieldDefinition | null>(null);
	const [tempIdCounter, setTempIdCounter] = useState(-1);

	const isSupportedSystem =
		workTrackingSystemType !== null &&
		workTrackingSystemType !== "Linear" &&
		workTrackingSystemType !== "Csv";

	const handleAddField = () => {
		// Use negative IDs for new fields (will be assigned server-side)
		const newField: IAdditionalFieldDefinition = {
			id: tempIdCounter,
			displayName: "",
			reference: "",
		};
		setTempIdCounter(tempIdCounter - 1);
		setEditingField(newField);
		setEditDialogOpen(true);
		onFieldsChanged();
	};

	const handleEditField = (field: IAdditionalFieldDefinition) => {
		setEditingField(field);
		setEditDialogOpen(true);
	};

	const handleDeleteField = (fieldToDelete: IAdditionalFieldDefinition) => {
		onChange(fields.filter((f) => f.id !== fieldToDelete.id));
		onFieldsChanged();
	};

	const handleSaveField = (savedField: IAdditionalFieldDefinition) => {
		const existingIndex = fields.findIndex((f) => f.id === savedField.id);
		if (existingIndex >= 0) {
			// Update existing field
			const updatedFields = [...fields];
			updatedFields[existingIndex] = savedField;
			onChange(updatedFields);
		} else {
			// Add new field
			onChange([...fields, savedField]);
		}
		setEditDialogOpen(false);
		setEditingField(null);
		onFieldsChanged();
	};

	const handleCancelEdit = () => {
		setEditDialogOpen(false);
		setEditingField(null);
	};

	return (
		<Box sx={{ mt: 2 }}>
			<Box
				sx={{
					display: "flex",
					justifyContent: "space-between",
					alignItems: "center",
					mb: 1,
				}}
			>
				<Typography variant="subtitle2" color="text.secondary">
					Additional Fields
				</Typography>
				<Button
					startIcon={<AddIcon />}
					size="small"
					onClick={handleAddField}
					variant="outlined"
					disabled={!isSupportedSystem}
				>
					Add Field
				</Button>
			</Box>

			{fields.length === 0 ? (
				<Typography variant="body2" color="text.secondary" sx={{ py: 1 }}>
					No additional fields configured.
				</Typography>
			) : (
				<List dense>
					{fields.map((field) => (
						<ListItem
							key={field.id}
							divider
							secondaryAction={
								<Box>
									<IconButton
										edge="end"
										aria-label="edit"
										onClick={() => handleEditField(field)}
										size="small"
										disabled={!isSupportedSystem}
									>
										<EditIcon fontSize="small" />
									</IconButton>
									<IconButton
										edge="end"
										aria-label="delete"
										onClick={() => handleDeleteField(field)}
										size="small"
										sx={{ ml: 1 }}
										disabled={!isSupportedSystem}
									>
										<DeleteIcon fontSize="small" />
									</IconButton>
								</Box>
							}
						>
							<ListItemText
								primary={field.displayName}
								secondary={field.reference}
							/>
						</ListItem>
					))}
				</List>
			)}

			<FieldEditDialog
				open={editDialogOpen}
				field={editingField}
				onSave={handleSaveField}
				onCancel={handleCancelEdit}
				workTrackingSystemType={workTrackingSystemType}
			/>
		</Box>
	);
};

export default AdditionalFieldsEditor;
