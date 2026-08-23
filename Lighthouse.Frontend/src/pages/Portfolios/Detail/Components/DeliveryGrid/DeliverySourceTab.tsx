import {
	Alert,
	Box,
	CircularProgress,
	FormControl,
	InputLabel,
	ListSubheader,
	MenuItem,
	Select,
	Typography,
} from "@mui/material";
import type React from "react";
import { useContext, useEffect, useMemo, useRef, useState } from "react";
import { FeatureGrid } from "../../../../../components/Common/FeatureGrid";
import LocalDateTimeDisplay from "../../../../../components/Common/LocalDateTimeDisplay/LocalDateTimeDisplay";
import type {
	DeliverySourcePreviewEmptyReason,
	IDeliverySourceOption,
	IDeliverySourcePreview,
	SourceOptionBlockReason,
} from "../../../../../models/Delivery/DeliverySource";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";

export interface DeliverySourceTabProps {
	portfolioId: number;
	sourceKey: string;
	sourceName: string;
	featuresTerm: string;
	portfolioTerm: string;
}

const optionCache = new Map<string, IDeliverySourceOption[]>();

/**
 * Forget what the work tracking system said last time. The list is worth keeping while one form is
 * open — it takes a second or two to fetch — but it goes stale the moment the form closes, and a
 * user who reopens it after giving a Release a date in Jira expects to see that date.
 */
export const clearDeliverySourceOptionsCache = (): void => {
	optionCache.clear();
};

const blockedReason: Record<SourceOptionBlockReason, string> = {
	NoDateSet: "No date set",
	RetiredAtSource: "No longer available",
};

const emptyPreviewExplanation = (
	reason: DeliverySourcePreviewEmptyReason,
	sourceName: string,
	featuresTerm: string,
	portfolioTerm: string,
): string => {
	if (reason === "NothingTaggedAgainstTheSource") {
		return `Nothing is tagged against this ${sourceName} yet. Tag the ${featuresTerm} you expect to ship with it and they will show up here.`;
	}

	if (reason === "TaggedWorkNotTrackedByThisPortfolio") {
		return `Work is tagged against this ${sourceName}, but none of those ${featuresTerm} are tracked by this ${portfolioTerm}. Widen what the ${portfolioTerm} covers to bring them in.`;
	}

	return `This ${sourceName} has no ${featuresTerm} to show.`;
};

interface OptionGroup {
	key: string;
	label: string;
	options: IDeliverySourceOption[];
}

/**
 * Two projects on one connection routinely name a Release the same thing, so a flat list would show
 * two identical rows and leave the user guessing which one they picked.
 */
const groupByProject = (options: IDeliverySourceOption[]): OptionGroup[] => {
	const groups = new Map<string, OptionGroup>();

	for (const option of options) {
		const group = groups.get(option.projectKey);
		if (group === undefined) {
			groups.set(option.projectKey, {
				key: option.projectKey,
				label: `${option.projectName} (${option.projectKey})`,
				options: [option],
			});
			continue;
		}

		group.options.push(option);
	}

	return [...groups.values()];
};

const useSourceOptions = (portfolioId: number, sourceKey: string) => {
	const { deliveryService } = useContext(ApiServiceContext);
	const cacheKey = `${portfolioId}:${sourceKey}`;
	const [options, setOptions] = useState<IDeliverySourceOption[] | null>(null);
	const [failed, setFailed] = useState(false);

	useEffect(() => {
		const cached = optionCache.get(cacheKey);
		if (cached === undefined) {
			let stillMounted = true;
			setOptions(null);
			setFailed(false);

			deliveryService
				.getDeliverySourceOptions(portfolioId, sourceKey)
				.then((fetched) => {
					optionCache.set(cacheKey, fetched);
					if (stillMounted) {
						setOptions(fetched);
					}
				})
				.catch(() => {
					if (stillMounted) {
						setFailed(true);
					}
				});

			return () => {
				stillMounted = false;
			};
		}

		setOptions(cached);
		setFailed(false);
	}, [cacheKey, portfolioId, sourceKey, deliveryService]);

	return { options, failed };
};

const SourceOptionLabel: React.FC<{ option: IDeliverySourceOption }> = ({
	option,
}) => (
	<Box
		sx={{ display: "flex", alignItems: "baseline", gap: 1, flexWrap: "wrap" }}
	>
		<span>{option.name}</span>
		<Typography variant="caption" color="text.secondary">
			{option.projectKey}
		</Typography>
		{option.date === null ? null : (
			// Carries a test id because a row holds several short pieces of text and the Release name is
			// free-form: a Release called "2027 Q1" reads as a date to anything that goes looking for one.
			<Typography
				variant="caption"
				color="text.secondary"
				data-testid="delivery-source-option-date"
			>
				<LocalDateTimeDisplay utcDate={option.date} />
			</Typography>
		)}
		{option.blockedBecause === null ? null : (
			<Typography variant="caption" color="text.secondary">
				{blockedReason[option.blockedBecause]}
			</Typography>
		)}
	</Box>
);

const SourceOptionSelect: React.FC<{
	sourceName: string;
	options: IDeliverySourceOption[];
	selectedId: string;
	onSelect: (option: IDeliverySourceOption) => void;
}> = ({ sourceName, options, selectedId, onSelect }) => {
	const groups = useMemo(() => groupByProject(options), [options]);
	const labelId = "delivery-source-option-label";
	const selectId = "delivery-source-option";

	const handleChange = (value: string) => {
		const picked = options.find((option) => option.id === value);
		if (picked) {
			onSelect(picked);
		}
	};

	return (
		<FormControl fullWidth>
			<InputLabel id={labelId}>{sourceName}</InputLabel>
			<Select
				labelId={labelId}
				id={selectId}
				label={sourceName}
				value={selectedId}
				onChange={(event) => handleChange(event.target.value)}
			>
				{groups.flatMap((group) => [
					<ListSubheader key={group.key}>{group.label}</ListSubheader>,
					...group.options.map((option) => (
						// Whether an option can be picked is the server's verdict, carried on the option
						// itself. Working it out again from the date here would give two answers that
						// drift apart, and the drift only shows up when somebody binds a Release the
						// picker told them they could not.
						<MenuItem
							key={option.id}
							value={option.id}
							disabled={!option.isSelectable}
						>
							<SourceOptionLabel option={option} />
						</MenuItem>
					)),
				])}
			</Select>
		</FormControl>
	);
};

const SourcePreview: React.FC<{
	preview: IDeliverySourcePreview;
	sourceName: string;
	featuresTerm: string;
	portfolioTerm: string;
	portfolioId: number;
}> = ({ preview, sourceName, featuresTerm, portfolioTerm, portfolioId }) => (
	<Box sx={{ mt: 3 }} data-testid="delivery-source-preview">
		<Typography variant="subtitle2" sx={{ mb: 1 }}>
			{preview.name} would set the date to{" "}
			<LocalDateTimeDisplay utcDate={preview.date} />
		</Typography>
		{preview.features.length === 0 ? (
			<Alert severity="info" data-testid="delivery-source-preview-empty">
				{emptyPreviewExplanation(
					preview.emptyBecause,
					sourceName,
					featuresTerm,
					portfolioTerm,
				)}
			</Alert>
		) : (
			<Box sx={{ height: 200 }}>
				<FeatureGrid
					features={preview.features}
					selectedFeatureIds={preview.features.map((feature) => feature.id)}
					storageKey={`delivery-source-preview-${portfolioId}`}
					mode="readonly"
				/>
			</Box>
		)}
	</Box>
);

/**
 * Shows what taking this Delivery's date from the work tracking system would mean. It only ever
 * looks: nothing here writes anything, so closing the form leaves the Delivery as it was.
 */
export const DeliverySourceTab: React.FC<DeliverySourceTabProps> = ({
	portfolioId,
	sourceKey,
	sourceName,
	featuresTerm,
	portfolioTerm,
}) => {
	const { deliveryService } = useContext(ApiServiceContext);
	const { options, failed } = useSourceOptions(portfolioId, sourceKey);
	const [selectedId, setSelectedId] = useState("");
	const [preview, setPreview] = useState<IDeliverySourcePreview | null>(null);
	const [previewFailed, setPreviewFailed] = useState(false);
	const awaitedOptionId = useRef("");

	/**
	 * A preview costs a round trip whose length depends on how much work carries the entry, so two
	 * picks in a row routinely come back in the other order. Anything but the answer to the pick
	 * that is still on screen is dropped, because the panel and the picker naming different entries
	 * is a confidently wrong answer to the only question this tab exists to ask.
	 */
	const handleSelect = (option: IDeliverySourceOption) => {
		setSelectedId(option.id);
		setPreview(null);
		setPreviewFailed(false);
		awaitedOptionId.current = option.id;

		deliveryService
			.previewDeliverySource(portfolioId, sourceKey, option.id)
			.then((fetched) => {
				if (awaitedOptionId.current === option.id) {
					setPreview(fetched);
				}
			})
			.catch(() => {
				if (awaitedOptionId.current === option.id) {
					setPreviewFailed(true);
				}
			});
	};

	if (failed) {
		return (
			<Alert severity="error">
				The list of {sourceName} entries could not be loaded. Check this{" "}
				{portfolioTerm}'s connection and try again.
			</Alert>
		);
	}

	if (options === null) {
		return (
			<Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
				<CircularProgress />
			</Box>
		);
	}

	return (
		<Box>
			<Typography variant="h6" sx={{ mb: 2 }}>
				Preview a {sourceName}
			</Typography>
			<SourceOptionSelect
				sourceName={sourceName}
				options={options}
				selectedId={selectedId}
				onSelect={handleSelect}
			/>
			{previewFailed && (
				<Alert severity="error" sx={{ mt: 2 }}>
					This {sourceName} could not be previewed. Try again in a moment.
				</Alert>
			)}
			{preview === null ? null : (
				<SourcePreview
					preview={preview}
					sourceName={sourceName}
					featuresTerm={featuresTerm}
					portfolioTerm={portfolioTerm}
					portfolioId={portfolioId}
				/>
			)}
		</Box>
	);
};
