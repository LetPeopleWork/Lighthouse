import {
	Alert,
	Box,
	Button,
	ButtonGroup,
	CircularProgress,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
	TextField,
	Typography,
} from "@mui/material";
import type React from "react";
import { useCallback, useContext, useEffect, useState } from "react";
import { LicenseTooltip } from "../../../../../components/App/License/LicenseToolTip";
import { DeliveryRuleBuilder } from "../../../../../components/Common/DeliveryRuleBuilder";
import { FeatureGrid } from "../../../../../components/Common/FeatureGrid";
import { FeatureSelector } from "../../../../../components/Common/FeatureSelector";
import { useLicenseRestrictions } from "../../../../../hooks/useLicenseRestrictions";
import type { IDelivery } from "../../../../../models/Delivery";
import {
	DeliverySelectionMode,
	type IDeliveryRuleCondition,
	type IDeliveryRuleSchema,
} from "../../../../../models/DeliveryRules";
import type { IFeature } from "../../../../../models/Feature";
import type { Portfolio } from "../../../../../models/Portfolio/Portfolio";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../../../services/TerminologyContext";

interface DeliveryCreateModalProps {
	open: boolean;
	portfolio: Portfolio;
	editingDelivery?: IDelivery | null;
	onClose: () => void;
	onSave: (deliveryData: {
		name: string;
		date: string;
		featureIds: number[];
		selectionMode?: DeliverySelectionMode;
		rules?: IDeliveryRuleCondition[];
	}) => void;
	onUpdate?: (deliveryData: {
		id: number;
		name: string;
		date: string;
		featureIds: number[];
		selectionMode?: DeliverySelectionMode;
		rules?: IDeliveryRuleCondition[];
	}) => void;
}

// Extracted component for loading state
const LoadingSpinner: React.FC = () => (
	<Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
		<CircularProgress />
	</Box>
);

// Extracted component for schema load failure
const SchemaLoadError: React.FC = () => (
	<Alert severity="warning">
		Failed to load rule schema. Please try again.
	</Alert>
);

// Extracted component for premium feature notice
const PremiumFeatureNotice: React.FC = () => (
	<Alert severity="info">
		Rule-based delivery selection is a premium feature. Please upgrade your
		license to use this functionality.
	</Alert>
);

// Extracted component for validation button with loading state
const ValidationButton: React.FC<{
	validatingRules: boolean;
	rulesLength: number;
	onClick: () => void;
}> = ({ validatingRules, rulesLength, onClick }) => (
	<Button
		variant="outlined"
		onClick={onClick}
		disabled={validatingRules || rulesLength === 0}
	>
		{validatingRules && <CircularProgress size={20} sx={{ mr: 1 }} />}
		Validate Rules
	</Button>
);

// Extracted component for matched features alert
const MatchedFeaturesAlert: React.FC<{ count: number }> = ({ count }) => (
	<Alert severity="success" sx={{ flex: 1 }}>
		<span data-testid="matched-count">{count} feature(s) matched</span>
	</Alert>
);

// Extracted component for matched features grid
const MatchedFeaturesGrid: React.FC<{
	features: IFeature[];
	featuresTerm: string;
	portfolioId: number;
}> = ({ features, featuresTerm, portfolioId }) => (
	<Box sx={{ mt: 2 }}>
		<Typography variant="subtitle2" sx={{ mb: 1 }}>
			Matched {featuresTerm}:
		</Typography>
		<Box sx={{ height: 200 }}>
			<FeatureGrid
				features={features}
				selectedFeatureIds={features.map((f) => f.id)}
				storageKey={`delivery-matched-features-${portfolioId}`}
				mode="readonly"
			/>
		</Box>
	</Box>
);

// Extracted component for rule-based content
const RuleBasedContent: React.FC<{
	loadingSchema: boolean;
	ruleSchema: IDeliveryRuleSchema | null;
	errors: { rules?: string };
	rules: IDeliveryRuleCondition[];
	validatingRules: boolean;
	rulesValidated: boolean;
	matchedFeatures: IFeature[];
	featuresTerm: string;
	portfolioId: number;
	onRulesChange: (rules: IDeliveryRuleCondition[]) => void;
	onValidateRules: () => void;
}> = ({
	loadingSchema,
	ruleSchema,
	errors,
	rules,
	validatingRules,
	rulesValidated,
	matchedFeatures,
	featuresTerm,
	portfolioId,
	onRulesChange,
	onValidateRules,
}) => {
	if (loadingSchema) {
		return <LoadingSpinner />;
	}

	if (!ruleSchema) {
		return <SchemaLoadError />;
	}

	const hasMatchedFeatures = rulesValidated && matchedFeatures.length > 0;

	return (
		<>
			{errors.rules && (
				<Alert severity="error" sx={{ mb: 2 }}>
					{errors.rules}
				</Alert>
			)}
			<DeliveryRuleBuilder
				rules={rules}
				onChange={onRulesChange}
				fields={ruleSchema.fields}
				operators={ruleSchema.operators}
				maxRules={ruleSchema.maxRules}
				maxValueLength={ruleSchema.maxValueLength}
			/>
			<Box sx={{ mt: 2, display: "flex", alignItems: "center", gap: 2 }}>
				<ValidationButton
					validatingRules={validatingRules}
					rulesLength={rules.length}
					onClick={onValidateRules}
				/>
				{hasMatchedFeatures && (
					<MatchedFeaturesAlert count={matchedFeatures.length} />
				)}
			</Box>
			{hasMatchedFeatures && (
				<MatchedFeaturesGrid
					features={matchedFeatures}
					featuresTerm={featuresTerm}
					portfolioId={portfolioId}
				/>
			)}
		</>
	);
};

// Extracted component for selection mode content
const SelectionModeContent: React.FC<{
	selectionMode: DeliverySelectionMode;
	isPremium: boolean;
	loadingSchema: boolean;
	ruleSchema: IDeliveryRuleSchema | null;
	errors: { features?: string; rules?: string };
	allFeatures: IFeature[];
	selectedFeatureIds: number[];
	rules: IDeliveryRuleCondition[];
	validatingRules: boolean;
	rulesValidated: boolean;
	matchedFeatures: IFeature[];
	featuresTerm: string;
	portfolioId: number;
	onSelectedFeaturesChange: (ids: number[]) => void;
	onRulesChange: (rules: IDeliveryRuleCondition[]) => void;
	onValidateRules: () => void;
}> = ({
	selectionMode,
	isPremium,
	loadingSchema,
	ruleSchema,
	errors,
	allFeatures,
	selectedFeatureIds,
	rules,
	validatingRules,
	rulesValidated,
	matchedFeatures,
	featuresTerm,
	portfolioId,
	onSelectedFeaturesChange,
	onRulesChange,
	onValidateRules,
}) => {
	if (selectionMode === DeliverySelectionMode.Manual) {
		return (
			<>
				<Typography variant="h6" sx={{ mb: 2 }}>
					Select {featuresTerm}
				</Typography>
				{errors.features && (
					<Typography color="error" sx={{ mb: 1 }}>
						{errors.features}
					</Typography>
				)}
				<Box sx={{ height: 300 }}>
					<FeatureSelector
						features={allFeatures}
						selectedFeatureIds={selectedFeatureIds}
						onChange={onSelectedFeaturesChange}
						storageKey={`delivery-create-features-${portfolioId}`}
					/>
				</Box>
			</>
		);
	}

	// Rule-based mode
	if (!isPremium) {
		return <PremiumFeatureNotice />;
	}

	return (
		<RuleBasedContent
			loadingSchema={loadingSchema}
			ruleSchema={ruleSchema}
			errors={errors}
			rules={rules}
			validatingRules={validatingRules}
			rulesValidated={rulesValidated}
			matchedFeatures={matchedFeatures}
			featuresTerm={featuresTerm}
			portfolioId={portfolioId}
			onRulesChange={onRulesChange}
			onValidateRules={onValidateRules}
		/>
	);
};

// Helper function to check if save button should be disabled
const isSaveDisabled = (
	name: string,
	date: string,
	selectionMode: DeliverySelectionMode,
	selectedFeatureIds: number[],
	rulesValidated: boolean,
	matchedFeaturesLength: number,
): boolean => {
	const hasName = name.trim().length > 0;
	const hasValidDate = isValidFutureDate(date);

	if (!hasName || !hasValidDate) {
		return true;
	}

	if (selectionMode === DeliverySelectionMode.Manual) {
		return selectedFeatureIds.length === 0;
	}

	// Rule-based mode: must be validated with matches
	return !rulesValidated || matchedFeaturesLength === 0;
};

// Helper function to validate future date
const isValidFutureDate = (date: string): boolean => {
	if (!date) {
		return false;
	}

	const selectedDate = new Date(date);
	const today = new Date();
	today.setHours(0, 0, 0, 0);
	selectedDate.setHours(0, 0, 0, 0);

	return selectedDate > today;
};

interface ValidationOptions {
	name: string;
	date: string;
	selectionMode: DeliverySelectionMode;
	selectedFeatureIds: number[];
	rulesValidated: boolean;
	matchedFeaturesLength: number;
	deliveryTerm: string;
	featureTerm: string;
}

const getFirstBlockingError = ({
	name,
	date,
	selectionMode,
	selectedFeatureIds,
	rulesValidated,
	matchedFeaturesLength,
	deliveryTerm,
	featureTerm,
}: ValidationOptions): string | null => {
	if (!name.trim()) {
		return `${deliveryTerm} name is required`;
	}
	if (!date) {
		return `${deliveryTerm} date is required`;
	}
	if (!isValidFutureDate(date)) {
		return `${deliveryTerm} date must be in the future`;
	}
	if (selectionMode === DeliverySelectionMode.Manual) {
		if (selectedFeatureIds.length === 0) {
			return `At least one ${featureTerm.toLowerCase()} must be selected`;
		}
	} else {
		// Rule-based mode
		if (!rulesValidated) {
			return "Rules must be validated before saving";
		}
		if (matchedFeaturesLength === 0) {
			return "No features match the rules";
		}
	}
	return null;
};

export const DeliveryCreateModal: React.FC<DeliveryCreateModalProps> = ({
	open,
	portfolio,
	editingDelivery,
	onClose,
	onSave,
	onUpdate,
}) => {
	const { featureService, deliveryService } = useContext(ApiServiceContext);
	const { getTerm } = useTerminology();
	const { licenseStatus } = useLicenseRestrictions();
	const isPremium = licenseStatus?.canUsePremiumFeatures ?? false;
	const deliveryTerm = getTerm(TERMINOLOGY_KEYS.DELIVERY);
	const isEditMode = !!editingDelivery;
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const [name, setName] = useState("");
	const [date, setDate] = useState("");
	const [selectedFeatureIds, setSelectedFeatureIds] = useState<number[]>([]);
	const [allFeatures, setAllFeatures] = useState<IFeature[]>([]);
	const [selectionMode, setSelectionMode] = useState<DeliverySelectionMode>(
		DeliverySelectionMode.Manual,
	);
	const [rules, setRules] = useState<IDeliveryRuleCondition[]>([]);
	const [ruleSchema, setRuleSchema] = useState<IDeliveryRuleSchema | null>(
		null,
	);
	const [loadingSchema, setLoadingSchema] = useState(false);
	const [validatingRules, setValidatingRules] = useState(false);
	const [rulesValidated, setRulesValidated] = useState(false);
	const [matchedFeatures, setMatchedFeatures] = useState<IFeature[]>([]);
	const [errors, setErrors] = useState<{
		name?: string;
		date?: string;
		features?: string;
		rules?: string;
	}>({});

	useEffect(() => {
		if (open && portfolio.features.length > 0) {
			const featureIds = portfolio.features.map((f) => f.id);
			featureService
				.getFeaturesByIds(featureIds)
				.then((features) => setAllFeatures(features))
				.catch((err) => console.error("Failed to load features:", err));
		}
	}, [open, portfolio.features, featureService]);

	// Load rule schema when switching to rule-based mode (only for premium users)
	useEffect(() => {
		if (
			open &&
			isPremium &&
			selectionMode === DeliverySelectionMode.RuleBased &&
			!ruleSchema &&
			!loadingSchema
		) {
			setLoadingSchema(true);
			deliveryService
				.getRuleSchema(portfolio.id)
				.then((schema) => setRuleSchema(schema))
				.catch((err) => console.error("Failed to load rule schema:", err))
				.finally(() => setLoadingSchema(false));
		}
	}, [
		open,
		isPremium,
		selectionMode,
		ruleSchema,
		loadingSchema,
		deliveryService,
		portfolio.id,
	]);

	const validateForm = () => {
		const newErrors: typeof errors = {};

		if (!name.trim()) {
			newErrors.name = `${deliveryTerm} name is required`;
		}

		if (!isValidFutureDate(date)) {
			if (date) {
				newErrors.date = `${deliveryTerm} date must be in the future`;
			} else {
				newErrors.date = `${deliveryTerm} date is required`;
			}
		}

		if (selectionMode === DeliverySelectionMode.Manual) {
			if (selectedFeatureIds.length === 0) {
				newErrors.features = `At least one ${featureTerm.toLowerCase()} must be selected`;
			}
		} else if (rules.length === 0) {
			newErrors.rules = "At least one rule must be defined";
		} else if (
			rules.some(
				(r) => !r.fieldKey.trim() || !r.operator.trim() || !r.value.trim(),
			)
		) {
			newErrors.rules = "All rule fields must be completed";
		} else if (!rulesValidated) {
			newErrors.rules = "Rules must be validated before saving";
		} else if (matchedFeatures.length === 0) {
			newErrors.rules = "No features match the rules";
		}

		setErrors(newErrors);
		return Object.keys(newErrors).length === 0;
	};

	const handleValidateRules = async () => {
		if (rules.length === 0) {
			setErrors((prev) => ({
				...prev,
				rules: "At least one rule must be defined",
			}));
			return;
		}

		if (
			rules.some(
				(r) => !r.fieldKey.trim() || !r.operator.trim() || !r.value.trim(),
			)
		) {
			setErrors((prev) => ({
				...prev,
				rules: "All rule fields must be completed",
			}));
			return;
		}

		setValidatingRules(true);
		setErrors((prev) => ({ ...prev, rules: undefined }));

		try {
			const matchedFeatures = await deliveryService.validateRules(
				portfolio.id,
				rules,
			);
			setMatchedFeatures(matchedFeatures);
			setRulesValidated(true);

			if (matchedFeatures.length === 0) {
				setErrors((prev) => ({
					...prev,
					rules: "No features match the rules",
				}));
			}
		} catch {
			setErrors((prev) => ({
				...prev,
				rules: "Failed to validate rules. Please try again.",
			}));
		} finally {
			setValidatingRules(false);
		}
	};

	const handleRulesChange = (newRules: IDeliveryRuleCondition[]) => {
		setRules(newRules);
		setRulesValidated(false);
		setMatchedFeatures([]);
	};

	const handleSave = () => {
		if (validateForm()) {
			if (isEditMode && editingDelivery && onUpdate) {
				onUpdate({
					id: editingDelivery.id,
					name: name.trim(),
					date,
					featureIds: selectedFeatureIds,
					selectionMode,
					rules:
						selectionMode === DeliverySelectionMode.RuleBased
							? rules
							: undefined,
				});
			} else {
				onSave({
					name: name.trim(),
					date,
					featureIds: selectedFeatureIds,
					selectionMode,
					rules:
						selectionMode === DeliverySelectionMode.RuleBased
							? rules
							: undefined,
				});
			}
		}
	};

	const resetForm = useCallback(() => {
		setName("");
		setDate("");
		setSelectedFeatureIds([]);
		setSelectionMode(DeliverySelectionMode.Manual);
		setRules([]);
		setRuleSchema(null);
		setRulesValidated(false);
		setMatchedFeatures([]);
		setErrors({});
	}, []);

	// Initialize form with editing delivery data
	useEffect(() => {
		if (open && editingDelivery) {
			setName(editingDelivery.name);
			setDate(editingDelivery.date.split("T")[0]); // Extract date part from ISO string
			setSelectedFeatureIds(editingDelivery.features || []);

			// Determine selection mode: if delivery has rules, it's rule-based
			const isRuleBased =
				editingDelivery.selectionMode === DeliverySelectionMode.RuleBased ||
				(editingDelivery.rules && editingDelivery.rules.length > 0);

			setSelectionMode(
				isRuleBased
					? DeliverySelectionMode.RuleBased
					: DeliverySelectionMode.Manual,
			);
			setRules(
				editingDelivery.rules?.map((r) => ({
					fieldKey: r.fieldKey,
					operator: r.operator,
					value: r.value,
				})) || [],
			);
			// Reset validation state for rule-based edits (user must re-validate)
			setRulesValidated(false);
			setMatchedFeatures([]);
		}
	}, [open, editingDelivery]);

	useEffect(() => {
		if (!open) {
			resetForm();
		}
	}, [open, resetForm]);

	return (
		<Dialog
			open={open}
			onClose={onClose}
			maxWidth="md"
			fullWidth
			slotProps={{
				paper: {
					sx: {
						resize: "both",
						overflow: "auto",
						minWidth: "400px",
						minHeight: "400px",
						maxWidth: "90vw",
						maxHeight: "90vh",
					},
				},
			}}
		>
			<DialogTitle>
				{isEditMode ? `Edit ${deliveryTerm}` : `Add ${deliveryTerm}`}
			</DialogTitle>
			<DialogContent>
				<Box sx={{ pt: 1 }}>
					<TextField
						autoFocus
						margin="dense"
						label={`${deliveryTerm} Name`}
						type="text"
						fullWidth
						variant="outlined"
						value={name}
						onChange={(e) => setName(e.target.value)}
						error={!!errors.name}
						helperText={errors.name}
						sx={{ mb: 2 }}
					/>

					<TextField
						margin="dense"
						label={`${deliveryTerm} Date`}
						type="date"
						fullWidth
						variant="outlined"
						value={date}
						onChange={(e) => setDate(e.target.value)}
						error={!!errors.date}
						helperText={errors.date}
						slotProps={{ inputLabel: { shrink: true } }}
						sx={{ mb: 2 }}
					/>

					<Box sx={{ mb: 2 }}>
						<Typography variant="subtitle2" sx={{ mb: 1 }}>
							Selection Mode
						</Typography>
						<ButtonGroup size="small">
							<Button
								variant={
									selectionMode === DeliverySelectionMode.Manual
										? "contained"
										: "outlined"
								}
								onClick={() => {
									setSelectionMode(DeliverySelectionMode.Manual);
									setRulesValidated(false);
									setMatchedFeatures([]);
								}}
								aria-pressed={selectionMode === DeliverySelectionMode.Manual}
							>
								Manual
							</Button>
							<LicenseTooltip
								canUseFeature={isPremium}
								defaultTooltip=""
								premiumExtraInfo="Please obtain a premium license to use rule-based deliveries."
							>
								<span>
									<Button
										variant={
											selectionMode === DeliverySelectionMode.RuleBased
												? "contained"
												: "outlined"
										}
										onClick={() =>
											setSelectionMode(DeliverySelectionMode.RuleBased)
										}
										disabled={!isPremium}
										aria-pressed={
											selectionMode === DeliverySelectionMode.RuleBased
										}
									>
										Rule-Based
									</Button>
								</span>
							</LicenseTooltip>
						</ButtonGroup>
					</Box>

					<SelectionModeContent
						selectionMode={selectionMode}
						isPremium={isPremium}
						loadingSchema={loadingSchema}
						ruleSchema={ruleSchema}
						errors={errors}
						allFeatures={allFeatures}
						selectedFeatureIds={selectedFeatureIds}
						rules={rules}
						validatingRules={validatingRules}
						rulesValidated={rulesValidated}
						matchedFeatures={matchedFeatures}
						featuresTerm={featuresTerm}
						portfolioId={portfolio.id}
						onSelectedFeaturesChange={setSelectedFeatureIds}
						onRulesChange={handleRulesChange}
						onValidateRules={handleValidateRules}
					/>
				</Box>
			</DialogContent>
			<DialogActions
				sx={{
					display: "flex",
					justifyContent: "space-between",
					alignItems: "center",
					gap: 2,
					px: 3,
					py: 2,
				}}
			>
				<Box sx={{ flex: 1, mr: 2 }}>
					{(() => {
						const error = getFirstBlockingError({
							name,
							date,
							selectionMode,
							selectedFeatureIds,
							rulesValidated,
							matchedFeaturesLength: matchedFeatures.length,
							deliveryTerm,
							featureTerm,
						});

						return (
							error && (
								<Alert severity="error" sx={{ py: 0 }}>
									{error}
								</Alert>
							)
						);
					})()}
				</Box>
				<Box sx={{ display: "flex", gap: 1 }}>
					<Button onClick={onClose}>Cancel</Button>
					<Button
						onClick={handleSave}
						variant="contained"
						disabled={isSaveDisabled(
							name,
							date,
							selectionMode,
							selectedFeatureIds,
							rulesValidated,
							matchedFeatures.length,
						)}
					>
						{isEditMode ? "Update" : "Save"}
					</Button>
				</Box>
			</DialogActions>
		</Dialog>
	);
};
