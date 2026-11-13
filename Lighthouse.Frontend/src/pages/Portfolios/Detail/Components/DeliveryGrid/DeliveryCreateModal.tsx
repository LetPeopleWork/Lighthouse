import {
	Alert,
	Box,
	Button,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
	TextField,
} from "@mui/material";
import type React from "react";
import {
	useCallback,
	useContext,
	useEffect,
	useMemo,
	useRef,
	useState,
} from "react";
import { useLicenseRestrictions } from "../../../../../hooks/useLicenseRestrictions";
import type { IDelivery } from "../../../../../models/Delivery";
import type { IDeliverySourceOption } from "../../../../../models/Delivery/DeliverySource";
import type { IFeature } from "../../../../../models/Feature";
import type { Portfolio } from "../../../../../models/Portfolio/Portfolio";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import type {
	DeliverySelectionMode,
	IWorkItemRuleCondition,
} from "../../../../../models/WorkItemRules";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../../../services/TerminologyContext";
import { SelectionModeContent } from "./DeliverySelectionContent";
import { SelectionTabRow } from "./DeliverySelectionTabRow";
import {
	boundSourceSelection,
	dateErrorOf,
	dateInputValue,
	getFirstBlockingError,
	nameErrorOf,
	offeredTabs,
} from "./deliveryFormRules";
import {
	awaitsBoundSourceTab,
	dialogKnowsItsAnswer,
	type SelectionBodyQuestion,
	saveIsAllowed,
	selectionBodyOf,
} from "./deliverySelectionBody";
import {
	type DeliveryRuleMode,
	type DeliverySelectionState,
	type DeliverySelectionTab,
	type DeliverySelectionTerms,
	defaultDeliverySelectionTab,
	deliverySelectionTabsFor,
	deliveryTabForDelivery,
	emptySelectionValues,
	RULE_BASED_SELECTION_TAB_KEY,
	ruleInputError,
} from "./deliverySelectionTabs";
import { premiumVerdictOf } from "./premiumVerdict";
import {
	useDialogFeatureList,
	useDialogRuleSchema,
} from "./useDeliveryFormQueries";
import { useDialogDeliverySources } from "./useDialogDeliverySources";

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
		sourceKey?: string;
		sourceReference?: string;
		publishForecastToSource?: boolean;
	}) => void;
	onUpdate?: (deliveryData: {
		id: number;
		name: string;
		date: string;
		featureIds: number[];
		selectionMode?: DeliverySelectionMode;
		rules?: IWorkItemRuleCondition[];
		mode?: DeliveryRuleMode;
		sourceKey?: string;
		sourceReference?: string;
		publishForecastToSource?: boolean;
		concurrencyToken?: string;
	}) => void;
}

export const DeliveryCreateModal: React.FC<DeliveryCreateModalProps> = ({
	open,
	portfolio,
	editingDelivery,
	onClose,
	onSave,
	onUpdate,
}) => {
	const { deliveryService } = useContext(ApiServiceContext);
	const { getTerm } = useTerminology();
	const { licenseStatus } = useLicenseRestrictions();
	const verdict = premiumVerdictOf(licenseStatus);
	const deliveryTerm = getTerm(TERMINOLOGY_KEYS.DELIVERY);
	const deliveriesTerm = getTerm(TERMINOLOGY_KEYS.DELIVERIES);
	const isEditMode = !!editingDelivery;
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const portfolioTerm = getTerm(TERMINOLOGY_KEYS.PORTFOLIO);
	const [name, setName] = useState("");
	const [date, setDate] = useState("");
	const [selectedFeatureIds, setSelectedFeatureIds] = useState<number[]>([]);
	const [selectedTabKey, setSelectedTabKey] = useState(
		defaultDeliverySelectionTab.key,
	);
	const { sources, sourcesStatus, sourceOptionsOf, opening, startNewOpening } =
		useDialogDeliverySources(portfolio.id, open, verdict);
	const featureList = useDialogFeatureList({
		portfolio,
		isOpen: open,
		opening,
	});
	const [rules, setRules] = useState<IWorkItemRuleCondition[]>([]);
	const [mode, setMode] = useState<DeliveryRuleMode>("and");
	const [validatingRules, setValidatingRules] = useState(false);
	const [rulesValidated, setRulesValidated] = useState(false);
	const [sourceReference, setSourceReference] = useState<string | null>(null);
	const [publishForecastToSource, setPublishForecastToSource] = useState(false);
	const [matchedFeatures, setMatchedFeatures] = useState<IFeature[]>([]);
	const hydratedDeliveryId = useRef<number | null>(null);
	const [errors, setErrors] = useState<{
		name?: string;
		date?: string;
		features?: string;
		rules?: string;
	}>({});

	const selectionTerms = useMemo<DeliverySelectionTerms>(
		() => ({ featureTerm, deliveryTerm, deliveriesTerm }),
		[featureTerm, deliveryTerm, deliveriesTerm],
	);
	const tabs = useMemo(
		() =>
			offeredTabs(
				deliverySelectionTabsFor(sources, selectionTerms),
				editingDelivery,
			),
		[sources, selectionTerms, editingDelivery],
	);
	const activeTab =
		tabs.find((tab) => tab.key === selectedTabKey) ??
		defaultDeliverySelectionTab;
	// On a tab that reads from the work tracking system, the name and the date belong to the entry the
	// user picks there, so both fields are filled in for them and neither is theirs to type over: the
	// tracker owns them from the moment the entry is picked, and typing over them here would be undone
	// by the next sync.
	const readsFromSource = activeTab.source !== undefined;
	const ruleSchema = useDialogRuleSchema({
		portfolioId: portfolio.id,
		isOpen: open,
		opening,
		verdict,
		ruleTabShowing: activeTab.key === RULE_BASED_SELECTION_TAB_KEY,
	});
	const bodyQuestion: SelectionBodyQuestion = {
		tab: activeTab,
		verdict,
		ruleSchemaStatus: ruleSchema.status,
		sourcesStatus,
		followsTrackerEntry: Boolean(editingDelivery?.sourceKey),
		featureListStatus: featureList.status,
	};
	const activeBody = selectionBodyOf(bodyQuestion);
	const standsOnNoTab = awaitsBoundSourceTab(bodyQuestion);
	const fieldsBelongToSource = readsFromSource || standsOnNoTab;
	const selectionState: DeliverySelectionState = {
		selectedFeatureIds,
		rules,
		mode,
		sourceReference,
		publishForecastToSource,
		rulesValidated,
		matchedFeaturesLength: matchedFeatures.length,
	};

	const validateForm = () => {
		const newErrors: typeof errors = activeTab.fieldErrors(
			selectionState,
			selectionTerms,
		);

		// A tab that fills the name and the date in itself owns both, and the date it brings is whatever
		// day the tracker holds — a Release that shipped last quarter among them. Judging those fields
		// here would refuse such a Release for a reason nobody can act on.
		if (readsFromSource) {
			setErrors(newErrors);
			return Object.keys(newErrors).length === 0;
		}

		const nameError = nameErrorOf(name, deliveryTerm);
		if (nameError !== undefined) {
			newErrors.name = nameError;
		}

		const dateError = dateErrorOf(date, deliveryTerm);
		if (dateError !== undefined) {
			newErrors.date = dateError;
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
		if (tab.key === selectedTabKey || standsOnNoTab) {
			return;
		}

		setSelectedTabKey(tab.key);
		setRulesValidated(false);
		setMatchedFeatures([]);
		// The picker starts empty again when the tab is next opened, so what it was showing before is
		// forgotten here too; the name and the date it filled in stay, which is the point of them.
		setSourceReference(null);
		// Broadcasting is a decision about one entry of one tracker. Carried across a tab change it
		// would arrive switched on for an entry nobody has chosen yet.
		setPublishForecastToSource(false);
	};

	const handleSourceOptionPicked = useCallback(
		(option: IDeliverySourceOption) => {
			setName(option.name);
			setDate(option.date === null ? "" : dateInputValue(option.date));
			setSourceReference(option.id);
			setErrors((prev) => ({ ...prev, name: undefined, date: undefined }));
		},
		[],
	);

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
		setRulesValidated(false);
		setMatchedFeatures([]);
		setSourceReference(values.sourceReference);
		setPublishForecastToSource(values.publishForecastToSource);
		hydratedDeliveryId.current = null;
		startNewOpening();
		setErrors({});
	}, [startNewOpening]);

	useEffect(() => {
		if (!(open && editingDelivery)) {
			return;
		}

		const tab = deliveryTabForDelivery(editingDelivery, tabs);

		// Which tab a delivery belongs on is answered twice, because the tabs that read from the work
		// tracking system only exist once the server has listed them. The second answer is worth
		// having in one case alone: it now names a tab that did not exist when the form opened.
		// Anything else it says is the same answer as before, and the reader may have moved to
		// another tab in the meantime — do not lift this back above the guard, or that move is
		// silently undone the moment the source list lands.
		if (hydratedDeliveryId.current === editingDelivery.id) {
			if (tab.source !== undefined) {
				setSelectedTabKey(tab.key);
			}
			return;
		}
		hydratedDeliveryId.current = editingDelivery.id;
		setSelectedTabKey(tab.key);

		const values = tab.hydrate(editingDelivery);
		setName(editingDelivery.name);
		setDate(editingDelivery.date.split("T")[0]);
		setSelectedFeatureIds(values.selectedFeatureIds);
		setRules(values.rules);
		setMode(values.mode);
		setSourceReference(values.sourceReference);
		setPublishForecastToSource(values.publishForecastToSource);
		// Whatever the rules matched before is not trustworthy once the form reopens, so
		// the user has to ask for them to be matched again before saving.
		setRulesValidated(false);
		setMatchedFeatures([]);
	}, [open, editingDelivery, tabs]);

	useEffect(() => {
		if (!open) {
			resetForm();
		}
	}, [open, resetForm]);

	const knowsItsAnswer = dialogKnowsItsAnswer(sourcesStatus, verdict);
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
						disabled={fieldsBelongToSource}
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
						disabled={fieldsBelongToSource}
						value={date}
						onChange={(e) => setDate(e.target.value)}
						error={!!errors.date}
						helperText={errors.date}
						slotProps={{ inputLabel: { shrink: true } }}
						sx={{ mb: 2 }}
					/>

					<SelectionTabRow
						tabs={tabs}
						activeTabKey={standsOnNoTab ? null : activeTab.key}
						verdict={verdict}
						awaitingBoundTab={standsOnNoTab}
						sourcesStatus={sourcesStatus}
						portfolioTerm={portfolioTerm}
						onSelect={handleSelectTab}
					/>

					<SelectionModeContent
						tab={activeTab}
						body={activeBody}
						ruleSchema={ruleSchema.data}
						errors={errors}
						listedFeatures={featureList.data ?? []}
						selectedFeatureIds={selectedFeatureIds}
						rules={rules}
						mode={mode}
						validatingRules={validatingRules}
						rulesValidated={rulesValidated}
						matchedFeatures={matchedFeatures}
						featuresTerm={featuresTerm}
						portfolioTerm={portfolioTerm}
						portfolioId={portfolio.id}
						currentSelection={boundSourceSelection(editingDelivery, activeTab)}
						onSelectedFeaturesChange={setSelectedFeatureIds}
						onRulesChange={handleRulesChange}
						onModeChange={(next) => {
							setMode(next);
							setRulesValidated(false);
							setMatchedFeatures([]);
						}}
						onValidateRules={handleValidateRules}
						onSourceOptionPicked={handleSourceOptionPicked}
						publishForecastToSource={publishForecastToSource}
						onPublishForecastChange={setPublishForecastToSource}
						sourceOptionsOf={sourceOptionsOf}
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
					{knowsItsAnswer && blockingError && (
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
						disabled={!saveIsAllowed(knowsItsAnswer, blockingError, activeBody)}
					>
						{isEditMode ? "Update" : "Save"}
					</Button>
				</Box>
			</DialogActions>
		</Dialog>
	);
};
