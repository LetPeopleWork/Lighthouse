import {
	Box,
	Button,
	ButtonGroup,
	CircularProgress,
	Typography,
} from "@mui/material";
import type React from "react";
import { LicenseTooltip } from "../../../../../components/App/License/LicenseToolTip";
import { selectionTabIsLocked } from "./deliverySelectionBody";
import type { DeliverySelectionTab } from "./deliverySelectionTabs";
import type { PremiumVerdict } from "./premiumVerdict";
import { RetryByReopeningNotice } from "./RetryByReopeningNotice";
import type { SourcesStatus } from "./useDialogDeliverySources";

const SelectionTabButton: React.FC<
	Readonly<{
		tab: DeliverySelectionTab;
		activeTabKey: string | null;
		verdict: PremiumVerdict;
		awaitingBoundTab: boolean;
		onSelect: (tab: DeliverySelectionTab) => void;
	}>
> = ({ tab, activeTabKey, verdict, awaitingBoundTab, onSelect }) => {
	const gate = tab.premiumGate;
	const isLocked = selectionTabIsLocked(tab, verdict, awaitingBoundTab);
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

	// Until the licence answers there is nothing true to say about it, so no tooltip says anything.
	if (gate?.tooltipExtraInfo === undefined || verdict === "unknown") {
		return button;
	}

	return (
		<LicenseTooltip
			canUseFeature={verdict === "premium"}
			defaultTooltip=""
			premiumExtraInfo={gate.tooltipExtraInfo}
		>
			<span>{button}</span>
		</LicenseTooltip>
	);
};

const BusySelectionTab: React.FC = () => (
	<Button disabled aria-busy="true">
		<CircularProgress size={20} sx={{ mr: 1 }} />
		Loading…
	</Button>
);

export const SelectionTabRow: React.FC<
	Readonly<{
		tabs: DeliverySelectionTab[];
		activeTabKey: string | null;
		verdict: PremiumVerdict;
		awaitingBoundTab: boolean;
		sourcesStatus: SourcesStatus;
		portfolioTerm: string;
		onSelect: (tab: DeliverySelectionTab) => void;
	}>
> = ({
	tabs,
	activeTabKey,
	verdict,
	awaitingBoundTab,
	sourcesStatus,
	portfolioTerm,
	onSelect,
}) => (
	<Box sx={{ mb: 2 }}>
		<Typography variant="subtitle2" sx={{ mb: 1 }}>
			Selection Mode
		</Typography>
		<ButtonGroup size="small" aria-label="Selection Mode">
			{tabs.map((tab) => (
				<SelectionTabButton
					key={tab.key}
					tab={tab}
					activeTabKey={activeTabKey}
					verdict={verdict}
					awaitingBoundTab={awaitingBoundTab}
					onSelect={onSelect}
				/>
			))}
			{sourcesStatus === "loading" && <BusySelectionTab />}
		</ButtonGroup>
		{sourcesStatus === "error" && (
			<RetryByReopeningNotice
				problem={`Couldn't check what this ${portfolioTerm}'s connection offers.`}
				sx={{ mt: 1 }}
			/>
		)}
	</Box>
);
