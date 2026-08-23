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
import { useCallback, useContext, useEffect, useMemo, useState } from "react";
import { LicenseTooltip } from "../../../../../components/App/License/LicenseToolTip";
import { DeliveryRuleBuilder } from "../../../../../components/Common/DeliveryRuleBuilder";
import { FeatureGrid } from "../../../../../components/Common/FeatureGrid";
import { FeatureSelector } from "../../../../../components/Common/FeatureSelector";
import { useLicenseRestrictions } from "../../../../../hooks/useLicenseRestrictions";
import type { IDelivery } from "../../../../../models/Delivery";
import type { IDeliverySource } from "../../../../../models/Delivery/DeliverySource";
import type { IFeature } from "../../../../../models/Feature";
import type { Portfolio } from "../../../../../models/Portfolio/Portfolio";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import type {
	DeliverySelectionMode,
	IWorkItemRuleCondition,
	IWorkItemRuleSchema,
} from "../../../../../models/WorkItemRules";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../../../services/TerminologyContext";
import {
	clearDeliverySourceOptionsCache,
	DeliverySourceTab,
} from "./DeliverySourceTab";
import {
	type DeliveryRuleMode,
	type DeliverySelectionState,
	type DeliverySelectionTab,
	type DeliverySelectionTerms,
	defaultDeliverySelectionTab,
	deliverySelectionTabsFor,
	deliveryTabForDelivery,
	emptySelectionValues,
	MANUAL_SELECTION_TAB_KEY,
	RULE_BASED_SELECTION_TAB_KEY,
	ruleInputError,
} from "./deliverySelectionTabs";

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
		rules?: IWorkItemRuleCondition[];
		mode?: DeliveryRuleMode;
	}) => void;
	onUpdate?: (deliveryData: {
		id: number;
		name: string;
		date: string;
		featureIds: number[];
		selectionMode?: DeliverySelectionMode;
		rules?: IWorkItemRuleCondition[];
		mode?: DeliveryRuleMode;
		concurrencyToken?: string;
	}) => void;
}

const LoadingSpinner: React.FC = () => (
	<Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
		<CircularProgress />
	</Box>
);

const SchemaLoadError: React.FC = () => (
	<Alert severity="warning">
		Failed to load rule schema. Please try again.
	</Alert>
);

const PremiumFeatureNotice: React.FC<{ message: string }> = ({ message }) => (
	<Alert severity="info" data-testid="premium-feature-notice">
		{message}
	</Alert>
);

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

const MatchedFeaturesAlert: React.FC<{ count: number }> = ({ count }) => (
	<Alert severity="success" sx={{ flex: 1 }}>
		<span data-testid="matched-count">{count} feature(s) matched</span>
	</Alert>
);

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

/** Everything any selection tab could need to draw itself; each uses the part it cares about. */
interface SelectionContentProps {
	loadingSchema: boolean;
	ruleSchema: IWorkItemRuleSchema | null;
	errors: { features?: string; rules?: string };
	allFeatures: IFeature[];
	selectedFeatureIds: number[];
	rules: IWorkItemRuleCondition[];
	mode: DeliveryRuleMode;
	validatingRules: boolean;
	rulesValidated: boolean;
	matchedFeatures: IFeature[];
	featuresTerm: string;
	portfolioId: number;
	onSelectedFeaturesChange: (ids: number[]) => void;
	onRulesChange: (rules: IWorkItemRuleCondition[]) => void;
	onModeChange: (mode: DeliveryRuleMode) => void;
	onValidateRules: () => void;
}

const ManualSelectionContent: React.FC<SelectionContentProps> = ({
	errors,
	allFeatures,
	selectedFeatureIds,
	featuresTerm,
	portfolioId,
	onSelectedFeaturesChange,
}) => (
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

const RuleBasedContent: React.FC<SelectionContentProps> = ({
	loadingSchema,
	ruleSchema,
	errors,
	rules,
	mode,
	validatingRules,
	rulesValidated,
	matchedFeatures,
	featuresTerm,
	portfolioId,
	onRulesChange,
	onModeChange,
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
				mode={mode}
				onModeChange={onModeChange}
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

const selectionTabContent: Record<string, React.FC<SelectionContentProps>> = {
	[MANUAL_SELECTION_TAB_KEY]: ManualSelectionContent,
	[RULE_BASED_SELECTION_TAB_KEY]: RuleBasedContent,
};

const SelectionModeContent: React.FC<
	SelectionContentProps & { tab: DeliverySelectionTab; isPremium: boolean }
> = ({ tab, isPremium, ...contentProps }) => {
	const gate = tab.premiumGate;
	if (gate && !isPremium) {
		return <PremiumFeatureNotice message={gate.notice} />;
	}

	if (tab.source) {
		return (
			<DeliverySourceTab
				portfolioId={contentProps.portfolioId}
				sourceKey={tab.source.key}
				sourceName={tab.source.displayName}
				featuresTerm={contentProps.featuresTerm}
			/>
		);
	}

	const Content = selectionTabContent[tab.key];
	return <Content {...contentProps} />;
};

const SelectionTabButton: React.FC<{
	tab: DeliverySelectionTab;
	activeTabKey: string;
	isPremium: boolean;
	onSelect: (tab: DeliverySelectionTab) => void;
}> = ({ tab, activeTabKey, isPremium, onSelect }) => {
	const gate = tab.premiumGate;
	const isLocked = !isPremium && gate?.whenLocked === "lockTab";
	const isSelected = activeTabKey === tab.key;
	const button = (
		<Button
			variant={isSelected ? "contained" : "outlined"}
			onClick={() => onSelect(tab)}
			disabled={isLocked}
			aria-pressed={isSelected}
		>
			{tab.label}
		</Button>
	);

	if (gate?.tooltipExtraInfo === undefined) {
		return button;
	}

	return (
		<LicenseTooltip
			canUseFeature={isPremium}
			defaultTooltip=""
			premiumExtraInfo={gate.tooltipExtraInfo}
		>
			<span>{button}</span>
		</LicenseTooltip>
	);
};

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
	tab: DeliverySelectionTab;
	state: DeliverySelectionState;
	terms: DeliverySelectionTerms;
	deliveryTerm: string;
}

const getFirstBlockingError = ({
	name,
	date,
	tab,
	state,
	terms,
	deliveryTerm,
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
	return tab.firstBlockingError(state, terms);
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
	const [selectedTabKey, setSelectedTabKey] = useState(
		defaultDeliverySelectionTab.key,
	);
	const [sources, setSources] = useState<IDeliverySource[]>([]);
	const [rules, setRules] = useState<IWorkItemRuleCondition[]>([]);
	const [mode, setMode] = useState<DeliveryRuleMode>("and");
	const [ruleSchema, setRuleSchema] = useState<IWorkItemRuleSchema | null>(
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

	const tabs = useMemo(() => deliverySelectionTabsFor(sources), [sources]);
	const activeTab =
		tabs.find((tab) => tab.key === selectedTabKey) ??
		defaultDeliverySelectionTab;
	const selectionState: DeliverySelectionState = {
		selectedFeatureIds,
		rules,
		mode,
		rulesValidated,
		matchedFeaturesLength: matchedFeatures.length,
	};
	const selectionTerms: DeliverySelectionTerms = { featureTerm };

	useEffect(() => {
		if (open && portfolio.features.length > 0) {
			const featureIds = portfolio.features.map((f) => f.id);
			featureService
				.getFeaturesByIds(featureIds)
				.then((features) => setAllFeatures(features))
				.catch((err) => console.error("Failed to load features:", err));
		}
	}, [open, portfolio.features, featureService]);

	// Which of these a connection offers is the server's answer, so a connection that grows another
	// one grows another tab here without a line changing.
	useEffect(() => {
		if (!open) {
			return;
		}

		deliveryService
			.getDeliverySources(portfolio.id)
			.then((offered) => setSources(offered))
			.catch(() => setSources([]));
	}, [open, deliveryService, portfolio.id]);

	// Load rule schema when switching to rule-based mode (only for premium users)
	useEffect(() => {
		if (
			open &&
			isPremium &&
			selectedTabKey === RULE_BASED_SELECTION_TAB_KEY &&
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
		selectedTabKey,
		ruleSchema,
		loadingSchema,
		deliveryService,
		portfolio.id,
	]);

	const validateForm = () => {
		const newErrors: typeof errors = activeTab.fieldErrors(
			selectionState,
			selectionTerms,
		);

		if (!name.trim()) {
			newErrors.name = `${deliveryTerm} name is required`;
		}

		if (!isValidFutureDate(date)) {
			newErrors.date = date
				? `${deliveryTerm} date must be in the future`
				: `${deliveryTerm} date is required`;
		}

		setErrors(newErrors);
		return Object.keys(newErrors).length === 0;
	};

	const handleValidateRules = async () => {
		const inputError = ruleInputError(rules);
		if (inputError !== null) {
			setErrors((prev) => ({ ...prev, rules: inputError }));
			return;
		}

		setValidatingRules(true);
		setErrors((prev) => ({ ...prev, rules: undefined }));

		try {
			const matched = await deliveryService.validateRules(
				portfolio.id,
				rules,
				mode,
			);
			setMatchedFeatures(matched);
			setRulesValidated(true);

			if (matched.length === 0) {
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

	const handleRulesChange = (newRules: IWorkItemRuleCondition[]) => {
		setRules(newRules);
		setRulesValidated(false);
		setMatchedFeatures([]);
	};

	const handleSelectTab = (tab: DeliverySelectionTab) => {
		if (tab.key === selectedTabKey) {
			return;
		}

		setSelectedTabKey(tab.key);
		setRulesValidated(false);
		setMatchedFeatures([]);
	};

	const handleSave = () => {
		if (!validateForm()) {
			return;
		}

		const basePayload = {
			name: name.trim(),
			date,
			selectionMode: activeTab.mode,
			...activeTab.toPayload(selectionState),
		};

		if (isEditMode && editingDelivery && onUpdate) {
			onUpdate({
				id: editingDelivery.id,
				...basePayload,
				concurrencyToken: editingDelivery.concurrencyToken,
			});
			return;
		}

		onSave(basePayload);
	};

	const resetForm = useCallback(() => {
		const values = emptySelectionValues();
		setName("");
		setDate("");
		setSelectedFeatureIds(values.selectedFeatureIds);
		setSelectedTabKey(defaultDeliverySelectionTab.key);
		setRules(values.rules);
		setMode(values.mode);
		setRuleSchema(null);
		setRulesValidated(false);
		setMatchedFeatures([]);
		setErrors({});
		clearDeliverySourceOptionsCache();
	}, []);

	useEffect(() => {
		if (open && editingDelivery) {
			const tab = deliveryTabForDelivery(editingDelivery);
			const values = tab.hydrate(editingDelivery);

			setName(editingDelivery.name);
			setDate(editingDelivery.date.split("T")[0]);
			setSelectedTabKey(tab.key);
			setSelectedFeatureIds(values.selectedFeatureIds);
			setRules(values.rules);
			setMode(values.mode);
			// Whatever the rules matched before is not trustworthy once the form reopens, so
			// the user has to ask for them to be matched again before saving.
			setRulesValidated(false);
			setMatchedFeatures([]);
		}
	}, [open, editingDelivery]);

	useEffect(() => {
		if (!open) {
			resetForm();
		}
	}, [open, resetForm]);

	const blockingError = getFirstBlockingError({
		name,
		date,
		tab: activeTab,
		state: selectionState,
		terms: selectionTerms,
		deliveryTerm,
	});

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
						<ButtonGroup size="small" aria-label="Selection Mode">
							{tabs.map((tab) => (
								<SelectionTabButton
									key={tab.key}
									tab={tab}
									activeTabKey={activeTab.key}
									isPremium={isPremium}
									onSelect={handleSelectTab}
								/>
							))}
						</ButtonGroup>
					</Box>

					<SelectionModeContent
						tab={activeTab}
						isPremium={isPremium}
						loadingSchema={loadingSchema}
						ruleSchema={ruleSchema}
						errors={errors}
						allFeatures={allFeatures}
						selectedFeatureIds={selectedFeatureIds}
						rules={rules}
						mode={mode}
						validatingRules={validatingRules}
						rulesValidated={rulesValidated}
						matchedFeatures={matchedFeatures}
						featuresTerm={featuresTerm}
						portfolioId={portfolio.id}
						onSelectedFeaturesChange={setSelectedFeatureIds}
						onRulesChange={handleRulesChange}
						onModeChange={(next) => {
							setMode(next);
							setRulesValidated(false);
							setMatchedFeatures([]);
						}}
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
					{blockingError && (
						<Alert severity="error" sx={{ py: 0 }}>
							{blockingError}
						</Alert>
					)}
				</Box>
				<Box sx={{ display: "flex", gap: 1 }}>
					<Button onClick={onClose}>Cancel</Button>
					<Button
						onClick={handleSave}
						variant="contained"
						disabled={blockingError !== null}
					>
						{isEditMode ? "Update" : "Save"}
					</Button>
				</Box>
			</DialogActions>
		</Dialog>
	);
};
