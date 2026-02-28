import {
	Add as AddIcon,
	Delete as DeleteIcon,
	Edit as EditIcon,
} from "@mui/icons-material";
import {
	Alert,
	Autocomplete,
	Box,
	Button,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
	FormControl,
	IconButton,
	InputLabel,
	List,
	ListItem,
	ListItemText,
	MenuItem,
	Select,
	type SelectChangeEvent,
	TextField,
	Typography,
} from "@mui/material";
import type React from "react";
import { useEffect, useMemo, useState } from "react";
import { LicenseTooltip } from "../../../components/App/License/LicenseToolTip";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import type { WorkTrackingSystemType } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import {
	APPLIES_TO_DISPLAY_NAMES,
	DATE_FORMAT_PRESETS,
	FORECAST_SOURCES,
	type IWriteBackMappingDefinition,
	PORTFOLIO_ONLY_SOURCES,
	VALUE_SOURCE_DISPLAY_NAMES,
	WriteBackAppliesTo,
	WriteBackTargetValueType,
	WriteBackValueSource,
} from "../../../models/WorkTracking/WriteBackMappingDefinition";

interface WriteBackMappingsEditorProps {
	additionalFields: IAdditionalFieldDefinition[];
	mappings: IWriteBackMappingDefinition[];
	onChange: (mappings: IWriteBackMappingDefinition[]) => void;
	workTrackingSystemType: WorkTrackingSystemType | null;
}

interface MappingEditDialogProps {
	open: boolean;
	mapping: IWriteBackMappingDefinition | null;
	additionalFields: IAdditionalFieldDefinition[];
	onSave: (mapping: IWriteBackMappingDefinition) => void;
	onCancel: () => void;
}

const MappingEditDialog: React.FC<MappingEditDialogProps> = ({
	open,
	mapping,
	additionalFields,
	onSave,
	onCancel,
}) => {
	const [targetFieldReference, setTargetFieldReference] = useState("");
	const [valueSource, setValueSource] = useState<WriteBackValueSource>(
		WriteBackValueSource.WorkItemAgeCycleTime,
	);
	const [appliesTo, setAppliesTo] = useState<WriteBackAppliesTo>(
		WriteBackAppliesTo.Team,
	);
	const [targetValueType, setTargetValueType] =
		useState<WriteBackTargetValueType>(WriteBackTargetValueType.Date);
	const [dateFormat, setDateFormat] = useState<string>("");

	useEffect(() => {
		if (mapping) {
			setTargetFieldReference(mapping.targetFieldReference);
			setValueSource(mapping.valueSource);
			setAppliesTo(mapping.appliesTo);
			setTargetValueType(mapping.targetValueType);
			setDateFormat(mapping.dateFormat ?? "");
		} else {
			setTargetFieldReference("");
			setValueSource(WriteBackValueSource.WorkItemAgeCycleTime);
			setAppliesTo(WriteBackAppliesTo.Team);
			setTargetValueType(WriteBackTargetValueType.Date);
			setDateFormat("");
		}
	}, [mapping]);

	const isForecastSource = FORECAST_SOURCES.has(valueSource);
	const showValueType = isForecastSource;
	const showDateFormat =
		isForecastSource &&
		targetValueType === WriteBackTargetValueType.FormattedText;

	const availableValueSources = useMemo(() => {
		return Object.values(WriteBackValueSource)
			.filter((v): v is WriteBackValueSource => typeof v === "number")
			.filter(
				(source) =>
					appliesTo === WriteBackAppliesTo.Portfolio ||
					!PORTFOLIO_ONLY_SOURCES.has(source),
			);
	}, [appliesTo]);

	const handleAppliesToChange = (event: SelectChangeEvent<number>) => {
		const newAppliesTo = event.target.value as WriteBackAppliesTo;
		setAppliesTo(newAppliesTo);

		// Reset value source if it's portfolio-only and we switched to Team
		if (
			newAppliesTo === WriteBackAppliesTo.Team &&
			PORTFOLIO_ONLY_SOURCES.has(valueSource)
		) {
			setValueSource(WriteBackValueSource.WorkItemAgeCycleTime);
			setTargetValueType(WriteBackTargetValueType.Date);
			setDateFormat("");
		}
	};

	const handleValueSourceChange = (event: SelectChangeEvent<number>) => {
		const newSource = event.target.value as WriteBackValueSource;
		setValueSource(newSource);

		if (!FORECAST_SOURCES.has(newSource)) {
			setTargetValueType(WriteBackTargetValueType.Date);
			setDateFormat("");
		}
	};

	const handleValueTypeChange = (event: SelectChangeEvent<number>) => {
		const newType = event.target.value as WriteBackTargetValueType;
		setTargetValueType(newType);

		if (newType === WriteBackTargetValueType.Date) {
			setDateFormat("");
		}
	};

	const isValid =
		targetFieldReference !== "" &&
		(!showDateFormat || dateFormat.trim() !== "");

	const handleSave = () => {
		if (mapping) {
			onSave({
				...mapping,
				targetFieldReference,
				valueSource,
				appliesTo,
				targetValueType,
				dateFormat: showDateFormat ? dateFormat.trim() : null,
			});
		}
	};

	return (
		<Dialog open={open} onClose={onCancel} maxWidth="sm" fullWidth>
			<DialogTitle>
				{mapping && mapping.id > 0 ? "Edit Sync Mapping" : "Add Sync Mapping"}
			</DialogTitle>
			<DialogContent>
				<FormControl fullWidth margin="normal">
					<InputLabel id="target-field-label">Target Field</InputLabel>
					<Select
						labelId="target-field-label"
						label="Target Field"
						value={targetFieldReference}
						onChange={(e) => setTargetFieldReference(e.target.value)}
					>
						{additionalFields.map((field) => (
							<MenuItem key={field.id} value={field.reference}>
								{field.displayName}
							</MenuItem>
						))}
					</Select>
				</FormControl>

				<FormControl fullWidth margin="normal">
					<InputLabel id="applies-to-label">Applies To</InputLabel>
					<Select
						labelId="applies-to-label"
						label="Applies To"
						value={appliesTo}
						onChange={handleAppliesToChange}
					>
						{Object.values(WriteBackAppliesTo)
							.filter((v): v is WriteBackAppliesTo => typeof v === "number")
							.map((value) => (
								<MenuItem key={value} value={value}>
									{APPLIES_TO_DISPLAY_NAMES[value]}
								</MenuItem>
							))}
					</Select>
				</FormControl>

				<FormControl fullWidth margin="normal">
					<InputLabel id="value-source-label">Sync Value</InputLabel>
					<Select
						labelId="value-source-label"
						label="Sync Value"
						value={valueSource}
						onChange={handleValueSourceChange}
					>
						{availableValueSources.map((source) => (
							<MenuItem key={source} value={source}>
								{VALUE_SOURCE_DISPLAY_NAMES[source]}
							</MenuItem>
						))}
					</Select>
				</FormControl>

				{showValueType && (
					<FormControl fullWidth margin="normal">
						<InputLabel id="value-type-label">Value Type</InputLabel>
						<Select
							labelId="value-type-label"
							label="Value Type"
							value={targetValueType}
							onChange={handleValueTypeChange}
						>
							<MenuItem value={WriteBackTargetValueType.Date}>Date</MenuItem>
							<MenuItem value={WriteBackTargetValueType.FormattedText}>
								Text
							</MenuItem>
						</Select>
					</FormControl>
				)}

				{showDateFormat && (
					<Autocomplete
						freeSolo
						options={[...DATE_FORMAT_PRESETS]}
						value={dateFormat}
						onInputChange={(_event, newValue) => setDateFormat(newValue)}
						renderInput={(params) => (
							<TextField
								{...params}
								label="Date Format"
								margin="normal"
								fullWidth
								helperText="Select a preset or enter a custom date format"
							/>
						)}
					/>
				)}
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

const WriteBackMappingsEditor: React.FC<WriteBackMappingsEditorProps> = ({
	additionalFields,
	mappings,
	onChange,
	workTrackingSystemType,
}) => {
	const [editDialogOpen, setEditDialogOpen] = useState(false);
	const [editingMapping, setEditingMapping] =
		useState<IWriteBackMappingDefinition | null>(null);
	const [tempIdCounter, setTempIdCounter] = useState(-1);
	const { licenseStatus } = useLicenseRestrictions();

	const isSupportedSystem =
		workTrackingSystemType !== null &&
		workTrackingSystemType !== "Linear" &&
		workTrackingSystemType !== "Csv";

	const canAddMapping =
		licenseStatus?.canUsePremiumFeatures === true && isSupportedSystem;

	const hasAdditionalFields = additionalFields.length > 0;

	const getFieldDisplayName = (reference: string): string => {
		const field = additionalFields.find((f) => f.reference === reference);
		return field?.displayName ?? reference;
	};

	const handleAddMapping = () => {
		const newMapping: IWriteBackMappingDefinition = {
			id: tempIdCounter,
			valueSource: WriteBackValueSource.WorkItemAgeCycleTime,
			appliesTo: WriteBackAppliesTo.Team,
			targetFieldReference: "",
			targetValueType: WriteBackTargetValueType.Date,
			dateFormat: null,
		};
		setTempIdCounter(tempIdCounter - 1);
		setEditingMapping(newMapping);
		setEditDialogOpen(true);
	};

	const handleEditMapping = (mapping: IWriteBackMappingDefinition) => {
		setEditingMapping(mapping);
		setEditDialogOpen(true);
	};

	const handleDeleteMapping = (
		mappingToDelete: IWriteBackMappingDefinition,
	) => {
		onChange(mappings.filter((m) => m.id !== mappingToDelete.id));
	};

	const handleSaveMapping = (savedMapping: IWriteBackMappingDefinition) => {
		const existingIndex = mappings.findIndex((m) => m.id === savedMapping.id);
		if (existingIndex >= 0) {
			const updatedMappings = [...mappings];
			updatedMappings[existingIndex] = savedMapping;
			onChange(updatedMappings);
		} else {
			onChange([...mappings, savedMapping]);
		}
		setEditDialogOpen(false);
		setEditingMapping(null);
	};

	const handleCancelEdit = () => {
		setEditDialogOpen(false);
		setEditingMapping(null);
	};

	const getMappingSecondary = (
		mapping: IWriteBackMappingDefinition,
	): React.ReactNode => {
		const showFormat =
			FORECAST_SOURCES.has(mapping.valueSource) &&
			mapping.targetValueType === WriteBackTargetValueType.FormattedText &&
			mapping.dateFormat;

		return (
			<>
				<span>{VALUE_SOURCE_DISPLAY_NAMES[mapping.valueSource]}</span>
				{" · "}
				<span>{APPLIES_TO_DISPLAY_NAMES[mapping.appliesTo]}</span>
				{showFormat && (
					<>
						{" · "}
						<span>Format: {mapping.dateFormat}</span>
					</>
				)}
			</>
		);
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
					Sync with Source
				</Typography>
				{hasAdditionalFields && (
					<LicenseTooltip
						canUseFeature={canAddMapping}
						defaultTooltip="Add a sync mapping"
						premiumExtraInfo="Sync with Source requires a premium license."
					>
						<span>
							<Button
								startIcon={<AddIcon />}
								size="small"
								onClick={handleAddMapping}
								variant="outlined"
								disabled={!canAddMapping}
							>
								Add Sync Mapping
							</Button>
						</span>
					</LicenseTooltip>
				)}
			</Box>

			{!hasAdditionalFields && (
				<Alert severity="info" sx={{ mt: 1 }}>
					Define additional fields first to configure sync mappings.
				</Alert>
			)}

			{hasAdditionalFields && mappings.length === 0 && (
				<Typography variant="body2" color="text.secondary" sx={{ py: 1 }}>
					No sync mappings configured.
				</Typography>
			)}

			{hasAdditionalFields && mappings.length > 0 && (
				<List dense>
					{mappings.map((mapping) => (
						<ListItem
							key={mapping.id}
							divider
							secondaryAction={
								<Box>
									<IconButton
										edge="end"
										aria-label="edit"
										onClick={() => handleEditMapping(mapping)}
										size="small"
										disabled={!isSupportedSystem}
									>
										<EditIcon fontSize="small" />
									</IconButton>
									<IconButton
										edge="end"
										aria-label="delete"
										onClick={() => handleDeleteMapping(mapping)}
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
								primary={getFieldDisplayName(mapping.targetFieldReference)}
								secondary={getMappingSecondary(mapping)}
							/>
						</ListItem>
					))}
				</List>
			)}

			<MappingEditDialog
				open={editDialogOpen}
				mapping={editingMapping}
				additionalFields={additionalFields}
				onSave={handleSaveMapping}
				onCancel={handleCancelEdit}
			/>
		</Box>
	);
};

export default WriteBackMappingsEditor;
