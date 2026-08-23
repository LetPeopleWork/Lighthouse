import {
	Alert,
	Autocomplete,
	Box,
	CircularProgress,
	TextField,
	Typography,
} from "@mui/material";
import type React from "react";
import { useContext, useEffect, useRef, useState } from "react";
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
	/** Told which entry is on screen, so the form can show the name and date it would take. */
	onOptionPicked: (option: IDeliverySourceOption) => void;
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
	NoDateSet: "No Release Date Set",
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

/**
 * Two projects on one connection routinely name a Release the same thing, so the project it came
 * from travels with every entry. It is what tells two same-named entries apart on screen, and it is
 * searchable too, so a reader who remembers only the project can type that instead.
 */
const projectSuffix = (option: IDeliverySourceOption): string =>
	`(${option.projectName})`;

const sourceOptionLabel = (option: IDeliverySourceOption): string =>
	`${option.name} ${projectSuffix(option)}`;

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

const SourceOptionRow: React.FC<{
	liProps: React.HTMLAttributes<HTMLLIElement>;
	option: IDeliverySourceOption;
}> = ({ liProps, option }) => {
	// Whether an entry can be picked is the server's verdict, carried on the entry itself. Working it
	// out again from the date here would give two answers that drift apart, and the drift only shows
	// up when somebody binds a Release the picker told them they could not. Taking the click handler
	// away is what makes the refusal real: greying the row out is only a stylesheet away.
	const rowProps = option.isSelectable
		? liProps
		: { ...liProps, onClick: undefined };

	return (
		<li {...rowProps}>
			<span>{option.name}</span>{" "}
			<Typography component="span" variant="caption" color="text.secondary">
				{projectSuffix(option)}
			</Typography>
			{option.blockedBecause === null ? null : (
				<Typography
					component="span"
					variant="caption"
					color="text.secondary"
					sx={{ ml: 1 }}
				>
					{blockedReason[option.blockedBecause]}
				</Typography>
			)}
		</li>
	);
};

const SourceOptionPicker: React.FC<{
	sourceName: string;
	options: IDeliverySourceOption[];
	selectedId: string;
	onSelect: (option: IDeliverySourceOption) => void;
}> = ({ sourceName, options, selectedId, onSelect }) => (
	<Autocomplete
		fullWidth
		options={options}
		value={options.find((option) => option.id === selectedId) ?? null}
		onChange={(_event, picked) => {
			if (picked !== null) {
				onSelect(picked);
			}
		}}
		getOptionLabel={sourceOptionLabel}
		getOptionKey={(option) => option.id}
		getOptionDisabled={(option) => !option.isSelectable}
		isOptionEqualToValue={(option, picked) => option.id === picked.id}
		renderOption={(props, option) => {
			const { key, ...liProps } = props;
			return <SourceOptionRow key={key} liProps={liProps} option={option} />;
		}}
		renderInput={(params) => <TextField {...params} label={sourceName} />}
	/>
);

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
	onOptionPicked,
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
		onOptionPicked(option);

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
			<SourceOptionPicker
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
