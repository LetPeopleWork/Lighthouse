import { Alert, Box, Typography } from "@mui/material";
import type React from "react";
import { DeliveryRuleBuilder } from "../../../../../components/Common/DeliveryRuleBuilder";
import { FeatureSelector } from "../../../../../components/Common/FeatureSelector";
import type { IDeliverySourceOption } from "../../../../../models/Delivery/DeliverySource";
import type { IFeature } from "../../../../../models/Feature";
import type {
	IWorkItemRuleCondition,
	IWorkItemRuleSchema,
} from "../../../../../models/WorkItemRules";
import {
	LoadingSpinner,
	MatchedFeaturesAlert,
	MatchedFeaturesGrid,
	PremiumFeatureNotice,
	SchemaLoadError,
	ValidationButton,
} from "./DeliveryDialogParts";
import {
	type DeliverySourceCurrentSelection,
	DeliverySourceTab,
	DeliverySourceTabLoading,
} from "./DeliverySourceTab";
import {
	FEATURE_LIST_FAILED,
	FEATURE_LIST_LOADING,
	PREMIUM_NOTICE,
	type SelectionBody,
	UNAVAILABLE,
} from "./deliverySelectionBody";
import {
	type DeliveryRuleMode,
	type DeliverySelectionTab,
	MANUAL_SELECTION_TAB_KEY,
	RULE_BASED_SELECTION_TAB_KEY,
} from "./deliverySelectionTabs";
import { RetryByReopeningNotice } from "./RetryByReopeningNotice";
import type { DeliverySourceOptionsState } from "./useDeliverySourceOptions";

interface CommonSelectionContentProps {
	errors: { features?: string; rules?: string };
	featuresTerm: string;
	portfolioId: number;
}

interface ManualSelectionContentProps extends CommonSelectionContentProps {
	listedFeatures: IFeature[];
	listIsLoading: boolean;
	selectedFeatureIds: number[];
	onSelectedFeaturesChange: (ids: number[]) => void;
}

interface RuleBasedContentProps extends CommonSelectionContentProps {
	ruleSchema: IWorkItemRuleSchema | undefined;
	rules: IWorkItemRuleCondition[];
	mode: DeliveryRuleMode;
	validatingRules: boolean;
	rulesValidated: boolean;
	matchedFeatures: IFeature[];
	onRulesChange: (rules: IWorkItemRuleCondition[]) => void;
	onModeChange: (mode: DeliveryRuleMode) => void;
	onValidateRules: () => void;
}

/**
 * The bag the form hands to whichever tab is showing. It is the sum of what every tab needs, so a
 * tab component that asks for less still fits the registry below and stays free to ignore the rest.
 */
type SelectionContentProps = ManualSelectionContentProps &
	RuleBasedContentProps;

const ManualSelectionContent: React.FC<ManualSelectionContentProps> = ({
	errors,
	listedFeatures,
	listIsLoading,
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
				features={listedFeatures}
				loading={listIsLoading}
				selectedFeatureIds={selectedFeatureIds}
				onChange={onSelectedFeaturesChange}
				storageKey={`delivery-create-features-${portfolioId}`}
			/>
		</Box>
	</>
);

const RuleBasedContent: React.FC<RuleBasedContentProps> = ({
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

const selectionTabContent: Record<
	string,
	React.FC<SelectionContentProps> | undefined
> = {
	[MANUAL_SELECTION_TAB_KEY]: ManualSelectionContent,
	[RULE_BASED_SELECTION_TAB_KEY]: RuleBasedContent,
};

export const SelectionModeContent: React.FC<
	Omit<SelectionContentProps, "listIsLoading"> & {
		tab: DeliverySelectionTab;
		body: SelectionBody;
		portfolioTerm: string;
		currentSelection: DeliverySourceCurrentSelection | null;
		onSourceOptionPicked: (option: IDeliverySourceOption) => void;
		publishForecastToSource: boolean;
		onPublishForecastChange: (publish: boolean) => void;
		sourceOptionsOf: (sourceKey: string) => DeliverySourceOptionsState;
	}
> = ({
	tab,
	body,
	portfolioTerm,
	currentSelection,
	onSourceOptionPicked,
	publishForecastToSource,
	onPublishForecastChange,
	sourceOptionsOf,
	...contentProps
}) => {
	if (body.kind === PREMIUM_NOTICE) {
		return <PremiumFeatureNotice message={body.notice} />;
	}

	if (body.kind === "loading" && tab.source) {
		return <DeliverySourceTabLoading sourceName={tab.source.displayName} />;
	}

	if (body.kind === "loading") {
		return <LoadingSpinner />;
	}

	if (body.kind === UNAVAILABLE) {
		return null;
	}

	if (body.kind === FEATURE_LIST_FAILED) {
		return (
			<RetryByReopeningNotice
				problem={`Couldn't load this ${portfolioTerm}'s ${contentProps.featuresTerm}.`}
			/>
		);
	}

	if (tab.source) {
		return (
			<DeliverySourceTab
				portfolioId={contentProps.portfolioId}
				sourceKey={tab.source.key}
				sourceName={tab.source.displayName}
				featuresTerm={contentProps.featuresTerm}
				portfolioTerm={portfolioTerm}
				currentSelection={currentSelection}
				onOptionPicked={onSourceOptionPicked}
				publishForecast={publishForecastToSource}
				onPublishForecastChange={onPublishForecastChange}
				optionList={sourceOptionsOf(tab.source.key)}
			/>
		);
	}

	// A tab that neither saves a selection nor reads one from the work tracking system has no body
	// registered here. Showing nothing keeps a tab added later from taking the whole form down.
	const Content = selectionTabContent[tab.key];
	return Content === undefined ? null : (
		<Content
			{...contentProps}
			listIsLoading={body.kind === FEATURE_LIST_LOADING}
		/>
	);
};
